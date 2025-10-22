#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using DANCustomTools.Core.Abstractions;

namespace DANCustomTools.Services
{
    /// <summary>
    /// Service for integrating with the game engine using engineWrapper
    /// </summary>
    public class EngineIntegrationService : IEngineIntegrationService
    {
        private readonly ILogService? _logService;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly object _lockObject = new();

        // Engine communication
        private dynamic? _engineWrapper;
        private dynamic? _pluginWrapper;
        private bool _isConnected = false;
        private string _sessionPath = string.Empty;
        private string _hostAddress = "127.0.0.1";
        private int _hostPort = 1001;

        // Plugin constants
        private const string PluginName = "ActorCreate_Plugin";
        private const string ActorExtension = ".act";

        // Message constants
        private const string MsgGetSessionInfo = "getSessionInfo";
        private const string MsgGetComponentList = "getComponentList";
        private const string MsgGetActorList = "getActorList";
        private const string MsgGetActorComponentList = "getActorComponentList";
        private const string MsgGetActorComponent = "getActorComponent";
        private const string MsgGetActorMainData = "getActorMainData";
        private const string MsgNewActor = "newActor";
        private const string MsgSetActor = "setActor";
        private const string MsgAddActorComponent = "addActorComponent";
        private const string MsgDelActorComponent = "delActorComponent";
        private const string MsgSaveActor = "saveActor";
        private const string MsgSetActorComponentData = "setActorComponentData";
        private const string MsgSetActorMainData = "setActorMainData";

        // Receive message constants
        private const string MsgSessionInfo = "SessionInfo";
        private const string MsgComponentList = "ComponentList";
        private const string MsgActorList = "ActorList";
        private const string MsgActorComponentList = "ActorComponentList";
        private const string MsgActorMainData = "ActorMainData";
        private const string MsgActorComponentData = "ActorComponentData";
        private const string MsgError = "Error";

        public bool IsConnected => _isConnected;
        public string SessionPath => _sessionPath;

        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<string>? SessionInfoReceived;
        public event EventHandler<List<string>>? ComponentListReceived;
        public event EventHandler<List<EngineActorInfo>>? ActorListReceived;
        public event EventHandler<ActorComponentListEventArgs>? ActorComponentListReceived;
        public event EventHandler<ActorMainDataEventArgs>? ActorMainDataReceived;
        public event EventHandler<ActorComponentDataEventArgs>? ActorComponentDataReceived;
        public event EventHandler<string>? ErrorReceived;

        public EngineIntegrationService(ILogService? logService = null)
        {
            _logService = logService;
            _logService?.Info("EngineIntegrationService initialized");
        }

        public async Task<bool> ConnectAsync(string hostAddress = "127.0.0.1", int hostPort = 1001)
        {
            try
            {
                _hostAddress = hostAddress;
                _hostPort = hostPort;

                _logService?.Info($"Connecting to engine at {hostAddress}:{hostPort}");

                // Initialize engine wrapper
                await InitializeEngineWrapperAsync();

                if (_engineWrapper != null)
                {
                    // Try to connect
                    bool connected = await Task.Run(() => _engineWrapper.connectToHost(hostAddress, hostPort));
                    
                    if (connected)
                    {
                        // Initialize plugin wrapper
                        _pluginWrapper = new { }; // This would be the actual pluginWrapper instance
                        _engineWrapper.addPlugin(PluginName, _pluginWrapper);

                        _isConnected = true;
                        _logService?.Info("Successfully connected to engine");

                        // Start message processing thread
                        _ = Task.Run(ProcessMessagesAsync, _cancellationTokenSource.Token);

                        // Send initial requests
                        await SendGetSessionInfoAsync();
                        await SendGetComponentListAsync();
                        await SendGetActorListAsync();

                        ConnectionStatusChanged?.Invoke(this, true);
                        return true;
                    }
                }

                _logService?.Warning("Failed to connect to engine");
                return false;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error connecting to engine: {ex.Message}");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _logService?.Info("Disconnecting from engine");

                _cancellationTokenSource.Cancel();

                if (_engineWrapper != null)
                {
                    await Task.Run(() => _engineWrapper.disconnect());
                    _engineWrapper = null;
                }

                _pluginWrapper = null;
                _isConnected = false;
                _sessionPath = string.Empty;

                ConnectionStatusChanged?.Invoke(this, false);
                _logService?.Info("Disconnected from engine");
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error disconnecting from engine: {ex.Message}");
            }
        }

