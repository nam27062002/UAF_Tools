#nullable enable
using System.Windows.Controls;
using DANCustomTools.ViewModels;
using TechnoControls.XMLPropertyGrid;
using System.Windows.Forms.Integration;

namespace DANCustomTools.Views
{
    public partial class PropertiesEditorView : System.Windows.Controls.UserControl
    {
        public PropertiesEditorView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                EnsureWindowsFormsHost();
            };
            Unloaded += (s, e) =>
            {
                if (_xmlPropertyGrid != null && OperatingSystem.IsWindows())
                {
                    _xmlPropertyGrid.propertiesChanged -= XmlPropertyGrid_propertiesChanged;
                    _xmlPropertyGrid.CloseAll();
                }
            };

            DataContextChanged += (s, e) =>
            {
                if (e.NewValue is PropertiesEditorViewModel vm)
                {
                    EnsureWindowsFormsHost();
                    if (_xmlPropertyGrid != null && OperatingSystem.IsWindows())
                    {
                        vm.LoadXmlIntoHostedGrid(_xmlPropertyGrid);
                    }
                    vm.PropertyChanged += (s2, e2) =>
                    {
                        if (_xmlPropertyGrid == null || !OperatingSystem.IsWindows())
                            return;

                        if (e2.PropertyName == nameof(PropertiesEditorViewModel.XmlDisplayText)
                            || e2.PropertyName == nameof(PropertiesEditorViewModel.HasData))
                        {
                            if (vm.IsLoadingFromEngine)
                            {
                                vm.LoadXmlIntoHostedGrid(_xmlPropertyGrid);
                            }
                        }
                        else if (e2.PropertyName == nameof(PropertiesEditorViewModel.DataPath))
                        {
                            if (OperatingSystem.IsWindows())
                            {
                                _xmlPropertyGrid.setDataPath(vm.DataPath);
                            }
                        }
                    };
                }
            };
        }

        public PropertiesEditorViewModel? ViewModel
        {
            get => DataContext as PropertiesEditorViewModel;
            set => DataContext = value;
        }

        private void XmlPropertyGrid_propertiesChanged(object sender, PropertiesChangedEventArgs e)
        {
            if (ViewModel == null) return;
            if (!OperatingSystem.IsWindows()) return;
            
            try
            {
                if (!string.IsNullOrEmpty(e.xmlText))
                {
                    System.Xml.Linq.XDocument.Parse(e.xmlText);
                }
                ViewModel.XmlDisplayText = e.xmlText;
            }
            catch (System.Xml.XmlException xmlEx)
            {
                System.Diagnostics.Debug.WriteLine($"XML serialization error from XMLPropertyGrid: {xmlEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Problematic XML: {e.xmlText}");
                
                var sanitizedXml = SanitizeXmlText(e.xmlText);
                try
                {
                    System.Xml.Linq.XDocument.Parse(sanitizedXml);
                    ViewModel.XmlDisplayText = sanitizedXml;
                    System.Diagnostics.Debug.WriteLine("XML successfully sanitized and applied");
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Failed to sanitize XML, skipping update to prevent crash");
                }
            }
        }

        private string SanitizeXmlText(string xmlText)
        {
            if (string.IsNullOrEmpty(xmlText))
                return xmlText;
            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(xmlText);
                return doc.ToString();
            }
            catch
            {
                var result = xmlText;
                result = System.Text.RegularExpressions.Regex.Replace(result,
                    @"(\w+\s*=\s*"")[^""]*("")",
                    match => {
                        var value = match.Value;
                        var start = value.IndexOf('"') + 1;
                        var end = value.LastIndexOf('"');
                        if (start < end)
                        {
                            var attributeValue = value.Substring(start, end - start);
                            var escapedValue = attributeValue
                                .Replace("&", "&amp;")
                                .Replace("<", "&lt;")
                                .Replace(">", "&gt;");
                            return value.Substring(0, start) + escapedValue + value.Substring(end);
                        }
                        return value;
                    });

                return result;
            }
        }

        private XMLPropertyGrid? _xmlPropertyGrid;

        private void EnsureWindowsFormsHost()
        {
            if (_xmlPropertyGrid != null) return;
            if (!OperatingSystem.IsWindows()) return;
            
            var host = new WindowsFormsHost();
            _xmlPropertyGrid = new XMLPropertyGrid();
            _xmlPropertyGrid.propertiesChanged += XmlPropertyGrid_propertiesChanged;
            host.Child = _xmlPropertyGrid;
            WinFormsHostContainer.Children.Clear();
            WinFormsHostContainer.Children.Add(host);
            if (ViewModel == null) return;
            ViewModel.LoadXmlIntoHostedGrid(_xmlPropertyGrid);
            if (!string.IsNullOrEmpty(ViewModel.DataPath))
            {
                _xmlPropertyGrid.setDataPath(ViewModel.DataPath);
            }
        }
    }
}
