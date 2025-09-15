#nullable enable
using DANCustomTools.MVVM;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DANCustomTools.Models.ActorCreate
{
    /// <summary>
    /// Represents an Actor with its components and properties - migrated from .NET Framework 3.5
    /// </summary>
    public class ActorInfo : ViewModelBase
    {
        #region Private Fields

        private string _name = string.Empty;
        private string _unicId = string.Empty;
        private int _flags = 0;
        private int _currentComponentIndex = -1;
        private ActorInfoParams? _parameters;

        #endregion

        #region Public Properties

        /// <summary>
        /// Name of the actor
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Unique identifier for the actor
        /// </summary>
        public string UnicId
        {
            get => _unicId;
            set => SetProperty(ref _unicId, value);
        }

        /// <summary>
        /// List of component names attached to this actor
        /// </summary>
        public ObservableCollection<string> ComponentList { get; private set; } = new();

        /// <summary>
        /// Actor flags (for engine-specific settings)
        /// </summary>
        public int Flags
        {
            get => _flags;
            set => SetProperty(ref _flags, value);
        }

        /// <summary>
        /// Index of the currently selected component
        /// </summary>
        public int CurrentComponentIndex
        {
            get => _currentComponentIndex;
            set => SetProperty(ref _currentComponentIndex, value);
        }

        /// <summary>
        /// Parameters for actor creation and saving
        /// </summary>
        public ActorInfoParams? Parameters
        {
            get => _parameters;
            set => SetProperty(ref _parameters, value);
        }

        /// <summary>
        /// Gets the currently selected component name
        /// </summary>
        public string? CurrentComponentName
        {
            get
            {
                if (_currentComponentIndex < 0 || _currentComponentIndex >= ComponentList.Count)
                    return null;
                return ComponentList[_currentComponentIndex];
            }
        }

        /// <summary>
        /// Display name for UI binding
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(_name) ? "Unnamed Actor" : _name;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public ActorInfo()
        {
            _parameters = new ActorInfoParams();
        }

        /// <summary>
        /// Constructor with name
        /// </summary>
        /// <param name="name">Actor name</param>
        public ActorInfo(string name) : this()
        {
            _name = name ?? string.Empty;
        }

        /// <summary>
        /// Constructor with name and ID
        /// </summary>
        /// <param name="name">Actor name</param>
        /// <param name="unicId">Unique ID</param>
        public ActorInfo(string name, string unicId) : this(name)
        {
            _unicId = unicId ?? string.Empty;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the index of a specific component in the component list
        /// </summary>
        /// <param name="componentName">Name of the component to find</param>
        /// <returns>Index of the component, or -1 if not found</returns>
        public int GetComponentIndex(string componentName)
        {
            if (string.IsNullOrEmpty(componentName))
                return -1;

            for (int i = 0; i < ComponentList.Count; i++)
            {
                if (ComponentList[i] == componentName)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Adds a component to the actor
        /// </summary>
        /// <param name="componentName">Name of the component to add</param>
        /// <returns>True if added successfully, false if already exists</returns>
        public bool AddComponent(string componentName)
        {
            if (string.IsNullOrEmpty(componentName) || ComponentList.Contains(componentName))
                return false;

            ComponentList.Add(componentName);
            OnPropertyChanged(nameof(ComponentList));
            return true;
        }

        /// <summary>
        /// Removes a component from the actor
        /// </summary>
        /// <param name="componentName">Name of the component to remove</param>
        /// <returns>True if removed successfully, false if not found</returns>
        public bool RemoveComponent(string componentName)
        {
            if (string.IsNullOrEmpty(componentName))
                return false;

            bool removed = ComponentList.Remove(componentName);
            if (removed)
            {
                // Adjust current component index if necessary
                if (_currentComponentIndex >= ComponentList.Count)
                    CurrentComponentIndex = ComponentList.Count - 1;

                OnPropertyChanged(nameof(ComponentList));
            }

            return removed;
        }

        /// <summary>
        /// Sets the current component by name
        /// </summary>
        /// <param name="componentName">Name of the component to select</param>
        /// <returns>True if component was found and selected</returns>
        public bool SetCurrentComponent(string componentName)
        {
            int index = GetComponentIndex(componentName);
            if (index >= 0)
            {
                CurrentComponentIndex = index;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Validates that the actor has all required data
        /// </summary>
        /// <returns>True if actor is valid for saving/use</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(_name) &&
                   !string.IsNullOrEmpty(_unicId) &&
                   _parameters?.IsValid() == true;
        }

        /// <summary>
        /// Creates a deep copy of this actor
        /// </summary>
        /// <returns>Cloned ActorInfo instance</returns>
        public ActorInfo Clone()
        {
            var clone = new ActorInfo(_name, _unicId)
            {
                _flags = _flags,
                _currentComponentIndex = _currentComponentIndex,
                _parameters = _parameters?.Clone()
            };

            // Copy component list
            foreach (var component in ComponentList)
            {
                clone.ComponentList.Add(component);
            }

            return clone;
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return DisplayName;
        }

        public override bool Equals(object? obj)
        {
            if (obj is ActorInfo other)
                return _unicId == other._unicId;
            return false;
        }

        public override int GetHashCode()
        {
            return _unicId?.GetHashCode() ?? 0;
        }

        #endregion
    }
}