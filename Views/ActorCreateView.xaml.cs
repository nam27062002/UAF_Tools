#nullable enable
using DANCustomTools.Models.ActorCreate;
using DANCustomTools.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DANCustomTools.Views
{
    public partial class ActorCreateView : System.Windows.Controls.UserControl
    {
        public ActorCreateView()
        {
            InitializeComponent();
        }

        private void ActorItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem listBoxItem &&
                listBoxItem.Content is ActorInfo actorInfo &&
                DataContext is ActorCreateViewModel viewModel)
            {
                // Load the double-clicked actor
                if (viewModel.LoadActorCommand.CanExecute(actorInfo))
                {
                    viewModel.LoadActorCommand.Execute(actorInfo);
                }
            }
        }
    }
}