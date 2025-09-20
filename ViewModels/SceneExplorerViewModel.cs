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
    public class SceneExplorerViewModel : SubToolViewModelBase, IDisposable
    {
        // Dependencies
        private readonly ISceneExplorerService _sceneService;
        private readonly IPropertiesEditorService _propertiesService;
        private readonly IComponentFilterService _componentFilterService;

        // Arguments
        private readonly string[] _arguments;

        // UI Properties
        private ObservableCollection<SceneTreeItemViewModel> _sceneTreeItems = new();
        private PropertiesEditorView _propertiesEditor = null!;
        private ObjectWithRefModel? _selectedObject;
        
        // Component filtering properties
        private ObservableCollection<ComponentFilterModel> _availableComponents = new();
        private HashSet<string> _selectedComponents = new(StringComparer.OrdinalIgnoreCase);
        private List<ActorModel> _originalActors = new();
        private bool _isComponentFilterEnabled;

        public override string SubToolName => "Scene Explorer";

        // Context menu commands
        public ICommand DuplicateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RenameCommand { get; }

        // Toolbar commands
        public ICommand RefreshCommand { get; }
        public ICommand SelectInEngineCommand { get; }
        
        // Component filter commands
        public ICommand ClearFiltersCommand { get; }
        public ICommand UnselectAllFiltersCommand { get; }

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
        
        // Component filtering properties
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

            // Initialize component filtering
            AvailableComponents = new ObservableCollection<ComponentFilterModel>();
            SelectedComponents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Initialize context menu commands
            DuplicateCommand = new AsyncRelayCommand(ExecuteDuplicateAsync, CanExecuteDuplicate);
            DeleteCommand = new AsyncRelayCommand(ExecuteDeleteAsync, CanExecuteDelete);
            RenameCommand = new RelayCommand<string>(ExecuteRename, CanExecuteRename);

            // Initialize toolbar commands
            RefreshCommand = new AsyncRelayCommand(async () => await RefreshSceneTreeAsync(null));
            SelectInEngineCommand = new AsyncRelayCommand(async () => await SelectInEngineAsync(null), () => SelectedObject != null);

            // Initialize filter commands
            ClearFiltersCommand = new RelayCommand(ClearAllFilters);

            // Subscribe to service events
            _sceneService.OnlineSceneTreeUpdated += OnOnlineSceneTreeUpdated;
            _sceneService.OfflineSceneTreesUpdated += OnOfflineSceneTreesUpdated;
            _sceneService.ObjectSelectedFromRuntime += OnObjectSelectedFromRuntime;

            // Subscribe to PropertiesEditor events to get selection notifications
            _propertiesService.PropertiesUpdated += OnPropertiesUpdated;

            // Subscribe to connection events
            SubscribeToConnectionEvents();

            // Initialize PropertiesEditor
            InitializePropertiesEditor();
            UpdateConnectionStatus();
            // Start services
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
                // Start both services
                await Task.WhenAll(
                    _sceneService.StartAsync(arguments),
                    _propertiesService.StartAsync(arguments)
                );
                LogService.Info("All services started successfully");

                // If services were already running, force a connection attempt to ensure they connect
                if (!_sceneService.IsConnected || !_propertiesService.IsConnected)
                {
                    LogService.Info("Services not connected, forcing connection attempts");
                    _sceneService.ForceConnectionAttempt();
                    _propertiesService.ForceConnectionAttempt();

                    // Give a moment for connection to establish, then request scene tree
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
                    // Services are already connected, request scene tree immediately
                    LogService.Info("Services already connected, requesting scene tree");
                    _sceneService.RequestSceneTree();

                    // Also request current selection to sync with Engine
                    _ = Task.Delay(500).ContinueWith(_ =>
                    {
                        try
                        {
                            LogService.Info("Requesting current selection from Engine");
                            _sceneService.RequestCurrentSelection();
                        }
                        catch (Exception ex)
                        {
                            LogService.Error("Failed to request current selection", ex);
                        }
                    });
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
                // Store original actors for filtering
                StoreOriginalActors(sceneTree);
                
                // Extract and populate component filters
                ExtractAndPopulateComponents(_originalActors);

                var treeItem = CreateSceneTreeItem(sceneTree);
                var items = new ObservableCollection<SceneTreeItemViewModel>();
                items.Add(treeItem);
                SceneTreeItems = items;
                LogService.Info($"Updated scene tree: {sceneTree.UniqueName}");
                LogService.Info($"SceneTreeItems.Count={SceneTreeItems.Count}");

                // Request current selection after scene tree is loaded
                _ = Task.Delay(300).ContinueWith(_ =>
                {
                    try
                    {
                        if (_sceneService.IsConnected)
                        {
                            LogService.Info("Requesting current selection after scene tree update");
                            _sceneService.RequestCurrentSelection();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Error("Failed to request current selection after scene tree update", ex);
                    }
                });
            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        private void OnOfflineSceneTreesUpdated(object? sender, List<SceneTreeModel> sceneTrees)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                // Store original actors from all scenes for filtering
                StoreOriginalActorsFromScenes(sceneTrees);
                
                // Extract and populate component filters
                ExtractAndPopulateComponents(_originalActors);

                SceneTreeItems.Clear();
                foreach (var sceneTree in sceneTrees)
                {
                    var treeItem = CreateSceneTreeItem(sceneTree);
                    SceneTreeItems.Add(treeItem);
                }
                LogService.Info($"Updated offline scene trees: {sceneTrees.Count} scenes");
            });
        }

        private void OnObjectSelectedFromRuntime(object? sender, uint objectRef)
        {
            LogService.Info($"✨ RUNTIME SELECTION EVENT: Object selected from runtime: {objectRef}");

            App.Current?.Dispatcher.Invoke(() =>
            {
                try
                {
                    LogService.Info($"🔍 Searching for object {objectRef} in scene tree with {SceneTreeItems.Count} root items");

                    // Find and select the object in the tree
                    var selectedItem = FindTreeItemByObjectRef(SceneTreeItems, objectRef);
                    if (selectedItem != null)
                    {
                        LogService.Info($"✅ Found object {objectRef} in scene tree!");

                        // Clear previous selection
                        ClearTreeSelection(SceneTreeItems);
                        LogService.Info($"🧹 Cleared previous tree selection");

                        // Set new selection
                        selectedItem.IsSelected = true;
                        selectedItem.IsExpanded = true;

                        // Expand parent hierarchy to make sure the item is visible
                        ExpandParentHierarchy(selectedItem);
                        LogService.Info($"📂 Expanded parent hierarchy for visibility");

                        // Update the SelectedObject property
                        if (selectedItem.Model is ObjectWithRefModel objectModel)
                        {
                            SelectedObject = objectModel;
                            LogService.Info($"🎯 Tree selection synced with runtime: {objectModel.FriendlyName} (ref: {objectRef})");
                        }
                    }
                    else
                    {
                        LogService.Warning($"❌ Could not find object with ref {objectRef} in scene tree (total items: {CountAllTreeItems()})");
                        LogAllTreeItemRefs();
                    }
                }
                catch (Exception ex)
                {
                    LogService.Error($"💥 Failed to sync tree selection with runtime", ex);
                }
            });
        }

        private void OnPropertiesUpdated(object? sender, PropertyModel propertyModel)
        {
            // Extract ObjectRef from property model and sync with scene tree
            if (propertyModel != null && propertyModel.ObjectRef != uint.MaxValue && propertyModel.ObjectRef != 0)
            {
                LogService.Info($"🔗 PropertiesEditor updated for object ref: {propertyModel.ObjectRef} - syncing with scene tree");

                // Reuse the same logic as runtime selection
                App.Current?.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var selectedItem = FindTreeItemByObjectRef(SceneTreeItems, propertyModel.ObjectRef);
                        if (selectedItem != null)
                        {
                            LogService.Info($"✅ Found and selecting object {propertyModel.ObjectRef} from properties update");

                            // Clear previous selection
                            ClearTreeSelection(SceneTreeItems);

                            // Set new selection
                            selectedItem.IsSelected = true;
                            selectedItem.IsExpanded = true;

                            // Expand parent hierarchy
                            ExpandParentHierarchy(selectedItem);

                            // Update the SelectedObject property
                            if (selectedItem.Model is ObjectWithRefModel objectModel)
                            {
                                SelectedObject = objectModel;
                                LogService.Info($"🎯 Tree selection synced from PropertiesEditor: {objectModel.FriendlyName} (ref: {propertyModel.ObjectRef})");
                            }
                        }
                        else
                        {
                            LogService.Warning($"❌ Could not find object {propertyModel.ObjectRef} from PropertiesEditor update in scene tree");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Error($"Failed to sync selection from PropertiesEditor update", ex);
                    }
                });
            }
        }

        // Tree Selection Helper Methods

        private SceneTreeItemViewModel? FindTreeItemByObjectRef(ObservableCollection<SceneTreeItemViewModel> items, uint objectRef)
        {
            foreach (var item in items)
            {
                // Check if this item matches
                if (item.Model is ObjectWithRefModel objectModel && objectModel.ObjectRef == objectRef)
                {
                    return item;
                }

                // Recursively search in children
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

            // Add child scenes first (recursive)
            foreach (var child in sceneTree.ChildScenes)
            {
                var childItem = CreateSceneTreeItem(child);
                item.Children.Add(childItem);
            }

            // Add actors group if there are actors
            if (sceneTree.Actors.Count > 0)
            {
                var actorsGroup = new SceneTreeItemViewModel
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

            // Add frises group if there are frises
            if (sceneTree.Frises.Count > 0)
            {
                var frisesGroup = new SceneTreeItemViewModel
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

            return item;
        }

        // Tree Selection Handler
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
                        // Auto-focus scene in engine
                        _sceneService.SelectScene(scene.UniqueName);
                        // Clear properties when scene is selected
                        PropertiesEditor.ViewModel?.ClearProperties();
                        SelectedObject = null;
                    }
                    break;

                case SceneTreeItemType.Actor:
                    if (selectedItem.Model is ActorModel actor)
                    {
                        SelectedObject = actor;
                        LogService.Info($"Selected actor: {actor.FriendlyName}");

                        // Load properties for the selected actor
                        PropertiesEditor.ViewModel?.LoadObjectProperties(actor);

                        // Auto-focus actor in engine
                        if (actor.IsOnline)
                        {
                            _sceneService.SelectObjects(new[] { actor });
                        }
                    }
                    break;

                case SceneTreeItemType.Frise:
                    if (selectedItem.Model is FriseModel frise)
                    {
                        SelectedObject = frise;
                        LogService.Info($"Selected frise: {frise.FriendlyName}");

                        // Load properties for the selected frise
                        PropertiesEditor.ViewModel?.LoadObjectProperties(frise);

                        // Auto-focus frise in engine
                        if (frise.IsOnline)
                        {
                            _sceneService.SelectObjects(new[] { frise });
                        }
                    }
                    break;

                case SceneTreeItemType.ActorSet:
                case SceneTreeItemType.FriseSet:
                    // Group selected - clear properties
                    PropertiesEditor.ViewModel?.ClearProperties();
                    SelectedObject = null;
                    LogService.Info($"Selected {selectedItem.ItemType} group");
                    break;
            }
        }

        // Command Implementations
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

                    // Clear properties after deletion
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

        // Context Menu Command Implementations
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
                // Duplicate at current position (0, 0, 0 offset)
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

            // Show confirmation dialog
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

                    // Execute deletion
                    _sceneService.DeleteObject(objectToDelete.ObjectRef);

                    // Clear selection and properties
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

                // Execute rename through service
                _sceneService.RenameObject(objectToRename.ObjectRef, trimmedNewName);

                // Find the corresponding item in the tree and update its name
                var treeItem = FindTreeItemByObjectRef(SceneTreeItems, objectToRename.ObjectRef);
                if (treeItem != null)
                {
                    // This assumes DisplayName property setter will notify the UI
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

        // Component Filtering Methods

        private void ExtractAndPopulateComponents(List<ActorModel> actors)
        {
            try
            {
                var allComponents = _componentFilterService.ExtractAllComponents(actors);
                var componentModels = new List<ComponentFilterModel>();

                foreach (var component in allComponents.OrderBy(c => c))
                {
                    var actorCount = actors.Count(a => _componentFilterService.ActorHasAnyComponent(a, new HashSet<string> { component }));
                    var model = new ComponentFilterModel(component, actorCount);
                    
                    // Default to selected (all components shown by default)
                    model.IsSelected = true;
                    
                    // Subscribe to selection changes
                    model.SelectionChanged += OnComponentSelectionChanged;
                    
                    componentModels.Add(model);
                }

                // Unsubscribe from old models
                foreach (var oldModel in AvailableComponents)
                {
                    oldModel.SelectionChanged -= OnComponentSelectionChanged;
                }

                // Initialize SelectedComponents with all components (default state)
                SelectedComponents.Clear();
                foreach (var component in allComponents)
                {
                    SelectedComponents.Add(component);
                }

                AvailableComponents.Clear();
                foreach (var model in componentModels)
                {
                    AvailableComponents.Add(model);
                }

                // Default to no filtering (all components selected = show all actors)
                IsComponentFilterEnabled = false;

                LogService.Info($"Populated {AvailableComponents.Count} unique components (all selected by default)");
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
                    // Component is re-selected (actors with this component will be shown again)
                    if (!SelectedComponents.Contains(componentModel.ComponentName))
                    {
                        SelectedComponents.Add(componentModel.ComponentName);
                        LogService.Info($"Re-enabled component: {componentModel.ComponentName}");
                    }
                }
                else
                {
                    // Component is deselected (hide actors with this component)
                    if (SelectedComponents.Contains(componentModel.ComponentName))
                    {
                        SelectedComponents.Remove(componentModel.ComponentName);
                        LogService.Info($"Disabled component filter: {componentModel.ComponentName}");
                    }
                }

                // Notify UI about SelectedComponents change
                OnPropertyChanged(nameof(SelectedComponents));
                
                ApplyComponentFilters();
                
                // Enable filtering when not all components are selected
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
                // Temporarily unsubscribe to avoid triggering events during bulk operation
                foreach (var component in AvailableComponents)
                {
                    component.SelectionChanged -= OnComponentSelectionChanged;
                    // Select all components (default state = show all actors)
                    component.IsSelected = true;
                    component.SelectionChanged += OnComponentSelectionChanged;
                }

                // Reset SelectedComponents to contain all components
                SelectedComponents.Clear();
                foreach (var component in AvailableComponents)
                {
                    SelectedComponents.Add(component.ComponentName);
                }
                
                // Notify UI about SelectedComponents change
                OnPropertyChanged(nameof(SelectedComponents));
                
                // All components selected = no filtering
                IsComponentFilterEnabled = false;

                // Rebuild scene tree to show all actors
                ApplyComponentFilters();

                LogService.Info("Reset all component filters - all components enabled, showing all actors");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to clear component filters", ex);
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
                    // All components are selected, show all actors
                    RebuildSceneTreeWithActors(_originalActors);
                    LogService.Info($"All components selected, showing all {_originalActors.Count} actors");
                    return;
                }

                if (selectedComponents == 0)
                {
                    // No components selected, hide all actors
                    RebuildSceneTreeWithActors(new List<ActorModel>());
                    LogService.Info("No components selected, hiding all actors");
                    return;
                }

                // Filter actors: Show only actors that DON'T have any of the DESELECTED components
                var deselectedComponents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var component in AvailableComponents)
                {
                    if (!component.IsSelected)
                    {
                        deselectedComponents.Add(component.ComponentName);
                    }
                }

                var filteredActors = new List<ActorModel>();
                foreach (var actor in _originalActors)
                {
                    // Show actor only if it doesn't have any deselected components
                    bool hasDeselectedComponent = _componentFilterService.ActorHasAnyComponent(actor, deselectedComponents);
                    if (!hasDeselectedComponent)
                    {
                        filteredActors.Add(actor);
                    }
                }

                RebuildSceneTreeWithActors(filteredActors);

                LogService.Info($"Applied component filters: {selectedComponents}/{totalComponents} components enabled");
                LogService.Info($"Deselected components: [{string.Join(", ", deselectedComponents)}]");
                LogService.Info($"Filtered actors: {filteredActors.Count}/{_originalActors.Count} actors shown");
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
                // Ensure we're on the UI thread
                App.Current?.Dispatcher.Invoke(() =>
                {
                    // Find the scene tree items and update their actor groups
                    foreach (var sceneItem in SceneTreeItems)
                    {
                        UpdateSceneActorsGroup(sceneItem, actorsToShow);
                    }
                    
                    // Force UI refresh
                    OnPropertyChanged(nameof(SceneTreeItems));
                });
                
                LogService.Info($"Rebuilt scene tree with {actorsToShow.Count} actors");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to rebuild scene tree with filtered actors", ex);
            }
        }

        private void UpdateSceneActorsGroup(SceneTreeItemViewModel sceneItem, List<ActorModel> actorsToShow)
        {
            if (sceneItem.Model is not SceneTreeModel sceneModel) return;

            // Find the actors group in this scene
            var actorsGroup = sceneItem.Children.FirstOrDefault(c => c.ItemType == SceneTreeItemType.ActorSet);
            if (actorsGroup != null)
            {
                // Get actors that belong to this scene
                var sceneActors = actorsToShow.Where(a => sceneModel.Actors.Contains(a)).ToList();

                LogService.Info($"Scene '{sceneModel.UniqueName}': Original actors: {sceneModel.Actors.Count}, Filtered actors: {sceneActors.Count}");

                // Update the actors group
                actorsGroup.Children.Clear();
                actorsGroup.DisplayName = $"Actors ({sceneActors.Count})";

                foreach (var actor in sceneActors)
                {
                    actorsGroup.Children.Add(new SceneTreeItemViewModel
                    {
                        DisplayName = actor.FriendlyName,
                        Model = actor,
                        ItemType = SceneTreeItemType.Actor
                    });
                }
                
                LogService.Info($"Updated actors group for scene '{sceneModel.UniqueName}' with {sceneActors.Count} actors");
            }

            // Recursively update child scenes
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
            // Unsubscribe from component events
            foreach (var component in AvailableComponents)
            {
                component.SelectionChanged -= OnComponentSelectionChanged;
            }

            _sceneService.OnlineSceneTreeUpdated -= OnOnlineSceneTreeUpdated;
            _sceneService.OfflineSceneTreesUpdated -= OnOfflineSceneTreesUpdated;
            _sceneService.ObjectSelectedFromRuntime -= OnObjectSelectedFromRuntime;
            _propertiesService.PropertiesUpdated -= OnPropertiesUpdated;
            PropertiesEditor?.ViewModel?.Dispose();
            base.Dispose();
        }
    }
}
