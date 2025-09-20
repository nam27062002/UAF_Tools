using System.Collections.Generic;
using DANCustomTools.Models.SceneExplorer;

namespace DANCustomTools.Services
{
    public interface IComponentFilterService
    {
        /// <summary>
        /// Extracts all unique components from all actors in the scene tree
        /// </summary>
        HashSet<string> ExtractAllComponents(List<ActorModel> actors);

        /// <summary>
        /// Filters actors based on selected components
        /// </summary>
        List<ActorModel> FilterActorsByComponents(List<ActorModel> actors, HashSet<string> selectedComponents);

        /// <summary>
        /// Parses component string into individual components
        /// </summary>
        List<string> ParseComponents(string componentsString);

        /// <summary>
        /// Checks if an actor has any of the specified components
        /// </summary>
        bool ActorHasAnyComponent(ActorModel actor, HashSet<string> components);

        /// <summary>
        /// Checks if an actor has all of the specified components
        /// </summary>
        bool ActorHasAllComponents(ActorModel actor, HashSet<string> components);
    }
}