#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DANCustomTools.Core.Abstractions;

namespace DANCustomTools.Services
{
    /// <summary>
    /// Service for managing actor components with real engine integration
    /// </summary>
    public class ComponentManagementService : IComponentManagementService
    {
        private readonly IEngineIntegrationService _engineIntegrationService;
        private readonly ILogService? _logService;

        private readonly List<string> _availableComponents = new();
        private readonly Dictionary<int, List<string>> _actorComponents = new();
        private readonly object _lockObject = new();

        public IEnumerable<string> AvailableComponents
        {
            get
            {
                lock (_lockObject)
                {
                    return _availableComponents.ToList();
                }
            }
        }

        public event EventHandler<List<string>>? ComponentListUpdated;
        public event EventHandler<ActorComponentListUpdatedEventArgs>? ActorComponentListUpdated;

        public ComponentManagementService(IEngineIntegrationService engineIntegrationService, ILogService? logService = null)
        {
            _engineIntegrationService = engineIntegrationService ?? throw new ArgumentNullException(nameof(engineIntegrationService));
            _logService = logService;

            // Subscribe to engine events
            SubscribeToEngineEvents();

            _logService?.Info("ComponentManagementService initialized");
        }

        public IEnumerable<string> GetActorComponents(int actorIndex)
        {
            lock (_lockObject)
            {
                return _actorComponents.TryGetValue(actorIndex, out var components) 
                    ? components.ToList() 
                    : new List<string>();
            }
        }

