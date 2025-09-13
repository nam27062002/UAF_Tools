#nullable enable
using System;

namespace DANCustomTools.Core.Abstractions
{
    public interface IToolConfigurationService
    {
        void ConfigureTools(IServiceProvider serviceProvider);
        void RegisterMainTool<T>() where T : class, IMainTool;
        void ValidateConfiguration();
    }
}