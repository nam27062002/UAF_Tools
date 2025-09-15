#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.MVVM;
using DANCustomTools.ViewModels;
using DANCustomTools.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DANCustomTools.Tools.Editor.SubTools.ActorCreate
{
    public class ActorCreateSubTool : SubToolBase
    {
        private readonly IMainTool _parentTool;

        public override string Name => "ActorCreate";
        public override string DisplayName => "Actor Creator";
        public override string Description => "Tool for creating and editing actors in UbiArt Framework";
        public override IMainTool ParentTool => _parentTool;

        public ActorCreateSubTool(IServiceProvider serviceProvider, IToolContext toolContext, IMainTool parentTool)
            : base(serviceProvider, toolContext)
        {
            _parentTool = parentTool ?? throw new ArgumentNullException(nameof(parentTool));
        }

        public override ViewModelBase CreateViewModel()
        {
            return CreateAndTrackViewModel<ActorCreateViewModel>();
        }

        public override void Initialize()
        {
            base.Initialize();

            // Additional initialization logic for ActorCreate tool
            var logService = ServiceProvider.GetService<ILogService>();
            logService?.Info($"ActorCreate SubTool initialized for parent: {ParentTool.DisplayName}");
        }

        public override void Cleanup()
        {
            // Any specific cleanup for ActorCreate
            base.Cleanup();
        }
    }
}
