#nullable enable
using DANCustomTools.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DANCustomTools.Views
{
    public partial class SceneExplorerView : System.Windows.Controls.UserControl
    {
        public SceneExplorerView()
        {
            InitializeComponent();
            
            // Ensure the view can receive keyboard input
            this.Focusable = true;
            this.Loaded += (s, e) => this.Focus();
            
            // Handle keyboard events
            this.PreviewKeyDown += SceneExplorerView_PreviewKeyDown;
        }

        private void SceneExplorerView_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // No keyboard shortcuts needed currently
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is SceneExplorerViewModel viewModel && e.NewValue is SceneTreeItemViewModel selectedItem)
            {
                viewModel.OnTreeItemSelected(selectedItem);
            }
        }

        private void TreeView_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not SceneExplorerViewModel viewModel)
                return;

            // Get the clicked item
            var treeView = sender as System.Windows.Controls.TreeView;
            var selectedItem = treeView?.SelectedItem as SceneTreeItemViewModel;

            // Only show context menu for actors and frises
            if (selectedItem?.ItemType == SceneTreeItemType.Actor || 
                selectedItem?.ItemType == SceneTreeItemType.Frise)
            {
                ShowContextMenu(selectedItem, e.GetPosition(this));
            }
        }

        private void TreeView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is not SceneExplorerViewModel viewModel)
                return;

            // Handle Delete key
            if (e.Key == Key.Delete)
            {
                var selectedItem = SceneTreeView.SelectedItem as SceneTreeItemViewModel;
                if (selectedItem?.ItemType == SceneTreeItemType.Actor || 
                    selectedItem?.ItemType == SceneTreeItemType.Frise)
                {
                    if (viewModel.DeleteCommand.CanExecute(null))
                    {
                        viewModel.DeleteCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            }
        }

        private void ShowContextMenu(SceneTreeItemViewModel selectedItem, System.Windows.Point position)
        {
            if (DataContext is not SceneExplorerViewModel viewModel)
                return;

            var contextMenu = new ContextMenu();

            // Duplicate command
            var duplicateMenuItem = new MenuItem
            {
                Header = "Duplicate",
                Command = viewModel.DuplicateCommand,
                Icon = new TextBlock { Text = "⧉", FontSize = 12 }
            };
            contextMenu.Items.Add(duplicateMenuItem);

            // Delete command
            var deleteMenuItem = new MenuItem
            {
                Header = "Delete",
                Command = viewModel.DeleteCommand,
                Icon = new TextBlock { Text = "🗑", FontSize = 12 }
            };
            contextMenu.Items.Add(deleteMenuItem);


            // Show context menu
            contextMenu.PlacementTarget = this;
            contextMenu.IsOpen = true;
        }
    }
}
