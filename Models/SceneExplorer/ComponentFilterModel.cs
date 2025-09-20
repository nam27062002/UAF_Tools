using System;
using DANCustomTools.MVVM;

namespace DANCustomTools.Models.SceneExplorer
{
    public class ComponentFilterModel : ModelBase
    {
        private string _componentName = string.Empty;
        private bool _isSelected;
        private int _actorCount;
        
        public event EventHandler<bool>? SelectionChanged;

        public string ComponentName
        {
            get => _componentName;
            set => SetProperty(ref _componentName, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set 
            {
                if (SetProperty(ref _isSelected, value))
                {
                    // Notify when IsSelected changes
                    SelectionChanged?.Invoke(this, value);
                }
            }
        }

        public int ActorCount
        {
            get => _actorCount;
            set => SetProperty(ref _actorCount, value);
        }

        public string DisplayText => $"{ComponentName} ({ActorCount})";

        public ComponentFilterModel(string componentName, int actorCount = 0)
        {
            ComponentName = componentName;
            ActorCount = actorCount;
        }

        public override void Validate()
        {
            if (string.IsNullOrWhiteSpace(ComponentName))
                throw new ArgumentException("Component name cannot be empty");
        }
    }
}