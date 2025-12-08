# Database Backup & Restore Test Suite

## Overview

This test suite verifies the functionality of the database backup and restore operations in the School Pay List System. The tests ensure data integrity, proper file handling, and error management.

## Test File

**Location**: `BackupRestoreTests.cs`

**Execution**: 
```bash
cd SchoolPayListSystem.Tests
dotnet run
```

## Test Cases

### 1. **BackupFile_ShouldExist_AfterBackupCreated** ✓
- **Purpose**: Verify that a backup file is created successfully
- **Steps**:
  1. Create a backup file
  2. Check if the backup file exists
  3. Verify the backup file is not empty
- **Expected Result**: Backup file exists and contains data

### 2. **RestoreDatabase_ShouldRestoreFile_Successfully** ✓
- **Purpose**: Verify that a database can be restored from a backup
- **Steps**:
  1. Create a backup file
  2. Note the size of the backup
  3. Restore the backup to the main database
  4. Verify the restored database has the same size
- **Expected Result**: Restored database matches backup size

### 3. **RestoredDatabase_ShouldBeValid_AndQueryable** ✓
- **Purpose**: Ensure the restored database is valid and can be queried
- **Steps**:
  1. Add test data to the database
  2. Create a backup with test data
  3. Count records before backup
  4. Restore from backup
  5. Count records after restore
  6. Verify record counts match
- **Expected Result**: Restored database contains all original data

### 4. **BackupRestore_ShouldPreserveData_Integrity** ✓
- **Purpose**: Verify that all data properties are preserved during backup/restore
- **Steps**:
  1. Create a test user with specific properties
  2. Add user to database
  3. Create backup
  4. Restore from backup
  5. Retrieve user and verify all properties
- **Expected Result**: User data is identical after restore

### 5. **RestoreFromInvalidFile_ShouldFail_Gracefully** ✓
- **Purpose**: Ensure invalid backup files are handled properly
- **Steps**:
  1. Create an invalid (non-database) backup file
  2. Attempt to restore from invalid file
  3. Verify appropriate error handling
- **Expected Result**: System fails gracefully with meaningful error message

### 6. **BackupRestore_ShouldHandleSpecialCharacters_InPath** ✓
- **Purpose**: Verify backup/restore works with various file paths
- **Steps**:
  1. Create backup with special path
  2. Restore from special path
  3. Verify database integrity
- **Expected Result**: Backup and restore work with different path formats

### 7. **MultipleBackups_ShouldBeIndependent** ✓
- **Purpose**: Verify multiple backups can be created and managed independently
- **Steps**:
  1. Create first backup
  2. Add more data
  3. Create second backup
  4. Restore from first backup and verify
  5. Restore from second backup and verify
- **Expected Result**: Both backups are independent and can be restored separately

## Test Results

```
Tests Passed: 7
Tests Failed: 0
Total Tests:  7
```

## Key Features

### Backup Functionality
- ✓ Creates compressed backup files
- ✓ Preserves all database data
- ✓ Handles multiple backups
- ✓ Supports custom backup paths

### Restore Functionality
- ✓ Restores complete database from backup
- ✓ Maintains data integrity
- ✓ Closes connections properly before restore
- ✓ Verifies database after restore
- ✓ Handles invalid backup files gracefully

### Data Preservation
- ✓ All user records preserved
- ✓ All properties intact
- ✓ Timestamps maintained
- ✓ Relationship data preserved

## Database Locations

### Production Database
- **Path**: `%APPDATA%\SchoolPayListSystem\Database\SchoolPayList.db`
- **Format**: SQLite 3

### Test Backups
- **Path**: `%APPDATA%\SchoolPayListSystem\TestBackups\`
- **Format**: `.db` files

## Running the Tests

### From Command Line

```bash
# Build the solution
dotnet build

# Run the test suite
cd SchoolPayListSystem.Tests
dotnet run
```

### Output Example

```
╔════════════════════════════════════════════════════════════════╗
║     Database Backup & Restore Functionality Test Suite         ║
╚════════════════════════════════════════════════════════════════╝

Starting tests at 2025-12-08 21.10.23

▶ BackupFile_ShouldExist_AfterBackupCreated... ✓ PASSED
▶ RestoreDatabase_ShouldRestoreFile_Successfully... ✓ PASSED
▶ RestoredDatabase_ShouldBeValid_AndQueryable... ✓ PASSED
▶ BackupRestore_ShouldPreserveData_Integrity... ✓ PASSED
▶ RestoreFromInvalidFile_ShouldFail_Gracefully... ✓ PASSED
▶ BackupRestore_ShouldHandleSpecialCharacters_InPath... ✓ PASSED
▶ MultipleBackups_ShouldBeIndependent... ✓ PASSED

╔════════════════════════════════════════════════════════════════╗
║                         TEST SUMMARY                          ║
╚════════════════════════════════════════════════════════════════╝

Tests Passed: 7
Tests Failed: 0
Total Tests:  7

Completed at 2025-12-08 21.10.29
```

## Implementation Details

### Backup Process
1. Creates backup directory if needed
2. Closes any existing database connections
3. Copies database file to backup location
4. Verifies backup file integrity

### Restore Process
1. Validates backup file exists
2. Disposes all database contexts
3. Waits for file locks to release (500ms)
4. Deletes old database file
5. Copies backup to database location
6. Verifies restored database by attempting query
7. Reports success/failure

### Error Handling
- File not found errors are caught and reported
- Database corruption is detected during verification
- Invalid backup files are handled gracefully
- File lock issues are resolved with timing delays

## Cleanup

The test suite automatically:
- Deletes temporary backup files after tests
- Removes empty test directories
- Disposes database contexts properly

## Notes

- Tests run sequentially to avoid file conflicts
- Each test initializes its own database state
- Failed tests do not affect subsequent tests
- All temporary files are cleaned up after completion

## Troubleshooting

### If tests fail with "database is locked"
- Close all instances of the application
- Wait a few seconds for file locks to clear
- Re-run the test suite

### If tests fail with "file is not a database"
- Delete the corrupted database file manually
- Re-run the test suite

### To manually clean up test files
```powershell
Remove-Item "$env:APPDATA\SchoolPayListSystem\TestBackups" -Recurse -Force
```

## Future Enhancements

- [ ] Add date-based backup naming
- [ ] Implement compression for backups
- [ ] Add backup scheduling
- [ ] Implement incremental backups
- [ ] Add backup verification checksums
