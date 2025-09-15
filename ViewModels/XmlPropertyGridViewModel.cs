#nullable enable
using DANCustomTools.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;

namespace DANCustomTools.ViewModels
{
    public class XmlPropertyGridViewModel : ViewModelBase
    {
        private readonly ObservableCollection<PropertyItem> _properties = new();
        private readonly ICollectionView _filteredProperties;
        private string _searchText = string.Empty;
        private XDocument? _xmlDocument;
        private bool _hasProperties = false;

        public XmlPropertyGridViewModel()
        {
            _filteredProperties = CollectionViewSource.GetDefaultView(_properties);
            _filteredProperties.Filter = FilterProperties;
            _filteredProperties.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

            ExpandAllCommand = new RelayCommand(ExecuteExpandAll);
            CollapseAllCommand = new RelayCommand(ExecuteCollapseAll);
            ClearSearchCommand = new RelayCommand(ExecuteClearSearch);
        }

        #region Properties

        public ICollectionView FilteredProperties => _filteredProperties;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _filteredProperties.Refresh();
                }
            }
        }

        public bool HasProperties
        {
            get => _hasProperties;
            set => SetProperty(ref _hasProperties, value);
        }

        public int PropertyCount => _properties.Count;

        #endregion

        #region Commands

        public ICommand ExpandAllCommand { get; }
        public ICommand CollapseAllCommand { get; }
        public ICommand ClearSearchCommand { get; }

        private void ExecuteExpandAll()
        {
            // This would be handled by the view to expand all expanders
            OnPropertyChanged(nameof(ExpandAllCommand));
        }

        private void ExecuteCollapseAll()
        {
            // This would be handled by the view to collapse all expanders
            OnPropertyChanged(nameof(CollapseAllCommand));
        }

        private void ExecuteClearSearch()
        {
            SearchText = string.Empty;
        }

        #endregion

        #region Public Methods

        public void LoadXmlDocument(XDocument? document)
        {
            _xmlDocument = document;
            RefreshProperties();
        }

        public void LoadXmlFromString(string? xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                LoadXmlDocument(null);
                return;
            }

            try
            {
                var document = XDocument.Parse(xmlContent);
                LoadXmlDocument(document);
            }
            catch (XmlException ex)
            {
                // Handle XML parsing error
                LoadXmlDocument(null);
                System.Diagnostics.Debug.WriteLine($"XML parsing error: {ex.Message}");
            }
        }

        public string? GetXmlString()
        {
            return _xmlDocument?.ToString();
        }

        #endregion

        #region Private Methods

        private void RefreshProperties()
        {
            _properties.Clear();

            if (_xmlDocument?.Root != null)
            {
                ProcessElement(_xmlDocument.Root, "Root");
            }

            HasProperties = _properties.Count > 0;
            OnPropertyChanged(nameof(PropertyCount));
            _filteredProperties.Refresh();
        }

        private void ProcessElement(XElement element, string parentPath)
        {
            var elementPath = string.IsNullOrEmpty(parentPath) ? element.Name.LocalName : $"{parentPath}.{element.Name.LocalName}";

            // Add attributes as properties
            foreach (var attribute in element.Attributes())
            {
                var property = new PropertyItem
                {
                    Name = attribute.Name.LocalName,
                    Value = attribute.Value,
                    Type = PropertyType.String,
                    Category = element.Name.LocalName,
                    Description = $"Attribute of {element.Name.LocalName}",
                    XPath = $"{elementPath}/@{attribute.Name.LocalName}",
                    XAttribute = attribute
                };

                property.PropertyChanged += Property_PropertyChanged;
                _properties.Add(property);
            }

            // Add text content if element has no child elements
            if (!element.HasElements && !string.IsNullOrWhiteSpace(element.Value))
            {
                var property = new PropertyItem
                {
                    Name = "Value",
                    Value = element.Value,
                    Type = PropertyType.String,
                    Category = element.Name.LocalName,
                    Description = $"Content of {element.Name.LocalName}",
                    XPath = elementPath,
                    XElement = element
                };

                property.PropertyChanged += Property_PropertyChanged;
                _properties.Add(property);
            }

            // Process child elements
            foreach (var childElement in element.Elements())
            {
                ProcessElement(childElement, elementPath);
            }
        }

        private void Property_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is PropertyItem property && e.PropertyName == nameof(PropertyItem.Value))
            {
                UpdateXmlValue(property);
            }
        }

        private void UpdateXmlValue(PropertyItem property)
        {
            try
            {
                if (property.XAttribute != null)
                {
                    property.XAttribute.Value = property.Value?.ToString() ?? string.Empty;
                }
                else if (property.XElement != null)
                {
                    property.XElement.Value = property.Value?.ToString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating XML value: {ex.Message}");
            }
        }

        private bool FilterProperties(object item)
        {
            if (item is PropertyItem property)
            {
                if (string.IsNullOrWhiteSpace(_searchText))
                    return true;

                return property.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                       property.Category.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                       (property.Value?.ToString()?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            return false;
        }

        #endregion
    }

    public enum PropertyType
    {
        String,
        Number,
        Boolean,
        Enum
    }

    public class PropertyItem : ViewModelBase
    {
        private object? _value;

        public string Name { get; set; } = string.Empty;

        public object? Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public PropertyType Type { get; set; } = PropertyType.String;
        public string Category { get; set; } = "General";
        public string Description { get; set; } = string.Empty;
        public string XPath { get; set; } = string.Empty;

        // References to XML objects for updates
        public XElement? XElement { get; set; }
        public XAttribute? XAttribute { get; set; }

        // For enum types
        public List<string>? EnumValues { get; set; }

        public override string ToString() => $"{Name}: {Value}";
    }

    // Data template selector for property value templates
    public class PropertyValueTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item is PropertyItem property)
            {
                return property.Type switch
                {
                    PropertyType.Boolean => element.FindResource("BooleanPropertyTemplate") as DataTemplate,
                    PropertyType.Number => element.FindResource("NumberPropertyTemplate") as DataTemplate,
                    PropertyType.Enum => element.FindResource("EnumPropertyTemplate") as DataTemplate,
                    _ => element.FindResource("StringPropertyTemplate") as DataTemplate
                } ?? (element.FindResource("StringPropertyTemplate") as DataTemplate);
            }

            return base.SelectTemplate(item, container);
        }
    }
}