#nullable enable
namespace DANCustomTools.MVVM
{
    public interface IView
    {
        ViewModelBase? DataContext { get; set; }
        void Show();
        void Hide();
        void Close();
        bool? ShowDialog();
    }

    public interface IView<TViewModel> : IView where TViewModel : ViewModelBase
    {
        new TViewModel? DataContext { get; set; }
    }
}
