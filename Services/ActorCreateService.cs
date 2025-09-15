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
        private readonly IEngineHostService _engineHostService;
        private readonly ILogService? _logService;

        private readonly List<ActorInfo> _actors = new();
        private readonly List<string> _components = new();
        private bool _isConnected = false;
        private bool _disposed = false;

        public ActorCreateService(IEngineHostService engineHostService, ILogService? logService = null)
        {
            _engineHostService = engineHostService ?? throw new ArgumentNullException(nameof(engineHostService));
            _logService = logService;

            // Initialize with some default components
            InitializeDefaultComponents();

            _logService?.Info("ActorCreateService initialized");
        }

        #region Properties

        public bool IsConnected => _isConnected && _engineHostService.IsConnected;

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
                _isConnected = _engineHostService.IsConnected;

                if (!_isConnected)
                {
                    _logService?.Warning("Engine not connected, using mock data");
                    LoadMockData();
                    return;
                }

                // TODO: Implement actual engine integration
                // For now, simulate engine communication
                await Task.Delay(300);

                // Mock data for development
                LoadMockData();

                _logService?.Info($"Refreshed {_actors.Count} actors");
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

                // TODO: Implement actual actor creation via engine
                await Task.Delay(500);

                // Add to local collection
                if (!_actors.Any(a => a.Name.Equals(actorName, StringComparison.OrdinalIgnoreCase)))
                {
                    var newActor = new ActorInfo
                    {
                        Name = actorName,
                        UnicId = Guid.NewGuid().ToString(),
                        Parameters = new ActorInfoParams()
                    };
                    newActor.Parameters.SetDefaultPaths(actorName);

                    _actors.Add(newActor);
                    _logService?.Info($"Actor '{actorName}' created successfully");
                    return true;
                }

                _logService?.Warning($"Actor '{actorName}' already exists");
                return false;
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

                // TODO: Implement actual actor saving via engine
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

        public async Task<bool> ConnectAsync()
        {
            ThrowIfDisposed();

            try
            {
                _logService?.Info("Connecting to engine...");

                // Use engine host service connection
                // TODO: Additional ActorCreate-specific connection logic if needed
                _isConnected = _engineHostService.IsConnected;

                if (!_isConnected)
                {
                    // Try to establish connection through engine host
                    await Task.Delay(100);
                    _isConnected = _engineHostService.IsConnected;
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

                // Cleanup resources
                _actors.Clear();
                _components.Clear();
                _isConnected = false;

                _disposed = true;
            }
        }

        #endregion
    }
}