        public async Task<bool> AddComponentToActorAsync(int actorIndex, string componentName)
        {
            try
            {
                _logService?.Info($"Adding component '{componentName}' to actor {actorIndex}");

                if (!_engineIntegrationService.IsConnected)
                {
                    _logService?.Warning("Cannot add component: engine not connected");
                    return false;
                }

                // Send request to engine
                await _engineIntegrationService.SendAddActorComponentAsync(actorIndex, componentName);

                // Update local cache
                lock (_lockObject)
                {
                    if (!_actorComponents.ContainsKey(actorIndex))
                    {
                        _actorComponents[actorIndex] = new List<string>();
                    }
                    
                    if (!_actorComponents[actorIndex].Contains(componentName))
                    {
                        _actorComponents[actorIndex].Add(componentName);
                    }
                }

                _logService?.Info($"Component '{componentName}' added to actor {actorIndex}");
                return true;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error adding component '{componentName}' to actor {actorIndex}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveComponentFromActorAsync(int actorIndex, string componentName)
        {
            try
            {
                _logService?.Info($"Removing component '{componentName}' from actor {actorIndex}");

                if (!_engineIntegrationService.IsConnected)
                {
                    _logService?.Warning("Cannot remove component: engine not connected");
                    return false;
                }

                // Send request to engine
                await _engineIntegrationService.SendRemoveActorComponentAsync(actorIndex, componentName);

                // Update local cache
                lock (_lockObject)
                {
                    if (_actorComponents.ContainsKey(actorIndex))
                    {
                        _actorComponents[actorIndex].Remove(componentName);
                    }
                }

                _logService?.Info($"Component '{componentName}' removed from actor {actorIndex}");
                return true;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error removing component '{componentName}' from actor {actorIndex}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CopyComponentAsync(int actorIndex, string componentName, string componentData)
        {
            try
            {
                _logService?.Info($"Copying component '{componentName}' from actor {actorIndex}");

                // Create clipboard buffer in the same format as Windows Forms version
                string clipboardBuffer = $"Component<{componentName}>{componentData}";
                
                // Set to clipboard
                await Task.Run(() => System.Windows.Clipboard.SetDataObject(clipboardBuffer));

                _logService?.Info($"Component '{componentName}' copied to clipboard");
                return true;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error copying component '{componentName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CutComponentAsync(int actorIndex, string componentName, string componentData)
        {
            try
            {
                _logService?.Info($"Cutting component '{componentName}' from actor {actorIndex}");

                // First copy to clipboard
                bool copySuccess = await CopyComponentAsync(actorIndex, componentName, componentData);
                if (!copySuccess)
                {
                    return false;
                }

                // Then remove from actor
                bool removeSuccess = await RemoveComponentFromActorAsync(actorIndex, componentName);
                if (!removeSuccess)
                {
                    _logService?.Warning($"Component copied but failed to remove from actor {actorIndex}");
                }

                _logService?.Info($"Component '{componentName}' cut from actor {actorIndex}");
                return true;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error cutting component '{componentName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PasteComponentAsync(int actorIndex)
        {
            try
            {
                _logService?.Info($"Pasting component to actor {actorIndex}");

                if (!_engineIntegrationService.IsConnected)
                {
                    _logService?.Warning("Cannot paste component: engine not connected");
                    return false;
                }

                // Get data from clipboard
                string? clipboardData = await Task.Run(() =>
                {
                    try
                    {
                        var dataObject = System.Windows.Clipboard.GetDataObject();
                        return dataObject?.GetData(System.Windows.DataFormats.Text) as string;
                    }
                    catch
                    {
                        return null;
                    }
                });

                if (string.IsNullOrEmpty(clipboardData))
                {
                    _logService?.Warning("No data in clipboard to paste");
                    return false;
                }

                // Parse clipboard data (same format as Windows Forms version)
                if (clipboardData.StartsWith("Component<") && clipboardData.Contains(">"))
                {
                    int compNameBegin = "Component<".Length;
                    int compNameEnd = clipboardData.IndexOf(">", compNameBegin);
                    
                    if (compNameEnd > compNameBegin)
                    {
                        string componentName = clipboardData.Substring(compNameBegin, compNameEnd - compNameBegin);
                        string componentData = clipboardData.Substring(compNameEnd + 1);

                        // Add component to actor
                        bool addSuccess = await AddComponentToActorAsync(actorIndex, componentName);
                        if (addSuccess)
                        {
                            // Set component data
                            await _engineIntegrationService.SendSetActorComponentDataAsync(actorIndex, componentName, componentData);
                            
                            _logService?.Info($"Component '{componentName}' pasted to actor {actorIndex}");
                            return true;
                        }
                    }
                }

                _logService?.Warning("Invalid clipboard data format for component paste");
                return false;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error pasting component to actor {actorIndex}: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> GetComponentDataAsync(int actorIndex, string componentName)
        {
            try
            {
                _logService?.Info($"Getting component data for '{componentName}' from actor {actorIndex}");

                if (!_engineIntegrationService.IsConnected)
                {
                    _logService?.Warning("Cannot get component data: engine not connected");
                    return null;
                }

                // Request component data from engine
                await _engineIntegrationService.SendGetActorComponentDataAsync(actorIndex, componentName);

                // Note: The actual data will be received via the ActorComponentDataReceived event
                // This method just triggers the request
                return null;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error getting component data for '{componentName}': {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SetComponentDataAsync(int actorIndex, string componentName, string componentData)
        {
            try
            {
                _logService?.Info($"Setting component data for '{componentName}' on actor {actorIndex}");

                if (!_engineIntegrationService.IsConnected)
                {
                    _logService?.Warning("Cannot set component data: engine not connected");
                    return false;
                }

                // Send component data to engine
                await _engineIntegrationService.SendSetActorComponentDataAsync(actorIndex, componentName, componentData);

                _logService?.Info($"Component data set for '{componentName}' on actor {actorIndex}");
                return true;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error setting component data for '{componentName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RefreshComponentListAsync()
        {
            try
            {
                _logService?.Info("Refreshing component list from engine");

                if (!_engineIntegrationService.IsConnected)
                {
                    _logService?.Warning("Cannot refresh component list: engine not connected");
                    return false;
                }

                // Request fresh component list from engine
                await _engineIntegrationService.SendGetComponentListAsync();

                _logService?.Info("Component list refresh requested");
                return true;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error refreshing component list: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RefreshActorComponentListAsync(int actorIndex)
        {
            try
            {
                _logService?.Info($"Refreshing component list for actor {actorIndex}");

                if (!_engineIntegrationService.IsConnected)
                {
                    _logService?.Warning("Cannot refresh actor component list: engine not connected");
                    return false;
                }

                // Request fresh component list for actor from engine
                await _engineIntegrationService.SendGetActorComponentListAsync(actorIndex);

                _logService?.Info($"Actor component list refresh requested for actor {actorIndex}");
                return true;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error refreshing actor component list for actor {actorIndex}: {ex.Message}");
                return false;
            }
        }

        #region Private Methods

        private void SubscribeToEngineEvents()
        {
            _engineIntegrationService.ComponentListReceived += OnComponentListReceived;
            _engineIntegrationService.ActorComponentListReceived += OnActorComponentListReceived;
            _engineIntegrationService.ActorComponentDataReceived += OnActorComponentDataReceived;
        }

        private void UnsubscribeFromEngineEvents()
        {
            _engineIntegrationService.ComponentListReceived -= OnComponentListReceived;
            _engineIntegrationService.ActorComponentListReceived -= OnActorComponentListReceived;
            _engineIntegrationService.ActorComponentDataReceived -= OnActorComponentDataReceived;
        }

        private void OnComponentListReceived(object? sender, List<string> componentList)
        {
            lock (_lockObject)
            {
                _availableComponents.Clear();
                _availableComponents.AddRange(componentList);
            }

            _logService?.Info($"Available components updated: {componentList.Count} components");
            ComponentListUpdated?.Invoke(this, componentList);
        }

        private void OnActorComponentListReceived(object? sender, ActorComponentListEventArgs e)
        {
            lock (_lockObject)
            {
                _actorComponents[e.ActorIndex] = new List<string>(e.ComponentList);
            }

            _logService?.Info($"Actor {e.ActorIndex} component list updated: {e.ComponentList.Count} components");
            ActorComponentListUpdated?.Invoke(this, new ActorComponentListUpdatedEventArgs
            {
                ActorIndex = e.ActorIndex,
                ComponentList = e.ComponentList,
                Action = "updated"
            });
        }

        private void OnActorComponentDataReceived(object? sender, ActorComponentDataEventArgs e)
        {
            _logService?.Info($"Component data received for '{e.ComponentName}' on actor {e.ActorIndex}");
            // Component data is available in e.ComponentData
            // This could be used to update UI or cache component data
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            try
            {
                UnsubscribeFromEngineEvents();

                lock (_lockObject)
                {
                    _availableComponents.Clear();
                    _actorComponents.Clear();
                }

                _logService?.Info("ComponentManagementService disposed");
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error disposing ComponentManagementService: {ex.Message}");
            }
        }

        #endregion
    }
}
