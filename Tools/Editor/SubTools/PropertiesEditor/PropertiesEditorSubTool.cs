#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.MVVM;
using DANCustomTools.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DANCustomTools.Tools.Editor.SubTools.PropertiesEditor
{
    public class PropertiesEditorSubTool : SubToolBase
    {
        private readonly IMainTool _parentTool;

        public override string Name => "PropertiesEditor";
        public override string DisplayName => "Properties Editor";
        public override string Description => "Edit object properties and attributes";
        public override IMainTool ParentTool => _parentTool;

        public PropertiesEditorSubTool(IServiceProvider serviceProvider, IToolContext toolContext, IMainTool parentTool)
            : base(serviceProvider, toolContext)
        {
            _parentTool = parentTool ?? throw new ArgumentNullException(nameof(parentTool));
        }

        public override ViewModelBase CreateViewModel()
        {
            return CreateAndTrackViewModel<PropertiesEditorViewModel>();
        }

        public override void Initialize()
        {
            base.Initialize();
            // Any specific initialization for PropertiesEditor
        }

        public override void Cleanup()
        {
            // Any specific cleanup for PropertiesEditor
            base.Cleanup();
        }
    }
}