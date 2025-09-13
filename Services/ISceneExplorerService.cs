#nullable enable
using DANCustomTools.Models.SceneExplorer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DANCustomTools.Services
{
    public interface ISceneExplorerService
    {
        event EventHandler<bool>? ConnectionStatusChanged;
        event EventHandler<SceneTreeModel>? OnlineSceneTreeUpdated;
        event EventHandler<List<SceneTreeModel>>? OfflineSceneTreesUpdated;
        event EventHandler<uint>? ObjectSelectedFromRuntime;

        bool IsConnected { get; }
        Task StartAsync(string[] arguments, CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        
        void SelectScene(string uniqueScene);
        void SelectObjects(IEnumerable<ObjectWithRefModel> objects);
        void RenameObject(uint objectRef, string newName);
        void DeleteObject(uint objectRef);
        void DuplicateAndMoveObject(uint objectRef, float dx, float dy, float dz);
        void RequestSceneTree();
        void RequestOfflineSceneTrees(string path);
    }
}