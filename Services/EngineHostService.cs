using PluginCommon;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace DANCustomTools.Services
{
	public class EngineHostService : IEngineHostService, IDisposable
	{
		private readonly ILogService _logService;
		private engineWrapper? _engine;
		private EngineSettings? _settings;
		private bool _connected;
		private readonly Dictionary<string, pluginWrapper> _plugins = new Dictionary<string, pluginWrapper>(StringComparer.Ordinal);

		public EngineHostService(ILogService logService)
		{
			_logService = logService;
		}

		public EngineSettings? Settings => _settings;
		public bool IsInitialized => _engine != null && _settings != null;
		public bool IsConnected => _connected;
		public object SyncRoot { get; } = new object();

		public void Initialize(string[] arguments)
		{
			lock (SyncRoot)
			{
				if (IsInitialized) return;

				var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
				culture.NumberFormat.NumberDecimalSeparator = ".";
				Thread.CurrentThread.CurrentCulture = culture;

				var cmdArgs = new CommandLineArgs(arguments);
				_settings = new EngineSettings(cmdArgs);
				_engine = new engineWrapper("");
				_logService.Info($"EngineHost initialized on port {_settings.Port}");
			}
		}

		public bool ConnectIfNeeded()
		{
			lock (SyncRoot)
			{
				if (_engine == null || _settings == null) return false;

				bool connected = _connected;
				if (connected)
				{
					var blob = new blobWrapper();
					connected = blob.sendToHost();
				}

				if (!connected)
				{
					_engine.disconnect();
					// Clear plugin cache when connection drops (like legacy SceneExplorer)
					_plugins.Clear();
					connected = _engine.connectToHost("127.0.0.1", _settings.Port);
					if (connected)
					{
						_logService.Info("EngineHost connected");
					}
				}

				_connected = connected;
				return _connected;
			}
		}

		public void Disconnect()
		{
			lock (SyncRoot)
			{
				if (_engine == null) return;
				_engine.disconnect();
				_connected = false;
				// Clear plugin cache to force new instances on reconnect (like legacy SceneExplorer)
				_plugins.Clear();
			}
		}

		public void Update()
		{
			lock (SyncRoot)
			{
				_engine?.update();
			}
		}

		public pluginWrapper RegisterPlugin(string pluginName)
		{
			lock (SyncRoot)
			{
				if (_engine == null) throw new InvalidOperationException("Engine not initialized");
				if (_plugins.TryGetValue(pluginName, out var existing))
				{
					return existing;
				}
				var plugin = new pluginWrapper();
				_engine.addPlugin(pluginName, plugin);
				_plugins[pluginName] = plugin;
				return plugin;
			}
		}

		public void Dispose()
		{
			Disconnect();
		}
	}
}


