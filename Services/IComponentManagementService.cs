#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DANCustomTools.Services
{
    /// <summary>
    /// Service for managing actor components
    /// </summary>
    public interface IComponentManagementService
    {
        /// <summary>
        /// Gets the list of available components
        /// </summary>
        IEnumerable<string> AvailableComponents { get; }

        /// <summary>
        /// Gets the list of components for a specific actor
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <returns>List of component names</returns>
        IEnumerable<string> GetActorComponents(int actorIndex);

        /// <summary>
        /// Event fired when component list is updated
        /// </summary>
        event EventHandler<List<string>>? ComponentListUpdated;

        /// <summary>
        /// Event fired when actor component list is updated
        /// </summary>
        event EventHandler<ActorComponentListUpdatedEventArgs>? ActorComponentListUpdated;

        /// <summary>
        /// Adds a component to an actor
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <param name="componentName">Name of the component to add</param>
        /// <returns>True if successful</returns>
        Task<bool> AddComponentToActorAsync(int actorIndex, string componentName);

        /// <summary>
        /// Removes a component from an actor
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <param name="componentName">Name of the component to remove</param>
        /// <returns>True if successful</returns>
        Task<bool> RemoveComponentFromActorAsync(int actorIndex, string componentName);

        /// <summary>
        /// Copies a component to clipboard
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <param name="componentName">Name of the component to copy</param>
        /// <param name="componentData">XML data of the component</param>
        /// <returns>True if successful</returns>
        Task<bool> CopyComponentAsync(int actorIndex, string componentName, string componentData);

        /// <summary>
        /// Cuts a component (copies and removes)
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <param name="componentName">Name of the component to cut</param>
        /// <param name="componentData">XML data of the component</param>
        /// <returns>True if successful</returns>
        Task<bool> CutComponentAsync(int actorIndex, string componentName, string componentData);

        /// <summary>
        /// Pastes a component from clipboard
        /// </summary>
        /// <param name="actorIndex">Index of the actor to paste to</param>
        /// <returns>True if successful</returns>
        Task<bool> PasteComponentAsync(int actorIndex);

        /// <summary>
        /// Gets component data for an actor
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <param name="componentName">Name of the component</param>
        /// <returns>XML data of the component</returns>
        Task<string?> GetComponentDataAsync(int actorIndex, string componentName);

        /// <summary>
        /// Sets component data for an actor
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <param name="componentName">Name of the component</param>
        /// <param name="componentData">XML data to set</param>
        /// <returns>True if successful</returns>
        Task<bool> SetComponentDataAsync(int actorIndex, string componentName, string componentData);

        /// <summary>
        /// Refreshes the component list from engine
        /// </summary>
        /// <returns>True if successful</returns>
        Task<bool> RefreshComponentListAsync();

        /// <summary>
        /// Refreshes the component list for a specific actor
        /// </summary>
        /// <param name="actorIndex">Index of the actor</param>
        /// <returns>True if successful</returns>
        Task<bool> RefreshActorComponentListAsync(int actorIndex);
    }

    /// <summary>
    /// Event args for actor component list updated
    /// </summary>
    public class ActorComponentListUpdatedEventArgs : EventArgs
    {
        public int ActorIndex { get; set; }
        public List<string> ComponentList { get; set; } = new();
        public string? Action { get; set; } // "added", "removed", "updated"
        public string? ComponentName { get; set; }
    }
}
