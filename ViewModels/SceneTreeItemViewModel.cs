#nullable enable
using System.Collections.ObjectModel;
using DANCustomTools.MVVM;

namespace DANCustomTools.ViewModels
{
    public class SceneTreeItemViewModel : ViewModelBase
    {
        private string _displayName = string.Empty;
        private object? _model;
        private SceneTreeItemType _itemType;
        private ObservableCollection<SceneTreeItemViewModel> _children = new();

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public object? Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        public SceneTreeItemType ItemType
        {
            get => _itemType;
            set => SetProperty(ref _itemType, value);
        }

        public ObservableCollection<SceneTreeItemViewModel> Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }
    }

    public enum SceneTreeItemType
    {
        Scene,
        Actor,
        Frise,
        ActorSet,
        FriseSet
    }
}