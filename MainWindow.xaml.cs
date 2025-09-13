using DANCustomTools.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DANCustomTools;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Set DataContext using DI container
        if (App.ServiceProvider != null)
        {
            DataContext = App.ServiceProvider.GetRequiredService<MainViewModel>();
        }
    }
}