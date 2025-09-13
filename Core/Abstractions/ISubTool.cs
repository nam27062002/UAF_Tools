#nullable enable
using DANCustomTools.MVVM;

namespace DANCustomTools.Core.Abstractions
{
    public interface ISubTool
    {
        string Name { get; }
        string DisplayName { get; }
        string Description { get; }
        ViewModelBase CreateViewModel();
        IMainTool ParentTool { get; }
        bool IsActive { get; set; }
        void Initialize();
        void Cleanup();
    }
}