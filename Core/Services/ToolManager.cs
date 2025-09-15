#nullable enable
using DANCustomTools.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DANCustomTools.Core.Services
{
    public class ToolManager : IToolManager, IDisposable
    {
        private readonly List<IMainTool> _mainTools = new();
        private IMainTool? _currentMainTool;
        private bool _disposed = false;

        public IReadOnlyCollection<IMainTool> MainTools => _mainTools.AsReadOnly();
        public IMainTool? CurrentMainTool => _currentMainTool;

        public event EventHandler<IMainTool?>? CurrentMainToolChanged;
        public event EventHandler<ISubTool?>? CurrentSubToolChanged;

        public void RegisterMainTool(IMainTool mainTool)
        {
            if (mainTool == null) throw new ArgumentNullException(nameof(mainTool));
            if (_mainTools.Any(mt => mt.Name == mainTool.Name))
                throw new InvalidOperationException($"MainTool with name '{mainTool.Name}' already registered");

            _mainTools.Add(mainTool);
        }

        public void SwitchToMainTool(string mainToolName)
        {
            var mainTool = GetMainTool(mainToolName);
            if (mainTool == null)
                throw new ArgumentException($"MainTool '{mainToolName}' not found");

            if (_currentMainTool != null)
            {
                _currentMainTool.IsActive = false;

                // Cleanup the current MainTool and its SubTools to release resources
                _currentMainTool.Cleanup();
            }

            _currentMainTool = mainTool;
            _currentMainTool.IsActive = true;

            CurrentMainToolChanged?.Invoke(this, _currentMainTool);

            // Notify about current sub tool change
            CurrentSubToolChanged?.Invoke(this, _currentMainTool.CurrentSubTool);
        }

        public void SwitchToSubTool(string mainToolName, string subToolName)
        {
            var mainTool = GetMainTool(mainToolName);
            if (mainTool == null)
                throw new ArgumentException($"MainTool '{mainToolName}' not found");

            // Switch to main tool if not current
            if (_currentMainTool != mainTool)
            {
                SwitchToMainTool(mainToolName);
            }

            // Switch to sub tool
            var previousSubTool = mainTool.CurrentSubTool;
            mainTool.SwitchToSubTool(subToolName);

            // Only fire event if sub tool actually changed
            if (previousSubTool != mainTool.CurrentSubTool)
            {
                CurrentSubToolChanged?.Invoke(this, mainTool.CurrentSubTool);
            }
        }

        public IMainTool? GetMainTool(string name)
        {
            return _mainTools.FirstOrDefault(mt => mt.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public ISubTool? GetSubTool(string mainToolName, string subToolName)
        {
            var mainTool = GetMainTool(mainToolName);
            return mainTool?.SubTools.FirstOrDefault(st => st.Name.Equals(subToolName, StringComparison.OrdinalIgnoreCase));
        }

        public void Initialize()
        {
            foreach (var mainTool in _mainTools)
            {
                mainTool.Initialize();
            }

            // Activate first main tool by default if available
            if (_mainTools.Count > 0 && _currentMainTool == null)
            {
                SwitchToMainTool(_mainTools.First().Name);
            }
        }

        public void Cleanup()
        {
            if (_currentMainTool != null)
            {
                _currentMainTool.IsActive = false;
            }

            foreach (var mainTool in _mainTools)
            {
                mainTool.Cleanup();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Cleanup();
                _disposed = true;
            }
        }
    }
}