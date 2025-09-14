# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DANCustomTools is a .NET 8.0 WPF application that provides extensible tools for game development with the UbiArt Framework. The application uses a sophisticated MainTool/SubTool architecture pattern with MVVM, dependency injection, and a pluggable tool system.

## Build Commands

```bash
# Build the project
dotnet build

# Build in Release mode
dotnet build --configuration Release

# Run the application
dotnet run

# Clean build artifacts
dotnet clean
```

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
1. Create tool structure in `Tools/[ToolName]/`
2. Implement `MainToolBase` in `[ToolName]MainTool.cs`
3. Create ViewModel inheriting `ViewModelBase`
4. Create XAML View with code-behind
5. Register in `ToolConfigurationService.ConfigureTools()`
6. Add ViewModel to DI in `App.xaml.cs:ConfigureViewModels()`
7. Add DataTemplate to `MainWindow.xaml`
8. Add navigation command to `MainViewModel`

### Adding a SubTool
1. Create in `Tools/[MainTool]/SubTools/[SubToolName]/`
2. Implement `SubToolBase` with parent MainTool reference
3. Register in parent MainTool's `Initialize()` method
4. SubTools share the MainTool's UI space and communicate via `IToolContext`

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
- .NET 8.0 with WPF and Windows Forms integration
- Microsoft.Extensions.Hosting and DependencyInjection
- CommunityToolkit.Mvvm for MVVM helpers
- Targets x64 architecture with unsafe blocks enabled for native interop
- to memorize