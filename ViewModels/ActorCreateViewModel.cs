#nullable enable
using DANCustomTools.Core.ViewModels;
using DANCustomTools.Models.ActorCreate;
using DANCustomTools.MVVM;
using DANCustomTools.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DANCustomTools.ViewModels
{
    public class ActorCreateViewModel : SubToolViewModelBase, IDisposable
    {
        // Dependencies
        private readonly IActorCreateService _actorCreateService;

        // Child ViewModels
        public ComponentListViewModel ComponentListViewModel { get; private set; }
        public XmlPropertyGridViewModel XmlPropertyGridViewModel { get; private set; }

        // UI Properties
        private string _statusMessage = "Ready";
        private bool _isLoading = false;
        private ActorInfo? _selectedActor;
        private string _newActorName = string.Empty;
        private string? _selectedAvailableComponent;
        private string? _selectedActorComponent;

        // Collections
        public ObservableCollection<ActorInfo> ActorList { get; private set; } = new();
        public ObservableCollection<string> ComponentList { get; private set; } = new();
        public ObservableCollection<string> SelectedActorComponents { get; private set; } = new();

        // Commands
        public ICommand CreateNewActorCommand { get; }
        public ICommand LoadActorCommand { get; }
        public ICommand SaveActorCommand { get; }
        public ICommand RefreshCommand { get; }

        public override string SubToolName => "ActorCreate";

        public ActorCreateViewModel(IActorCreateService actorCreateService, ILogService logService)
            : base(logService)
        {
            _actorCreateService = actorCreateService ?? throw new ArgumentNullException(nameof(actorCreateService));

            // Initialize child ViewModels
            ComponentListViewModel = new ComponentListViewModel();
            XmlPropertyGridViewModel = new XmlPropertyGridViewModel();

            // Initialize commands
            CreateNewActorCommand = new AsyncRelayCommand(CreateNewActorAsync);
            LoadActorCommand = new AsyncRelayCommand<ActorInfo>(LoadActorAsync);
            SaveActorCommand = new AsyncRelayCommand(SaveActorAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);

            // Subscribe to events
            SubscribeToConnectionEvents();
            SubscribeToComponentEvents();

            // Initialize data
            Task.Run(InitializeAsync);
        }

        #region Properties

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ActorInfo? SelectedActor
        {
            get => _selectedActor;
            set
            {
                if (SetProperty(ref _selectedActor, value))
                {
                    UpdateSelectedActorComponents();
                }
            }
        }

        public string NewActorName
        {
            get => _newActorName;
            set => SetProperty(ref _newActorName, value);
        }

        public string? SelectedAvailableComponent
        {
            get => _selectedAvailableComponent;
            set => SetProperty(ref _selectedAvailableComponent, value);
        }

        public string? SelectedActorComponent
        {
            get => _selectedActorComponent;
            set => SetProperty(ref _selectedActorComponent, value);
        }

        #endregion

        #region Commands Implementation

        private async Task CreateNewActorAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewActorName))
                {
                    StatusMessage = "Please enter a valid actor name";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Creating new actor: {NewActorName}...";

                var success = await _actorCreateService.CreateActorAsync(NewActorName);

                if (success)
                {
                    StatusMessage = $"Actor '{NewActorName}' created successfully";
                    NewActorName = string.Empty;
                    await RefreshAsync();
                }
                else
                {
                    StatusMessage = $"Failed to create actor '{NewActorName}'";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating actor: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadActorAsync(ActorInfo? actor)
        {
            if (actor == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = $"Loading actor: {actor.Name}...";

                var success = await _actorCreateService.LoadActorAsync(actor.Name);

                if (success)
                {
                    SelectedActor = actor;
                    StatusMessage = $"Actor '{actor.Name}' loaded successfully";
                }
                else
                {
                    StatusMessage = $"Failed to load actor '{actor.Name}'";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading actor: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveActorAsync()
        {
            try
            {
                if (SelectedActor == null)
                {
                    StatusMessage = "No actor selected to save";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Saving actor: {SelectedActor.Name}...";

                var success = await _actorCreateService.SaveActorAsync();

                if (success)
                {
                    StatusMessage = $"Actor '{SelectedActor.Name}' saved successfully";
                }
                else
                {
                    StatusMessage = $"Failed to save actor '{SelectedActor.Name}'";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving actor: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Refreshing actor list...";

                await _actorCreateService.RefreshActorsAsync();

                // Update collections
                ActorList.Clear();
                foreach (var actor in _actorCreateService.GetActors())
                {
                    ActorList.Add(actor);
                }

                ComponentList.Clear();
                var components = _actorCreateService.GetComponents().ToList();
                foreach (var component in components)
                {
                    ComponentList.Add(component);
                }

                // Update ComponentListViewModel
                ComponentListViewModel.LoadAvailableComponents(components);

                IsConnected = _actorCreateService.IsConnected;
                StatusMessage = $"Refreshed - Found {ActorList.Count} actors";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing: {ex.Message}";
                IsConnected = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Initialization

        private async Task InitializeAsync()
        {
            try
            {
                StatusMessage = "Initializing Actor Creator...";
                
                // Try to connect to engine first
                await _actorCreateService.ConnectAsync();
                
                await RefreshAsync();
                StatusMessage = "Actor Creator ready";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Initialization error: {ex.Message}";
            }
        }

        private void UpdateSelectedActorComponents()
        {
            SelectedActorComponents.Clear();

            if (SelectedActor != null)
            {
                foreach (var component in SelectedActor.ComponentList)
                {
                    SelectedActorComponents.Add(component);
                }

                // Update child ViewModels
                ComponentListViewModel.LoadUsedComponents(SelectedActor.ComponentList);
                // TODO: Load XML data for XmlPropertyGridViewModel
            }
        }

        private void SubscribeToComponentEvents()
        {
            ComponentListViewModel.ComponentAdded += OnComponentAdded;
            ComponentListViewModel.ComponentRemoved += OnComponentRemoved;
            ComponentListViewModel.ComponentMoved += OnComponentMoved;
        }

        private void UnsubscribeFromComponentEvents()
        {
            ComponentListViewModel.ComponentAdded -= OnComponentAdded;
            ComponentListViewModel.ComponentRemoved -= OnComponentRemoved;
            ComponentListViewModel.ComponentMoved -= OnComponentMoved;
        }

        private void OnComponentAdded(object? sender, ComponentItem component)
        {
            if (SelectedActor != null && !SelectedActor.ComponentList.Contains(component.Name))
            {
                SelectedActor.ComponentList.Add(component.Name);
                UpdateSelectedActorComponents();
            }
        }

        private void OnComponentRemoved(object? sender, ComponentItem component)
        {
            if (SelectedActor != null)
            {
                SelectedActor.ComponentList.Remove(component.Name);
                UpdateSelectedActorComponents();
            }
        }

        private void OnComponentMoved(object? sender, ComponentMoveEventArgs e)
        {
            if (SelectedActor != null)
            {
                var componentName = e.Component.Name;
                if (SelectedActor.ComponentList.Remove(componentName))
                {
                    var newIndex = Math.Min(e.NewIndex, SelectedActor.ComponentList.Count);
                    SelectedActor.ComponentList.Insert(newIndex, componentName);
                }
            }
        }

        #endregion

        #region Abstract Methods Implementation

        protected override void SubscribeToConnectionEvents()
        {
            // Subscribe to any connection events from ActorCreateService
            // For now, we'll update connection status during refresh
        }

        protected override void UnsubscribeFromConnectionEvents()
        {
            // Unsubscribe from connection events
            // No specific events to unsubscribe from in current implementation
            UnsubscribeFromComponentEvents();
        }

        #endregion

        #region IDisposable

        public override void Dispose()
        {
            // Cleanup resources
            UnsubscribeFromComponentEvents();
            ComponentListViewModel?.Dispose();
            XmlPropertyGridViewModel?.Dispose();
            _actorCreateService?.Dispose();
            base.Dispose();
        }

        #endregion

        #region Component Management Commands

        private async Task AddComponentAsync()
        {
            if (SelectedActor == null || string.IsNullOrEmpty(SelectedAvailableComponent))
            {
                StatusMessage = "Please select an actor and a component to add";
                return;
            }

            try
            {
                StatusMessage = $"Adding {SelectedAvailableComponent} to {SelectedActor.Name}...";
                
                // Use real component management service
                bool success = await _actorCreateService.AddComponentToActorAsync(SelectedActor.Name, SelectedAvailableComponent);
                
                if (success)
                {
                    // Add to local collection for immediate UI update
                    if (!SelectedActor.ComponentList.Contains(SelectedAvailableComponent))
                    {
                        SelectedActor.ComponentList.Add(SelectedAvailableComponent);
                        UpdateSelectedActorComponents();
                    }
                    
                    StatusMessage = $"Component {SelectedAvailableComponent} added successfully";
                }
                else
                {
                    StatusMessage = $"Failed to add component {SelectedAvailableComponent}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding component: {ex.Message}";
            }
        }

        private async Task RemoveComponentAsync()
        {
            if (SelectedActor == null || string.IsNullOrEmpty(SelectedActorComponent))
            {
                StatusMessage = "Please select an actor and a component to remove";
                return;
            }

            try
            {
                StatusMessage = $"Removing {SelectedActorComponent} from {SelectedActor.Name}...";
                
                // Use real component management service
                bool success = await _actorCreateService.RemoveComponentFromActorAsync(SelectedActor.Name, SelectedActorComponent);
                
                if (success)
                {
                    // Remove from local collection for immediate UI update
                    SelectedActor.ComponentList.Remove(SelectedActorComponent);
                    UpdateSelectedActorComponents();
                    
                    StatusMessage = $"Component {SelectedActorComponent} removed successfully";
                }
                else
                {
                    StatusMessage = $"Failed to remove component {SelectedActorComponent}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error removing component: {ex.Message}";
            }
        }

        private async Task CopyComponentAsync()
        {
            if (SelectedActor == null || string.IsNullOrEmpty(SelectedActorComponent))
            {
                StatusMessage = "Please select an actor and a component to copy";
                return;
            }

            try
            {
                StatusMessage = $"Copying {SelectedActorComponent} from {SelectedActor.Name}...";
                
                // Get component data first
                string? componentData = await _actorCreateService.GetComponentDataAsync(SelectedActor.Name, SelectedActorComponent);
                if (string.IsNullOrEmpty(componentData))
                {
                    StatusMessage = $"No data found for component {SelectedActorComponent}";
                    return;
                }
                
                // Use real component management service
                bool success = await _actorCreateService.CopyComponentAsync(SelectedActor.Name, SelectedActorComponent, componentData);
                
                if (success)
                {
                    StatusMessage = $"Component {SelectedActorComponent} copied to clipboard";
                }
                else
                {
                    StatusMessage = $"Failed to copy component {SelectedActorComponent}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error copying component: {ex.Message}";
            }
        }

        private async Task CutComponentAsync()
        {
            if (SelectedActor == null || string.IsNullOrEmpty(SelectedActorComponent))
            {
                StatusMessage = "Please select an actor and a component to cut";
                return;
            }

            try
            {
                StatusMessage = $"Cutting {SelectedActorComponent} from {SelectedActor.Name}...";
                
                // Get component data first
                string? componentData = await _actorCreateService.GetComponentDataAsync(SelectedActor.Name, SelectedActorComponent);
                if (string.IsNullOrEmpty(componentData))
                {
                    StatusMessage = $"No data found for component {SelectedActorComponent}";
                    return;
                }
                
                // Use real component management service
                bool success = await _actorCreateService.CutComponentAsync(SelectedActor.Name, SelectedActorComponent, componentData);
                
                if (success)
                {
                    // Remove from local collection for immediate UI update
                    SelectedActor.ComponentList.Remove(SelectedActorComponent);
                    UpdateSelectedActorComponents();
                    
                    StatusMessage = $"Component {SelectedActorComponent} cut to clipboard";
                }
                else
                {
                    StatusMessage = $"Failed to cut component {SelectedActorComponent}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error cutting component: {ex.Message}";
            }
        }

        private async Task PasteComponentAsync()
        {
            if (SelectedActor == null)
            {
                StatusMessage = "Please select an actor to paste component to";
                return;
            }

            try
            {
                StatusMessage = $"Pasting component to {SelectedActor.Name}...";
                
                // Use real component management service
                bool success = await _actorCreateService.PasteComponentAsync(SelectedActor.Name);
                
                if (success)
                {
                    // Refresh actor component list to get the new component
                    await RefreshAsync();
                    StatusMessage = $"Component pasted to {SelectedActor.Name} successfully";
                }
                else
                {
                    StatusMessage = $"Failed to paste component to {SelectedActor.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error pasting component: {ex.Message}";
            }
        }

        #endregion
    }
}