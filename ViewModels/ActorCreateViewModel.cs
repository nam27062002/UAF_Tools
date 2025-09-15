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

        // UI Properties
        private string _statusMessage = "Ready";
        private bool _isLoading = false;
        private ActorInfo? _selectedActor;
        private string _newActorName = string.Empty;

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

            // Initialize commands
            CreateNewActorCommand = new AsyncRelayCommand(CreateNewActorAsync);
            LoadActorCommand = new AsyncRelayCommand<ActorInfo>(LoadActorAsync);
            SaveActorCommand = new AsyncRelayCommand(SaveActorAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);

            // Subscribe to connection events
            SubscribeToConnectionEvents();

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
                foreach (var component in _actorCreateService.GetComponents())
                {
                    ComponentList.Add(component);
                }

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
        }

        #endregion

        #region IDisposable

        public override void Dispose()
        {
            // Cleanup resources
            _actorCreateService?.Dispose();
            base.Dispose();
        }

        #endregion
    }
}