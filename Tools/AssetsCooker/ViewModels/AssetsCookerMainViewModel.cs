#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.MVVM;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DANCustomTools.Tools.AssetsCooker.ViewModels
{
    public class AssetsCookerMainViewModel : ViewModelBase
    {
        private readonly IToolManager _toolManager;
        private readonly IServiceProvider _serviceProvider;
        private string _statusMessage = "Ready";
        private bool _isCooking = false;

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsCooking
        {
            get => _isCooking;
            set
            {
                _isCooking = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotCooking));
            }
        }

        public bool IsNotCooking => !_isCooking;

        public ObservableCollection<string> CookingLog { get; } = new();

        public ICommand StartCookingCommand { get; }
        public ICommand StopCookingCommand { get; }
        public ICommand ClearLogCommand { get; }

        public AssetsCookerMainViewModel(IToolManager toolManager, IServiceProvider serviceProvider)
        {
            _toolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            StartCookingCommand = new AsyncRelayCommand(StartCookingAsync, () => IsNotCooking);
            StopCookingCommand = new RelayCommand(StopCooking, () => IsCooking);
            ClearLogCommand = new RelayCommand(ClearLog);

            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            StatusMessage = "Assets Cooker initialized and ready";
            AddLogMessage("Assets Cooker started");
        }

        private async System.Threading.Tasks.Task StartCookingAsync()
        {
            try
            {
                IsCooking = true;
                StatusMessage = "Cooking assets...";
                AddLogMessage("Starting asset cooking process");

                // Simulate asset cooking process
                await System.Threading.Tasks.Task.Delay(1000);
                AddLogMessage("Scanning for assets...");

                await System.Threading.Tasks.Task.Delay(1000);
                AddLogMessage("Processing textures...");

                await System.Threading.Tasks.Task.Delay(1000);
                AddLogMessage("Processing audio files...");

                await System.Threading.Tasks.Task.Delay(1000);
                AddLogMessage("Generating asset database...");

                await System.Threading.Tasks.Task.Delay(1000);
                AddLogMessage("Asset cooking completed successfully!");

                StatusMessage = "Cooking completed";
            }
            catch (Exception ex)
            {
                AddLogMessage($"Error during cooking: {ex.Message}");
                StatusMessage = "Cooking failed";
            }
            finally
            {
                IsCooking = false;
            }
        }

        private void StopCooking()
        {
            IsCooking = false;
            StatusMessage = "Cooking stopped";
            AddLogMessage("Asset cooking stopped by user");
        }

        private void ClearLog()
        {
            CookingLog.Clear();
            AddLogMessage("Log cleared");
        }

        private void AddLogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            CookingLog.Add($"[{timestamp}] {message}");
        }

        public override void Dispose()
        {
            // Stop any running operations
            if (IsCooking)
            {
                StopCooking();
            }

            base.Dispose();
        }
    }
}