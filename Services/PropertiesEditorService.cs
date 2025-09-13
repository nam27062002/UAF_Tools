#nullable enable
using DANCustomTools.Models.PropertiesEditor;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace DANCustomTools.Services
{
    public class PropertiesEditorService : IPropertiesEditorService, IDisposable
    {
        private readonly ILogService _logService;
        private readonly IEngineHostService _engineHost;
        private pluginWrapper? _plugin;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isConnected;
        private string _dataPath = string.Empty;
        private readonly object _connectionLock = new();

        // Plugin messages
        private const string PLUGIN_NAME = "PropertiesEditor_Plugin";
        private const string MSG_PROPERTIES = "Properties";
        private const string MSG_DUMP_TO_FILE = "DumpToFile";
        private const string MSG_CLEAR = "Clear";
        private const string MSG_GET_SESSION_INFO = "getSessionInfo";
        private const string PLUGIN_ID = "PluginId";
        private const uint INVALID_OBJREF = 0;

        public event EventHandler<PropertyModel>? PropertiesUpdated;
        public event EventHandler<string>? DataPathUpdated;

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

        public string DataPath => _dataPath;

        public PropertiesEditorService(ILogService logService, IEngineHostService engineHost)
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

                _logService.Info($"Starting PropertiesEditor service on port {_engineHost.Settings?.Port}");

                // Start background network thread
                await Task.Run(() => NetworkThreadLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to start PropertiesEditor service", ex);
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_cancellationTokenSource == null)
                return Task.CompletedTask;

            _logService.Info("Stopping PropertiesEditor service");

            _cancellationTokenSource.Cancel();

            if (_isConnected)
            {
                _engineHost.Disconnect();
                lock (_connectionLock)
                {
                    _isConnected = false;
                }
            }

            return Task.CompletedTask;
        }

        public void RequestObjectProperties(uint objectRef)
        {
            // Properties are automatically sent when object is selected in SceneExplorer
            // This method can be used for explicit requests if needed
            _logService.Info($"Requesting properties for object ref: {objectRef}");
        }

        public void SendPropertiesUpdate(uint objectRef, string xmlData)
        {
            if (!_isConnected || objectRef == INVALID_OBJREF)
                return;

            try
            {
                var blob = new blobWrapper();
                blob.push(PLUGIN_NAME);
                blob.push(MSG_PROPERTIES);
                blob.push(objectRef);
                blob.push(xmlData);
                blob.sendToHost();

                _logService.Info($"Sent property update for object ref: {objectRef}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Failed to send properties update", ex);
            }
        }

        public void ClearProperties()
        {
            var propertyModel = new PropertyModel();
            propertyModel.Clear();
            PropertiesUpdated?.Invoke(this, propertyModel);
        }

        public void DumpToFile(string fileName)
        {
            if (!_isConnected)
                return;

            try
            {
                var blob = new blobWrapper();
                blob.push(PLUGIN_NAME);
                blob.push(MSG_DUMP_TO_FILE);
                blob.push(fileName);
                blob.sendToHost();

                _logService.Info($"Requested dump to file: {fileName}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Failed to send dump to file request", ex);
            }
        }

        private void NetworkThreadLoop(CancellationToken cancellationToken)
        {
            var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = culture;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UpdateConnectionStatus();

                    if (_isConnected && _plugin != null)
                    {
                        _engineHost.Update();
                        var blob = new blobWrapper();
                        while (_plugin.dispatch(blob))
                        {
                            ProcessMessage(blob);
                        }
                    }

                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    _logService.Error("Error in PropertiesEditor network thread", ex);
                    Thread.Sleep(1000);
                }
            }
        }

        private void UpdateConnectionStatus()
        {
            bool connected = _isConnected;

            // Test connection
            if (connected)
            {
                connected = _engineHost.ConnectIfNeeded();
            }

            // Try to connect if not connected
            if (!connected)
            {
                if (_isConnected)
                {
                    _engineHost.Disconnect();
                    _plugin = null;
                }

                if (_engineHost.IsInitialized)
                {
                    connected = _engineHost.ConnectIfNeeded();

                    if (connected)
                    {
                        _plugin = _engineHost.RegisterPlugin(PLUGIN_NAME);
                        SendConnectedMessage();
                        SendGetSessionInfo();
                    }
                }
            }

            // Update connection status if changed
            if (connected != _isConnected)
            {
                lock (_connectionLock)
                {
                    _isConnected = connected;
                }
                _logService.Info($"PropertiesEditor connection status: {(connected ? "Connected" : "Disconnected")}");
            }
        }

        private void ProcessMessage(blobWrapper blob)
        {
            try
            {
                string message = "";
                blob.extract(ref message);

                switch (message)
                {
                    case MSG_PROPERTIES:
                        ProcessPropertiesMessage(blob);
                        break;
                    case MSG_CLEAR:
                        ProcessClearMessage();
                        break;
                    case MSG_GET_SESSION_INFO:
                        ProcessSessionInfoMessage(blob);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to process PropertiesEditor message", ex);
            }
        }

        private void ProcessPropertiesMessage(blobWrapper blob)
        {
            uint objectRef = INVALID_OBJREF;
            string xmlData = "";

            blob.extract(ref objectRef);
            blob.extractString8(ref xmlData);

            var propertyModel = new PropertyModel
            {
                ObjectRef = objectRef,
                XmlData = xmlData,
                IsConnected = _isConnected
            };

            PropertiesUpdated?.Invoke(this, propertyModel);
            _logService.Info($"Received properties for object ref: {objectRef}");
        }

        private void ProcessClearMessage()
        {
            ClearProperties();
            _logService.Info("Cleared properties");
        }

        private void ProcessSessionInfoMessage(blobWrapper blob)
        {
            string dataPath = "";
            blob.extract(ref dataPath);

            _dataPath = dataPath;
            DataPathUpdated?.Invoke(this, dataPath);
            _logService.Info($"Received data path: {dataPath}");
        }

        private void SendConnectedMessage()
        {
            if (!_engineHost.IsInitialized) return;

            try
            {
                var blob = new blobWrapper();
                blob.push(PLUGIN_ID);
                blob.push(PLUGIN_NAME);
                blob.sendToHost();
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to send connected message", ex);
            }
        }

        private void SendGetSessionInfo()
        {
            if (!_engineHost.IsInitialized) return;

            try
            {
                var blob = new blobWrapper();
                blob.push(PLUGIN_NAME);
                blob.push(MSG_GET_SESSION_INFO);
                blob.sendToHost();
            }
            catch (Exception ex)
            {
                _logService.Error("Failed to send get session info", ex);
            }
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
            _cancellationTokenSource?.Dispose();
        }
    }
}
