#nullable enable
using System;
using System.Windows.Input;
using System.Xml.Linq;
using System.Linq;
using DANCustomTools.Core.ViewModels;
using DANCustomTools.MVVM;
using DANCustomTools.Models.PropertiesEditor;
using DANCustomTools.Models.SceneExplorer;
using DANCustomTools.Services;

namespace DANCustomTools.ViewModels
{
    public partial class PropertiesEditorViewModel : SubToolViewModelBase
    {
        private readonly IPropertiesEditorService _propertiesService;

        private PropertyModel _currentProperty = new();
        private bool _hasData = false;
        private string _xmlDisplayText = string.Empty;
        private System.Collections.ObjectModel.ObservableCollection<SimplePropertyRow> _parsedProperties = new();
        private System.Collections.ObjectModel.ObservableCollection<SimplePropertyRow> _visibleProperties = new();
        private bool _suppressSend = false;
        private DateTime _lastSendUtc = DateTime.MinValue;
        private string _dataPath = string.Empty;
        private bool _isLoadingFromEngine = false;

        public override string SubToolName => "Properties Editor";

        public PropertyModel CurrentProperty
        {
            get => _currentProperty;
            private set => SetProperty(ref _currentProperty, value);
        }

        public bool HasData
        {
            get => _hasData;
            private set => SetProperty(ref _hasData, value);
        }

        public string XmlDisplayText
        {
            get => _xmlDisplayText;
            set
            {
                if (SetProperty(ref _xmlDisplayText, value))
                {
                    // Send updated XML back to engine when user modifies it
                    if (!_suppressSend && !string.IsNullOrEmpty(value) && CurrentProperty.ObjectRef != uint.MaxValue)
                    {
                        var now = DateTime.UtcNow;
                        if ((now - _lastSendUtc).TotalMilliseconds >= 1000)
                        {
                            _lastSendUtc = now;
                            _propertiesService.SendPropertiesUpdate(CurrentProperty.ObjectRef, value);
                        }
                    }
                }
            }
        }

        public System.Collections.ObjectModel.ObservableCollection<SimplePropertyRow> ParsedProperties
        {
            get => _parsedProperties;
            private set => SetProperty(ref _parsedProperties, value);
        }

        public System.Collections.ObjectModel.ObservableCollection<SimplePropertyRow> VisibleProperties
        {
            get => _visibleProperties;
            private set => SetProperty(ref _visibleProperties, value);
        }

        public string DataPath
        {
            get => _dataPath;
            private set => SetProperty(ref _dataPath, value);
        }

        public bool IsLoadingFromEngine
        {
            get => _isLoadingFromEngine;
            private set => SetProperty(ref _isLoadingFromEngine, value);
        }

        public ICommand DumpToFileCommand { get; }
        public ICommand ClearPropertiesCommand { get; }
        public ICommand ToggleCategoryCommand { get; }

        public PropertiesEditorViewModel(IPropertiesEditorService propertiesService, ILogService logService)
            : base(logService)
        {
            _propertiesService = propertiesService ?? throw new ArgumentNullException(nameof(propertiesService));

            // Subscribe to service events
            _propertiesService.PropertiesUpdated += OnPropertiesUpdated;
            _propertiesService.DataPathUpdated += OnDataPathUpdated;

            // Subscribe to connection events
            SubscribeToConnectionEvents();

            // Initialize commands
            DumpToFileCommand = new RelayCommand(() => ExecuteDumpToFile(null), () => CanExecuteDumpToFile(null));
            ClearPropertiesCommand = new RelayCommand(() => ExecuteClearProperties(null));
            ToggleCategoryCommand = new RelayCommand<SimplePropertyRow>(ToggleCategory);

            UpdateConnectionStatus();
        }

        public void LoadObjectProperties(ObjectWithRefModel objectModel)
        {
            if (objectModel == null)
            {
                ClearProperties();
                return;
            }

            LogService.Info($"Loading properties for object: {objectModel.FriendlyName} (Ref: {objectModel.ObjectRef})");

            // Request properties from engine
            _propertiesService.RequestObjectProperties(objectModel.ObjectRef);
        }

        private void OnPropertiesUpdated(object? sender, PropertyModel propertyModel)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                // Check if this is the same object and data hasn't changed
                // If so, skip reload to avoid losing focus during user editing
                bool isSameObject = CurrentProperty.ObjectRef == propertyModel.ObjectRef;
                bool isSameData = string.Equals(CurrentProperty.XmlData?.Trim(), propertyModel.XmlData?.Trim(), StringComparison.Ordinal);

                if (isSameObject && isSameData && HasData)
                {
                    LogService.Info($"Skipping properties update for object ref {propertyModel.ObjectRef} - data unchanged");
                    return;
                }

                CurrentProperty = propertyModel;
                _suppressSend = true;
                IsLoadingFromEngine = true; // Signal that this is an engine update, not user input

                XmlDisplayText = propertyModel.XmlData;
                HasData = propertyModel.HasData;

                // We now host the original WinForms XMLPropertyGrid. Keep parsed list for future but not required.
                ParsedProperties.Clear();

