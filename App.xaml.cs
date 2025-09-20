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

            // Ensure tools are initialized
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
            // Core Services - Register in dependency order
            services.AddSingleton<ILogService, ConsoleLogService>();
            services.AddSingleton<IEngineHostService, EngineHostService>();

            // Engine Integration Service (new)
            services.AddSingleton<IEngineIntegrationService, EngineIntegrationService>();

            // Component Management Service (new)
            services.AddSingleton<IComponentManagementService, ComponentManagementService>();

            // Component Filter Service
            services.AddSingleton<IComponentFilterService, ComponentFilterService>();

            // Tool Services
            services.AddSingleton<ISceneExplorerService, SceneExplorerService>();
            services.AddSingleton<IPropertiesEditorService, PropertiesEditorService>();
            services.AddSingleton<IActorCreateService, ActorCreateService>();
        }

        private static void ConfigureToolFramework(IServiceCollection services)
        {
            // Tool Framework Core
            services.AddSingleton<IToolManager, ToolManager>();
            services.AddSingleton<IToolContext, ToolContext>();

            // Tool Registration and Configuration
            services.AddSingleton<IToolConfigurationService, ToolConfigurationService>();

            // Post-configuration step - this will run after container is built
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
            // ViewModels - Register as Transient for fresh instances
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
                // Dispose of the host which will dispose all registered services
                _host?.Dispose();

                // Clear service provider reference
                ServiceProvider = null;

                base.OnExit(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during app exit: {ex.Message}");
            }
            finally
            {
                // Force process termination if still running after 2 seconds
                System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(2000);
                    if (!System.Environment.HasShutdownStarted)
                    {
                        System.Diagnostics.Debug.WriteLine("Forcing process termination");
                        System.Environment.Exit(0);
                    }
                });
            }
        }
    }
}

