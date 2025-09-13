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
            // Wire WinForms XMLPropertyGrid events to ViewModel when available
            Loaded += (s, e) =>
            {
                EnsureWindowsFormsHost();
            };
            Unloaded += (s, e) =>
            {
                if (_xmlPropertyGrid != null)
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
                    if (_xmlPropertyGrid != null)
                    {
                        vm.LoadXmlIntoHostedGrid(_xmlPropertyGrid);
                    }
                    vm.PropertyChanged += (s2, e2) =>
                    {
                        if (_xmlPropertyGrid == null)
                            return;

                        if (e2.PropertyName == nameof(PropertiesEditorViewModel.XmlDisplayText)
                            || e2.PropertyName == nameof(PropertiesEditorViewModel.HasData))
                        {
                            vm.LoadXmlIntoHostedGrid(_xmlPropertyGrid);
                        }
                        else if (e2.PropertyName == nameof(PropertiesEditorViewModel.DataPath))
                        {
                            _xmlPropertyGrid.setDataPath(vm.DataPath);
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
            // Forward to ViewModel which will send to engine
            if (ViewModel != null)
            {
                ViewModel.XmlDisplayText = e.xmlText;
            }
        }

        private XMLPropertyGrid? _xmlPropertyGrid;

        private void EnsureWindowsFormsHost()
        {
            if (_xmlPropertyGrid != null) return;
            var host = new WindowsFormsHost();
            _xmlPropertyGrid = new XMLPropertyGrid();
            _xmlPropertyGrid.propertiesChanged += XmlPropertyGrid_propertiesChanged;
            host.Child = _xmlPropertyGrid;
            WinFormsHostContainer.Children.Clear();
            WinFormsHostContainer.Children.Add(host);
            if (ViewModel != null)
            {
                ViewModel.LoadXmlIntoHostedGrid(_xmlPropertyGrid);
                if (!string.IsNullOrEmpty(ViewModel.DataPath))
                {
                    _xmlPropertyGrid.setDataPath(ViewModel.DataPath);
                }
            }
        }
    }
}