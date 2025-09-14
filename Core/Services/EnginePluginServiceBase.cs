#nullable enable
using DANCustomTools.Services;
using PluginCommon;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace DANCustomTools.Core.Services
{
    public abstract class EnginePluginServiceBase : IDisposable
    {
        protected readonly ILogService LogService;
        protected readonly IEngineHostService EngineHost;
        protected readonly object ConnectionLock = new();

        private pluginWrapper? _plugin;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isConnected;

        public abstract string PluginName { get; }
        public event EventHandler<bool>? ConnectionStatusChanged;

        public bool IsConnected
        {
            get
            {
                lock (ConnectionLock)
                {
                    return _isConnected;
                }
            }
        }

        public bool IsRunning => _cancellationTokenSource != null;

        protected pluginWrapper? Plugin => _plugin;

        /// <summary>
        /// Forces a connection attempt. Useful when the service is already running but needs to reconnect.
        /// </summary>
        public virtual void ForceConnectionAttempt()
        {
            if (!IsRunning)
            {
                LogService.Warning($"Cannot force connection for {PluginName} - service is not running");
                return;
            }

            LogService.Info($"Forcing connection attempt for {PluginName}");

            // Reset connection state to trigger a fresh connection attempt
            lock (ConnectionLock)
            {
                if (_isConnected)
                {
                    EngineHost.Disconnect();
                    _plugin = null;
                    _isConnected = false;
                }
            }
        }

        protected EnginePluginServiceBase(ILogService logService, IEngineHostService engineHost)
        {
            LogService = logService ?? throw new ArgumentNullException(nameof(logService));
            EngineHost = engineHost ?? throw new ArgumentNullException(nameof(engineHost));
        }

        public virtual async Task StartAsync(string[] arguments, CancellationToken cancellationToken = default)
        {
            if (_cancellationTokenSource != null)
            {
                LogService.Info($"{PluginName} service is already started, ensuring proper initialization");

                // Service is running, but ensure engine host is initialized with current arguments
                try
                {
                    if (!EngineHost.IsInitialized)
                    {
                        LogService.Info($"Initializing engine host for already-running {PluginName} service");
                        EngineHost.Initialize(arguments);
                    }

                    // Call service-specific initialization if needed
                    await OnServiceInitializingAsync();
                }
                catch (Exception ex)
                {
                    LogService.Error($"Failed to reinitialize {PluginName} service", ex);
                    throw;
                }

                return; // Already started, but now properly initialized
            }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                EngineHost.Initialize(arguments);

                // Set culture for decimal separator consistency
                SetCultureForDecimalSeparator();

                LogService.Info($"Starting {PluginName} service on port {EngineHost.Settings?.Port}");

                // Initialize service-specific components
                await OnServiceInitializingAsync();

                // Start background network thread
                await Task.Run(() => NetworkThreadLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                LogService.Error($"Failed to start {PluginName} service", ex);
                throw;
            }
        }

        public virtual Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_cancellationTokenSource == null)
                return Task.CompletedTask;

            LogService.Info($"Stopping {PluginName} service");

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null; // Reset to allow restarting

            if (_isConnected)
            {
                EngineHost.Disconnect();
                lock (ConnectionLock)
                {
                    _isConnected = false;
                }
            }

            _plugin = null;
            ConnectionStatusChanged?.Invoke(this, false);

            // Allow derived classes to clean up
            OnServiceStopping();

            return Task.CompletedTask;
        }

        protected virtual void NetworkThreadLoop(CancellationToken cancellationToken)
        {
            SetCultureForDecimalSeparator();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var wasConnected = _isConnected;
                    UpdateConnectionStatus();

                    if (_isConnected && _plugin != null)
                    {
                        EngineHost.Update();

                        // Process incoming messages
                        ProcessIncomingMessages();

                        // Handle first-time connection
                        if (!wasConnected)
                        {
                            OnFirstTimeConnected();
                        }

                        // Process service-specific queued requests
                        ProcessQueuedRequests();
                    }

                    Thread.Sleep(GetNetworkLoopSleepInterval());
                }
                catch (Exception ex)
                {
                    LogService.Error($"Error in {PluginName} network thread", ex);
                    Thread.Sleep(1000); // Back off on error
                }
            }
        }

        protected virtual void UpdateConnectionStatus()
        {
            if (!EngineHost.IsInitialized)
                return;

            var wasConnected = _isConnected;
            var isCurrentlyConnected = _isConnected;

            // Test existing connection
            if (isCurrentlyConnected)
            {
                isCurrentlyConnected = EngineHost.ConnectIfNeeded();
            }

            // Try to connect if not connected
            if (!isCurrentlyConnected)
            {
                if (wasConnected)
                {
                    EngineHost.Disconnect();
                    _plugin = null;
                }

                // Attempt to connect
                isCurrentlyConnected = EngineHost.ConnectIfNeeded();

                if (isCurrentlyConnected)
                {
                    _plugin = EngineHost.RegisterPlugin(PluginName);
                    SendPluginRegistrationMessage();
                    OnPluginRegistered();
                }
            }

            // Update connection status if changed
            if (isCurrentlyConnected != wasConnected)
            {
                lock (ConnectionLock)
                {
                    _isConnected = isCurrentlyConnected;
                }

                ConnectionStatusChanged?.Invoke(this, _isConnected);
                LogService.Info($"{PluginName} connection status: {(_isConnected ? "Connected" : "Disconnected")}");
            }
        }

        protected virtual void ProcessIncomingMessages()
        {
            if (_plugin == null)
                return;

            var blob = new blobWrapper();
            while (_plugin.dispatch(blob))
            {
                try
                {
                    ProcessMessage(blob);
                }
                catch (Exception ex)
                {
                    LogService.Error($"Failed to process {PluginName} message", ex);
                }
            }
        }

        protected virtual void SendPluginRegistrationMessage()
        {
            if (!EngineHost.IsInitialized) return;

            try
            {
                var blob = new blobWrapper();
                blob.push("PluginId");
                blob.push(PluginName);
                blob.sendToHost();
            }
            catch (Exception ex)
            {
                LogService.Error($"Failed to send plugin registration for {PluginName}", ex);
            }
        }

        protected void SendMessage(Action<blobWrapper> buildMessage)
        {
            if (!_isConnected || _plugin == null)
                return;

            try
            {
                var blob = new blobWrapper();
                blob.push(PluginName);
                buildMessage(blob);
                blob.sendToHost();
            }
            catch (Exception ex)
            {
                LogService.Error($"Failed to send message from {PluginName}", ex);
            }
        }

        private static void SetCultureForDecimalSeparator()
        {
            var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = culture;
        }

        // Abstract and virtual methods for derived classes to override

        /// <summary>
        /// Process incoming messages from the engine. Override to handle plugin-specific messages.
        /// </summary>
        protected abstract void ProcessMessage(blobWrapper blob);

        /// <summary>
        /// Process any queued requests that need to be sent to the engine. Override if your service uses queues.
        /// </summary>
        protected virtual void ProcessQueuedRequests() { }

        /// <summary>
        /// Called when the plugin is first registered with the engine. Override for initialization tasks.
        /// </summary>
        protected virtual void OnPluginRegistered() { }

        /// <summary>
        /// Called on the first successful connection. Override to send initial requests.
        /// </summary>
        protected virtual void OnFirstTimeConnected() { }

        /// <summary>
        /// Called during service initialization, before starting the network thread. Override for setup tasks.
        /// </summary>
        protected virtual Task OnServiceInitializingAsync() => Task.CompletedTask;

        /// <summary>
        /// Called during service stopping. Override for cleanup tasks.
        /// </summary>
        protected virtual void OnServiceStopping() { }

        /// <summary>
        /// Override to customize the sleep interval in the network loop. Default is 100ms.
        /// </summary>
        protected virtual int GetNetworkLoopSleepInterval() => 100;

        public virtual void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
            _cancellationTokenSource?.Dispose();
        }
    }
}