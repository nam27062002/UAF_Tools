# Tech Stack and Code Conventions

## Technology Stack
- **.NET 8.0**: Core runtime targeting Windows x64
- **WPF + Windows Forms**: Hybrid UI framework with WindowsFormsIntegration
- **Microsoft.Extensions.Hosting**: Application lifetime management and DI container
- **CommunityToolkit.Mvvm**: MVVM infrastructure and helpers
- **MaterialDesignThemes**: UI theming framework
- **Unsafe Code**: Enabled for engine communication via external DLLs

## Code Style and Conventions
- **Nullable Reference Types**: Enabled (`#nullable enable`)
- **Implicit Usings**: Enabled in project
- **Naming**: PascalCase for classes, methods, properties; camelCase for fields with underscore prefix
- **Architecture**: MVVM pattern with ViewModelBase inheritance
- **Dependency Injection**: Constructor injection pattern throughout
- **File Organization**: 
  - Views/ for XAML files
  - ViewModels/ for view model classes  
  - Services/ for business logic
  - Core/ for framework abstractions

## Design Patterns
- **MainTool/SubTool Pattern**: Hierarchical tool system
- **MVVM**: Strict separation of concerns
- **Dependency Injection**: Service locator pattern with Microsoft.Extensions.DI
- **Command Pattern**: RelayCommand and AsyncRelayCommand for UI interactions

## Key Interfaces
- `IMainTool` / `MainToolBase` - Main tool abstractions
- `ISubTool` / `SubToolBase` - Sub tool abstractions  
- `IToolManager` - Tool lifecycle management
- `IToolContext` - Inter-tool communication