using DANCustomTools.Services;
using DANCustomTools.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System;
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
            // Services - Register in dependency order
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

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<SceneExplorerViewModel>();
            services.AddTransient<PropertiesEditorViewModel>();

            // Views will be resolved through ViewModels via DataTemplates
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }
    }
}

