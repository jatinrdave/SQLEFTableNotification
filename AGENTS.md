<general_rules>
- The repository uses a GitHub Actions workflow (`.github/workflows/codeql.yml`) for CodeQL analysis on the `master` branch. This suggests a focus on code quality and security.
- The project appears to have been initially scaffolded using a Visual Studio extension like "RestApiNEx/ApiNCoreEx", which uses T4 templates for code generation. When adding new entities, services, or controllers, be aware of these templates and consider using them to maintain consistency. Look for `_readme.txt` and `_nugets.txt` files in project directories for more information.
- No specific coding style guidelines were found, but maintain the existing style in the files you modify.
</general_rules>
<repository_structure>
The repository is a .NET Solution (`SQLTableNotification.sln`) containing several C# projects that work together to provide SQL Server table change notifications. The main components are:

- **`SQLDBEntityNotifier`**: A .NET 6 library that provides the core functionality for SQL Server change tracking notifications using Entity Framework Core.
- **`SQLEFTableNotification`**: A larger application built on top of the notifier. It is a .NET 5 project and includes:
  - `SQLEFTableNotification.Api`: A REST API.
  - `SQLEFTableNotification.Domain`: The domain logic and object mapping.
  - `SQLEFTableNotification.Entity`: The database entities and context.
  - `SQLEFTableNotification.Console`: A console application for demonstrating the notification service.
- **Test Projects**:
  - `SQLDBEntityNotifier.Tests`: Contains xUnit tests for the `SQLDBEntityNotifier` library.
  - `SQLEFTableNotificationTests`: Contains MSTest tests for the `SQLEFTableNotification` application.
- **Configuration**: Connection strings and other settings are managed in `appsettings.json` files within the `SQLEFTableNotification.Api` and `SQLEFTableNotification.Console` projects.
</repository_structure>
<dependencies_and_installation>
- The project uses NuGet for package management.
- To restore all dependencies for the solution, run the following command from the root directory:
  ```
  dotnet restore
  ```
- The solution includes projects targeting both `.net5.0` and `.net6.0`. The necessary SDKs must be installed.
- Key dependencies include:
  - `Microsoft.EntityFrameworkCore`
  - `AutoMapper`
  - `Serilog`
  - `Swashbuckle.AspNetCore`
  - `xUnit` and `MSTest` for testing.
</dependencies_and_installation>
<testing_instructions>
The repository contains two main test projects:

1.  **`SQLDBEntityNotifier.Tests`** (xUnit): This project tests the `SQLDBEntityNotifier` library. To run these tests, execute the following command from the root of the repository:
    ```
    dotnet test SQLDBEntityNotifier.Tests/SQLDBEntityNotifier.Tests.csproj
    ```
    To also collect code coverage, as suggested in the library's `README.md`:
    ```
    dotnet test SQLDBEntityNotifier.Tests/SQLDBEntityNotifier.Tests.csproj --collect:"Code Coverage"
    ```

2.  **`SQLEFTableNotificationTests`** (MSTest): This project tests the main `SQLEFTableNotification` application. While no specific command was found, a general `dotnet test` command targeting this project should work if the database connection string in `appsettings.json` is configured correctly.
    ```
    dotnet test SQLEFTableNotificationTests/SQLEFTableNotification.Tests.csproj
    ```
</testing_instructions>
<pull_request_formatting>
</pull_request_formatting>
