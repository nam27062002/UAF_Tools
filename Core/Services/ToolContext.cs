#nullable enable
using DANCustomTools.Core.Abstractions;
using System;
using System.Collections.Concurrent;

namespace DANCustomTools.Core.Services
{
    public class ToolContext : IToolContext
    {
        private readonly ConcurrentDictionary<string, object> _sharedData = new();

        public IServiceProvider ServiceProvider { get; }
        public IToolManager ToolManager { get; }

        public event EventHandler<object>? DataShared;

        public ToolContext(IServiceProvider serviceProvider, IToolManager toolManager)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            ToolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
        }

        public void ShareData(string key, object data)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _sharedData.AddOrUpdate(key, data, (k, oldValue) => data);
            DataShared?.Invoke(this, data);
        }

        public T? GetSharedData<T>(string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            return _sharedData.TryGetValue(key, out var data) && data is T typedData
                ? typedData
                : null;
        }

        public bool TryGetSharedData<T>(string key, out T? data) where T : class
        {
            data = GetSharedData<T>(key);
            return data != null;
        }

        public void RemoveSharedData(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _sharedData.TryRemove(key, out _);
            }
        }
    }
}