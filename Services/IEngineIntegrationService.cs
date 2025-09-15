#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DANCustomTools.Services
{
    /// <summary>
    /// Service for integrating with the game engine
    /// </summary>
    public interface IEngineIntegrationService : IDisposable
    {
        /// <summary>
        /// Gets whether the service is connected to the engine
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the current session information
        /// </summary>
        string SessionPath { get; }

        /// <summary>
        /// Event fired when connection status changes
        /// </summary>
        event EventHandler<bool>? ConnectionStatusChanged;

        /// <summary>
        /// Event fired when session info is received
        /// </summary>
        event EventHandler<string>? SessionInfoReceived;

        /// <summary>
        /// Event fired when component list is received
        /// </summary>
        event EventHandler<List<string>>? ComponentListReceived;

        /// <summary>
        /// Event fired when actor list is received
        /// </summary>
        event EventHandler<List<EngineActorInfo>>? ActorListReceived;

        /// <summary>
        /// Event fired when actor component list is received
        /// </summary>
        event EventHandler<ActorComponentListEventArgs>? ActorComponentListReceived;

        /// <summary>
        /// Event fired when actor main data is received
        /// </summary>
        event EventHandler<ActorMainDataEventArgs>? ActorMainDataReceived;

        /// <summary>
        /// Event fired when actor component data is received
        /// </summary>
        event EventHandler<ActorComponentDataEventArgs>? ActorComponentDataReceived;

        /// <summary>
        /// Event fired when an error occurs
        /// </summary>
        event EventHandler<string>? ErrorReceived;

        /// <summary>
        /// Connects to the engine
        /// </summary>
        /// <param name="hostAddress">Engine host address</param>
        /// <param name="hostPort">Engine host port</param>
        /// <returns>True if connection successful</returns>
        Task<bool> ConnectAsync(string hostAddress = "127.0.0.1", int hostPort = 1001);

        /// <summary>
        /// Disconnects from the engine
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Sends a request to get session information
        /// </summary>
        Task SendGetSessionInfoAsync();

        /// <summary>
        /// Sends a request to get component list
        /// </summary>
        Task SendGetComponentListAsync();

        /// <summary>
        /// Sends a request to get actor list
        /// </summary>
        Task SendGetActorListAsync();

        /// <summary>
        /// Sends a request to get actor component list
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        Task SendGetActorComponentListAsync(int actorIndex);

        /// <summary>
        /// Sends a request to get actor main data
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        Task SendGetActorMainDataAsync(int actorIndex);

        /// <summary>
        /// Sends a request to get actor component data
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <param name="componentName">Name of the component</param>
        Task SendGetActorComponentDataAsync(int actorIndex, string componentName);

        /// <summary>
        /// Sends a request to create a new actor
        /// </summary>
        /// <param name="actorPath">Path for the new actor</param>
        /// <param name="componentList">List of components to add</param>
        Task SendNewActorAsync(string actorPath, List<string> componentList);

        /// <summary>
        /// Sends a request to set actor data
        /// </summary>
        /// <param name="actorPath">Path of the actor</param>
        /// <param name="actorIndex">Index of the actor</param>
        /// <param name="mainInfo">Whether to load main info</param>
        /// <param name="forceReload">Whether to force reload</param>
        Task SendSetActorAsync(string actorPath, int actorIndex, bool mainInfo = true, bool forceReload = false);

        /// <summary>
        /// Sends a request to add a component to an actor
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <param name="componentName">Name of the component to add</param>
        Task SendAddActorComponentAsync(int actorIndex, string componentName);

        /// <summary>
        /// Sends a request to remove a component from an actor
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <param name="componentName">Name of the component to remove</param>
        Task SendRemoveActorComponentAsync(int actorIndex, string componentName);

        /// <summary>
        /// Sends a request to save an actor
        /// </summary>
        /// <param name="actorIndex">Index of the actor to save</param>
        Task SendSaveActorAsync(int actorIndex);

        /// <summary>
        /// Sends a request to set actor component data
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <param name="componentName">Name of the component</param>
        /// <param name="componentData">XML data for the component</param>
        Task SendSetActorComponentDataAsync(int actorIndex, string componentName, string componentData);

        /// <summary>
        /// Sends a request to set actor main data
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <param name="mainData">XML data for the actor</param>
        /// <param name="parameters">Actor parameters</param>
        Task SendSetActorMainDataAsync(int actorIndex, string mainData, object parameters);
    }

    /// <summary>
    /// Information about an actor from the engine
    /// </summary>
    public class EngineActorInfo
    {
        public string UniqueId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Flags { get; set; }
        public List<string> ComponentList { get; set; } = new();
    }

    /// <summary>
    /// Event args for actor component list received
    /// </summary>
    public class ActorComponentListEventArgs : EventArgs
    {
        public int ActorIndex { get; set; }
        public List<string> ComponentList { get; set; } = new();
    }

    /// <summary>
    /// Event args for actor main data received
    /// </summary>
    public class ActorMainDataEventArgs : EventArgs
    {
        public int ActorIndex { get; set; }
        public string MainData { get; set; } = string.Empty;
        public object Parameters { get; set; } = new();
    }

    /// <summary>
    /// Event args for actor component data received
    /// </summary>
    public class ActorComponentDataEventArgs : EventArgs
    {
        public int ActorIndex { get; set; }
        public string ComponentName { get; set; } = string.Empty;
        public string ComponentData { get; set; } = string.Empty;
    }
}
