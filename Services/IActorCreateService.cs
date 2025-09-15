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