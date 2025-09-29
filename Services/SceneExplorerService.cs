#nullable enable
using DANCustomTools.Core.Services;
using DANCustomTools.Models.SceneExplorer;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DANCustomTools.Services
{
    public class SceneExplorerService(ILogService logService, IEngineHostService engineHost)
        : EnginePluginServiceBase(logService, engineHost), ISceneExplorerService
    {

        // Queues for thread-safe communication
        private readonly Queue<string> _sceneSelectionQueue = new();
        private readonly Queue<IEnumerable<ObjectWithRefModel>> _objectSelectionQueue = new();
        private readonly Queue<(uint objectRef, string newName)> _renameQueue = new();
        private readonly Queue<uint> _deleteQueue = new();
        private readonly Queue<(uint objectRef, float dx, float dy, float dz)> _duplicateQueue = new();
        private string? _offlineSceneTreePath;
        private DateTime _lastSceneTreeResponse = DateTime.MinValue;

        public override string PluginName => "SceneExplorer_Plugin";

        public event EventHandler<SceneTreeModel>? OnlineSceneTreeUpdated;
        public event EventHandler<List<SceneTreeModel>>? OfflineSceneTreesUpdated;
        public event EventHandler<uint>? ObjectSelectedFromRuntime;


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
            if (IsConnected && Plugin != null)
            {
                LogService.Info("Requesting scene tree from engine");
                SendSceneTreeRequest();

                // Schedule a retry after 2 seconds if no response
                Task.Delay(2000).ContinueWith(_ =>
                {
                    if (IsConnected && Plugin != null && (DateTime.Now - _lastSceneTreeResponse).TotalSeconds > 1)
                    {
                        LogService.Info("Retrying scene tree request (no response received)");
                        SendSceneTreeRequest();
                    }
                });
            }
            else
            {
                LogService.Warning($"Cannot request scene tree - IsConnected: {IsConnected}, Plugin: {(Plugin != null ? "OK" : "NULL")}");
            }
        }

        public void RequestOfflineSceneTrees(string path)
        {
            _offlineSceneTreePath = path;
        }




        protected override void ProcessMessage(blobWrapper blob)
        {
            var command = "";
            blob.extract(ref command);

            LogService.Info($"SceneExplorer received message: '{command}' [after reconnect: {DateTime.Now:HH:mm:ss}]");

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
                default:
                    LogService.Warning($"Unknown SceneExplorer command: '{command}'");
                    break;
            }
        }

        private void ProcessOnlineSceneTree(blobWrapper blob)
        {
            try
            {
                var sceneTree = ConvertBlobToSceneTreeModel(blob, true);
                _lastSceneTreeResponse = DateTime.Now;
                LogService.Info($"SceneTree received: name='{sceneTree.UniqueName}', actors={sceneTree.Actors.Count}, frises={sceneTree.Frises.Count}, children={sceneTree.ChildScenes.Count}");
                OnlineSceneTreeUpdated?.Invoke(this, sceneTree);
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to process online scene tree", ex);
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
                LogService.Error("Failed to process offline scene trees", ex);
            }
        }

        private void ProcessRuntimeSelection(blobWrapper blob)
        {
            try
            {
                uint objectRef = 0;
                blob.extract(ref objectRef);
                LogService.Info($"Processing runtime selection for object ref: {objectRef}");
                ObjectSelectedFromRuntime?.Invoke(this, objectRef);
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to process runtime selection", ex);
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

        protected override void ProcessQueuedRequests()
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

        protected override void OnFirstTimeConnected()
        {
            LogService.Info("SceneExplorer first time connected - requesting scene tree");

            // Try multiple approaches to wake up engine
            Task.Run(async () =>
            {
                if (!IsConnected || Plugin == null) return;

                // First attempt: immediate request
                LogService.Info("Immediate scene tree request after connection");
                RequestSceneTree();

                // Second attempt: after delay
                await Task.Delay(500);
                if (IsConnected && Plugin != null)
                {
                    LogService.Info("Delayed scene tree request after connection");
                    SendSceneTreeRequest();
                }

                // Third attempt: aggressive retry
                await Task.Delay(1000);
                if (IsConnected && Plugin != null && (DateTime.Now - _lastSceneTreeResponse).TotalSeconds > 2)
                {
                    LogService.Info("Aggressive scene tree request (no response)");
                    SendSceneTreeRequest();
                }

                // Fourth attempt: wake-up approach
                await Task.Delay(2000);
                if (IsConnected && Plugin != null && (DateTime.Now - _lastSceneTreeResponse).TotalSeconds > 3)
                {
                    LogService.Info("Wake-up approach - sending multiple requests");
                    SendWakeupRequests();
                }
            });
        }

        private void SendSceneTreeRequest()
        {
            LogService.Info("Sending 'SendSceneTree' message to engine");
            SendMessage(blob => blob.push("SendSceneTree"));
        }

        private void SendWakeupRequests()
        {
            if (!IsConnected || Plugin == null) return;

            LogService.Info("Sending wake-up messages to engine");

            // Try different message types to wake up engine
            try
            {
                SendMessage(blob => blob.push("SendSceneTree"));

                // Sometimes engines need a "ping" first
                SendMessage(blob =>
                {
                    blob.push("RequestSelection");
                    blob.push(0u); // dummy object ref
                });
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to send wake-up requests", ex);
            }
        }


        private void SendOfflineSceneTreeRequest(string path)
        {
            SendMessage(blob =>
            {
                blob.push("SendSceneTree_Offline");
                blob.push(path);
            });
        }

        private void SendSelectSceneRequest(string sceneName)
        {
            SendMessage(blob =>
            {
                blob.push("SelectScene");
                blob.push(sceneName);
            });
        }

        private void SendSelectObjectsRequest(IEnumerable<ObjectWithRefModel> objects)
        {
            var onlineObjects = objects.Where(o => o.IsOnline).ToArray();

            SendMessage(blob =>
            {
                blob.push("SelectObjects");
                blob.push((uint)onlineObjects.Length);

                foreach (var obj in onlineObjects)
                {
                    blob.push(obj.ObjectRef);
                }
            });
        }

        private void SendRenameRequest(uint objectRef, string newName)
        {
            SendMessage(blob =>
            {
                blob.push("RenameItem");
                blob.push(objectRef);
                blob.push(newName);
            });
        }

        private void SendDeleteRequest(uint objectRef)
        {
            SendMessage(blob =>
            {
                blob.push("DeleteItem");
                blob.push(objectRef);
            });
        }

        private void SendDuplicateRequest(uint objectRef, float dx, float dy, float dz)
        {
            SendMessage(blob =>
            {
                blob.push("DuplicateAndMoveItem");
                blob.push(objectRef);
                blob.push(dx);
                blob.push(dy);
                blob.push(dz);
            });
        }

    }
}
