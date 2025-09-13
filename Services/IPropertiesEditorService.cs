#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using DANCustomTools.Models.PropertiesEditor;

namespace DANCustomTools.Services
{
    public interface IPropertiesEditorService
    {
        event EventHandler<PropertyModel>? PropertiesUpdated;
        event EventHandler<string>? DataPathUpdated;
        
        Task StartAsync(string[] arguments, CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        
        void RequestObjectProperties(uint objectRef);
        void SendPropertiesUpdate(uint objectRef, string xmlData);
        void ClearProperties();
        void DumpToFile(string fileName);
        
        string DataPath { get; }
        bool IsConnected { get; }
    }
}