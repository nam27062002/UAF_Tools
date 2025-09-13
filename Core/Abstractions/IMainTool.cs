#nullable enable
using DANCustomTools.MVVM;
using System.Collections.Generic;

namespace DANCustomTools.Core.Abstractions
{
    public interface IMainTool
    {
        string Name { get; }
        string DisplayName { get; }
        string Description { get; }
        ViewModelBase CreateMainViewModel();
        IReadOnlyCollection<ISubTool> SubTools { get; }
        ISubTool? CurrentSubTool { get; }
        bool IsActive { get; set; }

        void Initialize();
        void Cleanup();
        void SwitchToSubTool(string subToolName);
        void RegisterSubTool(ISubTool subTool);
    }
}