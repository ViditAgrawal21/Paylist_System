# School Pay List Generation System

## Overview
A complete Windows desktop application for managing school salary payroll systems using C# .NET WPF with MVVM architecture and Microsoft SQL Server LocalDB.

## Features

### 1. Authentication & User Management
- First-launch admin account creation
- SHA256 password hashing
- Secure login system
- User roles (Admin/User)

### 2. Master Data Management
- **Branch Addition**: Add, edit, delete branches with unique codes
- **School Type Management**: 3 default types (Primary School, High School, Junior College) + custom types
- **School Addition**: Manage schools with school type, bank account, and branch associations

### 3. Salary Entry
- Monthly salary/pay list entry with automatic total calculation
- Record-by-record navigation
- Add, modify, delete salary entries
- Multi-amount fields (Amount1, Amount2, Amount3)

### 4. Reporting
- **Branch-wise Report**: Salary entries grouped by branch with totals
- **School-wise Report**: School-specific salary information
- **Date-wise Report**: Date range filtering and reporting
- HTML-based report generation (printable/convertible to PDF)

### 5. Database Management
- **Backup**: Export .mdf + .ldf files to secure location
- **Restore**: Restore database from backup
- Automatic database initialization on first run
- LocalDB integration (no external database needed)

## Project Structure

```
SchoolPayListSystem/
├── SchoolPayListSystem.sln
├── SchoolPayListSystem.App/          (WPF UI)
│   ├── Views/
│   ├── ViewModels/
│   ├── Helpers/
│   ├── App.xaml
│   └── MainWindow.xaml
├── SchoolPayListSystem.Core/         (Models & DTOs)
│   ├── Models/
│   └── DTOs/
├── SchoolPayListSystem.Data/         (Database Access)
│   ├── Database/
│   ├── Repositories/
│   └── DataAccess/
├── SchoolPayListSystem.Services/     (Business Logic)
└── SchoolPayListSystem.Reports/      (PDF Generation)
```

## Database Schema

### Users Table
```sql
UserId (PK, Identity)
Username (Unique, Indexed)
PasswordHash (SHA256)
CreatedAt
IsActive
Role (Admin/User)
```

### Branch Table
```sql
BranchId (PK, Identity)
BranchCode (Unique, Indexed)
BranchName
CreatedAt
ModifiedAt
```

### SchoolType Table
```sql
SchoolTypeId (PK, Identity)
TypeName (Unique)
IsDefault (Boolean)
CreatedAt
```

### School Table
```sql
SchoolId (PK, Identity)
SchoolCode (Unique, Indexed)
SchoolName
SchoolTypeId (FK)
BankAccountNumber
BranchId (FK)
CreatedAt
ModifiedAt
```

### SalaryEntry Table
```sql
SalaryEntryId (PK, Identity)
EntryDate
SchoolId (FK)
AccountNumber
BranchId (FK)
Amount1 (Decimal 18,2)
Amount2 (Decimal 18,2)
Amount3 (Decimal 18,2)
TotalAmount (Decimal 18,2)
CreatedAt
ModifiedAt
```

## Technology Stack

- **Framework**: .NET 8
- **UI**: WPF (Windows Presentation Foundation)
- **Database**: Microsoft SQL Server LocalDB
- **ORM**: Entity Framework Core 8.0
- **Architecture**: MVVM (Model-View-ViewModel)
- **Security**: SHA256 Password Hashing

## Setup & Installation

### Prerequisites
- Windows 10 or later
- Visual Studio 2022 (or later)
- .NET 8 SDK
- SQL Server LocalDB (included with Visual Studio)

### Steps

1. **Clone/Open the Project**
   ```
   Open SchoolPayListSystem.sln in Visual Studio
   ```

2. **Restore NuGet Packages**
   - Visual Studio will automatically restore packages
   - Or run: `dotnet restore`

3. **Build Solution**
   ```
   Build > Build Solution (Ctrl+Shift+B)
   ```

4. **Run Application**
   - Press F5 or click Run
   - First launch: Create admin account
   - Subsequent launches: Login with admin credentials

