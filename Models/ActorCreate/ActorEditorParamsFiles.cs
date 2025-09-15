#nullable enable
using DANCustomTools.MVVM;
using System;
using System.IO;

namespace DANCustomTools.Models.ActorCreate
{
    /// <summary>
    /// Save type for component files
    /// </summary>
    public enum ComponentSaveType
    {
        NoSave = 0,
        SaveInActorBase = 1,
        SaveInComponent = 2
    }

    /// <summary>
    /// Parameters for component file management - migrated from .NET Framework 3.5
    /// </summary>
    public class ActorEditorParamsFiles : ViewModelBase
    {
        #region Private Fields

        private string _componentName = string.Empty;
        private string _path = string.Empty;
        private string _subPath = string.Empty;
        private ComponentSaveType _saveEnabled = ComponentSaveType.NoSave;

        #endregion

        #region Public Properties

        /// <summary>
        /// Name of the component this file parameter belongs to
        /// </summary>
        public string ComponentName
        {
            get => _componentName;
            set => SetProperty(ref _componentName, value ?? string.Empty);
        }

        /// <summary>
        /// Main file path for this component
        /// </summary>
        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value ?? string.Empty);
        }

        /// <summary>
        /// Sub-path or relative path for this component
        /// </summary>
        public string SubPath
        {
            get => _subPath;
            set => SetProperty(ref _subPath, value ?? string.Empty);
        }

        /// <summary>
        /// How this component file should be saved
        /// </summary>
        public ComponentSaveType SaveEnabled
        {
            get => _saveEnabled;
            set => SetProperty(ref _saveEnabled, value);
        }

        /// <summary>
        /// Full path combining Path and SubPath
        /// </summary>
        public string FullPath
        {
            get
            {
                if (string.IsNullOrEmpty(_path))
                    return _subPath;
                if (string.IsNullOrEmpty(_subPath))
                    return _path;
                return System.IO.Path.Combine(_path, _subPath);
            }
        }

        /// <summary>
        /// Display name for UI binding
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(_componentName) ? "Unknown Component" : _componentName;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public ActorEditorParamsFiles()
        {
        }

        /// <summary>
        /// Constructor with component name
        /// </summary>
        /// <param name="componentName">Name of the component</param>
        public ActorEditorParamsFiles(string componentName)
        {
            _componentName = componentName ?? string.Empty;
        }

        /// <summary>
        /// Constructor with component name and path
        /// </summary>
        /// <param name="componentName">Name of the component</param>
        /// <param name="path">File path</param>
        public ActorEditorParamsFiles(string componentName, string path) : this(componentName)
        {
            _path = path ?? string.Empty;
        }

        /// <summary>
        /// Constructor with all parameters
        /// </summary>
        /// <param name="componentName">Name of the component</param>
        /// <param name="path">Main file path</param>
        /// <param name="subPath">Sub path</param>
        /// <param name="saveType">Save type</param>
        public ActorEditorParamsFiles(string componentName, string path, string subPath, ComponentSaveType saveType)
            : this(componentName, path)
        {
            _subPath = subPath ?? string.Empty;
            _saveEnabled = saveType;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates that the file parameters are valid
        /// </summary>
        /// <returns>True if parameters are valid</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(_componentName) &&
                   (!string.IsNullOrEmpty(_path) || !string.IsNullOrEmpty(_subPath));
        }

        /// <summary>
        /// Creates a deep copy of these file parameters
        /// </summary>
        /// <returns>Cloned ActorEditorParamsFiles instance</returns>
        public ActorEditorParamsFiles Clone()
        {
            return new ActorEditorParamsFiles
            {
                _componentName = _componentName,
                _path = _path,
                _subPath = _subPath,
                _saveEnabled = _saveEnabled
            };
        }

        /// <summary>
        /// Checks if the file path exists
        /// </summary>
        /// <returns>True if the full path exists</returns>
        public bool FileExists()
        {
            var fullPath = FullPath;
            return !string.IsNullOrEmpty(fullPath) && File.Exists(fullPath);
        }

        /// <summary>
        /// Gets the directory of the full path
        /// </summary>
        /// <returns>Directory path, or empty string if no path set</returns>
        public string GetDirectory()
        {
            var fullPath = FullPath;
            if (string.IsNullOrEmpty(fullPath))
                return string.Empty;

            try
            {
                return System.IO.Path.GetDirectoryName(fullPath) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the filename from the full path
        /// </summary>
        /// <returns>Filename, or empty string if no path set</returns>
        public string GetFileName()
        {
            var fullPath = FullPath;
            if (string.IsNullOrEmpty(fullPath))
                return string.Empty;

            try
            {
                return System.IO.Path.GetFileName(fullPath);
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return $"{DisplayName}: {FullPath}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is ActorEditorParamsFiles other)
                return _componentName == other._componentName && FullPath == other.FullPath;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_componentName, FullPath);
        }

        #endregion
    }
}