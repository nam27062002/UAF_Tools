#nullable enable
using DANCustomTools.ViewModels;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DANCustomTools.Views
{
    public partial class SceneExplorerView : System.Windows.Controls.UserControl, IDisposable
    {
        private DispatcherTimer? _searchTimer;
        private DispatcherTimer? _scrollAnimationTimer;
        private string _pendingSearchText = string.Empty;
        private bool _disposed = false;

        public SceneExplorerView()
        {
            InitializeComponent();

            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchTimer.Tick += SearchTimer_Tick;

            this.Focusable = true;
            this.Loaded += (s, e) => this.Focus();

            this.PreviewKeyDown += SceneExplorerView_PreviewKeyDown;

            this.DataContextChanged += SceneExplorerView_DataContextChanged;

            this.Unloaded += SceneExplorerView_Unloaded;
        }

        private void SceneExplorerView_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void SceneExplorerView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SceneExplorerViewModel oldViewModel)
            {
                oldViewModel.ScrollToItemRequested -= OnScrollToItemRequested;
            }

            if (e.NewValue is SceneExplorerViewModel newViewModel)
            {
                newViewModel.ScrollToItemRequested += OnScrollToItemRequested;
            }
        }

        private void OnScrollToItemRequested(object? sender, SceneTreeItemViewModel item)
        {
            ScrollToTreeViewItem(item);
        }

        private void SceneExplorerView_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
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

            var treeView = sender as System.Windows.Controls.TreeView;
            var selectedItem = treeView?.SelectedItem as SceneTreeItemViewModel;

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
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };

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

            var duplicateMenuItem = new MenuItem
            {
                Header = "Duplicate Object",
                Command = viewModel.DuplicateCommand,
                Icon = new PackIcon { Kind = PackIconKind.ContentDuplicate, Width = 16, Height = 16 }
            };
            contextMenu.Items.Add(duplicateMenuItem);

            contextMenu.Items.Add(new Separator());

            var deleteMenuItem = new MenuItem
            {
                Header = "Delete Object",
                Command = viewModel.DeleteCommand,
                Icon = new PackIcon { Kind = PackIconKind.Delete, Width = 16, Height = 16 },
                Foreground = new SolidColorBrush(Colors.Red)
            };
            contextMenu.Items.Add(deleteMenuItem);

            contextMenu.PlacementTarget = this;
            contextMenu.IsOpen = true;
        }

        private void ShowRenameDialog(SceneTreeItemViewModel selectedItem, SceneExplorerViewModel viewModel)
        {
            string currentName = selectedItem.DisplayName ?? "";

            var inputWindow = new Window
            {
                Title = "Rename Object",
                Width = 400,
                Height = 180,
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

            if (dialogResult == true)
            {
                string newName = textBox.Text?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(newName) && newName != currentName)
                {
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

            _pendingSearchText = searchText ?? string.Empty;

            _searchTimer?.Stop();

            _searchTimer?.Start();
        }

        private void SearchTimer_Tick(object? sender, EventArgs e)
        {   
            _searchTimer?.Stop();

            FilterTreeView(_pendingSearchText);
        }

        private void FilterTreeView(string searchText)
        {
            if (DataContext is not SceneExplorerViewModel viewModel)
                return;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                ResetSearchVisibility(viewModel.SceneTreeItems);
                return;
            }

            var searchTextLower = searchText.ToLowerInvariant();
            ApplySearchFilter(viewModel.SceneTreeItems, searchTextLower);

            ExpandAllTreeViewItems(SceneTreeView);
        }

        private void ResetSearchVisibility(System.Collections.ObjectModel.ObservableCollection<SceneTreeItemViewModel> items)
        {
            foreach (var item in items)
            {
                if (item.ItemType != SceneTreeItemType.Actor)
                {
                    item.IsVisible = true;
                }
                
                if (item.Children.Count > 0)
                {
                    ResetSearchVisibility(item.Children);
                }
            }
        }

        private bool ApplySearchFilter(System.Collections.ObjectModel.ObservableCollection<SceneTreeItemViewModel> items, string searchText)
        {
            bool hasVisibleChild = false;

            foreach (var item in items)
            {
                bool currentMatches = item.DisplayName?.ToLowerInvariant().Contains(searchText) ?? false;
                bool childMatches = false;

                if (item.Children.Count > 0)
                {
                    childMatches = ApplySearchFilter(item.Children, searchText);
                }

                bool shouldBeVisible = currentMatches || childMatches;
                
                if (item.ItemType == SceneTreeItemType.Actor)
                {
                    item.IsVisible = item.IsVisible && shouldBeVisible;
                }
                else
                {
                    item.IsVisible = shouldBeVisible;
                }

                if (item.IsVisible)
                {
                    hasVisibleChild = true;
                }
            }

            return hasVisibleChild;
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

        private void ScrollToTreeViewItem(SceneTreeItemViewModel item)
        {
            try
            {
                SceneTreeView.UpdateLayout();

                var treeViewItem = FindTreeViewItem(SceneTreeView, item);
                if (treeViewItem != null)
                {
                    treeViewItem.BringIntoView();

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            CenterTreeViewItem(treeViewItem);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to center tree view item: {ex.Message}");
                        }
                    }), DispatcherPriority.Loaded);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to scroll to tree view item: {ex.Message}");
            }
        }

        private TreeViewItem? FindTreeViewItem(ItemsControl container, object item)
        {
            if (container == null) return null;

            var treeViewItem = container.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            if (treeViewItem != null) return treeViewItem;

            foreach (var childItem in container.Items)
            {
                var childContainer = container.ItemContainerGenerator.ContainerFromItem(childItem) as ItemsControl;
                if (childContainer != null)
                {
                    var result = FindTreeViewItem(childContainer, item);
                    if (result != null) return result;
                }
            }

            return null;
        }

        private void CenterTreeViewItem(TreeViewItem item)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(SceneTreeView);
            if (scrollViewer == null) return;

            var transform = item.TransformToAncestor(scrollViewer);
            var itemPosition = transform.Transform(new Point(0, 0));

            var viewportHeight = scrollViewer.ViewportHeight;
            var itemHeight = item.ActualHeight;
            
            var targetOffset = itemPosition.Y - (viewportHeight / 2) + (itemHeight / 2);
            
            targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.ScrollableHeight));

            AnimateScroll(scrollViewer, targetOffset);
        }

        private void AnimateScroll(ScrollViewer scrollViewer, double targetOffset)
        {
            if (_scrollAnimationTimer != null)
            {
                _scrollAnimationTimer.Stop();
                _scrollAnimationTimer.Tick -= null;
                _scrollAnimationTimer = null;
            }

            var currentOffset = scrollViewer.VerticalOffset;
            var distance = targetOffset - currentOffset;
            var duration = TimeSpan.FromMilliseconds(300);
            var startTime = DateTime.Now;

            _scrollAnimationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };

            _scrollAnimationTimer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - startTime;
                var progress = Math.Min(1.0, elapsed.TotalMilliseconds / duration.TotalMilliseconds);

                var easedProgress = 1 - Math.Pow(1 - progress, 3);
                
                var newOffset = currentOffset + (distance * easedProgress);
                scrollViewer.ScrollToVerticalOffset(newOffset);

                if (progress >= 1.0)
                {
                    _scrollAnimationTimer.Stop();
                }
            };

            _scrollAnimationTimer.Start();
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (_searchTimer != null)
                {
                    _searchTimer.Stop();
                    _searchTimer.Tick -= SearchTimer_Tick;
                    _searchTimer = null;
                }

                if (_scrollAnimationTimer != null)
                {
                    _scrollAnimationTimer.Stop();
                    _scrollAnimationTimer = null;
                }

                if (DataContext is SceneExplorerViewModel viewModel)
                {
                    viewModel.ScrollToItemRequested -= OnScrollToItemRequested;
                }

                this.DataContextChanged -= SceneExplorerView_DataContextChanged;
                this.PreviewKeyDown -= SceneExplorerView_PreviewKeyDown;
                this.Unloaded -= SceneExplorerView_Unloaded;

                _disposed = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing SceneExplorerView: {ex.Message}");
            }
        }

        #endregion
    }
}
