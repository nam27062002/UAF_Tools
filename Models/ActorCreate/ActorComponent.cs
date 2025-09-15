#nullable enable
using System;

namespace DANCustomTools.Models.ActorCreate
{
    /// <summary>
    /// Attribute to mark classes as Actor Components - migrated from .NET Framework 3.5
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ActorComponentAttribute : Attribute
    {
        #region Private Fields

        private readonly string _componentName;

        #endregion

        #region Public Properties

        /// <summary>
        /// Name of the component
        /// </summary>
        public string ComponentName => _componentName;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor with component name
        /// </summary>
        /// <param name="name">Name of the component</param>
        public ActorComponentAttribute(string name)
        {
            _componentName = name ?? throw new ArgumentNullException(nameof(name));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the component name (legacy method for compatibility)
        /// </summary>
        /// <returns>Component name</returns>
        public string GetActorComponent()
        {
            return _componentName;
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return _componentName;
        }

        public override bool Equals(object? obj)
        {
            if (obj is ActorComponentAttribute other)
                return _componentName == other._componentName;
            return false;
        }

        public override int GetHashCode()
        {
            return _componentName?.GetHashCode() ?? 0;
        }

        #endregion
    }

    /// <summary>
    /// Base class for all actor components
    /// </summary>
    public abstract class ActorComponentBase
    {
        #region Public Properties

        /// <summary>
        /// Name of this component type
        /// </summary>
        public abstract string ComponentName { get; }

        /// <summary>
        /// Description of what this component does
        /// </summary>
        public virtual string Description => "Actor Component";

        /// <summary>
        /// Whether this component is currently enabled
        /// </summary>
        public virtual bool IsEnabled { get; set; } = true;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the component
        /// </summary>
        public virtual void Initialize()
        {
            // Override in derived classes
        }

        /// <summary>
        /// Update the component
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        public virtual void Update(float deltaTime)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Cleanup the component
        /// </summary>
        public virtual void Cleanup()
        {
            // Override in derived classes
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return ComponentName;
        }

        #endregion
    }

    /// <summary>
    /// Data class representing a component instance attached to an actor
    /// </summary>
    public class ActorComponentData
    {
        #region Public Properties

        /// <summary>
        /// Type name of the component
        /// </summary>
        public string ComponentType { get; set; } = string.Empty;

        /// <summary>
        /// Display name for UI
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// XML data for this component instance
        /// </summary>
        public string XmlData { get; set; } = string.Empty;

        /// <summary>
        /// Whether this component is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Order/priority of this component
        /// </summary>
        public int Priority { get; set; } = 0;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public ActorComponentData()
        {
        }

        /// <summary>
        /// Constructor with component type
        /// </summary>
        /// <param name="componentType">Type of the component</param>
        public ActorComponentData(string componentType)
        {
            ComponentType = componentType ?? string.Empty;
            DisplayName = componentType ?? "Unknown Component";
        }

        /// <summary>
        /// Constructor with type and display name
        /// </summary>
        /// <param name="componentType">Type of the component</param>
        /// <param name="displayName">Display name for UI</param>
        public ActorComponentData(string componentType, string displayName)
        {
            ComponentType = componentType ?? string.Empty;
            DisplayName = displayName ?? componentType ?? "Unknown Component";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a copy of this component data
        /// </summary>
        /// <returns>Cloned instance</returns>
        public ActorComponentData Clone()
        {
            return new ActorComponentData
            {
                ComponentType = ComponentType,
                DisplayName = DisplayName,
                XmlData = XmlData,
                IsActive = IsActive,
                Priority = Priority
            };
        }

        /// <summary>
        /// Validates that the component data is valid
        /// </summary>
        /// <returns>True if valid</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ComponentType);
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return DisplayName;
        }

        public override bool Equals(object? obj)
        {
            if (obj is ActorComponentData other)
                return ComponentType == other.ComponentType;
            return false;
        }

        public override int GetHashCode()
        {
            return ComponentType?.GetHashCode() ?? 0;
        }

        #endregion
    }
}