        #region Send Methods

        public async Task SendGetSessionInfoAsync()
        {
            await SendMessageAsync(PluginName, MsgGetSessionInfo);
        }

        public async Task SendGetComponentListAsync()
        {
            await SendMessageAsync(PluginName, MsgGetComponentList);
        }

        public async Task SendGetActorListAsync()
        {
            await SendMessageAsync(PluginName, MsgGetActorList);
        }

        public async Task SendGetActorComponentListAsync(int actorIndex)
        {
            await SendMessageAsync(PluginName, MsgGetActorComponentList, actorIndex);
        }

        public async Task SendGetActorMainDataAsync(int actorIndex)
        {
            await SendMessageAsync(PluginName, MsgGetActorMainData, actorIndex);
        }

        public async Task SendGetActorComponentDataAsync(int actorIndex, string componentName)
        {
            await SendMessageAsync(PluginName, MsgGetActorComponent, actorIndex, componentName);
        }

        public async Task SendNewActorAsync(string actorPath, List<string> componentList)
        {
            await SendMessageAsync(PluginName, MsgNewActor, actorPath, componentList.Count, componentList.ToArray());
        }

        public async Task SendSetActorAsync(string actorPath, int actorIndex, bool mainInfo = true, bool forceReload = false)
        {
            await SendMessageAsync(PluginName, MsgSetActor, actorIndex, actorPath, mainInfo ? 1 : 0, forceReload ? 1 : 0);
        }

        public async Task SendAddActorComponentAsync(int actorIndex, string componentName)
        {
            await SendMessageAsync(PluginName, MsgAddActorComponent, actorIndex, componentName);
        }

        public async Task SendRemoveActorComponentAsync(int actorIndex, string componentName)
        {
            await SendMessageAsync(PluginName, MsgDelActorComponent, actorIndex, componentName);
        }

        public async Task SendSaveActorAsync(int actorIndex)
        {
            await SendMessageAsync(PluginName, MsgSaveActor, actorIndex);
        }

        public async Task SendSetActorComponentDataAsync(int actorIndex, string componentName, string componentData)
        {
            await SendMessageAsync(PluginName, MsgSetActorComponentData, actorIndex, componentName, componentData);
        }

        public async Task SendSetActorMainDataAsync(int actorIndex, string mainData, object parameters)
        {
            await SendMessageAsync(PluginName, MsgSetActorMainData, actorIndex, mainData, parameters);
        }

        #endregion

        #region Private Methods

        private async Task InitializeEngineWrapperAsync()
        {
            try
            {
                // This would initialize the actual engineWrapper
                // For now, we'll simulate it
                await Task.Delay(100);
                
                // In real implementation, this would be:
                // _engineWrapper = new engineWrapper("ActorCreate");
                
                _logService?.Info("Engine wrapper initialized");
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error initializing engine wrapper: {ex.Message}");
                throw;
            }
        }