                if (propertyModel.HasData)
                {
                    // Try to make XML more readable if possible
                    try
                    {
                        if (!string.IsNullOrEmpty(propertyModel.XmlData))
                        {
                            var doc = XDocument.Parse(propertyModel.XmlData);
                            XmlDisplayText = doc.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Info($"XML parsing for formatting failed: {ex.Message}");
                        XmlDisplayText = propertyModel.XmlData;
                    }
                }

                LogService.Info($"Properties updated for object ref: {propertyModel.ObjectRef}");

                IsLoadingFromEngine = false; // Reset flag after update
                _suppressSend = false;
            });
        }

        private void OnDataPathUpdated(object? sender, string dataPath)
        {
            DataPath = dataPath;
            LogService.Info($"PropertiesEditor data path updated: {dataPath}");
        }

        private void UpdateConnectionStatus()
        {
            IsConnected = _propertiesService.IsConnected;
        }

        private void ExecuteDumpToFile(object? parameter)
        {
            // This would typically open a SaveFileDialog
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"properties_dump_{timestamp}.xml";

            _propertiesService.DumpToFile(fileName);
            LogService.Info($"Requested dump to file: {fileName}");
        }

        private bool CanExecuteDumpToFile(object? parameter)
        {
            return HasData && _propertiesService.IsConnected;
        }

        private void ExecuteClearProperties(object? parameter)
        {
            ClearProperties();
        }

        public void ClearProperties()
        {
            _propertiesService.ClearProperties();
            LogService.Info("Properties cleared");
        }

        // Called by the View when TechnoControls XMLPropertyGrid has parsed content and raises change
        public void LoadXmlIntoHostedGrid(TechnoControls.XMLPropertyGrid.XMLPropertyGrid grid)
        {
            if (grid == null) return;
            if (HasData && !string.IsNullOrEmpty(XmlDisplayText))
            {
                grid.ParseXML(XmlDisplayText);
            }
            else
            {
                grid.Clear();
            }
        }

        protected override void SubscribeToConnectionEvents()
        {
            _propertiesService.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        protected override void UnsubscribeFromConnectionEvents()
        {
            _propertiesService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }

        public override void Dispose()
        {
            _propertiesService.PropertiesUpdated -= OnPropertiesUpdated;
            _propertiesService.DataPathUpdated -= OnDataPathUpdated;
            base.Dispose();
        }
    }

    public class SimplePropertyRow
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int Level { get; set; } = 0;
        public bool IsCategory { get; set; } = false;
        public string Path { get; set; } = string.Empty; // unique hierarchical path
        public bool IsExpanded { get; set; } = true;      // for category rows
        public bool IsVisible { get; set; } = true;       // computed visibility used for filtering
    }

    // Local helpers
    partial class PropertiesEditorViewModel
    {
        private void FlattenElement(XElement element, string prefix)
        {
            string name = string.IsNullOrEmpty(prefix) ? element.Name.LocalName : $"{prefix}/{element.Name.LocalName}";
            int level = string.IsNullOrEmpty(prefix) ? 0 : prefix.Count(c => c == '/') + 1;

            // Add attributes as individual rows first
            foreach (var attr in element.Attributes())
            {
                ParsedProperties.Add(new SimplePropertyRow
                {
                    Name = $"{attr.Name.LocalName}",
                    Value = attr.Value,
                    Level = level + 1,
                    IsCategory = false,
                    Path = name + ".@" + attr.Name.LocalName,
                    IsVisible = true
                });
            }

            // If element has no child elements, take its value
            if (!element.HasElements)
            {
                var val = element.Value?.Trim();
                if (!string.IsNullOrEmpty(val))
                {
                    ParsedProperties.Add(new SimplePropertyRow
                    {
                        Name = element.Name.LocalName,
                        Value = val,
                        Level = level,
                        IsCategory = false,
                        Path = name,
                        IsVisible = true
                    });
                }
            }
            else
            {
                // For complex nodes (like LOCAL_POINTS), add a summary row and then recurse
                int childCount = element.Elements().Count();
                bool defaultExpanded = !(level == 0 && (element.Name.LocalName == "COMPONENTS" || element.Name.LocalName == "POS2D" || element.Name.LocalName == "SCALE"));
                var cat = new SimplePropertyRow
                {
                    Name = element.Name.LocalName,
                    Value = $"({childCount} items)",
                    Level = level,
                    IsCategory = true,
                    Path = name,
                    IsExpanded = defaultExpanded,
                    IsVisible = true
                };
                ParsedProperties.Add(cat);

                foreach (var child in element.Elements())
                {
                    FlattenElement(child, name);
                }
            }
        }

        private void ApplyVisibility()
        {
            // Recompute VisibleProperties from ParsedProperties based on expanded categories
            VisibleProperties.Clear();
            var expandedPaths = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var row in ParsedProperties)
            {
                bool visible = true;
                // Any ancestor category not expanded => hide
                string[] parts = row.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    string accum = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        // find category row for accum
                        var cat = ParsedProperties.FirstOrDefault(r => r.Path == accum && r.IsCategory);
                        if (cat != null && !cat.IsExpanded)
                        {
                            visible = false; break;
                        }
                        accum += "/" + parts[i];
                    }
                }
                row.IsVisible = visible;
                if (visible) VisibleProperties.Add(row);
            }
        }

        private void ToggleCategory(SimplePropertyRow? row)
        {
            if (row == null || !row.IsCategory) return;
            row.IsExpanded = !row.IsExpanded;
            ApplyVisibility();
        }
    }
}
