#nullable enable
using DANCustomTools.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DANCustomTools.Controls
{
    public partial class ComponentListView : System.Windows.Controls.UserControl
    {
        public ComponentListView()
        {
            InitializeComponent();
        }

        private void AvailableComponents_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (DataContext is ComponentListViewModel viewModel)
            {
                viewModel.HandleDrop(e, isUsedComponentsList: false);
            }
        }

        private void UsedComponents_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (DataContext is ComponentListViewModel viewModel)
            {
                viewModel.HandleDrop(e, isUsedComponentsList: true);
            }
        }

        private void Components_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = System.Windows.DragDropEffects.Move;
            e.Handled = true;
        }
    }
}