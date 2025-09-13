#nullable enable
using System;

namespace DANCustomTools.Core.Abstractions
{
    public interface IToolInitializer
    {
        void Initialize(IServiceProvider serviceProvider);
        bool IsInitialized { get; }
    }
}