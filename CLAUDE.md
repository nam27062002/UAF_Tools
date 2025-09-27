# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DANCustomTools is a .NET 8.0 WPF application that provides extensible tools for game development with the UbiArt Framework. The application uses a sophisticated MainTool/SubTool architecture pattern with MVVM, dependency injection, and a pluggable tool system.

## Build Commands

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Build in Release mode
dotnet build --configuration Release

# Run the application
dotnet run

# Clean build artifacts
dotnet clean

# Publish for deployment (x64 Windows)
dotnet publish --configuration Release --runtime win-x64 --self-contained false
```

**Note**: No test framework or linting commands are configured. The project relies on manual testing by running the application.

## Architecture Overview

### MainTool/SubTool System
The application is built around a hierarchical tool system:

- **MainTool**: Top-level tools that provide broad functionality (e.g., Editor, AssetsCooker)
- **SubTool**: Specialized tools within a MainTool that work together (e.g., SceneExplorer, PropertiesEditor within Editor)
- **Tool Manager**: Central orchestrator that manages tool lifecycle, switching, and communication

### Core Framework (`Core/`)

**Tool Abstractions**:
- `IMainTool` / `MainToolBase` - Interface and base class for main tools
- `ISubTool` / `SubToolBase` - Interface and base class for sub tools
- `IToolManager` / `ToolManager` - Manages tool registration, initialization, and switching
- `IToolContext` / `ToolContext` - Shared context for inter-tool communication

**Tool Configuration**:
- `IToolConfigurationService` - Handles tool registration and setup
- `IToolInitializer` - Manages tool initialization lifecycle
- All tools are registered in `ToolConfigurationService.ConfigureTools()`

### Current Tools (`Tools/`)

**Editor MainTool** (`Tools/Editor/`):
- **EditorMainTool**: Main container with split-pane layout
- **SceneExplorerSubTool**: Left panel - scene hierarchy browser
- **PropertiesEditorSubTool**: Right panel - object property editor
- Layout: Scene Explorer (left) | Properties Editor (right) with resizable splitter

**AssetsCooker MainTool** (`Tools/AssetsCooker/`):
- **AssetsCookerMainTool**: Asset processing and cooking tool
- Single-view tool with async cooking simulation, logging, and progress tracking

### Dependency Injection Architecture

**Configuration Structure** (`App.xaml.cs`):
```
1. ConfigureCoreServices() - Infrastructure (Log, Engine, Domain services)
2. ConfigureToolFramework() - Tool system (ToolManager, ToolContext, Configuration)
3. ConfigureViewModels() - UI layer ViewModels
```

**Service Registration Order**:
1. `ILogService` → `ConsoleLogService`
2. `IEngineHostService` → `EngineHostService`
3. `ISceneExplorerService` → `SceneExplorerService`
4. `IPropertiesEditorService` → `PropertiesEditorService`
5. Tool Framework Services (`IToolManager`, `IToolContext`, etc.)
6. ViewModels (registered as Transient)

### MVVM Framework (`MVVM/`)
- Custom MVVM implementation with base classes
- `ViewModelBase` with INotifyPropertyChanged and disposal support
- `RelayCommand` and `AsyncRelayCommand` for command binding
- DataTemplates in `MainWindow.xaml` automatically resolve ViewModels to Views

## Tool Development Patterns

### Adding a New MainTool
1. **Create Directory Structure**: `Tools/[ToolName]/` with subdirectories:
   - `Views/` - XAML views and code-behind
   - `ViewModels/` - MVVM ViewModels
   - `SubTools/` - If the tool will have sub-tools
2. **Implement Tool Class**: Create `[ToolName]MainTool.cs` inheriting `MainToolBase`
   - Override `Name`, `DisplayName`, `Description` properties
   - Implement `CreateMainViewModel()` to return the tool's ViewModel
   - Override `Initialize()` if custom initialization is needed
3. **Create ViewModel**: Inherit from `ViewModelBase` in `ViewModels/[ToolName]MainViewModel.cs`
4. **Create View**: XAML view in `Views/[ToolName]MainView.xaml` with corresponding code-behind
5. **Register Services**: Add tool-specific services to `App.xaml.cs:ConfigureCoreServices()`
6. **Configure Tool**: Add configuration in `ToolConfigurationService.ConfigureTools()`
7. **Register ViewModel**: Add to DI container in `App.xaml.cs:ConfigureViewModels()`
8. **Add DataTemplate**: Map ViewModel to View in `MainWindow.xaml` resources
9. **Add Navigation**: Create navigation command in `MainViewModel` for tool switching

### Adding a SubTool
1. **Create Directory**: `Tools/[MainTool]/SubTools/[SubToolName]/` with:
   - `Views/` - XAML views (typically UserControls)
   - `ViewModels/` - Inherit from `SubToolViewModelBase`
2. **Implement SubTool**: Create `[SubToolName]SubTool.cs` inheriting `SubToolBase`
   - Pass parent MainTool reference to constructor
   - Override `Name`, `DisplayName` properties
   - Implement `CreateMainViewModel()` to return SubTool's ViewModel
3. **Register in Parent**: Call `RegisterSubTool()` in parent MainTool's `Initialize()` method
4. **Share UI Space**: SubTools share the parent MainTool's view area and communicate via `IToolContext`

### Inter-Tool Communication
- Use `IToolContext.ShareData()` / `GetSharedData<T>()` for data sharing
- Subscribe to `IToolManager` events for tool switching notifications
- SubTools within same MainTool can interact through shared context

## External Dependencies
- `engineWrapper.dll` - Engine communication wrapper (../../bin/)
- `PluginCommon.dll` - Common plugin utilities (../../bin/)
- `TechnoControls.dll` - Custom WPF controls (../../bin/)
- These DLLs are required for engine integration and must be present at runtime

## Technology Stack
- **.NET 8.0**: Core runtime targeting Windows x64 with unsafe blocks enabled
- **WPF + Windows Forms**: Hybrid UI framework with WindowsFormsIntegration
- **Microsoft.Extensions.Hosting**: Application lifetime management and DI container
- **CommunityToolkit.Mvvm**: MVVM infrastructure and helpers
- **MaterialDesignThemes**: UI theming framework
- **Native Interop**: Unsafe code enabled for engine communication via external DLLs

**Key NuGet Packages**:
- Microsoft.Windows.Compatibility (9.0.0)
- Microsoft.Extensions.DependencyInjection (8.0.0)
- CommunityToolkit.Mvvm (8.2.2)
- MaterialDesignThemes (5.1.0)

## Debugging and Development

### Prerequisites
- Ensure external dependencies are present in `../../bin/`:
  - `engineWrapper.dll`
  - `PluginCommon.dll`
  - `TechnoControls.dll`
- Visual Studio 2022 or VS Code with C# extension
- .NET 8.0 SDK

### Common Issues
- **DLL Loading Errors**: Verify external DLL paths are correct and accessible
- **Tool Initialization Failures**: Check `IToolInitializer.IsInitialized` status in logs
- **Service Resolution**: Ensure proper service registration order in `App.xaml.cs`
- **MVVM Binding Issues**: Verify DataTemplate mappings in `MainWindow.xaml`

### Logging
- Console logging available via `ILogService` → `ConsoleLogService`
- Tool initialization warnings logged during startup
- Service resolution errors captured during DI configuration