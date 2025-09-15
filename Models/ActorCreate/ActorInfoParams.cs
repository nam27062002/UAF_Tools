#nullable enable
using DANCustomTools.MVVM;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace DANCustomTools.Models.ActorCreate
{
    /// <summary>
    /// Save type for actor files
    /// </summary>
    public enum ActorSaveType
    {
        NoSave = 0,
        SaveAllInActor = 1,
        Save = 2
    }

    /// <summary>
    /// Parameters for actor creation and file management - migrated from .NET Framework 3.5
    /// </summary>
    public class ActorInfoParams : ViewModelBase
    {
        #region Private Fields

        private uint _indentationChars = 4;
        private string _actorFilePath = string.Empty;
        private ActorSaveType _saveActorEnabled = ActorSaveType.NoSave;
        private string _actorBaseFilePath = string.Empty;
        private bool _useActorBaseEnabled = false;
        private bool _saveActorBaseEnabled = false;

        #endregion

        #region Public Properties

        /// <summary>
        /// Number of characters for indentation in generated files
        /// </summary>
        public uint IndentationChars
        {
            get => _indentationChars;
            set => SetProperty(ref _indentationChars, value);
        }

        /// <summary>
        /// Collection of file parameters for the actor
        /// </summary>
        public ObservableCollection<ActorEditorParamsFiles> FilesParams { get; private set; } = new();

        /// <summary>
        /// Path to the main actor file
        /// </summary>
        public string ActorFilePath
        {
            get => _actorFilePath;
            set => SetProperty(ref _actorFilePath, value ?? string.Empty);
        }

        /// <summary>
        /// How the actor should be saved
        /// </summary>
        public ActorSaveType SaveActorEnabled
        {
            get => _saveActorEnabled;
            set => SetProperty(ref _saveActorEnabled, value);
        }

        /// <summary>
        /// Path to the actor base file (template)
        /// </summary>
        public string ActorBaseFilePath
        {
            get => _actorBaseFilePath;
            set => SetProperty(ref _actorBaseFilePath, value ?? string.Empty);
        }

        /// <summary>
        /// Whether to use actor base file
        /// </summary>
        public bool UseActorBaseEnabled
        {
            get => _useActorBaseEnabled;
            set => SetProperty(ref _useActorBaseEnabled, value);
        }

        /// <summary>
        /// Whether to save actor base file
        /// </summary>
        public bool SaveActorBaseEnabled
        {
            get => _saveActorBaseEnabled;
            set => SetProperty(ref _saveActorBaseEnabled, value);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates that all required parameters are set
        /// </summary>
        /// <returns>True if parameters are valid</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(_actorFilePath) &&
                   !string.IsNullOrEmpty(_actorBaseFilePath) &&
                   FilesParams.Count > 0;
        }

        /// <summary>
        /// Creates a deep copy of these parameters
        /// </summary>
        /// <returns>Cloned ActorInfoParams instance</returns>
        public ActorInfoParams Clone()
        {
            var clone = new ActorInfoParams
            {
                _indentationChars = _indentationChars,
                _actorFilePath = _actorFilePath,
                _saveActorEnabled = _saveActorEnabled,
                _actorBaseFilePath = _actorBaseFilePath,
                _useActorBaseEnabled = _useActorBaseEnabled,
                _saveActorBaseEnabled = _saveActorBaseEnabled
            };

            // Deep copy file parameters
            foreach (var fileParam in FilesParams)
            {
                clone.FilesParams.Add(fileParam.Clone());
            }

            return clone;
        }

        /// <summary>
        /// Adds a file parameter to the collection
        /// </summary>
        /// <param name="fileParam">File parameter to add</param>
        public void AddFileParam(ActorEditorParamsFiles fileParam)
        {
            if (fileParam != null)
            {
                FilesParams.Add(fileParam);
                OnPropertyChanged(nameof(FilesParams));
            }
        }

        /// <summary>
        /// Removes a file parameter from the collection
        /// </summary>
        /// <param name="fileParam">File parameter to remove</param>
        /// <returns>True if removed successfully</returns>
        public bool RemoveFileParam(ActorEditorParamsFiles fileParam)
        {
            if (fileParam != null && FilesParams.Remove(fileParam))
            {
                OnPropertyChanged(nameof(FilesParams));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets default file paths based on actor name
        /// </summary>
        /// <param name="actorName">Name of the actor</param>
        /// <param name="basePath">Base directory path</param>
        public void SetDefaultPaths(string actorName, string basePath = "")
        {
            if (string.IsNullOrEmpty(actorName))
                return;

            if (string.IsNullOrEmpty(basePath))
                basePath = Environment.CurrentDirectory;

            ActorFilePath = Path.Combine(basePath, $"{actorName}.act");
            ActorBaseFilePath = Path.Combine(basePath, $"{actorName}_base.act");
        }

        #endregion
    }
}