#nullable enable
using DANCustomTools.Core.ViewModels;
using DANCustomTools.MVVM;
using DANCustomTools.Services;
using System;
using System.Collections.ObjectModel;
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
        private string? _selectedActor;

        // Collections
        public ObservableCollection<string> ActorList { get; private set; } = new();
        public ObservableCollection<string> ComponentList { get; private set; } = new();

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
            LoadActorCommand = new AsyncRelayCommand<string>(LoadActorAsync);
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

        public string? SelectedActor
        {
            get => _selectedActor;
            set => SetProperty(ref _selectedActor, value);
        }

        #endregion

        #region Commands Implementation

        private async Task CreateNewActorAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Creating new actor...";

                // TODO: Implement actor creation logic
                await Task.Delay(1000); // Placeholder

                StatusMessage = "New actor created successfully";
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

        private async Task LoadActorAsync(string? actorName)
        {
            if (string.IsNullOrEmpty(actorName)) return;

            try
            {
                IsLoading = true;
                StatusMessage = $"Loading actor: {actorName}...";

                // TODO: Implement actor loading logic
                await Task.Delay(500); // Placeholder

                StatusMessage = $"Actor '{actorName}' loaded successfully";
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
                IsLoading = true;
                StatusMessage = "Saving actor...";

                // TODO: Implement actor saving logic
                await Task.Delay(800); // Placeholder

                StatusMessage = "Actor saved successfully";
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