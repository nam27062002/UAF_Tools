using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using DANCustomTools.Models.ActorCreate;

namespace DANCustomTools.Views.Dialogs
{
    public partial class ComponentSaveDialog : Window
    {
        public enum ActorSaveType
        {
            NoSave,
            Save,
            SaveAllInActor
        }

        public enum ComponentSaveType
        {
            NoSave,
            SaveInActorBase,
            SaveInComponent
        }

        public class ComponentFileInfo
        {
            public string ComponentName { get; set; } = string.Empty;
            public string ComponentPath { get; set; } = string.Empty;
            public ComponentSaveType SaveType { get; set; } = ComponentSaveType.SaveInActorBase;
        }

        public string ActorFilePath
        {
            get { return TextBoxActorPath.Text; }
            set { TextBoxActorPath.Text = value; }
        }

        public ActorSaveType ActorSaveMode
        {
            get
            {
                if (RadioSaveAllInActor.IsChecked == true) return ActorSaveType.SaveAllInActor;
                if (RadioSaveActor.IsChecked == true) return ActorSaveType.Save;
                return ActorSaveType.NoSave;
            }
            set
            {
                RadioSaveAllInActor.IsChecked = value == ActorSaveType.SaveAllInActor;
                RadioSaveActor.IsChecked = value == ActorSaveType.Save;
                RadioNoSaveActor.IsChecked = value == ActorSaveType.NoSave;
                UpdateControlStates();
            }
        }

        public string ActorBasePath
        {
            get { return TextBoxActorBasePath.Text; }
            set { TextBoxActorBasePath.Text = value; }
        }

        public bool UseActorBase
        {
            get { return CheckBoxUseActorBase.IsChecked == true; }
            set
            {
                CheckBoxUseActorBase.IsChecked = value;
                UpdateControlStates();
            }
        }

        public bool SaveActorBase
        {
            get { return RadioSaveActorBase.IsChecked == true; }
            set
            {
                RadioSaveActorBase.IsChecked = value;
                RadioNoSaveActorBase.IsChecked = !value;
                UpdateControlStates();
            }
        }

        public List<ComponentFileInfo> ComponentFiles { get; private set; } = new();

        private bool _updatingControls = false;

        public ComponentSaveDialog()
        {
            InitializeComponent();

            // Wire up events
            CheckBoxUseActorBase.Checked += (s, e) => UpdateControlStates();
            CheckBoxUseActorBase.Unchecked += (s, e) => UpdateControlStates();
            RadioSaveAllInActor.Checked += (s, e) => UpdateControlStates();
            RadioSaveActor.Checked += (s, e) => UpdateControlStates();
            RadioNoSaveActor.Checked += (s, e) => UpdateControlStates();
            RadioSaveActorBase.Checked += (s, e) => UpdateControlStates();
            RadioNoSaveActorBase.Checked += (s, e) => UpdateControlStates();
        }

        public void SetComponentFiles(List<ComponentFileInfo> componentFiles)
        {
            ComponentFiles = componentFiles ?? new List<ComponentFileInfo>();
            RefreshComponentsList();
            UpdateComponentsSaveTypeDisplay();
        }

        private void RefreshComponentsList()
        {
            PanelComponentsFiles.Children.Clear();

            foreach (var component in ComponentFiles)
            {
                var componentControl = CreateComponentControl(component);
                PanelComponentsFiles.Children.Add(componentControl);
            }

            GroupBoxComponents.Header = $"Components ({ComponentFiles.Count})";
        }

        private UIElement CreateComponentControl(ComponentFileInfo component)
        {
            var border = new System.Windows.Controls.Border
            {
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new System.Windows.Thickness(1),
                Margin = new System.Windows.Thickness(0, 2, 0, 0),
                Padding = new System.Windows.Thickness(8)
            };

            var grid = new System.Windows.Controls.Grid();
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(150) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = System.Windows.GridLength.Auto });

            // Component name
            var nameLabel = new System.Windows.Controls.Label
            {
                Content = component.ComponentName,
                FontWeight = System.Windows.FontWeights.SemiBold,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            System.Windows.Controls.Grid.SetColumn(nameLabel, 0);

            // Component path
            var pathTextBox = new System.Windows.Controls.TextBox
            {
                Text = component.ComponentPath,
                Margin = new System.Windows.Thickness(4, 0, 0, 0),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New, monospace"),
                FontSize = 11,
                Tag = component
            };
            pathTextBox.TextChanged += (s, e) => component.ComponentPath = pathTextBox.Text;
            System.Windows.Controls.Grid.SetColumn(pathTextBox, 1);

            // Browse button
            var browseButton = new System.Windows.Controls.Button
            {
                Content = "...",
                Width = 30,
                Margin = new System.Windows.Thickness(4, 0, 0, 0),
                Tag = pathTextBox
            };
            browseButton.Click += BrowseComponentPath_Click;
            System.Windows.Controls.Grid.SetColumn(browseButton, 2);

            grid.Children.Add(nameLabel);
            grid.Children.Add(pathTextBox);
            grid.Children.Add(browseButton);

            border.Child = grid;
            return border;
        }

