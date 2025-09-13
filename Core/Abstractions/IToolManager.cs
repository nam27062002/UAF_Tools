#nullable enable
using System;
using System.Collections.Generic;

namespace DANCustomTools.Core.Abstractions
{
    public interface IToolManager
    {
        IReadOnlyCollection<IMainTool> MainTools { get; }
        IMainTool? CurrentMainTool { get; }

        event EventHandler<IMainTool?>? CurrentMainToolChanged;
        event EventHandler<ISubTool?>? CurrentSubToolChanged;

        void RegisterMainTool(IMainTool mainTool);
        void SwitchToMainTool(string mainToolName);
        void SwitchToSubTool(string mainToolName, string subToolName);
        IMainTool? GetMainTool(string name);
        ISubTool? GetSubTool(string mainToolName, string subToolName);

        void Initialize();
        void Cleanup();
    }
}