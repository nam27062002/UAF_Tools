using DANCustomTools.Core.Abstractions;
using DANCustomTools.Core.Services;
using DANCustomTools.Services;
using DANCustomTools.Tools.Editor;
using DANCustomTools.Tools.Editor.SubTools.PropertiesEditor;
using DANCustomTools.Tools.Editor.SubTools.SceneExplorer;
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
            _host = CreateHostBuilder(e.Args).Build();
            ServiceProvider = _host.Services;

            base.OnStartup(e);
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                });

        private static void ConfigureServices(IServiceCollection services)
        {
            // Core Services - Register in dependency order
            services.AddSingleton<ILogService, ConsoleLogService>();
            services.AddSingleton<IEngineHostService>(serviceProvider =>
            {
                var logService = serviceProvider.GetRequiredService<ILogService>();
                return new EngineHostService(logService);
            });
            services.AddSingleton<ISceneExplorerService>(serviceProvider =>
            {
                var logService = serviceProvider.GetRequiredService<ILogService>();
                var engineHost = serviceProvider.GetRequiredService<IEngineHostService>();
                return new SceneExplorerService(logService, engineHost);
            });
            services.AddSingleton<IPropertiesEditorService>(serviceProvider =>
            {
                var logService = serviceProvider.GetRequiredService<ILogService>();
                var engineHost = serviceProvider.GetRequiredService<IEngineHostService>();
                return new PropertiesEditorService(logService, engineHost);
            });

            // Tool Framework Services
            services.AddSingleton<IToolManager>(serviceProvider =>
            {
                var toolManager = new ToolManager();
                var toolContext = new ToolContext(serviceProvider, toolManager);

                // Create and register EditorMainTool
                var editorTool = new EditorMainTool(serviceProvider, toolContext);

                // Register sub tools
                var sceneExplorerSubTool = new SceneExplorerSubTool(serviceProvider, toolContext, editorTool);
                var propertiesEditorSubTool = new PropertiesEditorSubTool(serviceProvider, toolContext, editorTool);

                editorTool.RegisterSubTool(sceneExplorerSubTool);
                editorTool.RegisterSubTool(propertiesEditorSubTool);

                // Register main tool and initialize
                toolManager.RegisterMainTool(editorTool);
                toolManager.Initialize();

                return toolManager;
            });

            services.AddSingleton<IToolContext>(serviceProvider =>
                new ToolContext(serviceProvider, serviceProvider.GetRequiredService<IToolManager>()));

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<EditorMainViewModel>();
            services.AddTransient<SceneExplorerViewModel>();
            services.AddTransient<PropertiesEditorViewModel>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }
    }
}

