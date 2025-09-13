#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.MVVM;
using DANCustomTools.Tools.AssetsCooker.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DANCustomTools.Tools.AssetsCooker
{
    public class AssetsCookerMainTool : MainToolBase
    {
        public override string Name => "AssetsCooker";
        public override string DisplayName => "Assets Cooker";
        public override string Description => "Asset processing and cooking tool for game development";

        public AssetsCookerMainTool(IServiceProvider serviceProvider, IToolContext toolContext)
            : base(serviceProvider, toolContext)
        {
        }

        public override ViewModelBase CreateMainViewModel()
        {
            return ServiceProvider.GetRequiredService<AssetsCookerMainViewModel>();
        }

        public override void Initialize()
        {
            base.Initialize();

            // AssetsCooker doesn't have SubTools initially
            // Future SubTools can be added here (e.g., TextureCooker, AudioCooker, etc.)
        }
    }
}