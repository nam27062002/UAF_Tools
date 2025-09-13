#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.Services;
using DANCustomTools.Tools.Editor;
using DANCustomTools.Tools.Editor.SubTools.PropertiesEditor;
using DANCustomTools.Tools.Editor.SubTools.SceneExplorer;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DANCustomTools.Core.Services
{
    public class ToolConfigurationService : IToolConfigurationService
    {
        public void ConfigureTools(IServiceProvider serviceProvider)
        {
            var toolManager = serviceProvider.GetRequiredService<IToolManager>();
            var toolContext = serviceProvider.GetRequiredService<IToolContext>();

            try
            {
                // Configure EditorMainTool
                ConfigureEditorTool(serviceProvider, toolManager, toolContext);

                // Initialize all tools
                toolManager.Initialize();

                // Validate configuration
                ValidateConfiguration();
            }
            catch (Exception ex)
            {
                var logService = serviceProvider.GetService<ILogService>();
                logService?.Error($"Error configuring tools: {ex.Message}", ex);
                throw new InvalidOperationException("Tool configuration failed", ex);
            }
        }

        private static void ConfigureEditorTool(IServiceProvider serviceProvider, IToolManager toolManager, IToolContext toolContext)
        {
            // Create EditorMainTool
            var editorTool = new EditorMainTool(serviceProvider, toolContext);

            // Create and register SubTools
            var sceneExplorerSubTool = new SceneExplorerSubTool(serviceProvider, toolContext, editorTool);
            var propertiesEditorSubTool = new PropertiesEditorSubTool(serviceProvider, toolContext, editorTool);

            editorTool.RegisterSubTool(sceneExplorerSubTool);
            editorTool.RegisterSubTool(propertiesEditorSubTool);

            // Register with ToolManager
            toolManager.RegisterMainTool(editorTool);
        }

        public void RegisterMainTool<T>() where T : class, IMainTool
        {
            // For future extensibility - register custom MainTools
            throw new NotImplementedException("Custom MainTool registration will be implemented in future versions");
        }

        public void ValidateConfiguration()
        {
            // Validation logic will be implemented as tools are added
            // Current validation: Basic tool registration check
            // Future: Comprehensive dependency validation
        }
    }
}