5. **Database Location**
   - Database files stored in: `%APPDATA%\SchoolPayListSystem\Database\`
   - MDF File: `SchoolPayList.mdf`
   - LDF File: `SchoolPayList_log.ldf`

## Usage Guide

### First Time Setup
1. Launch application
2. Create Admin account with username and password (min 6 characters)
3. Login with created credentials
4. Navigate to Master Entry → Branch Addition (add at least one branch)
5. Add School Types (if needed)
6. Add Schools with required information

### Adding Salary Entries
1. Go to Pay List Entry → Monthly Salary Entry
2. Fill in:
   - Entry Date
   - School/College from dropdown
   - Account Number
   - Amount1, Amount2, Amount3 (auto-calculates total)
3. Click "Add New" to save entry
4. View all entries in the grid below

### Generating Reports
1. Select report type from Reports menu
2. Optionally filter by date range
3. Click "Generate Report"
4. Click "View Report" to open in default browser
5. Print or save as PDF from browser

### Backup Database
1. Go to Utilities → Backup Database
2. Select destination folder
3. System creates timestamped backup of .mdf and .ldf files

### Restore Database
1. Go to Utilities → Backup Database
2. Click "Restore from Backup"
3. Select the backup .mdf file
4. System replaces current database and restarts needed

## Key Classes & Services

### Authentication
- `AuthenticationService`: Login, registration, password hashing
- `IAuthenticationService`: Service interface

### Data Services
- `BranchService`: Branch CRUD operations
- `SchoolService`: School management
- `SchoolTypeService`: School type management
- `SalaryService`: Salary entry operations
- `ReportService`: Report data aggregation
- `BackupService`: Database backup/restore

### Repositories
- `IRepository<T>`: Generic repository interface
- `BranchRepository`, `SchoolRepository`, `SalaryEntryRepository`, etc.

### ViewModels
- `LoginViewModel`: Authentication logic
- `BranchAdditionViewModel`: Branch management UI logic
- `SchoolAdditionViewModel`: School management UI logic
- `SalaryEntryViewModel`: Salary entry UI logic
- `ReportBranchWiseViewModel`: Report generation logic
- `BackupRestoreViewModel`: Backup/restore UI logic

## Default Data

**School Types** (created automatically):
1. Primary School
2. High School
3. Junior College

**Default Branch** (created automatically):
1. Head Office (Branch Code: 1)

## Database Initialization

The `LocalDbInitializer` class:
- Creates database and tables on first run (EnsureCreated)
- Seeds default school types
- Adds default branch
- Runs automatically on application startup

## Password Security

Passwords are hashed using SHA256:
```csharp
using (var sha256 = SHA256.Create())
{
    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(hashedBytes);
}
```

## Report Generation

Reports are generated as HTML files with professional styling:
- Colors and formatting
- Borders and tables
- Summary sections
- Sortable/printable output

**Report Paths**: `%APPDATA%\SchoolPayListSystem\Reports\`

## Logging & Error Handling

- Try-catch blocks in all service methods
- Error messages displayed via DialogService
- Debug output available in Output window
- Validation rules for user input

## Future Enhancements

1. PDF Export (using iText7 or PDFSharp)
2. Excel Export for reports
3. Advanced filtering and search
4. Multi-user authentication with roles
5. Audit trail for all transactions
6. Email report notifications
7. Database migration history
8. Performance optimization for large datasets

## Troubleshooting

### Database Connection Issues
- Ensure LocalDB is installed: `sqllocaldb v`
- Check AppData folder permissions
- Clear cache and rebuild solution

### UI Not Responding
- Check for long-running database queries
- Review debug output for errors
- Ensure all services are properly injected

### Reports Not Generating
- Verify data exists in database
- Check Reports folder exists in AppData
- Review error message in dialog

## Support & Contribution

For issues or enhancements, refer to the code comments and service interfaces for extension points.

## License

This application is proprietary and developed for school management systems.

---

**Version**: 1.0.0  
**Last Updated**: 2025-01-15  
**Built with**: .NET 8, WPF, Entity Framework Core 8
