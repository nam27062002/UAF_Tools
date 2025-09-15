# Task Completion Checklist

## When Completing Development Tasks

### Build Verification
1. **Build Check**: Run `dotnet build` to ensure compilation succeeds
2. **Dependency Check**: Verify external DLLs are present in ../../bin/:
   - engineWrapper.dll
   - PluginCommon.dll  
   - TechnoControls.dll
3. **Runtime Test**: Run `dotnet run` to verify application starts and functions

### Code Quality
- **No automated linting/formatting**: Project currently has no automated code quality tools
- **Manual Review**: Ensure code follows existing patterns and conventions
- **Service Registration**: If adding new services, update App.xaml.cs dependency injection setup
- **Tool Registration**: If adding new tools, update ToolConfigurationService.ConfigureTools()

### Testing
- **No Unit Tests**: Project currently has no automated test suite
- **Manual Testing**: Test functionality manually through the application UI
- **Integration Testing**: Verify tool switching and inter-tool communication works

### Documentation
- **Update CLAUDE.md**: If making architectural changes, update project documentation
- **Code Comments**: Add comments for complex business logic (nullable enabled)