# DEVELOPMENT SETUP GUIDE

## Prerequisites Installation

### 1. Visual Studio 2022 Setup
- Download and install Visual Studio 2022 Community Edition (free)
- Select "Desktop development with C++" and ".NET desktop development" workloads
- Include SQL Server LocalDB during installation

### 2. .NET 8 SDK
- Download from: https://dotnet.microsoft.com/download/dotnet/8.0
- Verify installation: `dotnet --version`

### 3. SQL Server LocalDB
- Included with Visual Studio 2022
- Verify: `sqllocaldb v` in PowerShell
- Start: `sqllocaldb start mssqllocaldb`

## Opening the Project

1. Open Visual Studio 2022
2. Click "Open a project or solution"
3. Navigate to: `C:\Users\agraw\OneDrive\Desktop\new_app_vs\SchoolPayListSystem`
4. Select `SchoolPayListSystem.sln`
5. Wait for solution to load (may take 1-2 minutes)

## Restoring Dependencies

### Method 1: Visual Studio (Recommended)
- Solution loads automatically
- Click Tools → NuGet Package Manager → Package Manager Console
- Visual Studio restores packages automatically

### Method 2: Command Line
```powershell
cd "C:\Users\agraw\OneDrive\Desktop\new_app_vs\SchoolPayListSystem"
dotnet restore
```

## Building the Solution

### Method 1: Visual Studio
- Press `Ctrl+Shift+B` or Build → Build Solution

### Method 2: Command Line
```powershell
dotnet build SchoolPayListSystem.sln --configuration Release
```

## Running the Application

### Method 1: Visual Studio
- Press `F5` or Debug → Start Debugging
- Or Ctrl+F5 to run without debugging

### Method 2: Command Line
```powershell
cd SchoolPayListSystem.App
dotnet run
```

## Debugging

### Visual Studio Debugger
- Set breakpoints by clicking in the left margin
- Press F9 to toggle breakpoints
- F10 to step over, F11 to step into
- Use Debug → Windows → Locals to view variables

### Output Window
- View → Output to see debug messages
- Check for any startup errors or warnings

## Modifying Code

### Adding a New Service
1. Create new file in `SchoolPayListSystem.Services\`
2. Implement `IMyService` interface
3. Update ViewModels to inject service via constructor
4. Update DI container if used

### Adding a New View
1. Create XAML file in `SchoolPayListSystem.App\Views\`
2. Create code-behind .xaml.cs
3. Create corresponding ViewModel in `ViewModels\`
4. Set `DataContext = new MyViewModel()` in code-behind

### Modifying Database Schema
1. Edit `SchoolPayListDbContext.cs` in `OnModelCreating()`
2. Delete existing `SchoolPayList.mdf` and `.ldf` files from AppData
3. Run application to recreate database with new schema

## NuGet Package Management

### Adding a Package
```powershell
# Via NuGet Package Manager
Tools → NuGet Package Manager → Package Manager Console
Install-Package PackageName

# Or via CLI
dotnet add package PackageName
```

### Current Dependencies
- Microsoft.EntityFrameworkCore (8.0.0)
- Microsoft.EntityFrameworkCore.SqlServer (8.0.0)

### Adding PDF Support (Optional)
```powershell
# Option 1: iText7
Install-Package itext7

# Option 2: PDFSharp
Install-Package PdfSharp
```

## Common Issues & Solutions

### Issue: "Cannot connect to LocalDB"
**Solution**:
```powershell
sqllocaldb stop mssqllocaldb
sqllocaldb delete mssqllocaldb
sqllocaldb create mssqllocaldb
sqllocaldb start mssqllocaldb
```

### Issue: "Build fails - missing packages"
**Solution**:
```powershell
dotnet clean
dotnet restore
dotnet build
```

### Issue: "XAML IntelliSense not working"
**Solution**:
- Rebuild solution: Ctrl+Shift+B
- Close and reopen Visual Studio
- Clear cache: Delete `.vs` folder in project directory

### Issue: "Database file locked"
**Solution**:
- Stop all instances of the application
- Restart Visual Studio
- Delete database files from `%APPDATA%\SchoolPayListSystem\Database\`

## Database Management Tools

### View LocalDB Instances
```powershell
sqllocaldb info
sqllocaldb info mssqllocaldb
```

### Connect via SQL Server Management Studio
1. Open SSMS
2. Server Name: `(localdb)\mssqllocaldb`
3. Authentication: Windows Authentication
4. Database: SchoolPayList

### Connect via Visual Studio
1. View → SQL Server Object Explorer
2. Add SQL Server → (LocalDB)\mssqllocaldb
3. Browse database and tables

## Publishing the Application

### Create Release Build
```powershell
dotnet publish SchoolPayListSystem.App\SchoolPayListSystem.App.csproj `
  --configuration Release `
  --framework net8.0-windows `
  --self-contained false
```

### Output Location
- `SchoolPayListSystem.App\bin\Release\net8.0-windows\publish\`
- Create installer using WiX Toolset or similar

## Performance Optimization Tips

1. **Async Operations**: Use async/await for database queries
2. **Lazy Loading**: Load related entities on-demand
3. **Connection Pooling**: EF Core handles automatically
4. **Indexing**: Add indexes on frequently queried fields
5. **Pagination**: Load data in chunks for large datasets

## Security Best Practices

1. **Never commit passwords**: Use environment variables
2. **Validate input**: Check all user inputs
3. **Use parameterized queries**: EF Core does this automatically
4. **Hash passwords**: Implemented with SHA256
5. **SQL Injection**: EF Core prevents by default

## Testing

### Unit Test Project (Optional)
```
Create new folder: SchoolPayListSystem.Tests
Add: xunit NuGet package
Create test files for services
```

### Running Tests
```powershell
dotnet test
```

## Version Control

### Initialize Git (if not already done)
```powershell
git init
git add .
git commit -m "Initial commit"
```

### .gitignore Example
```
bin/
obj/
.vs/
*.user
*.db-shm
*.db-wal
AppData/
```

## IDE Settings

### Recommended Visual Studio Extensions
1. **Productivity Power Tools**: Enhanced editing
2. **ReSharper**: Advanced code analysis (paid)
3. **GitHub Copilot**: AI code suggestions
4. **Live Share**: Collaborative coding

## Command Line Reference

```powershell
# Restore packages
dotnet restore

# Build solution
dotnet build --configuration Release

# Run application
dotnet run --project SchoolPayListSystem.App

# Create database migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# Clean build artifacts
dotnet clean
```

## Useful Visual Studio Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+B | Build Solution |
| Ctrl+Shift+B | Rebuild Solution |
| F5 | Start Debugging |
| Ctrl+F5 | Run without Debug |
| Ctrl+. | Quick Fix / Context Menu |
| F12 | Go to Definition |
| Shift+F12 | Find All References |
| Ctrl+H | Find and Replace |
| Ctrl+K, Ctrl+D | Format Document |

---

For more information, visit the README.md in the project root.
