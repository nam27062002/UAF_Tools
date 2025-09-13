#nullable enable
using DANCustomTools.Models.SceneExplorer;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DANCustomTools.Services
{
    public class SceneExplorerService : ISceneExplorerService, IDisposable
    {
        private readonly ILogService _logService;
        private readonly IEngineHostService _engineHost;
        private pluginWrapper? _plugin;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isConnected;
        private readonly object _connectionLock = new();

        // Queues for thread-safe communication
        private readonly Queue<string> _sceneSelectionQueue = new();
        private readonly Queue<IEnumerable<ObjectWithRefModel>> _objectSelectionQueue = new();
        private readonly Queue<(uint objectRef, string newName)> _renameQueue = new();
        private readonly Queue<uint> _deleteQueue = new();
        private readonly Queue<(uint objectRef, float dx, float dy, float dz)> _duplicateQueue = new();
        private string? _offlineSceneTreePath;

        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<SceneTreeModel>? OnlineSceneTreeUpdated;
        public event EventHandler<List<SceneTreeModel>>? OfflineSceneTreesUpdated;
        public event EventHandler<uint>? ObjectSelectedFromRuntime;

        public bool IsConnected
        {
            get
            {
                lock (_connectionLock)
                {
                    return _isConnected;
                }
            }
        }

        public SceneExplorerService(ILogService logService, IEngineHostService engineHost)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _engineHost = engineHost ?? throw new ArgumentNullException(nameof(engineHost));
        }

        public async Task StartAsync(string[] arguments, CancellationToken cancellationToken = default)
        {
            if (_cancellationTokenSource != null)
                throw new InvalidOperationException("Service is already started");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                _engineHost.Initialize(arguments);

                // Set culture for decimal separator
                var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                culture.NumberFormat.NumberDecimalSeparator = ".";
                Thread.CurrentThread.CurrentCulture = culture;

                _logService.Info($"Starting SceneExplorer service on port {_engineHost.Settings?.Port}");

                // Start background network thread
                await Task.Run(() => NetworkThreadLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to start SceneExplorer service", ex);
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_cancellationTokenSource == null)
                return Task.CompletedTask;

            _logService.Info("Stopping SceneExplorer service");

            _cancellationTokenSource.Cancel();

            if (_isConnected)
            {
                _engineHost.Disconnect();
                lock (_connectionLock)
                {
                    _isConnected = false;
                }
            }

            _plugin = null;
            ConnectionStatusChanged?.Invoke(this, false);
            return Task.CompletedTask;
        }

        public void SelectScene(string uniqueScene)
        {
            lock (_sceneSelectionQueue)
            {
                _sceneSelectionQueue.Enqueue(uniqueScene);
            }
        }

        public void SelectObjects(IEnumerable<ObjectWithRefModel> objects)
        {
            lock (_objectSelectionQueue)
            {
                _objectSelectionQueue.Enqueue(objects);
            }
        }

        public void RenameObject(uint objectRef, string newName)
        {
            lock (_renameQueue)
            {
                _renameQueue.Enqueue((objectRef, newName));
            }
        }

        public void DeleteObject(uint objectRef)
        {
            lock (_deleteQueue)
            {
                _deleteQueue.Enqueue(objectRef);
            }
        }

        public void DuplicateAndMoveObject(uint objectRef, float dx, float dy, float dz)
        {
            lock (_duplicateQueue)
            {
                _duplicateQueue.Enqueue((objectRef, dx, dy, dz));
            }
        }

        public void RequestSceneTree()
        {
            if (_isConnected && _plugin != null)
            {
                SendSceneTreeRequest();
            }
        }

        public void RequestOfflineSceneTrees(string path)
        {
            _offlineSceneTreePath = path;
        }

        private void NetworkThreadLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var wasConnected = _isConnected;
                    UpdateConnectionStatus();

                    // Receive messages if connected
                    if (_isConnected && _plugin != null)
                    {
                        _engineHost.Update();
                        ProcessIncomingMessages();

                        // Handle first-time connection
                        if (!wasConnected)
                        {
                            RequestSceneTree();
                        }

                        // Process queued requests
                        ProcessQueuedRequests();
                    }

                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    _logService.Error("Error in network thread", ex);
                    Thread.Sleep(1000); // Back off on error
                }
            }
        }

        private void UpdateConnectionStatus()
        {
            if (!_engineHost.IsInitialized)
                return;

            var wasConnected = _isConnected;
            var isCurrentlyConnected = _isConnected;

            if (isCurrentlyConnected)
            {
                // Test existing connection via host
                isCurrentlyConnected = _engineHost.ConnectIfNeeded();
            }

            if (!isCurrentlyConnected)
            {
                if (wasConnected)
                {
                    _engineHost.Disconnect();
                    _plugin = null;
                }

                // Attempt to connect via host
                isCurrentlyConnected = _engineHost.ConnectIfNeeded();

                if (isCurrentlyConnected)
                {
                    _plugin = _engineHost.RegisterPlugin("SceneExplorer_Plugin");

                    var blob = new blobWrapper();
                    blob.push("PluginId");
                    blob.push("SceneExplorer_Plugin");
                    blob.sendToHost();
                }
            }

            if (isCurrentlyConnected != wasConnected)
            {
                lock (_connectionLock)
                {
                    _isConnected = isCurrentlyConnected;
                }

                ConnectionStatusChanged?.Invoke(this, _isConnected);
                _logService.Info($"SceneExplorerService connection status: {(_isConnected ? "Connected" : "Disconnected")}");
            }
        }

        private void ProcessIncomingMessages()
        {
            if (_plugin == null)
                return;

            var blob = new blobWrapper();
            while (_plugin.dispatch(blob))
            {
                var command = "";
                blob.extract(ref command);

                switch (command)
                {
                    case "SceneTree":
                        ProcessOnlineSceneTree(blob);
                        break;
                    case "SceneTree_Offline":
                        ProcessOfflineSceneTrees(blob);
                        break;
                    case "ObjectsSelectedInRuntime":
                        ProcessRuntimeSelection(blob);
                        break;
                }
            }
        }

        private void ProcessOnlineSceneTree(blobWrapper blob)
        {
            try
            {
                var sceneTree = ConvertBlobToSceneTreeModel(blob, true);
                _logService.Info($"SceneTree received: name='{sceneTree.UniqueName}', actors={sceneTree.Actors.Count}, frises={sceneTree.Frises.Count}, children={sceneTree.ChildScenes.Count}");
                OnlineSceneTreeUpdated?.Invoke(this, sceneTree);
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to process online scene tree", ex);
            }
        }

        private void ProcessOfflineSceneTrees(blobWrapper blob)
        {
            try
            {
                uint treeCount = 0;
                blob.extract(ref treeCount);

                var sceneTrees = new List<SceneTreeModel>();
                for (int i = 0; i < treeCount; i++)
                {
                    var sceneTree = ConvertBlobToSceneTreeModel(blob, false);
                    sceneTrees.Add(sceneTree);
                }

                OfflineSceneTreesUpdated?.Invoke(this, sceneTrees);
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to process offline scene trees", ex);
            }
        }

        private void ProcessRuntimeSelection(blobWrapper blob)
        {
            try
            {
                uint objectRef = 0;
                blob.extract(ref objectRef);
                ObjectSelectedFromRuntime?.Invoke(this, objectRef);
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to process runtime selection", ex);
            }
        }

        private SceneTreeModel ConvertBlobToSceneTreeModel(blobWrapper blob, bool isOnline)
        {
            var model = new SceneTreeModel { IsOnline = isOnline };

            string uniqueName = "";
            string path = "";
            blob.extract(ref uniqueName);
            blob.extract(ref path);

            model.UniqueName = uniqueName;
            model.Path = path;

            // Extract actors
            uint actorCount = 0;
            blob.extract(ref actorCount);
            for (uint i = 0; i < actorCount; i++)
            {
                var actor = ConvertBlobToActorModel(blob, isOnline);
                model.Actors.Add(actor);
            }

            // Extract frises
            uint friseCount = 0;
            blob.extract(ref friseCount);
            for (uint i = 0; i < friseCount; i++)
            {
                var frise = ConvertBlobToFriseModel(blob, isOnline);
                model.Frises.Add(frise);
            }

            // Extract child scenes
            uint childCount = 0;
            blob.extract(ref childCount);
            for (uint i = 0; i < childCount; i++)
            {
                var child = ConvertBlobToSceneTreeModel(blob, isOnline);
                model.ChildScenes.Add(child);
            }

            return model;
        }

        private ActorModel ConvertBlobToActorModel(blobWrapper blob, bool isOnline)
        {
            var model = new ActorModel { IsOnline = isOnline };

            if (isOnline)
            {
                uint objectRef = 0;
                blob.extract(ref objectRef);
                model.ObjectRef = objectRef;
            }
            else
            {
                string offlineId = "";
                blob.extract(ref offlineId);
                model.OfflineId = offlineId;
            }

            string friendlyName = "";
            string components = "";
            string luaPath = "";

            blob.extract(ref friendlyName);
            blob.extract(ref components);
            blob.extract(ref luaPath);

            model.FriendlyName = friendlyName;
            model.Components = components;
            model.LuaPath = luaPath;

            return model;
        }

        private FriseModel ConvertBlobToFriseModel(blobWrapper blob, bool isOnline)
        {
            var model = new FriseModel { IsOnline = isOnline };

            if (isOnline)
            {
                uint objectRef = 0;
                blob.extract(ref objectRef);
                model.ObjectRef = objectRef;
            }
            else
            {
                string offlineId = "";
                blob.extract(ref offlineId);
                model.OfflineId = offlineId;
            }

            string friendlyName = "";
            string configPath = "";

            blob.extract(ref friendlyName);
            blob.extract(ref configPath);

            model.FriendlyName = friendlyName;
            model.ConfigPath = configPath;

            return model;
        }

        private void ProcessQueuedRequests()
        {
            // Process scene selections
            lock (_sceneSelectionQueue)
            {
                while (_sceneSelectionQueue.Count > 0)
                {
                    var scene = _sceneSelectionQueue.Dequeue();
                    SendSelectSceneRequest(scene);
                }
            }

            // Process object selections
            lock (_objectSelectionQueue)
            {
                while (_objectSelectionQueue.Count > 0)
                {
                    var objects = _objectSelectionQueue.Dequeue();
                    SendSelectObjectsRequest(objects);
                }
            }

            // Process renames
            lock (_renameQueue)
            {
                while (_renameQueue.Count > 0)
                {
                    var (objectRef, newName) = _renameQueue.Dequeue();
                    SendRenameRequest(objectRef, newName);
                }
            }

            // Process deletes
            lock (_deleteQueue)
            {
                while (_deleteQueue.Count > 0)
                {
                    var objectRef = _deleteQueue.Dequeue();
                    SendDeleteRequest(objectRef);
                }
            }

            // Process duplicates
            lock (_duplicateQueue)
            {
                while (_duplicateQueue.Count > 0)
                {
                    var (objectRef, dx, dy, dz) = _duplicateQueue.Dequeue();
                    SendDuplicateRequest(objectRef, dx, dy, dz);
                }
            }

            // Process offline scene tree request
            if (_offlineSceneTreePath != null)
            {
                SendOfflineSceneTreeRequest(_offlineSceneTreePath);
                _offlineSceneTreePath = null;
            }
        }

        private void SendSceneTreeRequest()
        {
            var blob = new blobWrapper();
            blob.push("SceneExplorer_Plugin");
            blob.push("SendSceneTree");
            blob.sendToHost();
        }

        private void SendOfflineSceneTreeRequest(string path)
        {
            var blob = new blobWrapper();
            blob.push("SceneExplorer_Plugin");
            blob.push("SendSceneTree_Offline");
            blob.push(path);
            blob.sendToHost();
        }

        private void SendSelectSceneRequest(string sceneName)
        {
            var blob = new blobWrapper();
            blob.push("SceneExplorer_Plugin");
            blob.push("SelectScene");
            blob.push(sceneName);
            blob.sendToHost();
        }

        private void SendSelectObjectsRequest(IEnumerable<ObjectWithRefModel> objects)
        {
            var onlineObjects = objects.Where(o => o.IsOnline).ToArray();

            var blob = new blobWrapper();
            blob.push("SceneExplorer_Plugin");
            blob.push("SelectObjects");
            blob.push((uint)onlineObjects.Length);

            foreach (var obj in onlineObjects)
            {
                blob.push(obj.ObjectRef);
            }

            blob.sendToHost();
        }

        private void SendRenameRequest(uint objectRef, string newName)
        {
            var blob = new blobWrapper();
            blob.push("SceneExplorer_Plugin");
            blob.push("RenameItem");
            blob.push(objectRef);
            blob.push(newName);
            blob.sendToHost();
        }

        private void SendDeleteRequest(uint objectRef)
        {
            var blob = new blobWrapper();
            blob.push("SceneExplorer_Plugin");
            blob.push("DeleteItem");
            blob.push(objectRef);
            blob.sendToHost();
        }

        private void SendDuplicateRequest(uint objectRef, float dx, float dy, float dz)
        {
            var blob = new blobWrapper();
            blob.push("SceneExplorer_Plugin");
            blob.push("DuplicateAndMoveItem");
            blob.push(objectRef);
            blob.push(dx);
            blob.push(dy);
            blob.push(dz);
            blob.sendToHost();
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
            _cancellationTokenSource?.Dispose();
        }
    }
}
