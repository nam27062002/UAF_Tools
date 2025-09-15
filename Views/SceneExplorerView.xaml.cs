#nullable enable
using DANCustomTools.ViewModels;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DANCustomTools.Views
{
    public partial class SceneExplorerView : System.Windows.Controls.UserControl
    {
        private List<SceneTreeItemViewModel>? _originalTreeItems;

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
            // Handle Ctrl+F for search focus
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SearchTextBox.Focus();
                SearchTextBox.SelectAll();
                e.Handled = true;
            }
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

            var selectedItem = SceneTreeView.SelectedItem as SceneTreeItemViewModel;

            // Handle Delete key
            if (e.Key == Key.Delete)
            {
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
            // Handle F2 key for rename
            else if (e.Key == Key.F2)
            {
                if (selectedItem?.ItemType == SceneTreeItemType.Actor ||
                    selectedItem?.ItemType == SceneTreeItemType.Frise)
                {
                    ShowRenameDialog(selectedItem, viewModel);
                    e.Handled = true;
                }
            }
        }

        private void ShowContextMenu(SceneTreeItemViewModel selectedItem, System.Windows.Point position)
        {
            if (DataContext is not SceneExplorerViewModel viewModel)
                return;

            var contextMenu = new ContextMenu
            {
                // Use default style - clean and simple
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };

            // Rename command (only for Actor and Frise)
            if (selectedItem.ItemType == SceneTreeItemType.Actor || selectedItem.ItemType == SceneTreeItemType.Frise)
            {
                var renameMenuItem = new MenuItem
                {
                    Header = "Rename Object",
                    Icon = new PackIcon { Kind = PackIconKind.Rename, Width = 16, Height = 16 }
                };
                renameMenuItem.Click += (s, e) => ShowRenameDialog(selectedItem, viewModel);
                contextMenu.Items.Add(renameMenuItem);
                contextMenu.Items.Add(new Separator());
            }

            // Duplicate command
            var duplicateMenuItem = new MenuItem
            {
                Header = "Duplicate Object",
                Command = viewModel.DuplicateCommand,
                Icon = new PackIcon { Kind = PackIconKind.ContentDuplicate, Width = 16, Height = 16 }
            };
            contextMenu.Items.Add(duplicateMenuItem);

            contextMenu.Items.Add(new Separator());

            // Delete command
            var deleteMenuItem = new MenuItem
            {
                Header = "Delete Object",
                Command = viewModel.DeleteCommand,
                Icon = new PackIcon { Kind = PackIconKind.Delete, Width = 16, Height = 16 },
                Foreground = new SolidColorBrush(Colors.Red)
            };
            contextMenu.Items.Add(deleteMenuItem);

            // Show context menu
            contextMenu.PlacementTarget = this;
            contextMenu.IsOpen = true;
        }

        private void ShowRenameDialog(SceneTreeItemViewModel selectedItem, SceneExplorerViewModel viewModel)
        {
            string currentName = selectedItem.DisplayName ?? "";

            // Create a simple WPF input dialog using a message box alternative
            var inputWindow = new Window
            {
                Title = "Rename Object",
                Width = 400,
                Height = 180, // Changed from 150 to 180
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(20)
            };

            var label = new System.Windows.Controls.Label
            {
                Content = $"Enter new name for '{currentName}':",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var textBox = new System.Windows.Controls.TextBox
            {
                Text = currentName,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 75,
                Height = 25,
                IsDefault = true,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 75,
                Height = 25,
                IsCancel = true
            };

            bool? dialogResult = null;
            okButton.Click += (s, e) => { dialogResult = true; inputWindow.Close(); };
            cancelButton.Click += (s, e) => { dialogResult = false; inputWindow.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);

            inputWindow.Content = stackPanel;
            textBox.Focus();
            textBox.SelectAll();

            inputWindow.ShowDialog();

            // If user clicked OK and entered a name
            if (dialogResult == true)
            {
                string newName = textBox.Text?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(newName) && newName != currentName)
                {
                    // Execute rename command
                    if (viewModel.RenameCommand.CanExecute(newName))
                    {
                        viewModel.RenameCommand.Execute(newName);
                    }
                }
            }
        }

        #region Search Functionality

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = ((System.Windows.Controls.TextBox)sender).Text;
            FilterTreeView(searchText);
        }

        private void FilterTreeView(string searchText)
        {
            if (DataContext is not SceneExplorerViewModel viewModel)
                return;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Restore original items
                if (_originalTreeItems != null)
                {
                    viewModel.SceneTreeItems.Clear();
                    foreach (var item in _originalTreeItems)
                    {
                        viewModel.SceneTreeItems.Add(item);
                    }
                }
                return;
            }

            // Store original items if not stored yet
            if (_originalTreeItems == null)
            {
                _originalTreeItems = new List<SceneTreeItemViewModel>(viewModel.SceneTreeItems);
            }

            // Filter items
            var filteredItems = FilterItemsRecursive(_originalTreeItems, searchText.ToLowerInvariant());

            viewModel.SceneTreeItems.Clear();
            foreach (var item in filteredItems)
            {
                viewModel.SceneTreeItems.Add(item);
            }

            // Expand all filtered results
            ExpandAllTreeViewItems(SceneTreeView);
        }

        private List<SceneTreeItemViewModel> FilterItemsRecursive(IEnumerable<SceneTreeItemViewModel> items, string searchText)
        {
            var filteredItems = new List<SceneTreeItemViewModel>();

            foreach (var item in items)
            {
                // Check if current item matches
                bool currentMatches = item.DisplayName?.ToLowerInvariant().Contains(searchText) ?? false;

                // Filter children
                var filteredChildren = FilterItemsRecursive(item.Children, searchText);

                // Include item if it matches or has matching children
                if (currentMatches || filteredChildren.Any())
                {
                    var clonedItem = new SceneTreeItemViewModel
                    {
                        DisplayName = item.DisplayName ?? string.Empty,
                        Model = item.Model,
                        ItemType = item.ItemType
                    };

                    // Add filtered children
                    foreach (var child in filteredChildren)
                    {
                        clonedItem.Children.Add(child);
                    }

                    // If current item matches, include all original children
                    if (currentMatches)
                    {
                        foreach (var child in item.Children)
                        {
                            if (!filteredChildren.Contains(child))
                            {
                                clonedItem.Children.Add(child);
                            }
                        }
                    }

                    filteredItems.Add(clonedItem);
                }
            }

            return filteredItems;
        }

        #endregion

        #region Tree Operations

        private void ExpandAllButton_Click(object sender, RoutedEventArgs e)
        {
            ExpandAllTreeViewItems(SceneTreeView);
        }

        private void CollapseAllButton_Click(object sender, RoutedEventArgs e)
        {
            CollapseAllTreeViewItems(SceneTreeView);
        }

        private void ExpandAllTreeViewItems(System.Windows.Controls.TreeView treeView)
        {
            foreach (var item in treeView.Items)
            {
                var treeViewItem = treeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                ExpandTreeViewItem(treeViewItem);
            }
        }

        private void CollapseAllTreeViewItems(System.Windows.Controls.TreeView treeView)
        {
            foreach (var item in treeView.Items)
            {
                var treeViewItem = treeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                CollapseTreeViewItem(treeViewItem);
            }
        }

        private void ExpandTreeViewItem(TreeViewItem? item)
        {
            if (item == null) return;

            item.IsExpanded = true;
            item.UpdateLayout();

            foreach (var subItem in item.Items)
            {
                var subTreeViewItem = item.ItemContainerGenerator.ContainerFromItem(subItem) as TreeViewItem;
                ExpandTreeViewItem(subTreeViewItem);
            }
        }

        private void CollapseTreeViewItem(TreeViewItem? item)
        {
            if (item == null) return;

            foreach (var subItem in item.Items)
            {
                var subTreeViewItem = item.ItemContainerGenerator.ContainerFromItem(subItem) as TreeViewItem;
                CollapseTreeViewItem(subTreeViewItem);
            }

            item.IsExpanded = false;
        }

        #endregion
    }
}
