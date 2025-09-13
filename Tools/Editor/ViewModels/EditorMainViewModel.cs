#nullable enable
using DANCustomTools.Core.Abstractions;
using DANCustomTools.MVVM;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace DANCustomTools.Tools.Editor.ViewModels
{
    public class EditorMainViewModel : ViewModelBase
    {
        private readonly IToolManager _toolManager;
        private readonly IToolContext _toolContext;
        private ViewModelBase? _currentSubToolViewModel;
        private string _currentSubToolName = "None";

        public ObservableCollection<ISubTool> SubTools { get; } = new();

        public ViewModelBase? CurrentSubToolViewModel
        {
            get => _currentSubToolViewModel;
            set
            {
                _currentSubToolViewModel = value;
                OnPropertyChanged();
            }
        }

        public string CurrentSubToolName
        {
            get => _currentSubToolName;
            set
            {
                _currentSubToolName = value;
                OnPropertyChanged();
            }
        }

        public ICommand SwitchToSceneExplorerCommand { get; }
        public ICommand SwitchToPropertiesEditorCommand { get; }

        public EditorMainViewModel(IToolManager toolManager, IToolContext toolContext)
        {
            _toolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
            _toolContext = toolContext ?? throw new ArgumentNullException(nameof(toolContext));

            SwitchToSceneExplorerCommand = new RelayCommand(() => SwitchToSubTool("SceneExplorer"));
            SwitchToPropertiesEditorCommand = new RelayCommand(() => SwitchToSubTool("PropertiesEditor"));

            // Subscribe to tool manager events
            _toolManager.CurrentSubToolChanged += OnCurrentSubToolChanged;

            // Initialize sub tools collection
            LoadSubTools();
        }

        private void LoadSubTools()
        {
            var editorTool = _toolManager.GetMainTool("Editor");
            if (editorTool != null)
            {
                SubTools.Clear();
                foreach (var subTool in editorTool.SubTools)
                {
                    SubTools.Add(subTool);
                }
            }
        }

        private void SwitchToSubTool(string subToolName)
        {
            try
            {
                _toolManager.SwitchToSubTool("Editor", subToolName);
            }
            catch (ArgumentException ex)
            {
                // Handle tool not found error
                System.Diagnostics.Debug.WriteLine($"Error switching to subtool: {ex.Message}");
            }
        }

        private void OnCurrentSubToolChanged(object? sender, ISubTool? subTool)
        {
            if (subTool != null && subTool.ParentTool.Name == "Editor")
            {
                CurrentSubToolViewModel = subTool.CreateViewModel();
                CurrentSubToolName = subTool.DisplayName;
            }
            else
            {
                CurrentSubToolViewModel = null;
                CurrentSubToolName = "None";
            }
        }

        public override void Dispose()
        {
            _toolManager.CurrentSubToolChanged -= OnCurrentSubToolChanged;
            base.Dispose();
        }
    }
}