        private async Task SendMessageAsync(string pluginName, string message, params object[] parameters)
        {
            if (!_isConnected || _engineWrapper == null)
            {
                _logService?.Warning($"Cannot send message {message}: not connected to engine");
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    // This would use the actual blobWrapper to send messages
                    // For now, we'll simulate the message sending
                    _logService?.Info($"Sending message: {message} with {parameters.Length} parameters");
                });
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error sending message {message}: {ex.Message}");
            }
        }

        private async Task ProcessMessagesAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested && _isConnected)
            {
                try
                {
                    if (_engineWrapper != null && _pluginWrapper != null)
                    {
                        // This would process actual messages from the engine
                        // For now, we'll simulate message processing
                        await Task.Delay(100);
                        
                        // In real implementation, this would be:
                        // blobWrapper blob = new blobWrapper();
                        // while (_pluginWrapper.dispatch(blob))
                        // {
                        //     ProcessReceivedMessage(blob);
                        // }
                    }
                }
                catch (Exception ex)
                {
                    _logService?.Error($"Error processing messages: {ex.Message}");
                }

                await Task.Delay(50, _cancellationTokenSource.Token);
            }
        }

        private void ProcessReceivedMessage(dynamic blob)
        {
            try
            {
                string message = "";
                blob.extract(ref message);

                switch (message)
                {
                    case MsgSessionInfo:
                        ProcessSessionInfo(blob);
                        break;
                    case MsgComponentList:
                        ProcessComponentList(blob);
                        break;
                    case MsgActorList:
                        ProcessActorList(blob);
                        break;
                    case MsgActorComponentList:
                        ProcessActorComponentList(blob);
                        break;
                    case MsgActorMainData:
                        ProcessActorMainData(blob);
                        break;
                    case MsgActorComponentData:
                        ProcessActorComponentData(blob);
                        break;
                    case MsgError:
                        ProcessError(blob);
                        break;
                    default:
                        _logService?.Warning($"Unknown message received: {message}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error processing received message: {ex.Message}");
            }
        }

        private void ProcessSessionInfo(dynamic blob)
        {
            string path = "";
            blob.extract(ref path);
            
            lock (_lockObject)
            {
                _sessionPath = path;
            }
            
            _logService?.Info($"Session info received: {path}");
            SessionInfoReceived?.Invoke(this, path);
        }

        private void ProcessComponentList(dynamic blob)
        {
            int componentCount = 0;
            blob.extract(ref componentCount);
            
            var componentList = new List<string>();
            for (int i = 0; i < componentCount; i++)
            {
                string componentName = "";
                blob.extract(ref componentName);
                componentList.Add(componentName);
            }
            
            _logService?.Info($"Component list received: {componentList.Count} components");
            ComponentListReceived?.Invoke(this, componentList);
        }

        private void ProcessActorList(dynamic blob)
        {
            int actorCount = 0;
            blob.extract(ref actorCount);
            
            var actorList = new List<EngineActorInfo>();
            for (int i = 0; i < actorCount; i++)
            {
                string uniqueId = "";
                string name = "";
                int flags = 0;
                
                blob.extract(ref uniqueId);
                blob.extract(ref name);
                blob.extract(ref flags);
                
                actorList.Add(new EngineActorInfo
                {
                    UniqueId = uniqueId,
                    Name = name,
                    Flags = flags
                });
            }
            
            _logService?.Info($"Actor list received: {actorList.Count} actors");
            ActorListReceived?.Invoke(this, actorList);
        }

        private void ProcessActorComponentList(dynamic blob)
        {
            int actorIndex = 0;
            blob.extract(ref actorIndex);
            
            int componentCount = 0;
            blob.extract(ref componentCount);
            
            var componentList = new List<string>();
            for (int i = 0; i < componentCount; i++)
            {
                string componentName = "";
                blob.extract(ref componentName);
                componentList.Add(componentName);
            }
            
            _logService?.Info($"Actor component list received for actor {actorIndex}: {componentList.Count} components");
            ActorComponentListReceived?.Invoke(this, new ActorComponentListEventArgs
            {
                ActorIndex = actorIndex,
                ComponentList = componentList
            });
        }

        private void ProcessActorMainData(dynamic blob)
        {
            int actorIndex = 0;
            blob.extract(ref actorIndex);
            
            string mainData = "";
            blob.extract(ref mainData);
            
            // Extract parameters (this would be more complex in real implementation)
            var parameters = new { };
            
            _logService?.Info($"Actor main data received for actor {actorIndex}");
            ActorMainDataReceived?.Invoke(this, new ActorMainDataEventArgs
            {
                ActorIndex = actorIndex,
                MainData = mainData,
                Parameters = parameters
            });
        }

        private void ProcessActorComponentData(dynamic blob)
        {
            string componentName = "";
            blob.extract(ref componentName);
            
            string componentData = "";
            blob.extract(ref componentData);
            
            _logService?.Info($"Actor component data received for component {componentName}");
            ActorComponentDataReceived?.Invoke(this, new ActorComponentDataEventArgs
            {
                ComponentName = componentName,
                ComponentData = componentData
            });
        }

        private void ProcessError(dynamic blob)
        {
            string errorMessage = "";
            blob.extract(ref errorMessage);
            
            _logService?.Error($"Engine error: {errorMessage}");
            ErrorReceived?.Invoke(this, errorMessage);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            try
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                }

                if (_isConnected)
                {
                    // Fire and forget
                    _ = DisconnectAsync();
                }

                _logService?.Info("EngineIntegrationService disposed");
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error disposing EngineIntegrationService: {ex.Message}");
            }
        }

        #endregion
    }
}
