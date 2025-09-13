#nullable enable  
using DANCustomTools.MVVM;
using DANCustomTools.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;

namespace DANCustomTools.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private ViewModelBase? _currentViewModel;

        public ViewModelBase? CurrentViewModel
        {
            get { return _currentViewModel; }
            set
            {
                _currentViewModel = value;
                OnPropertyChanged();
            }
        }

        public ICommand OpenSceneExplorerCommand { get; }

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            OpenSceneExplorerCommand = new RelayCommand(OpenSceneExplorer);
            
            // Start with Scene Explorer view
            OpenSceneExplorer();
        }

        private void OpenSceneExplorer()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<SceneExplorerViewModel>();
        }
    }
}
