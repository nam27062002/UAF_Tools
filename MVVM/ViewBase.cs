#nullable enable
using System;
using System.Windows;

namespace DANCustomTools.MVVM
{
    public abstract class ViewBase : Window, IView
    {
        public new ViewModelBase? DataContext
        {
            get => base.DataContext as ViewModelBase;
            set => base.DataContext = value;
        }

        protected ViewBase()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModelBase vm)
            {
                vm.OnViewLoaded();
            }
        }

        protected virtual void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModelBase vm)
            {
                vm.OnViewUnloaded();
            }
        }
    }

    public abstract class ViewBase<TViewModel> : ViewBase, IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        public new TViewModel? DataContext
        {
            get => base.DataContext as TViewModel;
            set => base.DataContext = value;
        }

        ViewModelBase? IView.DataContext
        {
            get => DataContext;
            set => DataContext = value as TViewModel;
        }
    }
}
