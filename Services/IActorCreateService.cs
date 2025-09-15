#nullable enable
using DANCustomTools.Models.ActorCreate;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DANCustomTools.Services
{
    public interface IActorCreateService : IDisposable
    {
        /// <summary>
        /// Gets whether the service is connected to the engine
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the current list of actors
        /// </summary>
        /// <returns>Collection of ActorInfo objects</returns>
        IEnumerable<ActorInfo> GetActors();

        /// <summary>
        /// Gets the current list of actor names (for simple UI binding)
        /// </summary>
        /// <returns>Collection of actor names</returns>
        IEnumerable<string> GetActorNames();

        /// <summary>
        /// Gets a specific actor by name
        /// </summary>
        /// <param name="actorName">Name of the actor</param>
        /// <returns>ActorInfo if found, null otherwise</returns>
        ActorInfo? GetActor(string actorName);

        /// <summary>
        /// Gets the current list of available components
        /// </summary>
        /// <returns>Collection of component names</returns>
        IEnumerable<string> GetComponents();

        /// <summary>
        /// Refreshes the actor list from the engine
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task RefreshActorsAsync();

        /// <summary>
        /// Creates a new actor with the specified name
        /// </summary>
        /// <param name="actorName">Name of the actor to create</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> CreateActorAsync(string actorName);

        /// <summary>
        /// Loads an existing actor
        /// </summary>
        /// <param name="actorName">Name of the actor to load</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> LoadActorAsync(string actorName);

        /// <summary>
        /// Saves the current actor
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveActorAsync();

        #region Component Management

        /// <summary>
        /// Adds a component to an actor
        /// </summary>
        /// <param name="actorName">Name of the actor</param>
        /// <param name="componentName">Name of the component to add</param>
        /// <returns>True if successful</returns>
        Task<bool> AddComponentToActorAsync(string actorName, string componentName);

        /// <summary>
        /// Removes a component from an actor
        /// </summary>
        /// <param name="actorName">Name of the actor</param>
        /// <param name="componentName">Name of the component to remove</param>
        /// <returns>True if successful</returns>
        Task<bool> RemoveComponentFromActorAsync(string actorName, string componentName);

        /// <summary>
        /// Copies a component to clipboard
        /// </summary>
        /// <param name="actorName">Name of the actor</param>
        /// <param name="componentName">Name of the component to copy</param>
        /// <param name="componentData">XML data of the component</param>
        /// <returns>True if successful</returns>
        Task<bool> CopyComponentAsync(string actorName, string componentName, string componentData);

        /// <summary>
        /// Cuts a component (copies and removes)
        /// </summary>
        /// <param name="actorName">Name of the actor</param>
        /// <param name="componentName">Name of the component to cut</param>
        /// <param name="componentData">XML data of the component</param>
        /// <returns>True if successful</returns>
        Task<bool> CutComponentAsync(string actorName, string componentName, string componentData);

        /// <summary>
        /// Pastes a component from clipboard
        /// </summary>
        /// <param name="actorName">Name of the actor to paste to</param>
        /// <returns>True if successful</returns>
        Task<bool> PasteComponentAsync(string actorName);

        /// <summary>
        /// Gets component data for an actor
        /// </summary>
        /// <param name="actorName">Name of the actor</param>
        /// <param name="componentName">Name of the component</param>
        /// <returns>XML data of the component</returns>
        Task<string?> GetComponentDataAsync(string actorName, string componentName);

        /// <summary>
        /// Sets component data for an actor
        /// </summary>
        /// <param name="actorName">Name of the actor</param>
        /// <param name="componentName">Name of the component</param>
        /// <param name="componentData">XML data to set</param>
        /// <returns>True if successful</returns>
        Task<bool> SetComponentDataAsync(string actorName, string componentName, string componentData);

        #endregion

        /// <summary>
        /// Connects to the engine
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Disconnects from the engine
        /// </summary>
        Task DisconnectAsync();
    }
}