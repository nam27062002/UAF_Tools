#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.MVVM;
using DANCustomTools.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
        public ICommand ReconnectCommand { get; }

        public MainViewModel(IToolManager toolManager)
        {
            _toolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
            SwitchToEditorCommand = new RelayCommand(() => SwitchToMainTool("Editor"), () => !IsEditorActive);
            SwitchToAssetsCookerCommand = new RelayCommand(() => SwitchToMainTool("AssetsCooker"), () => !IsAssetsCookerActive);
            LaunchTeaBoxCommand = new RelayCommand(LaunchTeaBox);
            ReconnectCommand = new AsyncRelayCommand(ReconnectToServer);
            _toolManager.CurrentMainToolChanged += OnCurrentMainToolChanged;

            LoadMainTools();

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

                bool isInitialization = CurrentToolViewModel == null;

                if (CurrentToolName == toolName && !isInitialization)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Tool {toolName} already active, ignoring");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Calling ToolManager.SwitchToMainTool({toolName})");
                _toolManager.SwitchToMainTool(toolName);
                CurrentToolName = toolName;

                System.Diagnostics.Debug.WriteLine($"[MainViewModel] CurrentToolName set to: {CurrentToolName}");
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] IsEditorActive: {IsEditorActive}, IsAssetsCookerActive: {IsAssetsCookerActive}");

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
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

                string teaBoxPath = Path.Combine(currentDirectory, "..", "Teabox", "TeaBox.exe");
                teaBoxPath = Path.GetFullPath(teaBoxPath);

                if (File.Exists(teaBoxPath))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = teaBoxPath,
                        UseShellExecute = true
                    };

                    Process.Start(startInfo);
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Launched TeaBox from: {teaBoxPath}");
                }
                else
                {
                    System.Windows.MessageBox.Show($"TeaBox executable not found at: {teaBoxPath}",
                                  "TeaBox Not Found",
                                  System.Windows.MessageBoxButton.OK,
                                  System.Windows.MessageBoxImage.Warning);
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] TeaBox not found at: {teaBoxPath}");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to launch TeaBox: {ex.Message}",
                              "Launch Error",
                              System.Windows.MessageBoxButton.OK,
                              System.Windows.MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Error launching TeaBox: {ex.Message}");
            }
        }

        public override void Dispose()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel disposing...");

                _toolManager.CurrentMainToolChanged -= OnCurrentMainToolChanged;

                if (_currentToolViewModel is IDisposable disposableViewModel)
                {
                    System.Diagnostics.Debug.WriteLine($"Disposing current tool: {_currentToolViewModel.GetType().Name}");
                    disposableViewModel.Dispose();
                }

                foreach (var tool in MainTools)
                {
                    if (tool is IDisposable disposableTool)
                    {
                        System.Diagnostics.Debug.WriteLine($"Disposing tool: {tool.GetType().Name}");
                        disposableTool.Dispose();
                    }
                }

                if (_toolManager is IDisposable disposableManager)
                {
                    System.Diagnostics.Debug.WriteLine("Disposing ToolManager");
                    disposableManager.Dispose();
                }

                System.Diagnostics.Debug.WriteLine("MainViewModel disposed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing MainViewModel: {ex.Message}");
            }
            finally
            {
                base.Dispose();
            }
        }

        private async Task ReconnectToServer()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting reconnection to server...");
                
                // Get services from DI container
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider == null)
                {
                    System.Diagnostics.Debug.WriteLine("ServiceProvider not available");
                    return;
                }

                // Disconnect from current server if connected
                var engineIntegrationService = serviceProvider.GetService(typeof(IEngineIntegrationService)) as IEngineIntegrationService;
                if (engineIntegrationService != null)
                {
                    System.Diagnostics.Debug.WriteLine("Disconnecting from current server...");
                    await engineIntegrationService.DisconnectAsync();
                }

                // Wait a moment before reconnecting
                await Task.Delay(1000);

                // Reconnect to server
                System.Diagnostics.Debug.WriteLine("Reconnecting to server...");
                if (engineIntegrationService != null)
                {
                    await engineIntegrationService.ConnectAsync();
                }

                // Refresh current tool to reload data
                var currentTool = _toolManager.CurrentMainTool;
                if (currentTool != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Refreshing current tool: {currentTool.Name}");
                    // Trigger refresh of current tool
                    _toolManager.SwitchToMainTool(currentTool.Name);
                }

                System.Diagnostics.Debug.WriteLine("Reconnection completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during reconnection: {ex.Message}");
                // You might want to show a message to the user here
            }
        }
    }
}
