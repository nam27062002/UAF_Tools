#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.MVVM;
using DANCustomTools.Tools.Editor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DANCustomTools.Tools.Editor
{
    public class EditorMainTool : MainToolBase
    {
        public override string Name => "Editor";
        public override string DisplayName => "Game Editor";
        public override string Description => "Main editor tool with scene explorer and properties editor";

        public EditorMainTool(IServiceProvider serviceProvider, IToolContext toolContext)
            : base(serviceProvider, toolContext)
        {
        }

        public override ViewModelBase CreateMainViewModel()
        {
            return ServiceProvider.GetRequiredService<EditorMainViewModel>();
        }

        public override void Initialize()
        {
            base.Initialize();

            if (SubTools.Count > 0)
            {
                SwitchToSubTool("SceneExplorer");
            }
        }
    }
}