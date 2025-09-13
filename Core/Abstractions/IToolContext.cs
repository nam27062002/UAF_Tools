#nullable enable
using System;

namespace DANCustomTools.Core.Abstractions
{
    public interface IToolContext
    {
        IServiceProvider ServiceProvider { get; }
        IToolManager ToolManager { get; }

        event EventHandler<object>? DataShared;

        void ShareData(string key, object data);
        T? GetSharedData<T>(string key) where T : class;
        bool TryGetSharedData<T>(string key, out T? data) where T : class;
        void RemoveSharedData(string key);
    }
}