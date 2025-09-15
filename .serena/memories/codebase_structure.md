# Codebase Structure

## Directory Organization

```
DANCustomTools/
├── Core/                    # Framework abstractions and base classes
│   ├── Abstractions/        # Interfaces (IMainTool, ISubTool, IToolManager)
│   ├── Services/           # Core service implementations
│   └── ViewModels/         # Base view model classes
├── MVVM/                   # Custom MVVM framework
├── Tools/                  # Tool implementations
│   ├── Editor/             # Editor MainTool
│   │   ├── Views/          # XAML views
│   │   ├── ViewModels/     # View models
│   │   └── SubTools/       # SceneExplorer, PropertiesEditor
│   └── AssetsCooker/       # AssetsCooker MainTool
├── Services/               # Application services
├── ViewModels/             # Main application view models
├── Views/                  # Main application views
├── Models/                 # Data models
├── Controls/               # Custom WPF controls
├── Converters/            # Value converters
└── Images/                # Application resources
```

## Key Files
- `App.xaml.cs`: Application startup and DI configuration
- `MainWindow.xaml`: Main application shell with DataTemplates
- `ToolConfigurationService.cs`: Tool registration and setup
- `ToolManager.cs`: Tool lifecycle management
- `CLAUDE.md`: Project documentation and development guide

## External Dependencies
- `../../bin/engineWrapper.dll`: Engine communication wrapper
- `../../bin/PluginCommon.dll`: Common plugin utilities  
- `../../bin/TechnoControls.dll`: Custom WPF controls