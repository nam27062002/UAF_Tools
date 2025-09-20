using System;
using System.Collections.Generic;
using System.Linq;
using DANCustomTools.Models.SceneExplorer;

namespace DANCustomTools.Services
{
    public class ComponentFilterService : IComponentFilterService
    {
        private readonly ILogService _logService;

        public ComponentFilterService(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public HashSet<string> ExtractAllComponents(List<ActorModel> actors)
        {
            var allComponents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var actor in actors)
            {
                if (!string.IsNullOrWhiteSpace(actor.Components))
                {
                    var components = ParseComponents(actor.Components);
                    foreach (var component in components)
                    {
                        allComponents.Add(component);
                    }
                }
            }

            _logService.Info($"Extracted {allComponents.Count} unique components from {actors.Count} actors");
            return allComponents;
        }

        public List<ActorModel> FilterActorsByComponents(List<ActorModel> actors, HashSet<string> selectedComponents)
        {
            if (selectedComponents == null || selectedComponents.Count == 0)
            {
                return actors; // No filter applied, return all actors
            }

            var filteredActors = new List<ActorModel>();

            foreach (var actor in actors)
            {
                if (ActorHasAnyComponent(actor, selectedComponents))
                {
                    filteredActors.Add(actor);
                }
            }

            _logService.Info($"Filtered {actors.Count} actors down to {filteredActors.Count} actors based on {selectedComponents.Count} selected components");
            return filteredActors;
        }

        public List<string> ParseComponents(string componentsString)
        {
            if (string.IsNullOrWhiteSpace(componentsString))
            {
                return new List<string>();
            }

            return componentsString
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();
        }

        public bool ActorHasAnyComponent(ActorModel actor, HashSet<string> components)
        {
            if (string.IsNullOrWhiteSpace(actor.Components) || components.Count == 0)
            {
                return false;
            }

            var actorComponents = ParseComponents(actor.Components);
            return actorComponents.Any(ac => components.Contains(ac, StringComparer.OrdinalIgnoreCase));
        }

        public bool ActorHasAllComponents(ActorModel actor, HashSet<string> components)
        {
            if (string.IsNullOrWhiteSpace(actor.Components) || components.Count == 0)
            {
                return false;
            }

            var actorComponents = new HashSet<string>(
                ParseComponents(actor.Components),
                StringComparer.OrdinalIgnoreCase);

            return components.All(c => actorComponents.Contains(c));
        }
    }
}