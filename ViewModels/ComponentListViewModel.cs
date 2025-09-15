#nullable enable
using DANCustomTools.Models.ActorCreate;
using DANCustomTools.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace DANCustomTools.ViewModels
{
    public class ComponentListViewModel : ViewModelBase
    {
        private readonly ObservableCollection<ComponentItem> _availableComponents = new();
        private readonly ObservableCollection<ComponentItem> _usedComponents = new();
        private readonly ICollectionView _filteredAvailableComponents;

        private string _filterText = string.Empty;
        private ComponentItem? _selectedAvailableComponent;
        private ComponentItem? _selectedUsedComponent;
        private ComponentItem? _clipboardComponent;

        public ComponentListViewModel()
        {
            _filteredAvailableComponents = CollectionViewSource.GetDefaultView(_availableComponents);
            _filteredAvailableComponents.Filter = FilterAvailableComponents;

            InitializeCommands();
            LoadDefaultComponents();
        }

        #region Properties

        public ICollectionView FilteredAvailableComponents => _filteredAvailableComponents;
        public ObservableCollection<ComponentItem> UsedComponents => _usedComponents;

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    _filteredAvailableComponents.Refresh();
                }
            }
        }

        public ComponentItem? SelectedAvailableComponent
        {
            get => _selectedAvailableComponent;
            set => SetProperty(ref _selectedAvailableComponent, value);
        }

        public ComponentItem? SelectedUsedComponent
        {
            get => _selectedUsedComponent;
            set => SetProperty(ref _selectedUsedComponent, value);
        }

        #endregion

        #region Commands

        public ICommand AddComponentCommand { get; private set; } = null!;
        public ICommand RemoveComponentCommand { get; private set; } = null!;
        public ICommand AddAllComponentsCommand { get; private set; } = null!;
        public ICommand RemoveAllComponentsCommand { get; private set; } = null!;
        public ICommand MoveUpCommand { get; private set; } = null!;
        public ICommand MoveDownCommand { get; private set; } = null!;
        public ICommand CutCommand { get; private set; } = null!;
        public ICommand CopyCommand { get; private set; } = null!;
        public ICommand PasteCommand { get; private set; } = null!;
        public ICommand DeleteCommand { get; private set; } = null!;
        public ICommand ClearFilterCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddComponentCommand = new RelayCommand<ComponentItem>(ExecuteAddComponent, CanExecuteAddComponent);
            RemoveComponentCommand = new RelayCommand<ComponentItem>(ExecuteRemoveComponent, CanExecuteRemoveComponent);
            AddAllComponentsCommand = new RelayCommand(ExecuteAddAllComponents, CanExecuteAddAllComponents);
            RemoveAllComponentsCommand = new RelayCommand(ExecuteRemoveAllComponents, CanExecuteRemoveAllComponents);
            MoveUpCommand = new RelayCommand<ComponentItem>(ExecuteMoveUp, CanExecuteMoveUp);
            MoveDownCommand = new RelayCommand<ComponentItem>(ExecuteMoveDown, CanExecuteMoveDown);
            CutCommand = new RelayCommand<ComponentItem>(ExecuteCut, CanExecuteCut);
            CopyCommand = new RelayCommand<ComponentItem>(ExecuteCopy, CanExecuteCopy);
            PasteCommand = new RelayCommand(ExecutePaste, CanExecutePaste);
            DeleteCommand = new RelayCommand<ComponentItem>(ExecuteDelete, CanExecuteDelete);
            ClearFilterCommand = new RelayCommand(ExecuteClearFilter);
        }

        #endregion

        #region Command Implementations

        private bool CanExecuteAddComponent(ComponentItem? component)
        {
            return component != null && !_usedComponents.Any(c => c.Name == component.Name);
        }

        private void ExecuteAddComponent(ComponentItem? component)
        {
            if (component != null && CanExecuteAddComponent(component))
            {
                var newComponent = component.Clone();
                _usedComponents.Add(newComponent);
                OnComponentAdded(newComponent);
            }
        }

        private bool CanExecuteRemoveComponent(ComponentItem? component)
        {
            return component != null && _usedComponents.Contains(component);
        }

        private void ExecuteRemoveComponent(ComponentItem? component)
        {
            if (component != null && _usedComponents.Remove(component))
            {
                OnComponentRemoved(component);
            }
        }

        private bool CanExecuteAddAllComponents()
        {
            return _filteredAvailableComponents.Cast<ComponentItem>().Any(c => !_usedComponents.Any(u => u.Name == c.Name));
        }

        private void ExecuteAddAllComponents()
        {
            var componentsToAdd = _filteredAvailableComponents.Cast<ComponentItem>()
                .Where(c => !_usedComponents.Any(u => u.Name == c.Name))
                .ToList();

            foreach (var component in componentsToAdd)
            {
                var newComponent = component.Clone();
                _usedComponents.Add(newComponent);
                OnComponentAdded(newComponent);
            }
        }

        private bool CanExecuteRemoveAllComponents()
        {
            return _usedComponents.Count > 0;
        }

        private void ExecuteRemoveAllComponents()
        {
            var componentsToRemove = _usedComponents.ToList();
            _usedComponents.Clear();

            foreach (var component in componentsToRemove)
            {
                OnComponentRemoved(component);
            }
        }

        private bool CanExecuteMoveUp(ComponentItem? component)
        {
            return component != null && _usedComponents.IndexOf(component) > 0;
        }

        private void ExecuteMoveUp(ComponentItem? component)
        {
            if (component != null)
            {
                var index = _usedComponents.IndexOf(component);
                if (index > 0)
                {
                    _usedComponents.RemoveAt(index);
                    _usedComponents.Insert(index - 1, component);
                    SelectedUsedComponent = component;
                    OnComponentMoved(component, index - 1);
                }
            }
        }

        private bool CanExecuteMoveDown(ComponentItem? component)
        {
            return component != null && _usedComponents.IndexOf(component) < _usedComponents.Count - 1;
        }

        private void ExecuteMoveDown(ComponentItem? component)
        {
            if (component != null)
            {
                var index = _usedComponents.IndexOf(component);
                if (index >= 0 && index < _usedComponents.Count - 1)
                {
                    _usedComponents.RemoveAt(index);
                    _usedComponents.Insert(index + 1, component);
                    SelectedUsedComponent = component;
                    OnComponentMoved(component, index + 1);
                }
            }
        }

        private bool CanExecuteCut(ComponentItem? component)
        {
            return component != null && _usedComponents.Contains(component);
        }

        private void ExecuteCut(ComponentItem? component)
        {
            if (component != null)
            {
                _clipboardComponent = component.Clone();
                ExecuteRemoveComponent(component);
            }
        }

        private bool CanExecuteCopy(ComponentItem? component)
        {
            return component != null;
        }

        private void ExecuteCopy(ComponentItem? component)
        {
            if (component != null)
            {
                _clipboardComponent = component.Clone();
            }
        }

        private bool CanExecutePaste()
        {
            return _clipboardComponent != null && !_usedComponents.Any(c => c.Name == _clipboardComponent.Name);
        }

        private void ExecutePaste()
        {
            if (_clipboardComponent != null && CanExecutePaste())
            {
                var newComponent = _clipboardComponent.Clone();
                _usedComponents.Add(newComponent);
                OnComponentAdded(newComponent);
            }
        }

        private bool CanExecuteDelete(ComponentItem? component)
        {
            return CanExecuteRemoveComponent(component);
        }

        private void ExecuteDelete(ComponentItem? component)
        {
            ExecuteRemoveComponent(component);
        }

        private void ExecuteClearFilter()
        {
            FilterText = string.Empty;
        }

        #endregion

        #region Public Methods

        public void LoadAvailableComponents(IEnumerable<string> componentNames)
        {
            _availableComponents.Clear();

            foreach (var name in componentNames)
            {
                _availableComponents.Add(new ComponentItem
                {
                    Name = name,
                    Type = "Component",
                    Description = $"Actor component: {name}"
                });
            }

            _filteredAvailableComponents.Refresh();
        }

        public void LoadUsedComponents(IEnumerable<string> componentNames)
        {
            _usedComponents.Clear();

            foreach (var name in componentNames)
            {
                var component = _availableComponents.FirstOrDefault(c => c.Name == name);
                if (component != null)
                {
                    _usedComponents.Add(component.Clone());
                }
                else
                {
                    _usedComponents.Add(new ComponentItem
                    {
                        Name = name,
                        Type = "Component",
                        Description = $"Actor component: {name}"
                    });
                }
            }
        }

        public List<string> GetUsedComponentNames()
        {
            return _usedComponents.Select(c => c.Name).ToList();
        }

        public void HandleDrop(System.Windows.DragEventArgs e, bool isUsedComponentsList)
        {
            if (e.Data.GetDataPresent(typeof(ComponentItem)))
            {
                var component = e.Data.GetData(typeof(ComponentItem)) as ComponentItem;
                if (component != null)
                {
                    if (isUsedComponentsList)
                    {
                        ExecuteAddComponent(component);
                    }
                    else
                    {
                        ExecuteRemoveComponent(component);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private bool FilterAvailableComponents(object item)
        {
            if (item is ComponentItem component)
            {
                if (string.IsNullOrWhiteSpace(_filterText))
                    return true;

                return component.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                       component.Type.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                       component.Description.Contains(_filterText, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private void LoadDefaultComponents()
        {
            var defaultComponents = new[]
            {
                "AnimatedComponent",
                "LightComponent",
                "SoundComponent",
                "PhysicsComponent",
                "TriggerComponent",
                "PlayerComponent",
                "EnemyComponent",
                "CollectibleComponent",
                "PlatformComponent",
                "CameraComponent",
                "HealthComponent",
                "InventoryComponent",
                "DialogueComponent",
                "QuestComponent",
                "WeaponComponent"
            };

            LoadAvailableComponents(defaultComponents);
        }

        #endregion

        #region Events

        public event EventHandler<ComponentItem>? ComponentAdded;
        public event EventHandler<ComponentItem>? ComponentRemoved;
        public event EventHandler<ComponentMoveEventArgs>? ComponentMoved;

        private void OnComponentAdded(ComponentItem component)
        {
            ComponentAdded?.Invoke(this, component);
        }

        private void OnComponentRemoved(ComponentItem component)
        {
            ComponentRemoved?.Invoke(this, component);
        }

        private void OnComponentMoved(ComponentItem component, int newIndex)
        {
            ComponentMoved?.Invoke(this, new ComponentMoveEventArgs(component, newIndex));
        }

        #endregion
    }

    public class ComponentItem : ViewModelBase
    {
        private string _name = string.Empty;
        private string _type = string.Empty;
        private string _description = string.Empty;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public ComponentItem Clone()
        {
            return new ComponentItem
            {
                Name = Name,
                Type = Type,
                Description = Description
            };
        }

        public override string ToString() => Name;

        public override bool Equals(object? obj)
        {
            return obj is ComponentItem other && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }
    }

    public class ComponentMoveEventArgs : EventArgs
    {
        public ComponentItem Component { get; }
        public int NewIndex { get; }

        public ComponentMoveEventArgs(ComponentItem component, int newIndex)
        {
            Component = component;
            NewIndex = newIndex;
        }
    }
}