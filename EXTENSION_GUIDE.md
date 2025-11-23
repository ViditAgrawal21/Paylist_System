# Integration & Extension Guide

## Adding New Features

### Adding a New Entity Type

1. **Create Model** in `SchoolPayListSystem.Core\Models\`:
```csharp
public class MyEntity
{
    public int MyEntityId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

2. **Add DbSet** in `SchoolPayListDbContext.cs`:
```csharp
public DbSet<MyEntity> MyEntities { get; set; }
```

3. **Configure Entity** in `OnModelCreating()`:
```csharp
modelBuilder.Entity<MyEntity>(entity =>
{
    entity.HasKey(e => e.MyEntityId);
    entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
});
```

4. **Create Repository** in `SchoolPayListSystem.Data\Repositories\`:
```csharp
public interface IMyEntityRepository : IRepository<MyEntity>
{
    Task<MyEntity> GetByNameAsync(string name);
}

public class MyEntityRepository : BaseRepository<MyEntity>, IMyEntityRepository
{
    public MyEntityRepository(SchoolPayListDbContext context) : base(context) { }
    
    public async Task<MyEntity> GetByNameAsync(string name)
    {
        return await _context.MyEntities.FirstOrDefaultAsync(e => e.Name == name);
    }
}
```

5. **Create Service** in `SchoolPayListSystem.Services\`:
```csharp
public interface IMyEntityService
{
    Task<MyEntity> GetByIdAsync(int id);
    Task<(bool success, string message)> AddAsync(string name);
}

public class MyEntityService : IMyEntityService
{
    private readonly IMyEntityRepository _repository;
    
    public MyEntityService(IMyEntityRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<MyEntity> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }
    
    public async Task<(bool success, string message)> AddAsync(string name)
    {
        // Implementation
    }
}
```

### Adding a New View

1. **Create ViewModel** in `SchoolPayListSystem.App\ViewModels\`:
```csharp
public class MyFeatureViewModel : BaseViewModel
{
    private string _myProperty;
    public string MyProperty
    {
        get => _myProperty;
        set => SetProperty(ref _myProperty, value);
    }
    
    public ICommand MyCommand => new RelayCommand(o => MyMethod());
    
