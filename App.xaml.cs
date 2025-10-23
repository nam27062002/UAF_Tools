#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.Core.Services;
using DANCustomTools.Services;
using DANCustomTools.Tools.AssetsCooker.ViewModels;
using DANCustomTools.Tools.Editor.ViewModels;
using DANCustomTools.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;
namespace DANCustomTools
{
    public partial class App : System.Windows.Application
    {
        private IHost? _host;

        public static IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = CreateHostBuilder(e.Args).Build();
            ServiceProvider = _host.Services;

            var toolInitializer = ServiceProvider.GetRequiredService<IToolInitializer>();
            if (!toolInitializer.IsInitialized)
            {
                var logService = ServiceProvider.GetService<ILogService>();
                logService?.Warning("Tool initializer was not properly initialized during DI setup");
            }

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                });

        private static void ConfigureServices(IServiceCollection services)
        {
            // 1. Core Infrastructure Services
            ConfigureCoreServices(services);

            // 2. Tool Framework Services
            ConfigureToolFramework(services);

            // 3. ViewModels
            ConfigureViewModels(services);
        }

        private static void ConfigureCoreServices(IServiceCollection services)
        {
            services.AddSingleton<ILogService, ConsoleLogService>();
            services.AddSingleton<IEngineHostService, EngineHostService>();

            services.AddSingleton<IEngineIntegrationService, EngineIntegrationService>();

            services.AddSingleton<IComponentManagementService, ComponentManagementService>();

            services.AddSingleton<IComponentFilterService, ComponentFilterService>();

            services.AddSingleton<ISceneExplorerService, SceneExplorerService>();
            services.AddSingleton<IPropertiesEditorService, PropertiesEditorService>();
            services.AddSingleton<IActorCreateService, ActorCreateService>();
        }

        private static void ConfigureToolFramework(IServiceCollection services)
        {
            services.AddSingleton<IToolManager, ToolManager>();
            services.AddSingleton<IToolContext, ToolContext>();

            services.AddSingleton<IToolConfigurationService, ToolConfigurationService>();

            services.AddSingleton<IToolInitializer>(serviceProvider =>
            {
                var toolConfig = serviceProvider.GetRequiredService<IToolConfigurationService>();
                var initializer = new ToolInitializer(toolConfig);
                initializer.Initialize(serviceProvider);
                return initializer;
            });
        }

        private static void ConfigureViewModels(IServiceCollection services)
        {
            services.AddTransient<MainViewModel>();
            services.AddTransient<EditorMainViewModel>();
            services.AddTransient<AssetsCookerMainViewModel>();
            services.AddTransient<SceneExplorerViewModel>();
            services.AddTransient<PropertiesEditorViewModel>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("App OnExit - starting cleanup...");
                StopAllServices();

                if (_host != null)
                {
                    System.Diagnostics.Debug.WriteLine("Disposing DI host...");
                    _host.Dispose();
                    _host = null;
                    System.Diagnostics.Debug.WriteLine("DI host disposed");
                }

                ServiceProvider = null;

                System.Diagnostics.Debug.WriteLine("App cleanup completed");

                base.OnExit(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during app exit: {ex.Message}");
            }
            finally
            {
                // Force process termination after 3 seconds to allow proper cleanup
                var forceExitTimer = new System.Threading.Timer(_ =>
                {
                    System.Diagnostics.Debug.WriteLine("Force terminating process...");
                    System.Environment.Exit(0);
                }, null, 3000, System.Threading.Timeout.Infinite);
            }
        }

        private void StopAllServices()
        {
            try
            {
                if (ServiceProvider == null) return;

                System.Diagnostics.Debug.WriteLine("Stopping all services...");

                // Stop all services that implement IDisposable and have async operations
                var servicesToStop = new[]
                {
                    typeof(ISceneExplorerService),
                    typeof(IPropertiesEditorService),
                    typeof(IEngineIntegrationService),
                    typeof(IEngineHostService),
                    typeof(IToolManager)
                };

                foreach (var serviceType in servicesToStop)
                {
                    try
                    {
                        var service = ServiceProvider.GetService(serviceType);
                        if (service != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Stopping service: {serviceType.Name}");
                            
                            // Stop async services
                            if (service is ISceneExplorerService sceneService)
                            {
                                sceneService.StopAsync().GetAwaiter().GetResult();
                            }
                            else if (service is IPropertiesEditorService propService)
                            {
                                propService.StopAsync().GetAwaiter().GetResult();
                            }
                            else if (service is IEngineIntegrationService engineService)
                            {
                                engineService.DisconnectAsync().GetAwaiter().GetResult();
                            }
                            
                            // Dispose if implements IDisposable
                            if (service is IDisposable disposableService)
                            {
                                disposableService.Dispose();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error stopping service {serviceType.Name}: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("All services stopped");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping services: {ex.Message}");
            }
        }
    }
}

