#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.MVVM;
using DANCustomTools.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DANCustomTools.Tools.Editor.SubTools.SceneExplorer
{
    public class SceneExplorerSubTool : SubToolBase
    {
        private readonly IMainTool _parentTool;

        public override string Name => "SceneExplorer";
        public override string DisplayName => "Scene Explorer";
        public override string Description => "Browse and manage scene hierarchy";
        public override IMainTool ParentTool => _parentTool;

        public SceneExplorerSubTool(IServiceProvider serviceProvider, IToolContext toolContext, IMainTool parentTool)
            : base(serviceProvider, toolContext)
        {
            _parentTool = parentTool ?? throw new ArgumentNullException(nameof(parentTool));
        }

        public override ViewModelBase CreateViewModel()
        {
            return ServiceProvider.GetRequiredService<SceneExplorerViewModel>();
        }

        public override void Initialize()
        {
            base.Initialize();
            // Any specific initialization for SceneExplorer
        }

        public override void Cleanup()
        {
            // Any specific cleanup for SceneExplorer
            base.Cleanup();
        }
    }
}