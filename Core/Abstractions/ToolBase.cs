#nullable enable
using DANCustomTools.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DANCustomTools.Core.Abstractions
{
    public abstract class MainToolBase : IMainTool
    {
        private readonly List<ISubTool> _subTools = new();
        private ISubTool? _currentSubTool;

        public abstract string Name { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public bool IsActive { get; set; }

        public IReadOnlyCollection<ISubTool> SubTools => _subTools.AsReadOnly();
        public ISubTool? CurrentSubTool => _currentSubTool;

        protected IServiceProvider ServiceProvider { get; }
        protected IToolContext ToolContext { get; }

        protected MainToolBase(IServiceProvider serviceProvider, IToolContext toolContext)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            ToolContext = toolContext ?? throw new ArgumentNullException(nameof(toolContext));
        }

        public abstract ViewModelBase CreateMainViewModel();

        public virtual void Initialize()
        {
            foreach (var subTool in _subTools)
            {
                subTool.Initialize();
            }
        }

        public virtual void Cleanup()
        {
            foreach (var subTool in _subTools)
            {
                subTool.Cleanup();
            }
        }

        public void RegisterSubTool(ISubTool subTool)
        {
            if (subTool == null) throw new ArgumentNullException(nameof(subTool));
            if (_subTools.Any(st => st.Name == subTool.Name))
                throw new InvalidOperationException($"SubTool with name '{subTool.Name}' already registered");

            _subTools.Add(subTool);
        }

        public void SwitchToSubTool(string subToolName)
        {
            var subTool = _subTools.FirstOrDefault(st => st.Name == subToolName);
            if (subTool == null)
                throw new ArgumentException($"SubTool '{subToolName}' not found");

            if (_currentSubTool != null)
                _currentSubTool.IsActive = false;

            _currentSubTool = subTool;
            _currentSubTool.IsActive = true;
        }
    }

    public abstract class SubToolBase : ISubTool
    {
        public abstract string Name { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public abstract IMainTool ParentTool { get; }
        public bool IsActive { get; set; }

        protected IServiceProvider ServiceProvider { get; }
        protected IToolContext ToolContext { get; }

        protected SubToolBase(IServiceProvider serviceProvider, IToolContext toolContext)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            ToolContext = toolContext ?? throw new ArgumentNullException(nameof(toolContext));
        }

        public abstract ViewModelBase CreateViewModel();

        public virtual void Initialize()
        {
        }

        public virtual void Cleanup()
        {
        }
    }
}