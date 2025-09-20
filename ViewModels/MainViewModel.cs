#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.MVVM;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace DANCustomTools.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IToolManager _toolManager;
        private ViewModelBase? _currentToolViewModel;
        private string _currentToolName = "Editor";

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

        public string CurrentToolName
        {
            get => _currentToolName;
            set
            {
                if (_currentToolName != value)
                {
                    _currentToolName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsEditorActive));
                    OnPropertyChanged(nameof(IsAssetsCookerActive));
                }
            }
        }

        public bool IsEditorActive => CurrentToolName == "Editor";
        public bool IsAssetsCookerActive => CurrentToolName == "AssetsCooker";

        public ICommand SwitchToEditorCommand { get; }
        public ICommand SwitchToAssetsCookerCommand { get; }
        public ICommand LaunchTeaBoxCommand { get; }

        public MainViewModel(IToolManager toolManager)
        {
            _toolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
            SwitchToEditorCommand = new RelayCommand(() => SwitchToMainTool("Editor"), () => !IsEditorActive);
            SwitchToAssetsCookerCommand = new RelayCommand(() => SwitchToMainTool("AssetsCooker"), () => !IsAssetsCookerActive);
            LaunchTeaBoxCommand = new RelayCommand(LaunchTeaBox);

            // Subscribe to tool manager events
            _toolManager.CurrentMainToolChanged += OnCurrentMainToolChanged;

            // Load available main tools
            LoadMainTools();

            // Initialize with Editor tool - defer to ensure UI is ready
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new Action(() => SwitchToMainTool("Editor"))
            );
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
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Switching to tool: {toolName}");

                // For initialization, allow switching even to the same tool
                bool isInitialization = CurrentToolViewModel == null;

                if (CurrentToolName == toolName && !isInitialization)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Tool {toolName} already active, ignoring");
                    return; // Ignore if already active (except during initialization)
                }

                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Calling ToolManager.SwitchToMainTool({toolName})");
                _toolManager.SwitchToMainTool(toolName);
                CurrentToolName = toolName;

                System.Diagnostics.Debug.WriteLine($"[MainViewModel] CurrentToolName set to: {CurrentToolName}");
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] IsEditorActive: {IsEditorActive}, IsAssetsCookerActive: {IsAssetsCookerActive}");

                // Refresh command states
                (SwitchToEditorCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (SwitchToAssetsCookerCommand as RelayCommand)?.RaiseCanExecuteChanged();

                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Command states refreshed");
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Error switching to main tool: {ex.Message}");
            }
        }

        private void OnCurrentMainToolChanged(object? sender, IMainTool? mainTool)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] OnCurrentMainToolChanged - Tool: {mainTool?.Name ?? "null"}");
            CurrentToolViewModel = mainTool?.CreateMainViewModel();
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] CurrentToolViewModel created: {CurrentToolViewModel?.GetType().Name ?? "null"}");
        }

        private void LaunchTeaBox()
        {
            try
            {
                // Get the current application directory
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // Navigate to the TeaBox directory (../Teabox/TeaBox.exe)
                string teaBoxPath = Path.Combine(currentDirectory, "..", "Teabox", "TeaBox.exe");
                teaBoxPath = Path.GetFullPath(teaBoxPath); // Resolve relative path

                if (File.Exists(teaBoxPath))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = teaBoxPath,
                        UseShellExecute = true // Use shell execute to launch the executable properly
                    };

                    Process.Start(startInfo);
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Launched TeaBox from: {teaBoxPath}");
                }
                else
                {
                    MessageBox.Show($"TeaBox executable not found at: {teaBoxPath}",
                                  "TeaBox Not Found",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] TeaBox not found at: {teaBoxPath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch TeaBox: {ex.Message}",
                              "Launch Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Error launching TeaBox: {ex.Message}");
            }
        }

        public override void Dispose()
        {
            _toolManager.CurrentMainToolChanged -= OnCurrentMainToolChanged;
            base.Dispose();
        }
    }
}
