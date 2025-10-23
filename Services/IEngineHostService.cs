#nullable enable
using PluginCommon;
using System;

namespace DANCustomTools.Services
{
	public interface IEngineHostService
	{
		EngineSettings? Settings { get; }
		bool IsInitialized { get; }
		bool IsConnected { get; }
		object SyncRoot { get; }

		void Initialize(string[] arguments);
		bool ConnectIfNeeded();
		void Disconnect();
		void Update();
		pluginWrapper RegisterPlugin(string pluginName);
	}
}



