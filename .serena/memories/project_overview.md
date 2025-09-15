# Project Overview

DANCustomTools is a .NET 8.0 WPF application designed for game development with the UbiArt Framework. It implements a sophisticated MainTool/SubTool architecture with MVVM pattern, dependency injection, and a pluggable tool system.

## Purpose
- Provides extensible tools for UbiArt Framework game development
- Scene exploration and editing capabilities
- Asset processing and cooking functionality
- Property editing interface for game objects

## Current Tools
- **Editor MainTool**: Split-pane layout with SceneExplorer (left) and PropertiesEditor (right)
- **AssetsCooker MainTool**: Asset processing with async cooking, logging, and progress tracking

## Key Dependencies
- External DLLs required: engineWrapper.dll, PluginCommon.dll, TechnoControls.dll (in ../../bin/)
- Material Design theming
- Hybrid WPF + Windows Forms integration
- Custom MVVM framework implementation