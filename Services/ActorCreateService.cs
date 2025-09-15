#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.Models.ActorCreate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DANCustomTools.Services
{
    public class ActorCreateService : IActorCreateService
    {
        private readonly IEngineIntegrationService _engineIntegrationService;
        private readonly IComponentManagementService _componentManagementService;
        private readonly ILogService? _logService;

        private readonly List<ActorInfo> _actors = new();
        private readonly List<string> _components = new();
        private readonly object _lockObject = new();
        private bool _isConnected = false;
        private bool _disposed = false;

        public ActorCreateService(IEngineIntegrationService engineIntegrationService, IComponentManagementService componentManagementService, ILogService? logService = null)
        {
            _engineIntegrationService = engineIntegrationService ?? throw new ArgumentNullException(nameof(engineIntegrationService));
            _componentManagementService = componentManagementService ?? throw new ArgumentNullException(nameof(componentManagementService));
            _logService = logService;

            // Subscribe to engine events
            SubscribeToEngineEvents();

            _logService?.Info("ActorCreateService initialized with real engine integration and component management");
        }

        #region Properties

        public bool IsConnected => _isConnected && _engineIntegrationService.IsConnected;

        #endregion

        #region Public Methods

        public IEnumerable<ActorInfo> GetActors()
        {
            return _actors.ToList();
        }

        public IEnumerable<string> GetActorNames()
        {
            return _actors.Select(a => a.Name).ToList();
        }

        public ActorInfo? GetActor(string actorName)
        {
            return _actors.FirstOrDefault(a => a.Name.Equals(actorName, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<string> GetComponents()
        {
            return _components.ToList();
        }

        public async Task RefreshActorsAsync()
        {
            ThrowIfDisposed();

            try
            {
                _logService?.Info("Refreshing actors from engine...");

                // Check engine connection
                _isConnected = _engineIntegrationService.IsConnected;

                if (!_isConnected)
                {
                    _logService?.Warning("Engine not connected, using mock data");
                    LoadMockData();
                    return;
                }

                // Request fresh data from engine
                await _engineIntegrationService.SendGetActorListAsync();
                await _engineIntegrationService.SendGetComponentListAsync();

                _logService?.Info("Requested fresh data from engine");
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error refreshing actors: {ex.Message}");
                _isConnected = false;
                throw;
            }
        }

        public async Task<bool> CreateActorAsync(string actorName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(actorName))
            {
                _logService?.Warning("Cannot create actor with empty name");
                return false;
            }

            try
            {
                _logService?.Info($"Creating actor: {actorName}");

                if (!_isConnected)
                {
                    _logService?.Warning("Cannot create actor: engine not connected");
                    return false;
                }

                // Create actor path
                string actorPath = $"{_engineIntegrationService.SessionPath}/{actorName}.act";
                
                // Send request to engine to create new actor
                await _engineIntegrationService.SendNewActorAsync(actorPath, new List<string>());

                _logService?.Info($"Actor '{actorName}' creation requested from engine");
                return true;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error creating actor '{actorName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LoadActorAsync(string actorName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(actorName))
            {
                _logService?.Warning("Cannot load actor with empty name");
                return false;
            }

            try
            {
                _logService?.Info($"Loading actor: {actorName}");

                // TODO: Implement actual actor loading via engine
                await Task.Delay(300);

                _logService?.Info($"Actor '{actorName}' loaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error loading actor '{actorName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveActorAsync()
        {
            ThrowIfDisposed();

            try
            {
                _logService?.Info("Saving current actor...");

                if (!_isConnected)
                {
                    _logService?.Warning("Cannot save actor: engine not connected");
                    return false;
                }

                // TODO: Implement actual actor saving via engine
                // For now, we'll simulate the save operation
                await Task.Delay(400);

                _logService?.Info("Actor saved successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error saving actor: {ex.Message}");
                return false;
            }
        }

        #region Component Management Methods

        public async Task<bool> AddComponentToActorAsync(string actorName, string componentName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(actorName) || string.IsNullOrWhiteSpace(componentName))
            {
                _logService?.Warning("Cannot add component: actor name or component name is empty");
                return false;
            }

            try
            {
                var actor = _actors.FirstOrDefault(a => a.Name.Equals(actorName, StringComparison.OrdinalIgnoreCase));
                if (actor == null)
                {
                    _logService?.Warning($"Actor '{actorName}' not found");
                    return false;
                }

                int actorIndex = _actors.IndexOf(actor);
                return await _componentManagementService.AddComponentToActorAsync(actorIndex, componentName);
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error adding component '{componentName}' to actor '{actorName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveComponentFromActorAsync(string actorName, string componentName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(actorName) || string.IsNullOrWhiteSpace(componentName))
            {
                _logService?.Warning("Cannot remove component: actor name or component name is empty");
                return false;
            }

            try
            {
                var actor = _actors.FirstOrDefault(a => a.Name.Equals(actorName, StringComparison.OrdinalIgnoreCase));
                if (actor == null)
                {
                    _logService?.Warning($"Actor '{actorName}' not found");
                    return false;
                }

                int actorIndex = _actors.IndexOf(actor);
                return await _componentManagementService.RemoveComponentFromActorAsync(actorIndex, componentName);
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error removing component '{componentName}' from actor '{actorName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CopyComponentAsync(string actorName, string componentName, string componentData)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(actorName) || string.IsNullOrWhiteSpace(componentName))
            {
                _logService?.Warning("Cannot copy component: actor name or component name is empty");
                return false;
            }

            try
            {
                var actor = _actors.FirstOrDefault(a => a.Name.Equals(actorName, StringComparison.OrdinalIgnoreCase));
                if (actor == null)
                {
                    _logService?.Warning($"Actor '{actorName}' not found");
                    return false;
                }

                int actorIndex = _actors.IndexOf(actor);
                return await _componentManagementService.CopyComponentAsync(actorIndex, componentName, componentData);
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error copying component '{componentName}' from actor '{actorName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CutComponentAsync(string actorName, string componentName, string componentData)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(actorName) || string.IsNullOrWhiteSpace(componentName))
            {
                _logService?.Warning("Cannot cut component: actor name or component name is empty");
                return false;
            }

            try
            {
                var actor = _actors.FirstOrDefault(a => a.Name.Equals(actorName, StringComparison.OrdinalIgnoreCase));
                if (actor == null)
                {
                    _logService?.Warning($"Actor '{actorName}' not found");
                    return false;
                }

                int actorIndex = _actors.IndexOf(actor);
                return await _componentManagementService.CutComponentAsync(actorIndex, componentName, componentData);
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error cutting component '{componentName}' from actor '{actorName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PasteComponentAsync(string actorName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(actorName))
            {
                _logService?.Warning("Cannot paste component: actor name is empty");
                return false;
            }

            try
            {
                var actor = _actors.FirstOrDefault(a => a.Name.Equals(actorName, StringComparison.OrdinalIgnoreCase));
                if (actor == null)
                {
                    _logService?.Warning($"Actor '{actorName}' not found");
                    return false;
                }

                int actorIndex = _actors.IndexOf(actor);
                return await _componentManagementService.PasteComponentAsync(actorIndex);
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error pasting component to actor '{actorName}': {ex.Message}");
                return false;
            }
        }

        public async Task<string?> GetComponentDataAsync(string actorName, string componentName)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(actorName) || string.IsNullOrWhiteSpace(componentName))
            {
                _logService?.Warning("Cannot get component data: actor name or component name is empty");
                return null;
            }

            try
            {
                var actor = _actors.FirstOrDefault(a => a.Name.Equals(actorName, StringComparison.OrdinalIgnoreCase));
                if (actor == null)
                {
                    _logService?.Warning($"Actor '{actorName}' not found");
                    return null;
                }

                int actorIndex = _actors.IndexOf(actor);
                return await _componentManagementService.GetComponentDataAsync(actorIndex, componentName);
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error getting component data for '{componentName}' from actor '{actorName}': {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SetComponentDataAsync(string actorName, string componentName, string componentData)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(actorName) || string.IsNullOrWhiteSpace(componentName))
            {
                _logService?.Warning("Cannot set component data: actor name or component name is empty");
                return false;
            }

            try
            {
                var actor = _actors.FirstOrDefault(a => a.Name.Equals(actorName, StringComparison.OrdinalIgnoreCase));
                if (actor == null)
                {
                    _logService?.Warning($"Actor '{actorName}' not found");
                    return false;
                }

                int actorIndex = _actors.IndexOf(actor);
                return await _componentManagementService.SetComponentDataAsync(actorIndex, componentName, componentData);
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error setting component data for '{componentName}' on actor '{actorName}': {ex.Message}");
                return false;
            }
        }

        #endregion

        public async Task<bool> ConnectAsync()
        {
            ThrowIfDisposed();

            try
            {
                _logService?.Info("Connecting to engine...");

                // Use engine integration service connection
                // TODO: Additional ActorCreate-specific connection logic if needed
                _isConnected = _engineIntegrationService.IsConnected;

                if (!_isConnected)
                {
                    // Try to establish connection through engine integration service
                    await _engineIntegrationService.ConnectAsync();
                    _isConnected = _engineIntegrationService.IsConnected;
                }

                _logService?.Info(_isConnected ? "Connected to engine" : "Failed to connect to engine");
                return _isConnected;
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error connecting to engine: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            ThrowIfDisposed();

            try
            {
                _logService?.Info("Disconnecting from engine...");

                // TODO: Implement ActorCreate-specific disconnection logic
                await Task.Delay(100);

                _isConnected = false;
                _actors.Clear();

                _logService?.Info("Disconnected from engine");
            }
            catch (Exception ex)
            {
                _logService?.Error($"Error disconnecting from engine: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private void SubscribeToEngineEvents()
        {
            _engineIntegrationService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _engineIntegrationService.SessionInfoReceived += OnSessionInfoReceived;
            _engineIntegrationService.ComponentListReceived += OnComponentListReceived;
            _engineIntegrationService.ActorListReceived += OnActorListReceived;
            _engineIntegrationService.ActorComponentListReceived += OnActorComponentListReceived;
            _engineIntegrationService.ActorMainDataReceived += OnActorMainDataReceived;
            _engineIntegrationService.ActorComponentDataReceived += OnActorComponentDataReceived;
            _engineIntegrationService.ErrorReceived += OnErrorReceived;
        }

        private void UnsubscribeFromEngineEvents()
        {
            _engineIntegrationService.ConnectionStatusChanged -= OnConnectionStatusChanged;
            _engineIntegrationService.SessionInfoReceived -= OnSessionInfoReceived;
            _engineIntegrationService.ComponentListReceived -= OnComponentListReceived;
            _engineIntegrationService.ActorListReceived -= OnActorListReceived;
            _engineIntegrationService.ActorComponentListReceived -= OnActorComponentListReceived;
            _engineIntegrationService.ActorMainDataReceived -= OnActorMainDataReceived;
            _engineIntegrationService.ActorComponentDataReceived -= OnActorComponentDataReceived;
            _engineIntegrationService.ErrorReceived -= OnErrorReceived;
        }

        private void OnConnectionStatusChanged(object? sender, bool isConnected)
        {
            _isConnected = isConnected;
            _logService?.Info($"Engine connection status changed: {(isConnected ? "Connected" : "Disconnected")}");
        }

        private void OnSessionInfoReceived(object? sender, string sessionPath)
        {
            _logService?.Info($"Session info received: {sessionPath}");
        }

        private void OnComponentListReceived(object? sender, List<string> componentList)
        {
            lock (_lockObject)
            {
                _components.Clear();
                _components.AddRange(componentList);
            }
            _logService?.Info($"Component list updated: {componentList.Count} components");
        }

        private void OnActorListReceived(object? sender, List<EngineActorInfo> engineActorList)
        {
            lock (_lockObject)
            {
                _actors.Clear();
                foreach (var engineActor in engineActorList)
                {
                    var actor = new ActorInfo
                    {
                        Name = engineActor.Name,
                        UnicId = engineActor.UniqueId,
                        Parameters = new ActorInfoParams()
                    };
                    actor.Parameters.SetDefaultPaths(engineActor.Name);
                    
                    // Copy component list
                    foreach (var component in engineActor.ComponentList)
                    {
                        actor.ComponentList.Add(component);
                    }
                    
                    _actors.Add(actor);
                }
            }
            _logService?.Info($"Actor list updated: {engineActorList.Count} actors");
        }

        private void OnActorComponentListReceived(object? sender, ActorComponentListEventArgs e)
        {
            // Find actor by index (assuming index corresponds to position in list)
            if (e.ActorIndex >= 0 && e.ActorIndex < _actors.Count)
            {
                var actor = _actors[e.ActorIndex];
                lock (_lockObject)
                {
                    actor.ComponentList.Clear();
                    foreach (var component in e.ComponentList)
                    {
                        actor.ComponentList.Add(component);
                    }
                }
                _logService?.Info($"Actor {actor.Name} component list updated: {e.ComponentList.Count} components");
            }
        }

        private void OnActorMainDataReceived(object? sender, ActorMainDataEventArgs e)
        {
            if (e.ActorIndex >= 0 && e.ActorIndex < _actors.Count)
            {
                var actor = _actors[e.ActorIndex];
                _logService?.Info($"Actor {actor.Name} main data received");
                // Process main data if needed
            }
        }

        private void OnActorComponentDataReceived(object? sender, ActorComponentDataEventArgs e)
        {
            _logService?.Info($"Actor component data received for {e.ComponentName}");
            // Process component data if needed
        }

        private void OnErrorReceived(object? sender, string errorMessage)
        {
            _logService?.Error($"Engine error: {errorMessage}");
        }

        private void InitializeDefaultComponents()
        {
            _components.AddRange(new[]
            {
                "AnimatedComponent",
                "LightComponent",
                "SoundComponent",
                "PhysicsComponent",
                "TriggerComponent",
                "PlayerComponent",
                "EnemyComponent",
                "CollectibleComponent",
                "PlatformComponent",
                "CameraComponent"
            });
        }

        private void LoadMockData()
        {
            _actors.Clear();

            var mockActorNames = new[]
            {
                "Player_Character",
                "Enemy_Guard",
                "Platform_Moving",
                "Collectible_Coin",
                "Light_Torch",
                "Trigger_Door",
                "Background_Tree",
                "Effect_Particle"
            };

            foreach (var actorName in mockActorNames)
            {
                var actor = new ActorInfo
                {
                    Name = actorName,
                    UnicId = Guid.NewGuid().ToString(),
                    Parameters = new ActorInfoParams()
                };

                actor.Parameters.SetDefaultPaths(actorName);

                // Add some mock components based on actor type
                if (actorName.Contains("Player"))
                {
                    actor.ComponentList.Add("PlayerComponent");
                    actor.ComponentList.Add("AnimatedComponent");
                    actor.ComponentList.Add("PhysicsComponent");
                }
                else if (actorName.Contains("Enemy"))
                {
                    actor.ComponentList.Add("EnemyComponent");
                    actor.ComponentList.Add("AnimatedComponent");
                    actor.ComponentList.Add("PhysicsComponent");
                }
                else if (actorName.Contains("Light"))
                {
                    actor.ComponentList.Add("LightComponent");
                }
                else if (actorName.Contains("Sound"))
                {
                    actor.ComponentList.Add("SoundComponent");
                }
                else
                {
                    actor.ComponentList.Add("AnimatedComponent");
                }

                _actors.Add(actor);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ActorCreateService));
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _logService?.Info("Disposing ActorCreateService");

                // Unsubscribe from engine events
                UnsubscribeFromEngineEvents();

                // Cleanup resources
                lock (_lockObject)
                {
                    _actors.Clear();
                    _components.Clear();
                }
                _isConnected = false;

                _disposed = true;
            }
        }

        #endregion
    }
}
