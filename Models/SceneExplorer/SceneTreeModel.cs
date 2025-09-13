using System;  
#nullable enable
using DANCustomTools.MVVM;
using System.Collections.ObjectModel;

namespace DANCustomTools.Models.SceneExplorer
{
    public class SceneTreeModel : ModelBase
    {
        private string _uniqueName = string.Empty;
        private string _path = string.Empty;
        private bool _isOnline = false;
        private ObservableCollection<SceneTreeModel> _childScenes = new();
        private ObservableCollection<ActorModel> _actors = new();
        private ObservableCollection<FriseModel> _frises = new();

        public string UniqueName
        {
            get => _uniqueName;
            set => SetProperty(ref _uniqueName, value);
        }

        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public bool IsOnline
        {
            get => _isOnline;
            set => SetProperty(ref _isOnline, value);
        }

        public ObservableCollection<SceneTreeModel> ChildScenes
        {
            get => _childScenes;
            set => SetProperty(ref _childScenes, value);
        }

        public ObservableCollection<ActorModel> Actors
        {
            get => _actors;
            set => SetProperty(ref _actors, value);
        }

        public ObservableCollection<FriseModel> Frises
        {
            get => _frises;
            set => SetProperty(ref _frises, value);
        }

        public override void Validate()
        {
            if (string.IsNullOrWhiteSpace(UniqueName))
                throw new ArgumentException("Unique name cannot be empty");
            
            if (string.IsNullOrWhiteSpace(Path))
                throw new ArgumentException("Path cannot be empty");
        }
    }
}