# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DANCustomTools is a .NET 8.0 WPF application that provides custom tools for interacting with a game engine. The application follows MVVM architecture pattern with dependency injection using Microsoft.Extensions DI container.

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

## Project Architecture

### Core Structure
- **MVVM Pattern**: Custom MVVM framework implementation in `MVVM/` folder
  - `ViewModelBase`, `ModelBase`, `ViewBase` as base classes
  - `ObservableObject` for property change notifications
  - `RelayCommand` and `AsyncRelayCommand` for command implementations

### Dependency Injection Setup
- Services are configured in `App.xaml.cs:ConfigureServices()`
- Registration order: Services first (in dependency order), then ViewModels
- ViewModels are resolved through the DI container in MainWindow

### Key Components

**Services** (in dependency order):
1. `ILogService` → `ConsoleLogService` - Logging functionality
2. `IEngineHostService` → `EngineHostService` - Engine communication layer
3. `ISceneExplorerService` → `SceneExplorerService` - Scene tree management
4. `IPropertiesEditorService` → `PropertiesEditorService` - Property editing

**Main Features**:
- **Scene Explorer**: Tree-based scene hierarchy viewer with models in `Models/SceneExplorer/`
- **Properties Editor**: Dynamic property editing interface with models in `Models/PropertiesEditor/`

### External Dependencies
- References three external DLLs from `../../bin/`:
  - `engineWrapper.dll` - Engine communication wrapper
  - `PluginCommon.dll` - Common plugin utilities
  - `TechnoControls.dll` - Custom WPF controls
- These DLLs are essential for engine integration functionality

### Technology Stack
- .NET 8.0 with WPF and Windows Forms integration
- CommunityToolkit.Mvvm for MVVM helpers
- Microsoft.Extensions for DI and hosting
- Custom converters for data binding in `Converters/`

## Development Notes

- The application targets x64 architecture specifically (`win-x64` runtime identifier)
- Uses unsafe blocks for potential native interop with engine DLLs
- Main view switching is handled through `MainViewModel.CurrentViewModel` property
- Views are defined as XAML files in `Views/` with corresponding code-behind files