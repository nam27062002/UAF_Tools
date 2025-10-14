#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace DANCustomTools.Tools.Editor.ViewModels
{
    public class EditorMainViewModel : ViewModelBase
    {
        private readonly IToolManager _toolManager;
        private readonly IServiceProvider _serviceProvider;

        public DANCustomTools.ViewModels.SceneExplorerViewModel? SceneExplorerViewModel { get; }
        public DANCustomTools.ViewModels.PropertiesEditorViewModel? PropertiesEditorViewModel { get; }

        public EditorMainViewModel(IToolManager toolManager, IServiceProvider serviceProvider)
        {
            _toolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            SceneExplorerViewModel = CreateSceneExplorerViewModel();
            PropertiesEditorViewModel = CreatePropertiesEditorViewModel();
        }

        private DANCustomTools.ViewModels.SceneExplorerViewModel? CreateSceneExplorerViewModel()
        {
            try
            {
                return _serviceProvider.GetService<DANCustomTools.ViewModels.SceneExplorerViewModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating SceneExplorerViewModel: {ex.Message}");
                return null;
            }
        }

        private DANCustomTools.ViewModels.PropertiesEditorViewModel? CreatePropertiesEditorViewModel()
        {
            try
            {
                return _serviceProvider.GetService<DANCustomTools.ViewModels.PropertiesEditorViewModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating PropertiesEditorViewModel: {ex.Message}");
                return null;
            }
        }

        public override void Dispose()
        {
            SceneExplorerViewModel?.Dispose();
            PropertiesEditorViewModel?.Dispose();
            base.Dispose();
        }
    }
}
