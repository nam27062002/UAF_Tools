using DANCustomTools.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Windows;

namespace DANCustomTools;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // Set DataContext using DI container
        if (App.ServiceProvider != null)
        {
            _viewModel = App.ServiceProvider.GetRequiredService<MainViewModel>();
            DataContext = _viewModel;
        }

        // Handle window closing
        this.Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        try
        {
            // Dispose of the ViewModel which should dispose all services
            _viewModel?.Dispose();

            // Force application shutdown
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during window close: {ex.Message}");
            // Force exit even if there are errors
            Environment.Exit(0);
        }
    }
}