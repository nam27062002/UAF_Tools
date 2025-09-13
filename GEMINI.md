# Project Overview

This is a WPF application built with .NET 8 that serves as a custom toolset for a game engine. It allows developers to explore the scene hierarchy, view and edit object properties, and interact with the engine in real-time.

**Key Technologies:**

*   **.NET 8:** The underlying framework for the application.
*   **WPF:** Used for building the user interface.
*   **MVVM (Model-View-ViewModel):** The architectural pattern used to structure the application.
*   **CommunityToolkit.Mvvm:** A library that provides MVVM helpers.
*   **Microsoft.Extensions.DependencyInjection:** Used for dependency injection.

**Architecture:**

The application is divided into the following layers:

*   **Views:** The UI of the application, written in XAML.
*   **ViewModels:** The logic that drives the UI.
*   **Models:** The data structures that represent the application's data.
*   **Services:** The services that provide the application's functionality, such as connecting to the engine and managing the scene tree.

# Building and Running

To build and run the project, you will need to have the .NET 8 SDK installed.

1.  **Restore Dependencies:**
    ```
    dotnet restore
    ```
2.  **Build the Project:**
    ```
    dotnet build
    ```
3.  **Run the Project:**
    ```
    dotnet run
    ```

# Development Conventions

*   **Coding Style:** The project follows the standard C# coding conventions.
*   **MVVM:** The project uses the MVVM pattern, so all UI logic should be placed in the ViewModels.
*   **Dependency Injection:** The project uses dependency injection, so all services should be registered in the `App.xaml.cs` file.
*   **Async/Await:** The project uses async/await for all long-running operations.
