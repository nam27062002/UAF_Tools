using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace DANCustomTools.Views.Dialogs
{
    public partial class NewActorDialog : Window, INotifyPropertyChanged
    {
        private ObservableCollection<string> _availableComponents = new();
        private ObservableCollection<string> _addedComponents = new();
        private ObservableCollection<string> _filteredAvailableComponents = new();
        private List<string> _allComponents = new();
        private string _filterText = string.Empty;
        private bool _isActorPathValid;
        private bool _hasComponents;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<string> AvailableComponents => _filteredAvailableComponents;
        public ObservableCollection<string> AddedComponents => _addedComponents;

        public string ActorFilePath
        {
            get { return TextBoxActorPath.Text; }
            set { TextBoxActorPath.Text = value; }
        }

        public List<string> SelectedComponents => _addedComponents.ToList();

        public bool IsActorPathValid
        {
            get => _isActorPathValid;
            private set
            {
                if (_isActorPathValid != value)
                {
                    _isActorPathValid = value;
                    OnPropertyChanged();
                    UpdateValidation();
                }
            }
        }

        public bool HasComponents
        {
            get => _hasComponents;
            private set
            {
                if (_hasComponents != value)
                {
                    _hasComponents = value;
                    OnPropertyChanged();
                    UpdateValidation();
                }
            }
        }

        public NewActorDialog()
        {
            InitializeComponent();
            DataContext = this;

            ListBoxAvailableComponents.ItemsSource = AvailableComponents;
            ListBoxAddedComponents.ItemsSource = AddedComponents;

            _addedComponents.CollectionChanged += (s, e) =>
            {
                HasComponents = _addedComponents.Count > 0;
                ApplyFilter();
            };

            UpdateValidation();
        }

        public void SetAvailableComponents(List<string> components)
        {
            _allComponents = components ?? new List<string>();
            _availableComponents.Clear();
            foreach (var component in _allComponents)
            {
                _availableComponents.Add(component);
            }
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            _filteredAvailableComponents.Clear();

            var filtered = _allComponents
                .Where(component => !_addedComponents.Contains(component))
                .Where(component => string.IsNullOrEmpty(_filterText) ||
                                  component.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(component => component);

            foreach (var component in filtered)
            {
                _filteredAvailableComponents.Add(component);
            }
        }

        private void UpdateValidation()
        {
            // Validate actor path
            var actorPath = TextBoxActorPath.Text?.Trim();
            IsActorPathValid = !string.IsNullOrEmpty(actorPath) && actorPath.Length > 0;

            // Update UI colors
            LabelActorPath.Style = IsActorPathValid
                ? FindResource("ValidationLabelStyle") as Style
                : FindResource("ValidationLabelStyle") as Style; // The style handles validation color via DataTrigger

            GroupBoxComponents.Style = HasComponents
                ? FindResource("ValidationGroupBoxStyle") as Style
                : FindResource("ValidationGroupBoxStyle") as Style; // The style handles validation color via DataTrigger

            // Enable/disable Create button
            ButtonCreateAndSave.IsEnabled = IsActorPathValid && HasComponents;
        }

        private void ActorPath_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateValidation();
        }

        private void Filter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _filterText = TextBoxFilter.Text?.Trim() ?? string.Empty;
            ApplyFilter();
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            TextBoxFilter.Text = string.Empty;
        }

        private void BrowseActorPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Actor files (*.act)|*.act|All files (*.*)|*.*",
                Title = "Select actor file path",
                DefaultExt = "act"
            };

            if (!string.IsNullOrEmpty(ActorFilePath))
            {
                dialog.FileName = Path.GetFileName(ActorFilePath);
                dialog.InitialDirectory = Path.GetDirectoryName(ActorFilePath);
            }

            if (dialog.ShowDialog() == true)
            {
                ActorFilePath = dialog.FileName;
            }
        }

        private void AddComponent_Click(object sender, RoutedEventArgs e)
        {
            var selected = ListBoxAvailableComponents.SelectedItem as string;
            if (!string.IsNullOrEmpty(selected))
            {
                _addedComponents.Add(selected);
                ApplyFilter();
            }
        }

        private void RemoveComponent_Click(object sender, RoutedEventArgs e)
        {
            var selected = ListBoxAddedComponents.SelectedItem as string;
            if (!string.IsNullOrEmpty(selected))
            {
                _addedComponents.Remove(selected);
                ApplyFilter();
            }
        }

        private void AddAllComponents_Click(object sender, RoutedEventArgs e)
        {
            var componentsToAdd = _filteredAvailableComponents.ToList();
            foreach (var component in componentsToAdd)
            {
                _addedComponents.Add(component);
            }
            ApplyFilter();
        }

        private void RemoveAllComponents_Click(object sender, RoutedEventArgs e)
        {
            _addedComponents.Clear();
            ApplyFilter();
        }

        private void AvailableComponents_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selected = ListBoxAvailableComponents.SelectedItem as string;
            if (!string.IsNullOrEmpty(selected))
            {
                _addedComponents.Add(selected);
                ApplyFilter();
            }
        }

        private void AddedComponents_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selected = ListBoxAddedComponents.SelectedItem as string;
            if (!string.IsNullOrEmpty(selected))
            {
                _addedComponents.Remove(selected);
                ApplyFilter();
            }
        }

        private void CreateAndSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsActorPathValid || !HasComponents)
            {
                System.Windows.MessageBox.Show("Please specify an actor path and select at least one component.", "Validation Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}