    private void MyMethod()
    {
        // Implementation
    }
}
```

2. **Create XAML View** in `SchoolPayListSystem.App\Views\`:
```xml
<Window x:Class="SchoolPayListSystem.App.Views.MyFeatureView"
        Title="My Feature" Height="400" Width="600">
    <Grid>
        <!-- Your UI -->
    </Grid>
</Window>
```

3. **Create Code-Behind**:
```csharp
public partial class MyFeatureView : Window
{
    public MyFeatureView()
    {
        InitializeComponent();
        this.DataContext = new MyFeatureViewModel();
    }
}
```

4. **Wire Up in MenuView**:
```xml
<Button Content="My Feature" 
        Click="MyFeature_Click"/>
```

```csharp
private void MyFeature_Click(object sender, RoutedEventArgs e)
{
    var window = new MyFeatureView();
    window.Show();
}
```

## Dependency Injection (DIContainer)

Current setup uses manual DI in constructors. For larger projects, implement:

```csharp
// In App.xaml.cs or create ServiceProvider.cs
var services = new ServiceCollection();
services.AddScoped<SchoolPayListDbContext>();
services.AddScoped<IBranchRepository, BranchRepository>();
services.AddScoped<IBranchService, BranchService>();
services.AddScoped<BranchAdditionViewModel>();

var serviceProvider = services.BuildServiceProvider();
```

## Event Handling

### MVVM Command Pattern (Recommended)
```csharp
public ICommand MyCommand => new RelayCommand(
    execute: (param) => MyMethod(),
    canExecute: (param) => CanExecuteMethod()
);

private bool CanExecuteMethod() => true;
private void MyMethod() { }
```

### Direct Event Binding (for code-behind)
```xml
<Button Click="Button_Click"/>
```

```csharp
private void Button_Click(object sender, RoutedEventArgs e)
{
    // Handle event
}
```

## Data Binding

### Simple Property Binding
```xml
<TextBox Text="{Binding MyProperty, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
```

### Formatted Binding
```xml
<TextBlock Text="{Binding MyDate, StringFormat=dd-MM-yyyy}"/>
<TextBlock Text="{Binding MyAmount, StringFormat=N2}"/>
```

### Collection Binding
```xml
<DataGrid ItemsSource="{Binding MyCollection}"/>
<ComboBox ItemsSource="{Binding DropdownItems}" SelectedItem="{Binding SelectedItem}"/>
```

## Error Handling Pattern

```csharp
try
{
    IsLoading = true;
    var result = await _service.DoSomethingAsync();
    
    if (result.success)
    {
        _dialogService.ShowMessage("Success", result.message);
        // Update UI
    }
    else
    {
        _dialogService.ShowError("Error", result.message);
    }
}
catch (Exception ex)
{
    _dialogService.ShowError("Error", $"An error occurred: {ex.Message}");
}
finally
{
    IsLoading = false;
}
```

## Async/Await Best Practices

```csharp
// Good: Async all the way down
public async Task<Result> GetDataAsync()
{
    return await _repository.GetAsync();
}

// In ViewModel
private async void LoadDataAsync()
{
    var data = await _service.GetDataAsync();
}

// Call from UI (fire and forget is okay for void commands)
public ICommand LoadCommand => new RelayCommand(async (o) => await LoadDataAsync());
```

## Validation

### Model Validation
```csharp
if (string.IsNullOrWhiteSpace(input))
{
    return (false, "Input is required");
}

if (input.Length < 3)
{
    return (false, "Input must be at least 3 characters");
}
```

### UI Validation (WPF)
```xml
<TextBox>
    <TextBox.Text>
        <Binding Path="MyProperty" Mode="TwoWay" UpdateSourceTrigger="PropertyChanged">
            <Binding.ValidationRules>
                <ExceptionValidationRule/>
            </Binding.ValidationRules>
        </Binding>
    </TextBox.Text>
</TextBox>
```

## Database Querying

### LINQ Queries
```csharp
// Filter
var branches = await _context.Branches
    .Where(b => b.CreatedAt > DateTime.Now.AddMonths(-1))
    .ToListAsync();

// Join
var schools = await _context.Schools
    .Include(s => s.SchoolType)
    .Include(s => s.Branch)
    .Where(s => s.BranchId == 1)
    .ToListAsync();

// Group
var grouped = await _context.SalaryEntries
    .GroupBy(s => s.BranchId)
    .Select(g => new
    {
        BranchId = g.Key,
        Total = g.Sum(s => s.TotalAmount),
        Count = g.Count()
    })
    .ToListAsync();
```

## Report Generation Extension

### Adding Custom Report Type
```csharp
// In ReportService.cs
public async Task<MyReportDTO> GenerateMyReportAsync()
{
    var data = await _repository.GetAsync();
    // Process data
    return new MyReportDTO { /* populated */ };
}

// In HtmlPdfGenerator.cs
public async Task<(bool success, string message, string filePath)> GenerateMyReportAsync(MyReportDTO report)
{
    string html = GenerateMyReportHtml(report);
    string filePath = Path.Combine(_reportFolder, $"MyReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
    File.WriteAllText(filePath, html);
    return (true, "Report generated", filePath);
}

private string GenerateMyReportHtml(MyReportDTO report)
{
    // Build HTML
}
```

## Logging Implementation

Add logging (optional enhancement):

```powershell
Install-Package Serilog
Install-Package Serilog.Sinks.File
```

```csharp
var logger = new LoggerConfiguration()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Usage
logger.Information("Operation completed: {@Report}", report);
logger.Error(ex, "Error occurred");
```

## Unit Testing Setup

Create `SchoolPayListSystem.Tests\` project:

```csharp
using Xunit;
using Moq;

public class BranchServiceTests
{
    private readonly Mock<IBranchRepository> _mockRepository;
    private readonly BranchService _service;
    
    public BranchServiceTests()
    {
        _mockRepository = new Mock<IBranchRepository>();
        _service = new BranchService(_mockRepository.Object);
    }
    
    [Fact]
    public async Task AddBranch_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var result = await _service.AddBranchAsync(1, "Test Branch");
        
        // Assert
        Assert.True(result.success);
    }
}
```

## Performance Optimization

### Query Optimization
```csharp
// Bad: N+1 queries
var schools = await _context.Schools.ToListAsync();
foreach (var school in schools)
{
    var branch = await _context.Branches.FindAsync(school.BranchId); // Repeated queries
}

// Good: Single query with joins
var schools = await _context.Schools
    .Include(s => s.Branch)
    .ToListAsync();
```

### Pagination
```csharp
public async Task<PagedResult<SalaryEntry>> GetEntriesPaginatedAsync(int page, int pageSize)
{
    var totalCount = await _context.SalaryEntries.CountAsync();
    var entries = await _context.SalaryEntries
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
        
    return new PagedResult<SalaryEntry>
    {
        Items = entries,
        TotalCount = totalCount,
        PageNumber = page,
        PageSize = pageSize
    };
}
```

---

For more specific implementation details, refer to existing service and repository classes in the project.
