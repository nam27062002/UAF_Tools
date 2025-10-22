#nullable enable

using DANCustomTools.Core.ViewModels;
using DANCustomTools.MVVM;
using DANCustomTools.Services;
using DANCustomTools.Models.SceneExplorer;
using DANCustomTools.Models.PropertiesEditor;
using DANCustomTools.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DANCustomTools.ViewModels
{
    public enum ObjectTypeFilter
    {
        All,
        ActorsOnly,
        FrisesOnly
    }

    public class SceneExplorerViewModel : SubToolViewModelBase, IDisposable
    {
        private readonly ISceneExplorerService _sceneService;
        private readonly IPropertiesEditorService _propertiesService;
        private readonly IComponentFilterService _componentFilterService;

        private readonly string[] _arguments;

        public event EventHandler<SceneTreeItemViewModel>? ScrollToItemRequested;

        private ObservableCollection<SceneTreeItemViewModel> _sceneTreeItems = new();
        private PropertiesEditorView _propertiesEditor = null!;
        private ObjectWithRefModel? _selectedObject;

        private ObservableCollection<ComponentFilterModel> _availableComponents = new();
        private HashSet<string> _selectedComponents = new(StringComparer.OrdinalIgnoreCase);
        private List<ActorModel> _originalActors = new();
        private bool _isComponentFilterEnabled;

    // Selection loop prevention / state flags
    private bool _isProcessingRuntimeSelection = false; // true while applying a runtime-originated selection
    private bool _isProcessingPropertiesSelection = false; // true while applying a properties-originated selection
    private DateTime _lastRuntimeSelectionTime = DateTime.MinValue; // timestamp of last runtime selection applied
    private uint _lastRequestedEngineSelectionRef = uint.MaxValue; // last object ref we explicitly sent to engine
    private const int PropertiesSelectionDebounceMs = 300; // ignore differing properties selection within this window after runtime selection

        private ObjectTypeFilter _currentObjectTypeFilter = ObjectTypeFilter.All;
        private Dictionary<SceneTreeItemViewModel, (SceneTreeItemViewModel? actorsGroup, SceneTreeItemViewModel? frisesGroup)> _originalSceneGroups = new();
        private System.Threading.Timer? _filterThrottleTimer;
        private ObjectTypeFilter? _pendingFilter;
        private readonly object _filterLock = new object();
        private string _searchText = string.Empty;

        public override string SubToolName => "Scene Explorer";

        public ICommand DuplicateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RenameCommand { get; }

        public ICommand RefreshCommand { get; }
        public ICommand SelectInEngineCommand { get; }

        public ICommand ClearFiltersCommand { get; }
        public ICommand UnselectAllFiltersCommand { get; }

        public ICommand ShowAllObjectsCommand { get; }
        public ICommand ShowActorsOnlyCommand { get; }
        public ICommand ShowFrisesOnlyCommand { get; }

        public ObservableCollection<SceneTreeItemViewModel> SceneTreeItems
        {
            get => _sceneTreeItems;
            set => SetProperty(ref _sceneTreeItems, value);
        }

        public PropertiesEditorView PropertiesEditor
        {
            get => _propertiesEditor;
            private set => SetProperty(ref _propertiesEditor, value);
        }

        public ObjectWithRefModel? SelectedObject
        {
            get => _selectedObject;
            private set => SetProperty(ref _selectedObject, value);
        }

        public ObservableCollection<ComponentFilterModel> AvailableComponents
        {
            get => _availableComponents;
            set => SetProperty(ref _availableComponents, value);
        }

        public HashSet<string> SelectedComponents
        {
            get => _selectedComponents;
            private set => SetProperty(ref _selectedComponents, value);
        }

        public bool IsComponentFilterEnabled
        {
            get => _isComponentFilterEnabled;
            set => SetProperty(ref _isComponentFilterEnabled, value);
        }
        public bool HasComponents
        {
            get => AvailableComponents.Count > 0;
        }

        public int SelectedComponentsCount
        {
            get => SelectedComponents.Count;
        }

        public ObjectTypeFilter CurrentObjectTypeFilter
        {
            get => _currentObjectTypeFilter;
            private set => SetProperty(ref _currentObjectTypeFilter, value);
        }

        public bool IsShowingAll => CurrentObjectTypeFilter == ObjectTypeFilter.All;
        public bool IsShowingActorsOnly => CurrentObjectTypeFilter == ObjectTypeFilter.ActorsOnly;
        public bool IsShowingFrisesOnly => CurrentObjectTypeFilter == ObjectTypeFilter.FrisesOnly;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // Apply all filters when search text changes
                    ApplyAllFilters();
                }
            }
        }

        public SceneExplorerViewModel(
            ILogService logService,
            ISceneExplorerService sceneService,
            IPropertiesEditorService propertiesService,
            IComponentFilterService componentFilterService,
            IEngineHostService engineHost)
            : base(logService)
        {
            _sceneService = sceneService ?? throw new ArgumentNullException(nameof(sceneService));
            _propertiesService = propertiesService ?? throw new ArgumentNullException(nameof(propertiesService));
            _componentFilterService = componentFilterService ?? throw new ArgumentNullException(nameof(componentFilterService));
            _arguments = ["--port", "12345"]; // Default arguments

            AvailableComponents = new ObservableCollection<ComponentFilterModel>();
            SelectedComponents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            DuplicateCommand = new AsyncRelayCommand(ExecuteDuplicateAsync, CanExecuteDuplicate);
            DeleteCommand = new AsyncRelayCommand(ExecuteDeleteAsync, CanExecuteDelete);
            RenameCommand = new RelayCommand<string>(ExecuteRename, CanExecuteRename);

            RefreshCommand = new AsyncRelayCommand(async () => await RefreshSceneTreeAsync(null));
            SelectInEngineCommand = new AsyncRelayCommand(async () => await SelectInEngineAsync(null), () => SelectedObject != null);

            ClearFiltersCommand = new RelayCommand(ClearAllFilters);
            UnselectAllFiltersCommand = new RelayCommand(UnselectAllFilters);

            ShowAllObjectsCommand = new RelayCommand(() => SetObjectTypeFilter(ObjectTypeFilter.All));
            ShowActorsOnlyCommand = new RelayCommand(() => SetObjectTypeFilter(ObjectTypeFilter.ActorsOnly));
            ShowFrisesOnlyCommand = new RelayCommand(() => SetObjectTypeFilter(ObjectTypeFilter.FrisesOnly));

            _sceneService.OnlineSceneTreeUpdated += OnOnlineSceneTreeUpdated;
            _sceneService.OfflineSceneTreesUpdated += OnOfflineSceneTreesUpdated;
            _sceneService.ObjectSelectedFromRuntime += OnObjectSelectedFromRuntime;

            _propertiesService.PropertiesUpdated += OnPropertiesUpdated;

            SubscribeToConnectionEvents();

            InitializePropertiesEditor();
            UpdateConnectionStatus();
            _ = StartServicesAsync(_arguments);
        }

        private void InitializePropertiesEditor()
        {
            var propertiesViewModel = new PropertiesEditorViewModel(_propertiesService, LogService);
            PropertiesEditor = new PropertiesEditorView
            {
                ViewModel = propertiesViewModel
            };
        }

        private async Task StartServicesAsync(string[] arguments)
        {
            try
            {
                await Task.WhenAll(
                    _sceneService.StartAsync(arguments),
                    _propertiesService.StartAsync(arguments)
                );
                LogService.Info("All services started successfully");

                if (!_sceneService.IsConnected || !_propertiesService.IsConnected)
                {
                    LogService.Info("Services not connected, forcing connection attempts");
                    _sceneService.ForceConnectionAttempt();
                    _propertiesService.ForceConnectionAttempt();

                    _ = Task.Delay(1000).ContinueWith(_ =>
                    {
                        try
                        {
                            if (_sceneService.IsConnected)
                            {
                                LogService.Info("Auto-requesting scene tree after connection attempt");
                                _sceneService.RequestSceneTree();
                            }
                            else
                            {
                                LogService.Warning("Scene service still not connected after force connection attempt");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogService.Error("Failed to auto-request scene tree", ex);
                        }
                    });
                }
                else
                {
                    LogService.Info("Services already connected, requesting scene tree");
                    _sceneService.RequestSceneTree();

                }
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to start one or more services", ex);
            }
        }

        // Event Handlers

        private void OnOnlineSceneTreeUpdated(object? sender, SceneTreeModel sceneTree)
        {
            App.Current?.Dispatcher.InvokeAsync(() =>
            {
                _originalSceneGroups.Clear();

                StoreOriginalActors(sceneTree);

                // Preserve existing component selections if filter is active
                HashSet<string>? preserveSelection = IsComponentFilterEnabled
                    ? new HashSet<string>(SelectedComponents, StringComparer.OrdinalIgnoreCase)
                    : null;

                ExtractAndPopulateComponents(_originalActors, preserveSelection);

                var treeItem = CreateSceneTreeItem(sceneTree);
                var items = new ObservableCollection<SceneTreeItemViewModel>();
                items.Add(treeItem);
                SceneTreeItems = items;
                LogService.Info($"Updated scene tree: {sceneTree.UniqueName}");
                LogService.Info($"SceneTreeItems.Count={SceneTreeItems.Count}");

                // If component filter was active before refresh, reapply filtering
                if (IsComponentFilterEnabled)
                {
                    LogService.Info("Reapplying all filters after scene refresh");
                    ApplyAllFilters();
                }

            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        private void OnOfflineSceneTreesUpdated(object? sender, List<SceneTreeModel> sceneTrees)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                _originalSceneGroups.Clear();

                StoreOriginalActorsFromScenes(sceneTrees);

                // Preserve existing component selections if filter is active
                HashSet<string>? preserveSelection = IsComponentFilterEnabled
                    ? new HashSet<string>(SelectedComponents, StringComparer.OrdinalIgnoreCase)
                    : null;

                ExtractAndPopulateComponents(_originalActors, preserveSelection);

                SceneTreeItems.Clear();
                foreach (var sceneTree in sceneTrees)
                {
                    var treeItem = CreateSceneTreeItem(sceneTree);
                    SceneTreeItems.Add(treeItem);
                }
                LogService.Info($"Updated offline scene trees: {sceneTrees.Count} scenes");

                if (IsComponentFilterEnabled)
                {
                    LogService.Info("Reapplying all filters after offline scenes refresh");
                    ApplyAllFilters();
                }
            });
        }

        private void OnObjectSelectedFromRuntime(object? sender, uint objectRef)
        {
            LogService.Info($"✨ RUNTIME SELECTION EVENT: Object selected from runtime: {objectRef}");

            App.Current?.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (_isProcessingRuntimeSelection)
                    {
                        LogService.Info("⚠️ Runtime selection re-entered; ignoring nested call.");
                        return;
                    }

                    if (SelectedObject is ObjectWithRefModel existing && existing.ObjectRef == objectRef)
                    {
                        LogService.Info("🔁 Runtime selection matches current SelectedObject; ignoring to prevent loop.");
                        return;
                    }

                    _isProcessingRuntimeSelection = true;
                    LogService.Info($"🔍 Searching for object {objectRef} in scene tree with {SceneTreeItems.Count} root items");

                    var selectedItem = FindTreeItemByObjectRef(SceneTreeItems, objectRef);
                        if (selectedItem != null)
                    {
                        LogService.Info($"✅ Found object {objectRef} in scene tree!");

                        ClearTreeSelection(SceneTreeItems);
                        LogService.Info($"🧹 Cleared previous tree selection");

                        selectedItem.IsSelected = true;
                        selectedItem.IsExpanded = true;

                        ExpandParentHierarchy(selectedItem);
                        LogService.Info($"📂 Expanded parent hierarchy for visibility");

                        if (selectedItem.Model is ObjectWithRefModel objectModel)
                        {
                            SelectedObject = objectModel;
                            LogService.Info($"🎯 Tree selection synced with runtime: {objectModel.FriendlyName} (ref: {objectRef})");
                        }

                        RequestScrollToItem(selectedItem);
                    }
                    else
                    {
                        LogService.Warning($"❌ Could not find object with ref {objectRef} in scene tree (total items: {CountAllTreeItems()})");
                        LogAllTreeItemRefs();
                    }
                    _lastRuntimeSelectionTime = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    LogService.Error($"💥 Failed to sync tree selection with runtime", ex);
                }
                finally
                {
                    _isProcessingRuntimeSelection = false;
                }
            });
        }

        private void OnPropertiesUpdated(object? sender, PropertyModel propertyModel)
        {
            // Extract ObjectRef from property model and sync with scene tree
            if (propertyModel != null && propertyModel.ObjectRef != uint.MaxValue && propertyModel.ObjectRef != 0)
            {
                LogService.Info($"🔗 PropertiesEditor updated for object ref: {propertyModel.ObjectRef} - syncing with scene tree");

                App.Current?.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Debounce: if runtime selection just happened very recently and this refers to a different object, ignore to avoid loop
                        if ((DateTime.UtcNow - _lastRuntimeSelectionTime).TotalMilliseconds < PropertiesSelectionDebounceMs &&
                            SelectedObject is ObjectWithRefModel so && so.ObjectRef != propertyModel.ObjectRef)
                        {
                            LogService.Info($"⏱️ Ignoring properties selection {propertyModel.ObjectRef} due to recent runtime selection {so.ObjectRef}");
                            return;
                        }

                        if (_isProcessingPropertiesSelection)
                        {
                            LogService.Info("⚠️ Re-entrant properties selection ignored");
                            return;
                        }

                        if (SelectedObject is ObjectWithRefModel existing && existing.ObjectRef == propertyModel.ObjectRef)
                        {
                            LogService.Info("🔁 Properties selection matches current; ignoring");
                            return;
                        }

                        _isProcessingPropertiesSelection = true;
                        var selectedItem = FindTreeItemByObjectRef(SceneTreeItems, propertyModel.ObjectRef);
                        if (selectedItem != null)
                        {
                            LogService.Info($"✅ Found and selecting object {propertyModel.ObjectRef} from properties update");

                            ClearTreeSelection(SceneTreeItems);

                            selectedItem.IsSelected = true;
                            selectedItem.IsExpanded = true;

                            ExpandParentHierarchy(selectedItem);

                            if (selectedItem.Model is ObjectWithRefModel objectModel)
                            {
                                SelectedObject = objectModel;
                                LogService.Info($"Tree selection synced from PropertiesEditor: {objectModel.FriendlyName} (ref: {propertyModel.ObjectRef})");
                            }

                            RequestScrollToItem(selectedItem);
                        }
                        else
                        {
                            LogService.Warning($"Could not find object {propertyModel.ObjectRef} from PropertiesEditor update in scene tree");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Error($"Failed to sync selection from PropertiesEditor update", ex);
                    }
                    finally
                    {
                        _isProcessingPropertiesSelection = false;
                    }
                });
            }
        }

        // Tree Selection Helper Methods

        private SceneTreeItemViewModel? FindTreeItemByObjectRef(ObservableCollection<SceneTreeItemViewModel> items, uint objectRef)
        {
            foreach (var item in items)
            {
                if (item.Model is ObjectWithRefModel objectModel && objectModel.ObjectRef == objectRef)
                {
                    return item;
                }

                var foundInChildren = FindTreeItemByObjectRef(item.Children, objectRef);
                if (foundInChildren != null)
                {
                    return foundInChildren;
                }
            }
            return null;
        }

        private void UpdateConnectionStatus()
        {
            IsConnected = _sceneService.IsConnected;
        }

        private void RequestScrollToItem(SceneTreeItemViewModel item)
        {
            // Use a lightweight dispatcher callback (no long artificial delay)
            App.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    ScrollToItemRequested?.Invoke(this, item);
                    LogService.Info($"📜 Requested scroll to item: {item.DisplayName}");
                }
                catch (Exception ex)
                {
                    LogService.Error("Failed to request scroll to item", ex);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }


        private void SetObjectTypeFilter(ObjectTypeFilter filter)
        {
            if (CurrentObjectTypeFilter == filter) return;

            lock (_filterLock)
            {
                _filterThrottleTimer?.Dispose();
                _filterThrottleTimer = null;

                _pendingFilter = filter;

                _filterThrottleTimer = new System.Threading.Timer(ApplyPendingFilter, null, TimeSpan.FromMilliseconds(100), Timeout.InfiniteTimeSpan);
            }

            CurrentObjectTypeFilter = filter;
            OnPropertyChanged(nameof(IsShowingAll));
            OnPropertyChanged(nameof(IsShowingActorsOnly));
            OnPropertyChanged(nameof(IsShowingFrisesOnly));

            LogService.Info($"Object type filter scheduled: {filter}");
        }


        private void ApplyPendingFilter(object? state)
        {
            ObjectTypeFilter? filterToApply = null;

            lock (_filterLock)
            {
                if (_pendingFilter.HasValue)
                {
                    filterToApply = _pendingFilter.Value;
                    _pendingFilter = null;
                }
            }

            if (filterToApply.HasValue)
            {
                try
                {
                    App.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            ApplyAllFilters();
                        }
                        catch (Exception ex)
                        {
                            LogService.Error("Failed to apply pending filters", ex);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    LogService.Error("Failed to schedule pending object type filter", ex);
                }
            }
        }

        private void ApplyObjectTypeFilter()
        {
            try
            {
                var startTime = DateTime.Now;

                // Apply object type filter to each scene in the tree
                App.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    using (var deferRefresh = DeferTreeViewRefresh())
                    {
                        var scenesToRemove = new List<SceneTreeItemViewModel>();
                        foreach (var sceneItem in SceneTreeItems.ToList())
                        {
                            ApplyObjectTypeFilterToScene(sceneItem);

                            if (CurrentObjectTypeFilter == ObjectTypeFilter.ActorsOnly && !HasVisibleGroups(sceneItem))
                            {
                                scenesToRemove.Add(sceneItem);
                            }
                        }

                        foreach (var sceneToRemove in scenesToRemove)
                        {
                            SceneTreeItems.Remove(sceneToRemove);
                        }
                    }

                    OnPropertyChanged(nameof(SceneTreeItems));

                    var duration = (DateTime.Now - startTime).TotalMilliseconds;
                    LogService.Info($"Object type filter '{CurrentObjectTypeFilter}' applied in {duration:F0}ms");
                }));

                LogService.Info($"Scheduled object type filter application: {CurrentObjectTypeFilter}");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to apply object type filter", ex);
            }
        }

        /// <summary>
        /// Áp dụng tất cả các filter (Object Type + Component + Search) cho toàn bộ tree
        /// </summary>
        private void ApplyAllFilters()
        {
            try
            {
                var startTime = DateTime.Now;

                App.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    using (var deferRefresh = DeferTreeViewRefresh())
                    {
                        foreach (var sceneItem in SceneTreeItems.ToList())
                        {
                            ApplyAllFiltersToScene(sceneItem);
                        }
                    }

                    OnPropertyChanged(nameof(SceneTreeItems));

                    var duration = (DateTime.Now - startTime).TotalMilliseconds;
                    LogService.Info($"All filters applied in {duration:F0}ms");
                }));

                LogService.Info("Scheduled application of all filters");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to apply all filters", ex);
            }
        }

        private void ApplyAllFiltersToScene(SceneTreeItemViewModel sceneItem)
        {
            if (sceneItem.Model is not SceneTreeModel sceneModel) return;

            if (!_originalSceneGroups.TryGetValue(sceneItem, out var originalGroups))
            {
                return;
            }

            var (originalActorsGroup, originalFrisesGroup) = originalGroups;

            // Apply visibility filter to all groups
            ApplyVisibilityFilterToGroups(sceneItem);

            // Apply to child scenes
            foreach (var childScene in sceneItem.Children.Where(c => c.ItemType == SceneTreeItemType.Scene).ToList())
            {
                ApplyAllFiltersToScene(childScene);
            }
        }

        private void ApplyObjectTypeFilterToScene(SceneTreeItemViewModel sceneItem)
        {
            if (sceneItem.Model is not SceneTreeModel sceneModel) return;

            if (!_originalSceneGroups.TryGetValue(sceneItem, out var originalGroups))
            {
                return;
            }

            var (originalActorsGroup, originalFrisesGroup) = originalGroups;
            bool hasVisibleContent = false;

            var groupsToAdd = new List<SceneTreeItemViewModel>();
            var groupsToRemove = sceneItem.Children.Where(c =>
                c.ItemType == SceneTreeItemType.ActorSet ||
                c.ItemType == SceneTreeItemType.FriseSet).ToList();

            switch (CurrentObjectTypeFilter)
            {
                case ObjectTypeFilter.All:
                    if (originalActorsGroup != null && sceneModel.Actors.Count > 0)
                    {
                        groupsToAdd.Add(originalActorsGroup);
                        hasVisibleContent = true;
                    }
                    if (originalFrisesGroup != null && sceneModel.Frises.Count > 0)
                    {
                        groupsToAdd.Add(originalFrisesGroup);
                        hasVisibleContent = true;
                    }
                    break;

                case ObjectTypeFilter.ActorsOnly:
                    if (originalActorsGroup != null && sceneModel.Actors.Count > 0)
                    {
                        groupsToAdd.Add(originalActorsGroup);
                        hasVisibleContent = true;
                    }
                    break;

                case ObjectTypeFilter.FrisesOnly:
                    if (originalFrisesGroup != null && sceneModel.Frises.Count > 0)
                    {
                        groupsToAdd.Add(originalFrisesGroup);
                        hasVisibleContent = true;
                    }
                    break;
            }

            foreach (var group in groupsToRemove)
            {
                sceneItem.Children.Remove(group);
            }

            foreach (var group in groupsToAdd.OrderBy(g => GetInsertIndexForGroup(sceneItem.Children, g.ItemType)))
            {
                AddGroupToScene(sceneItem, group);
            }
            ApplyVisibilityFilterToGroups(sceneItem);

            var childScenesWithContent = 0;
            foreach (var childScene in sceneItem.Children.Where(c => c.ItemType == SceneTreeItemType.Scene).ToList())
            {
                ApplyObjectTypeFilterToScene(childScene);

                if (HasVisibleGroups(childScene))
                {
                    hasVisibleContent = true;
                    childScenesWithContent++;
                }
                else
                {
                    SetSceneVisibility(childScene, false);
                }
            }

            if (sceneModel.Actors.Count > 0 || sceneModel.Frises.Count > 0 || childScenesWithContent > 0)
            {
                LogService.Info($"Scene '{sceneModel.UniqueName}': {CurrentObjectTypeFilter}, A:{sceneModel.Actors.Count}, F:{sceneModel.Frises.Count}, Children:{childScenesWithContent}");
            }
        }

        private void ApplyVisibilityFilterToGroups(SceneTreeItemViewModel sceneItem)
        {
            foreach (var group in sceneItem.Children.Where(c => 
                c.ItemType == SceneTreeItemType.ActorSet || 
                c.ItemType == SceneTreeItemType.FriseSet))
            {
                foreach (var item in group.Children)
                {
                    // Áp dụng tất cả các filter: Object Type + Component + Search
                    bool shouldBeVisible = ShouldItemBeVisible(item);
                    item.IsVisible = shouldBeVisible;
                }
            }
        }

        private bool ShouldItemBeVisible(SceneTreeItemViewModel item)
        {
            // 1. Object Type Filter
            bool objectTypeMatch = CurrentObjectTypeFilter switch
            {
                ObjectTypeFilter.All => true,
                ObjectTypeFilter.ActorsOnly => item.ItemType == SceneTreeItemType.Actor,
                ObjectTypeFilter.FrisesOnly => item.ItemType == SceneTreeItemType.Frise,
                _ => true
            };

            if (!objectTypeMatch) return false;

            // 2. Component Filter (chỉ áp dụng cho actors)
            if (item.ItemType == SceneTreeItemType.Actor && IsComponentFilterEnabled && SelectedComponents.Count > 0)
            {
                if (item.Model is ActorModel actor)
                {
                    var selectedComponentsSet = new HashSet<string>(SelectedComponents, StringComparer.OrdinalIgnoreCase);
                    bool hasSelectedComponent = _componentFilterService.ActorHasAnyComponent(actor, selectedComponentsSet);
                    if (!hasSelectedComponent) return false;
                }
            }

            // 3. Search Filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                bool searchMatch = item.DisplayName?.ToLowerInvariant().Contains(SearchText.ToLowerInvariant()) ?? false;
                if (!searchMatch) return false;
            }

            return true;
        }

        private void AddGroupToScene(SceneTreeItemViewModel sceneItem, SceneTreeItemViewModel groupItem)
        {
            int correctIndex = GetInsertIndexForGroup(sceneItem.Children, groupItem.ItemType);

            if (correctIndex < sceneItem.Children.Count &&
                sceneItem.Children[correctIndex] == groupItem)
            {
                return;
            }

            if (sceneItem.Children.Contains(groupItem))
            {
                sceneItem.Children.Remove(groupItem);
            }

            if (correctIndex >= sceneItem.Children.Count)
            {
                sceneItem.Children.Add(groupItem);
            }
            else
            {
                sceneItem.Children.Insert(correctIndex, groupItem);
            }

            // LogService.Info($"Added {groupItem.ItemType} group '{groupItem.DisplayName}' to scene at index {correctIndex}");
        }

        private bool HasVisibleGroups(SceneTreeItemViewModel sceneItem)
        {
            var actorsGroup = sceneItem.Children.FirstOrDefault(c => c.ItemType == SceneTreeItemType.ActorSet);
            var frisesGroup = sceneItem.Children.FirstOrDefault(c => c.ItemType == SceneTreeItemType.FriseSet);

            bool hasVisibleGroups = false;

            if (actorsGroup != null && sceneItem.Children.Contains(actorsGroup))
                hasVisibleGroups = true;

            if (frisesGroup != null && sceneItem.Children.Contains(frisesGroup))
                hasVisibleGroups = true;

            foreach (var childScene in sceneItem.Children.Where(c => c.ItemType == SceneTreeItemType.Scene))
            {
                if (HasVisibleGroups(childScene))
                {
                    hasVisibleGroups = true;
                    break;
                }
            }

            return hasVisibleGroups;
        }

        private void SetSceneVisibility(SceneTreeItemViewModel sceneItem, bool isVisible)
        {
            var parentScene = FindParentSceneForScene(sceneItem);
            if (parentScene == null) return;

            if (isVisible)
            {
                if (!parentScene.Children.Contains(sceneItem))
                {
                    int insertIndex = 0;
                    parentScene.Children.Insert(insertIndex, sceneItem);
                }
            }
            else
            {   
                if (parentScene.Children.Contains(sceneItem))
                {
                    parentScene.Children.Remove(sceneItem);
                }
            }
        }

        private SceneTreeItemViewModel? FindParentSceneForScene(SceneTreeItemViewModel targetScene)
        {
            foreach (var sceneItem in SceneTreeItems)
            {
                var foundParent = FindParentSceneForSceneRecursive(sceneItem, targetScene);
                if (foundParent != null) return foundParent;
            }
            return null;
        }

        private SceneTreeItemViewModel? FindParentSceneForSceneRecursive(SceneTreeItemViewModel currentScene, SceneTreeItemViewModel targetScene)
        {
            if (currentScene.Children.Contains(targetScene))
            {
                return currentScene;
            }

            foreach (var childScene in currentScene.Children.Where(c => c.ItemType == SceneTreeItemType.Scene))
            {
                var foundParent = FindParentSceneForSceneRecursive(childScene, targetScene);
                if (foundParent != null) return foundParent;
            }

            return null;
        }

        private void SetGroupVisibility(SceneTreeItemViewModel groupItem, bool isVisible)
        {
            var parentScene = FindParentSceneForGroup(groupItem);
            if (parentScene == null) return;

            if (isVisible)
            {
                if (!parentScene.Children.Contains(groupItem))
                {
                    int insertIndex = GetInsertIndexForGroup(parentScene.Children, groupItem.ItemType);
                    parentScene.Children.Insert(insertIndex, groupItem);
                }
            }
            else
            {
                if (parentScene.Children.Contains(groupItem))
                {
                    parentScene.Children.Remove(groupItem);
                }
            }
        }

        private SceneTreeItemViewModel? FindParentSceneForGroup(SceneTreeItemViewModel groupItem)
        {
            foreach (var sceneItem in SceneTreeItems)
            {
                var foundParent = FindParentSceneRecursive(sceneItem, groupItem);
                if (foundParent != null) return foundParent;
            }
            return null;
        }

        private SceneTreeItemViewModel? FindParentSceneRecursive(SceneTreeItemViewModel currentScene, SceneTreeItemViewModel targetGroup)
        {
            if (currentScene.Children.Contains(targetGroup))
            {
                return currentScene;
            }

            foreach (var childScene in currentScene.Children.Where(c => c.ItemType == SceneTreeItemType.Scene))
            {
                var foundParent = FindParentSceneRecursive(childScene, targetGroup);
                if (foundParent != null) return foundParent;
            }

            return null;
        }

        private int GetInsertIndexForGroup(ObservableCollection<SceneTreeItemViewModel> children, SceneTreeItemType groupType)
        {
            if (groupType == SceneTreeItemType.ActorSet)
            {
                var lastSceneIndex = -1;
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i].ItemType == SceneTreeItemType.Scene)
                        lastSceneIndex = i;
                    else if (children[i].ItemType == SceneTreeItemType.FriseSet)
                        return i;
                }
                return lastSceneIndex + 1;
            }
            else if (groupType == SceneTreeItemType.FriseSet)
            {
                return children.Count;
            }

            return children.Count;
        }

        private void ClearTreeSelection(ObservableCollection<SceneTreeItemViewModel> items)
        {
            foreach (var item in items)
            {
                item.IsSelected = false;
                ClearTreeSelection(item.Children);
            }
        }

        private void ExpandParentHierarchy(SceneTreeItemViewModel item)
        {
            var parent = FindParentItem(SceneTreeItems, item);
            while (parent != null)
            {
                parent.IsExpanded = true;
                parent = FindParentItem(SceneTreeItems, parent);
            }
        }

        private SceneTreeItemViewModel? FindParentItem(ObservableCollection<SceneTreeItemViewModel> items, SceneTreeItemViewModel targetItem)
        {
            foreach (var item in items)
            {
                if (item.Children.Contains(targetItem))
                {
                    return item;
                }

                var foundInChildren = FindParentItem(item.Children, targetItem);
                if (foundInChildren != null)
                {
                    return foundInChildren;
                }
            }
            return null;
        }

        private int CountAllTreeItems()
        {
            return CountTreeItemsRecursive(SceneTreeItems);
        }

        private int CountTreeItemsRecursive(ObservableCollection<SceneTreeItemViewModel> items)
        {
            var count = items.Count;
            foreach (var item in items)
            {
                count += CountTreeItemsRecursive(item.Children);
            }
            return count;
        }

        private void LogAllTreeItemRefs()
        {
            LogService.Info("📋 Logging all ObjectRefs in scene tree:");
            LogTreeItemRefsRecursive(SceneTreeItems, 0);
        }

        private void LogTreeItemRefsRecursive(ObservableCollection<SceneTreeItemViewModel> items, int depth)
        {
            var indent = new string(' ', depth * 2);
            foreach (var item in items)
            {
                if (item.Model is ObjectWithRefModel objModel)
                {
                    LogService.Info($"{indent}- {objModel.FriendlyName} (ref: {objModel.ObjectRef})");
                }
                else
                {
                    LogService.Info($"{indent}- {item.DisplayName} (no ObjectRef)");
                }
                LogTreeItemRefsRecursive(item.Children, depth + 1);
            }
        }

        private SceneTreeItemViewModel CreateSceneTreeItem(SceneTreeModel sceneTree)
        {
            var displayName = string.IsNullOrWhiteSpace(sceneTree.UniqueName)
                ? (string.IsNullOrWhiteSpace(sceneTree.Path) ? "(Scene)" : System.IO.Path.GetFileNameWithoutExtension(sceneTree.Path))
                : sceneTree.UniqueName;

            var item = new SceneTreeItemViewModel
            {
                DisplayName = displayName,
                Model = sceneTree,
                ItemType = SceneTreeItemType.Scene
            };

            SceneTreeItemViewModel? actorsGroup = null;
            SceneTreeItemViewModel? frisesGroup = null;

            foreach (var child in sceneTree.ChildScenes)
            {
                var childItem = CreateSceneTreeItem(child);
                item.Children.Add(childItem);
            }

            if (sceneTree.Actors.Count > 0)
            {
                actorsGroup = new SceneTreeItemViewModel
                {
                    DisplayName = $"Actors ({sceneTree.Actors.Count})",
                    Model = null,
                    ItemType = SceneTreeItemType.ActorSet
                };

                foreach (var actor in sceneTree.Actors)
                {
                    actorsGroup.Children.Add(new SceneTreeItemViewModel
                    {
                        DisplayName = actor.FriendlyName,
                        Model = actor,
                        ItemType = SceneTreeItemType.Actor
                    });
                }

                item.Children.Add(actorsGroup);
            }

            if (sceneTree.Frises.Count > 0)
            {
                frisesGroup = new SceneTreeItemViewModel
                {
                    DisplayName = $"Frises ({sceneTree.Frises.Count})",
                    Model = null,
                    ItemType = SceneTreeItemType.FriseSet
                };

                foreach (var frise in sceneTree.Frises)
                {
                    frisesGroup.Children.Add(new SceneTreeItemViewModel
                    {
                        DisplayName = frise.FriendlyName,
                        Model = frise,
                        ItemType = SceneTreeItemType.Frise
                    });
                }

                item.Children.Add(frisesGroup);
            }

            _originalSceneGroups[item] = (actorsGroup, frisesGroup);

            return item;
        }

        public void OnTreeItemSelected(SceneTreeItemViewModel selectedItem)
        {
            if (selectedItem == null) return;

            LogService.Info($"Tree item selected: {selectedItem.DisplayName} ({selectedItem.ItemType})");

            switch (selectedItem.ItemType)
            {
                case SceneTreeItemType.Scene:
                    if (selectedItem.Model is SceneTreeModel scene)
                    {
                        LogService.Info($"Selected scene: {scene.UniqueName}");
                        // Avoid re-requesting scene tree if name is empty or same scene to prevent refresh that resets filters
                        if (!string.IsNullOrWhiteSpace(scene.UniqueName))
                        {
                            _sceneService.SelectScene(scene.UniqueName);
                        }
                        PropertiesEditor.ViewModel?.ClearProperties();
                        SelectedObject = null;
                    }
                    break;

                case SceneTreeItemType.Actor:
                    if (selectedItem.Model is ActorModel actor)
                    {
                        // Prevent loops: if runtime or properties selection is being applied, do not echo selection back to engine
                        if (_isProcessingRuntimeSelection || _isProcessingPropertiesSelection)
                        {
                            LogService.Info("🔄 Skipping engine select (actor) due to internal selection processing");
                        }
                        else
                        {
                            if (SelectedObject is ObjectWithRefModel ex && ex.ObjectRef == actor.ObjectRef)
                            {
                                LogService.Info("🔁 Actor already current; skipping duplicate engine select");
                            }
                            else
                            {
                                SelectedObject = actor;
                                LogService.Info($"Selected actor: {actor.FriendlyName}");
                                PropertiesEditor.ViewModel?.LoadObjectProperties(actor);
                                if (actor.IsOnline)
                                {
                                    _lastRequestedEngineSelectionRef = actor.ObjectRef;
                                    _sceneService.SelectObjects(new[] { actor });
                                }
                            }
                        }
                    }
                    break;

                case SceneTreeItemType.Frise:
                    if (selectedItem.Model is FriseModel frise)
                    {
                        if (_isProcessingRuntimeSelection || _isProcessingPropertiesSelection)
                        {
                            LogService.Info("🔄 Skipping engine select (frise) due to internal selection processing");
                        }
                        else
                        {
                            if (SelectedObject is ObjectWithRefModel ex && ex.ObjectRef == frise.ObjectRef)
                            {
                                LogService.Info("🔁 Frise already current; skipping duplicate engine select");
                            }
                            else
                            {
                                SelectedObject = frise;
                                LogService.Info($"Selected frise: {frise.FriendlyName}");
                                PropertiesEditor.ViewModel?.LoadObjectProperties(frise);
                                if (frise.IsOnline)
                                {
                                    _lastRequestedEngineSelectionRef = frise.ObjectRef;
                                    _sceneService.SelectObjects(new[] { frise });
                                }
                            }
                        }
                    }
                    break;

                case SceneTreeItemType.ActorSet:
                case SceneTreeItemType.FriseSet:
                    PropertiesEditor.ViewModel?.ClearProperties();
                    SelectedObject = null;
                    LogService.Info($"Selected {selectedItem.ItemType} group");
                    break;
            }
        }

        private async Task SelectInEngineAsync(object? parameter)
        {
            try
            {
                if (SelectedObject != null)
                {
                    _sceneService.SelectObjects(new[] { SelectedObject });
                    LogService.Info($"Selected object in engine: {SelectedObject.FriendlyName}");
                }
                else
                {
                    LogService.Info("No object selected to focus in engine");
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to select in engine", ex);
            }
            await Task.CompletedTask;
        }

        private async Task SelectHighlightedAsync(object? parameter)
        {
            try
            {
                if (SelectedObject != null)
                {
                    _sceneService.SelectObjects(new[] { SelectedObject });
                    LogService.Info($"Selected highlighted object: {SelectedObject.FriendlyName}");
                }
                else
                {
                    LogService.Info("No highlighted object to select");
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to select highlighted object", ex);
            }
            await Task.CompletedTask;
        }

        private async Task DeleteHighlightedAsync(object? parameter)
        {
            try
            {
                if (SelectedObject != null)
                {
                    _sceneService.DeleteObject(SelectedObject.ObjectRef);
                    LogService.Info($"Deleted object: {SelectedObject.FriendlyName}");

                    PropertiesEditor.ViewModel?.ClearProperties();
                    SelectedObject = null;
                }
                else
                {
                    LogService.Info("No object selected to delete");
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to delete object", ex);
            }
            await Task.CompletedTask;
        }

        private async Task RefreshSceneTreeAsync(object? parameter)
        {
            try
            {
                _sceneService.RequestSceneTree();
                LogService.Info("Requested scene tree refresh");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to refresh scene tree", ex);
            }
            await Task.CompletedTask;
        }

        private bool CanExecuteDuplicate() =>
            SelectedObject != null &&
            SelectedObject.IsOnline &&
            _sceneService.IsConnected;

        private async Task ExecuteDuplicateAsync()
        {
            if (!CanExecuteDuplicate() || SelectedObject == null)
                return;

            try
            {   
                _sceneService.DuplicateAndMoveObject(SelectedObject.ObjectRef, 0.0f, 0.0f, 0.0f);
                LogService.Info($"Duplicated object '{SelectedObject.FriendlyName}' at current position");
            }
            catch (Exception ex)
            {
                LogService.Error($"Failed to duplicate object '{SelectedObject?.FriendlyName}'", ex);
                System.Windows.MessageBox.Show($"Failed to duplicate object: {ex.Message}", "Duplication Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            await Task.CompletedTask;
        }

        private bool CanExecuteDelete() =>
            SelectedObject != null &&
            SelectedObject.IsOnline &&
            _sceneService.IsConnected;

        private async Task ExecuteDeleteAsync()
        {
            if (!CanExecuteDelete() || SelectedObject == null)
                return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete '{SelectedObject.FriendlyName}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    var objectToDelete = SelectedObject;

                    _sceneService.DeleteObject(objectToDelete.ObjectRef);

                    SelectedObject = null;
                    PropertiesEditor.ViewModel?.ClearProperties();

                    LogService.Info($"Deleted object '{objectToDelete.FriendlyName}'");
                }
                catch (Exception ex)
                {
                    LogService.Error($"Failed to delete object '{SelectedObject?.FriendlyName}'", ex);
                    System.Windows.MessageBox.Show($"Failed to delete object: {ex.Message}", "Deletion Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }

            await Task.CompletedTask;
        }

        private bool CanExecuteRename(string? newName) =>
            SelectedObject != null &&
            SelectedObject.IsOnline &&
            _sceneService.IsConnected;

        private void ExecuteRename(string? newName)
        {
            if (!CanExecuteRename(newName) || SelectedObject == null || string.IsNullOrWhiteSpace(newName))
                return;

            try
            {
                var objectToRename = SelectedObject;
                var oldName = objectToRename.FriendlyName;
                var trimmedNewName = newName.Trim();

                _sceneService.RenameObject(objectToRename.ObjectRef, trimmedNewName);

                var treeItem = FindTreeItemByObjectRef(SceneTreeItems, objectToRename.ObjectRef);
                if (treeItem != null)
                {
                    treeItem.DisplayName = trimmedNewName;
                }

                LogService.Info($"Renamed object from '{oldName}' to '{trimmedNewName}'");
            }
            catch (Exception ex)
            {
                LogService.Error($"Failed to rename object '{SelectedObject?.FriendlyName}' to '{newName}'", ex);
                System.Windows.MessageBox.Show($"Failed to rename object: {ex.Message}", "Rename Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }


        private void ExtractAndPopulateComponents(List<ActorModel> actors, HashSet<string>? preserveSelection = null)
        {
            try
            {
                var allComponents = _componentFilterService.ExtractAllComponents(actors);
                var componentModels = new List<ComponentFilterModel>();

                foreach (var component in allComponents.OrderBy(c => c))
                {
                    var actorCount = actors.Count(a => _componentFilterService.ActorHasAnyComponent(a, new HashSet<string> { component }));
                    var model = new ComponentFilterModel(component, actorCount);

                    // Preserve selection if provided; otherwise select all by default
                    if (preserveSelection != null)
                    {
                        model.IsSelected = preserveSelection.Contains(component);
                    }
                    else
                    {
                        model.IsSelected = true;
                    }

                    model.SelectionChanged += OnComponentSelectionChanged;

                    componentModels.Add(model);
                }

                foreach (var oldModel in AvailableComponents)
                {
                    oldModel.SelectionChanged -= OnComponentSelectionChanged;
                }

                SelectedComponents.Clear();
                foreach (var model in componentModels)
                {
                    if (model.IsSelected)
                        SelectedComponents.Add(model.ComponentName);
                }

                AvailableComponents.Clear();
                foreach (var model in componentModels)
                {
                    AvailableComponents.Add(model);
                }

                var total = AvailableComponents.Count + componentModels.Count; // AvailableComponents will be cleared below
                var selectedCount = SelectedComponents.Count;
                // If we had a preserved selection, re-compute based on that; otherwise all are selected
                IsComponentFilterEnabled = preserveSelection != null ? selectedCount < allComponents.Count : false;

                OnPropertyChanged(nameof(HasComponents));
                OnPropertyChanged(nameof(SelectedComponents));
                OnPropertyChanged(nameof(SelectedComponentsCount));

                LogService.Info($"Populated {allComponents.Count} unique components (selected {SelectedComponents.Count}{(preserveSelection != null ? " (preserved)" : " (all)")})");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to extract and populate components", ex);
            }
        }



        private void OnComponentSelectionChanged(object? sender, bool isSelected)
        {
            if (sender is not ComponentFilterModel componentModel) return;

            try
            {
                if (isSelected)
                {
                    if (!SelectedComponents.Contains(componentModel.ComponentName))
                    {
                        SelectedComponents.Add(componentModel.ComponentName);
                        LogService.Info($"Re-enabled component: {componentModel.ComponentName}");
                    }
                }
                else
                {
                    if (SelectedComponents.Contains(componentModel.ComponentName))
                    {
                        SelectedComponents.Remove(componentModel.ComponentName);
                        LogService.Info($"Disabled component filter: {componentModel.ComponentName}");
                    }
                }

                OnPropertyChanged(nameof(SelectedComponents));
                OnPropertyChanged(nameof(SelectedComponentsCount));

                ApplyAllFilters();

                var totalComponents = AvailableComponents.Count;
                var selectedCount = SelectedComponents.Count;
                IsComponentFilterEnabled = selectedCount < totalComponents;

                LogService.Info($"Component filtering: {selectedCount}/{totalComponents} components enabled, Filter active: {IsComponentFilterEnabled}");
                LogService.Info($"Enabled components: [{string.Join(", ", SelectedComponents)}]");
            }
            catch (Exception ex)
            {
                LogService.Error($"Failed to handle component selection change for {componentModel.ComponentName}", ex);
            }
        }

        private void ClearAllFilters()
        {
            try
            {
                foreach (var component in AvailableComponents)
                {
                    component.SelectionChanged -= OnComponentSelectionChanged;
                    component.IsSelected = true;
                    component.SelectionChanged += OnComponentSelectionChanged;
                }

                SelectedComponents.Clear();
                foreach (var component in AvailableComponents)
                {
                    SelectedComponents.Add(component.ComponentName);
                }

                OnPropertyChanged(nameof(SelectedComponents));
                OnPropertyChanged(nameof(SelectedComponentsCount));

                IsComponentFilterEnabled = false;

                ApplyAllFilters();

                LogService.Info("Reset all component filters - all components enabled, showing all actors");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to clear component filters", ex);
            }
        }

        private void UnselectAllFilters()
        {
            try
            {
                foreach (var component in AvailableComponents)
                {
                    component.SelectionChanged -= OnComponentSelectionChanged;
                    component.IsSelected = false;
                    component.SelectionChanged += OnComponentSelectionChanged;
                }

                SelectedComponents.Clear();

                OnPropertyChanged(nameof(SelectedComponents));
                OnPropertyChanged(nameof(SelectedComponentsCount));

                IsComponentFilterEnabled = true;

                ApplyAllFilters();

                LogService.Info("Unselected all component filters - no components enabled, hiding all actors");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to unselect all component filters", ex);
            }
        }

        private void ApplyComponentFilters()
        {
            try
            {
                if (_originalActors.Count == 0)
                {
                    LogService.Warning("No original actors to filter");
                    return;
                }

                var totalComponents = AvailableComponents.Count;
                var selectedComponents = SelectedComponents.Count;

                if (selectedComponents == totalComponents)
                {   
                    RebuildSceneTreeWithActors(_originalActors);
                    LogService.Info($"All components selected, showing all {_originalActors.Count} actors");
                    return;
                }

                if (selectedComponents == 0)
                {
                    RebuildSceneTreeWithActors(new List<ActorModel>());
                    LogService.Info("No components selected, hiding all actors");
                    return;
                }

                var selectedComponentsSet = new HashSet<string>(SelectedComponents, StringComparer.OrdinalIgnoreCase);

                var filteredActors = new List<ActorModel>();
                foreach (var actor in _originalActors)
                {
                    bool hasSelectedComponent = _componentFilterService.ActorHasAnyComponent(actor, selectedComponentsSet);
                    if (hasSelectedComponent)
                    {
                        filteredActors.Add(actor);
                    }
                }

                RebuildSceneTreeWithActors(filteredActors);

                LogService.Info($"Applied component filters with OR logic: {selectedComponents}/{totalComponents} components enabled");
                LogService.Info($"Selected components: [{string.Join(", ", SelectedComponents)}]");
                LogService.Info($"Filtered actors: {filteredActors.Count}/{_originalActors.Count} actors shown (actors with ANY selected component)");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to apply component filters", ex);
            }
        }

        private void RebuildSceneTreeWithActors(List<ActorModel> actorsToShow)
        {
            try
            {
                var startTime = DateTime.Now;

                var actorsToShowSet = new HashSet<ActorModel>(actorsToShow);

                App.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (var sceneItem in SceneTreeItems)
                    {
                        UpdateActorVisibility(sceneItem, actorsToShowSet);
                    }

                    var duration = (DateTime.Now - startTime).TotalMilliseconds;
                    LogService.Info($"Actor visibility updated for {actorsToShow.Count} actors in {duration:F0}ms");
                }));

                LogService.Info($"Scheduled actor visibility update for {actorsToShow.Count} actors");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to update actor visibility", ex);
            }
        }

        /// <summary>
        /// Creates a disposable object that defers UI refresh until disposed
        /// </summary>
        private IDisposable DeferTreeViewRefresh()
        {
            return new TreeViewRefreshDeferrer();
        }

        /// <summary>
        /// Helper class to batch UI updates for better performance
        /// </summary>
        private class TreeViewRefreshDeferrer : IDisposable
        {
            public void Dispose()
            {   
            }
        }

        private void UpdateActorVisibility(SceneTreeItemViewModel sceneItem, HashSet<ActorModel> actorsToShowSet)
        {
            foreach (var child in sceneItem.Children)
            {
                if (child.ItemType == SceneTreeItemType.Actor && child.Model is ActorModel actor)
                {
                    child.IsVisible = actorsToShowSet.Contains(actor);
                }
                else if (child.ItemType == SceneTreeItemType.ActorSet || 
                         child.ItemType == SceneTreeItemType.FriseSet || 
                         child.ItemType == SceneTreeItemType.Scene)
                {
                    UpdateActorVisibility(child, actorsToShowSet);
                }
            }
        }

        private void UpdateSceneActorsGroup(SceneTreeItemViewModel sceneItem, List<ActorModel> actorsToShow)
        {
            if (sceneItem.Model is not SceneTreeModel sceneModel) return;

            var actorsGroup = sceneItem.Children.FirstOrDefault(c => c.ItemType == SceneTreeItemType.ActorSet);
            if (actorsGroup != null)
            {
                var sceneActors = actorsToShow.Where(a => sceneModel.Actors.Contains(a)).ToList();

                var currentActorCount = actorsGroup.Children.Count;
                var newActorCount = sceneActors.Count;

                bool needsUpdate = currentActorCount != newActorCount;
                if (!needsUpdate && sceneActors.Count > 0)
                {
                    var currentActors = actorsGroup.Children.Select(c => c.Model).OfType<ActorModel>().ToArray();
                    needsUpdate = !sceneActors.SequenceEqual(currentActors);
                }

                if (needsUpdate)
                {
                    var children = actorsGroup.Children;

                    if (sceneActors.Count == 0)
                    {
                        children.Clear();
                    }
                    else
                    {
                        children.Clear();

                        foreach (var actor in sceneActors)
                        {
                            children.Add(new SceneTreeItemViewModel
                            {
                                DisplayName = actor.FriendlyName,
                                Model = actor,
                                ItemType = SceneTreeItemType.Actor
                            });
                        }
                    }

                    actorsGroup.DisplayName = $"Actors ({sceneActors.Count})";

                    if (sceneActors.Count > 10 || currentActorCount > 10)
                    {
                        LogService.Info($"Updated actors group for scene '{sceneModel.UniqueName}': {currentActorCount} -> {sceneActors.Count} actors");
                    }
                }
            }

            foreach (var childScene in sceneItem.Children.Where(c => c.ItemType == SceneTreeItemType.Scene))
            {
                UpdateSceneActorsGroup(childScene, actorsToShow);
            }
        }


        private void StoreOriginalActors(SceneTreeModel sceneTree)
        {
            _originalActors.Clear();
            CollectActorsFromScene(sceneTree, _originalActors);
            LogService.Info($"Stored {_originalActors.Count} original actors from scene tree");
        }

        private void StoreOriginalActorsFromScenes(List<SceneTreeModel> sceneTrees)
        {
            _originalActors.Clear();
            foreach (var scene in sceneTrees)
            {
                CollectActorsFromScene(scene, _originalActors);
            }
            LogService.Info($"Stored {_originalActors.Count} original actors from {sceneTrees.Count} scenes");
        }

        private void CollectActorsFromScene(SceneTreeModel scene, List<ActorModel> actors)
        {
            actors.AddRange(scene.Actors);
            foreach (var childScene in scene.ChildScenes)
            {
                CollectActorsFromScene(childScene, actors);
            }
        }

        protected override void SubscribeToConnectionEvents()
        {
            _sceneService.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        protected override void UnsubscribeFromConnectionEvents()
        {
            _sceneService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }

        public override void Dispose()
        {
            try
            {
                LogService?.Info("Disposing SceneExplorerViewModel...");

                try
                {
                    _sceneService.OnlineSceneTreeUpdated -= OnOnlineSceneTreeUpdated;
                    _sceneService.OfflineSceneTreesUpdated -= OnOfflineSceneTreesUpdated;
                    _sceneService.ObjectSelectedFromRuntime -= OnObjectSelectedFromRuntime;
                    _propertiesService.PropertiesUpdated -= OnPropertiesUpdated;
                }
                catch (Exception ex)
                {
                    LogService?.Warning($"Error unsubscribing from events: {ex.Message}");
                }

                lock (_filterLock)
                {
                    _filterThrottleTimer?.Dispose();
                    _filterThrottleTimer = null;
                    _pendingFilter = null;
                }

                foreach (var component in AvailableComponents)
                {
                    component.SelectionChanged -= OnComponentSelectionChanged;
                }

                try
                {
                    PropertiesEditor?.ViewModel?.Dispose();
                }
                catch (Exception ex)
                {
                    LogService?.Warning($"Error disposing PropertiesEditor: {ex.Message}");
                }

                LogService?.Info("SceneExplorerViewModel disposed successfully");
            }
            catch (Exception ex)
            {
                LogService?.Error("Error during SceneExplorerViewModel disposal", ex);
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}