        private void BrowseComponentPath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is System.Windows.Controls.TextBox textBox)
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Component files (*.ilu)|*.ilu|All files (*.*)|*.*",
                    Title = "Select component file path"
                };

                if (!string.IsNullOrEmpty(textBox.Text))
                {
                    dialog.FileName = System.IO.Path.GetFileName(textBox.Text);
                    dialog.InitialDirectory = System.IO.Path.GetDirectoryName(textBox.Text);
                }

                if (dialog.ShowDialog() == true)
                {
                    textBox.Text = dialog.FileName;
                }
            }
        }

        private void ComponentSaveType_Changed(object sender, RoutedEventArgs e)
        {
            if (_updatingControls) return;

            var saveType = ComponentSaveType.SaveInActorBase;
            if (RadioSaveInComponent.IsChecked == true) saveType = ComponentSaveType.SaveInComponent;
            else if (RadioNoSaveComponents.IsChecked == true) saveType = ComponentSaveType.NoSave;

            // Apply to all components
            foreach (var component in ComponentFiles)
            {
                component.SaveType = saveType;
            }
        }

        private void UpdateComponentsSaveTypeDisplay()
        {
            if (ComponentFiles.Count == 0) return;

            _updatingControls = true;

            // Check if all components have the same save type
            var firstType = ComponentFiles[0].SaveType;
            bool allSame = true;
            foreach (var component in ComponentFiles)
            {
                if (component.SaveType != firstType)
                {
                    allSame = false;
                    break;
                }
            }

            if (allSame)
            {
                RadioSaveInActorBase.IsChecked = firstType == ComponentSaveType.SaveInActorBase;
                RadioSaveInComponent.IsChecked = firstType == ComponentSaveType.SaveInComponent;
                RadioNoSaveComponents.IsChecked = firstType == ComponentSaveType.NoSave;
            }
            else
            {
                // Mixed states - uncheck all
                RadioSaveInActorBase.IsChecked = false;
                RadioSaveInComponent.IsChecked = false;
                RadioNoSaveComponents.IsChecked = false;
            }

            _updatingControls = false;
        }

        private void UpdateControlStates()
        {
            if (_updatingControls) return;

            bool actorPathEnabled = ActorSaveMode != ActorSaveType.NoSave;
            TextBoxActorPath.IsEnabled = actorPathEnabled;

            bool useActorBase = UseActorBase;
            GroupBoxActorBase.IsEnabled = useActorBase;

            bool actorBasePathEnabled = useActorBase && SaveActorBase;
            TextBoxActorBasePath.IsEnabled = actorBasePathEnabled;

            bool separatedFilesEnabled = ActorSaveMode != ActorSaveType.SaveAllInActor;
            GroupBoxActorBase.IsEnabled = separatedFilesEnabled && useActorBase;
            GroupBoxComponents.IsEnabled = separatedFilesEnabled;
        }

        private void SelectActorPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Actor files (*.act)|*.act|All files (*.*)|*.*",
                Title = "Select actor file path"
            };

            if (!string.IsNullOrEmpty(ActorFilePath))
            {
                dialog.FileName = System.IO.Path.GetFileName(ActorFilePath);
                dialog.InitialDirectory = System.IO.Path.GetDirectoryName(ActorFilePath);
            }

            if (dialog.ShowDialog() == true)
            {
                ActorFilePath = dialog.FileName;
            }
        }

        private void SelectActorBasePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Actor base files (*.ilu)|*.ilu|All files (*.*)|*.*",
                Title = "Select actor base file path"
            };

            if (!string.IsNullOrEmpty(ActorBasePath))
            {
                dialog.FileName = System.IO.Path.GetFileName(ActorBasePath);
                dialog.InitialDirectory = System.IO.Path.GetDirectoryName(ActorBasePath);
            }

            if (dialog.ShowDialog() == true)
            {
                ActorBasePath = dialog.FileName;
            }
        }

        private void ResetPaths_Click(object sender, RoutedEventArgs e)
        {
            // Use actor path as base for component paths
            if (string.IsNullOrEmpty(ActorFilePath)) return;

            var basePath = System.IO.Path.GetDirectoryName(ActorFilePath);
            var baseFileName = System.IO.Path.GetFileNameWithoutExtension(ActorFilePath);

            foreach (var component in ComponentFiles)
            {
                component.ComponentPath = System.IO.Path.Combine(basePath ?? "", $"{baseFileName}_{component.ComponentName}.ilu");
            }

            RefreshComponentsList();
        }

        private void ValidatePaths_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for engine validation
            System.Windows.MessageBox.Show("Path validation would be performed by the engine here.", "Path Validation",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}