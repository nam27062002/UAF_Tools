using DANCustomTools.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Windows;

namespace DANCustomTools;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        if (App.ServiceProvider != null)
        {
            _viewModel = App.ServiceProvider.GetRequiredService<MainViewModel>();
            DataContext = _viewModel;
        }

        this.Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MainWindow closing - starting cleanup...");

            _viewModel?.Dispose();
            _viewModel = null;

            System.Diagnostics.Debug.WriteLine("ViewModel disposed");

            DataContext = null;

            System.Diagnostics.Debug.WriteLine("DataContext cleared");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during window close: {ex.Message}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        try
        {
            System.Diagnostics.Debug.WriteLine("MainWindow closed - forcing app shutdown");
            
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
            Environment.Exit(0);
        }
    }
}