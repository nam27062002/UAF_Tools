#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.MVVM;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DANCustomTools.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IToolManager _toolManager;
        private ViewModelBase? _currentToolViewModel;

        public ObservableCollection<IMainTool> MainTools { get; } = new();

        public ViewModelBase? CurrentToolViewModel
        {
            get => _currentToolViewModel;
            set
            {
                _currentToolViewModel = value;
                OnPropertyChanged();
            }
        }

        public ICommand SwitchToEditorCommand { get; }
        public ICommand SwitchToAssetsCookerCommand { get; }

        public MainViewModel(IToolManager toolManager)
        {
            _toolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
            SwitchToEditorCommand = new RelayCommand(() => SwitchToMainTool("Editor"));
            SwitchToAssetsCookerCommand = new RelayCommand(() => SwitchToMainTool("AssetsCooker"));

            // Subscribe to tool manager events
            _toolManager.CurrentMainToolChanged += OnCurrentMainToolChanged;

            // Load available main tools
            LoadMainTools();

            // Initialize with Editor tool
            SwitchToMainTool("Editor");
        }

        private void LoadMainTools()
        {
            MainTools.Clear();
            foreach (var mainTool in _toolManager.MainTools)
            {
                MainTools.Add(mainTool);
            }
        }

        private void SwitchToMainTool(string toolName)
        {
            try
            {
                _toolManager.SwitchToMainTool(toolName);
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error switching to main tool: {ex.Message}");
            }
        }

        private void OnCurrentMainToolChanged(object? sender, IMainTool? mainTool)
        {
            CurrentToolViewModel = mainTool?.CreateMainViewModel();
        }

        public override void Dispose()
        {
            _toolManager.CurrentMainToolChanged -= OnCurrentMainToolChanged;
            base.Dispose();
        }
    }
}
