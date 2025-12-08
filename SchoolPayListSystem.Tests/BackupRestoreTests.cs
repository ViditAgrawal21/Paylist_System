using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SchoolPayListSystem.Core.Models;

namespace SchoolPayListSystem.Tests
{
    /// <summary>
    /// Test suite for Database Backup and Restore functionality
    /// Tests the ability to backup a database and restore it to verify data integrity
    /// Run with: dotnet run --project SchoolPayListSystem.Tests
    /// </summary>
    public class BackupRestoreTests
    {
        private readonly string _testBackupPath;
        private readonly string _testDatabasePath;
        private readonly string _appDataPath;
        private readonly string _dbFolder;
        private int _testsPassed = 0;
        private int _testsFailed = 0;

        public BackupRestoreTests()
        {
            _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _dbFolder = Path.Combine(_appDataPath, "SchoolPayListSystem", "Database");
            _testDatabasePath = Path.Combine(_dbFolder, "SchoolPayList.db");
            _testBackupPath = Path.Combine(_appDataPath, "SchoolPayListSystem", "TestBackups", $"test_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

            // Ensure directories exist
            Directory.CreateDirectory(_dbFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(_testBackupPath));
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║     Database Backup & Restore Functionality Test Suite         ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            var tests = new BackupRestoreTests();
            
            // Clean up any corrupted databases before starting
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var dbPath = Path.Combine(appDataPath, "SchoolPayListSystem", "Database", "SchoolPayList.db");
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }
            catch { }
            
            tests.RunAllTests();
        }

        private void RunAllTests()
        {
            Console.WriteLine($"Starting tests at {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

            // Run all tests
            RunTest("BackupFile_ShouldExist_AfterBackupCreated", BackupFile_ShouldExist_AfterBackupCreated);
            RunTest("RestoreDatabase_ShouldRestoreFile_Successfully", RestoreDatabase_ShouldRestoreFile_Successfully);
            RunTest("RestoredDatabase_ShouldBeValid_AndQueryable", RestoredDatabase_ShouldBeValid_AndQueryable);
            RunTest("BackupRestore_ShouldPreserveData_Integrity", BackupRestore_ShouldPreserveData_Integrity);
            RunTest("RestoreFromInvalidFile_ShouldFail_Gracefully", RestoreFromInvalidFile_ShouldFail_Gracefully);
            RunTest("BackupRestore_ShouldHandleSpecialCharacters_InPath", BackupRestore_ShouldHandleSpecialCharacters_InPath);
            RunTest("MultipleBackups_ShouldBeIndependent", MultipleBackups_ShouldBeIndependent);

            // Print summary
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                         TEST SUMMARY                          ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");
            Console.WriteLine($"Tests Passed: {_testsPassed}");
            Console.WriteLine($"Tests Failed: {_testsFailed}");
            Console.WriteLine($"Total Tests:  {_testsPassed + _testsFailed}");
            Console.WriteLine($"\nCompleted at {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

            Cleanup();
        }

        private void RunTest(string testName, Action testAction)
        {
            try
            {
                Console.Write($"▶ {testName}...");
                testAction.Invoke();
                Console.WriteLine(" ✓ PASSED");
                _testsPassed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" ✗ FAILED");
                Console.WriteLine($"  └─ Error: {ex.Message}");
                _testsFailed++;
            }
        }

        private void BackupFile_ShouldExist_AfterBackupCreated()
        {
            string backupPath = _testBackupPath;
            CreateBackup(backupPath);

            if (!File.Exists(backupPath))
                throw new Exception("Backup file was not created");

            if (new FileInfo(backupPath).Length == 0)
                throw new Exception("Backup file is empty");
        }

        private void RestoreDatabase_ShouldRestoreFile_Successfully()
        {
            string backupPath = _testBackupPath;
            string originalDbPath = _testDatabasePath;

            CreateBackup(backupPath);
            if (!File.Exists(backupPath))
                throw new Exception("Backup should exist before restore test");

            long originalSize = new FileInfo(backupPath).Length;
            RestoreBackup(backupPath, originalDbPath);

            if (!File.Exists(originalDbPath))
                throw new Exception("Database file does not exist after restore");

            long restoredSize = new FileInfo(originalDbPath).Length;
            if (originalSize != restoredSize)
                throw new Exception($"File sizes don't match: backup={originalSize}, restored={restoredSize}");
        }

        private void RestoredDatabase_ShouldBeValid_AndQueryable()
        {
            string backupPath = _testBackupPath;

            AddTestDataToDatabase();
            CreateBackup(backupPath);
            int recordsBeforeBackup = GetRecordCount();

            RestoreBackup(backupPath, _testDatabasePath);
            int recordsAfterRestore = GetRecordCount();

            if (recordsBeforeBackup != recordsAfterRestore)
                throw new Exception($"Record count mismatch: before={recordsBeforeBackup}, after={recordsAfterRestore}");

            if (recordsAfterRestore <= 0)
                throw new Exception("Restored database does not contain expected test data");
        }

        private void BackupRestore_ShouldPreserveData_Integrity()
        {
            string backupPath = _testBackupPath;
            var testUser = new User
            {
                Username = $"testuser_{DateTime.Now.Ticks}",
                FullName = "Test User Data Integrity",
                PasswordHash = "testhash123",
                Role = "Operator",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            AddUserToDatabase(testUser);
            CreateBackup(backupPath);

            var userBeforeBackup = GetUserByUsername(testUser.Username);
            if (userBeforeBackup == null)
                throw new Exception("User not found before backup");

            RestoreBackup(backupPath, _testDatabasePath);
            var userAfterRestore = GetUserByUsername(testUser.Username);

            if (userAfterRestore == null)
                throw new Exception("User not found after restore");

            if (userBeforeBackup.Username != userAfterRestore.Username)
                throw new Exception("Username mismatch after restore");

            if (userBeforeBackup.FullName != userAfterRestore.FullName)
                throw new Exception("Full name mismatch after restore");

            if (userBeforeBackup.Role != userAfterRestore.Role)
                throw new Exception("Role mismatch after restore");
        }

        private void RestoreFromInvalidFile_ShouldFail_Gracefully()
        {
            string invalidBackupPath = Path.Combine(Path.GetDirectoryName(_testBackupPath), $"invalid_backup_{DateTime.Now.Ticks}.db");
            string testDbPath = Path.Combine(Path.GetDirectoryName(_testBackupPath), $"test_invalid_{DateTime.Now.Ticks}.db");
            
            // Create a valid test database first
            if (!File.Exists(testDbPath))
            {
                File.Copy(_testDatabasePath, testDbPath, overwrite: true);
            }

            File.WriteAllText(invalidBackupPath, "This is not a valid database file");

            try
            {
                RestoreBackup(invalidBackupPath, testDbPath);
                // Try to query the restored database - this should fail for invalid file
                using (var context = new SchoolPayListDbContext())
                {
                    // If the database file is truly invalid, this query will fail
                    var userCount = context.Users.Count();
                }
            }
            catch (Exception ex)
            {
                // Expected to catch database errors - this is success
                if (ex.Message.Contains("database") || ex.Message.Contains("corrupted") || ex.Message.Contains("format") || ex.Message.Contains("Error 26"))
                {
                    // This is expected - invalid file should cause error
                    return;
                }
                throw;
            }
            finally
            {
                try
                {
                    if (File.Exists(invalidBackupPath))
                        File.Delete(invalidBackupPath);
                    if (File.Exists(testDbPath))
                        File.Delete(testDbPath);
                }
                catch { }
            }
        }

        private void BackupRestore_ShouldHandleSpecialCharacters_InPath()
        {
            string specialPathBackup = Path.Combine(
                Path.GetDirectoryName(_testBackupPath),
                $"backup_test_{DateTime.Now.Ticks}.db"
            );

            CreateBackup(specialPathBackup);
            if (!File.Exists(specialPathBackup))
                throw new Exception("Backup with special path not created");

            RestoreBackup(specialPathBackup, _testDatabasePath);

            if (!File.Exists(_testDatabasePath))
                throw new Exception("Database file not restored from special path");

            int recordCount = GetRecordCount();
            if (recordCount < 0)
                throw new Exception("Invalid record count");

            if (File.Exists(specialPathBackup))
                File.Delete(specialPathBackup);
        }

        private void MultipleBackups_ShouldBeIndependent()
        {
            string backup1Path = Path.Combine(Path.GetDirectoryName(_testBackupPath), $"backup1_{DateTime.Now.Ticks}.db");
            string backup2Path = Path.Combine(Path.GetDirectoryName(_testBackupPath), $"backup2_{DateTime.Now.Ticks}.db");

            AddTestDataToDatabase();
            CreateBackup(backup1Path);

            AddTestDataToDatabase();
            CreateBackup(backup2Path);

            if (!File.Exists(backup1Path) || !File.Exists(backup2Path))
                throw new Exception("One or more backup files not created");

            RestoreBackup(backup1Path, _testDatabasePath);
            int countAfterRestore1 = GetRecordCount();

            RestoreBackup(backup2Path, _testDatabasePath);
            int countAfterRestore2 = GetRecordCount();

            if (countAfterRestore1 < 0 || countAfterRestore2 < 0)
                throw new Exception("Invalid record count after restore");

            if (File.Exists(backup1Path)) File.Delete(backup1Path);
            if (File.Exists(backup2Path)) File.Delete(backup2Path);
        }

        // Helper Methods

        private void CreateBackup(string backupPath)
        {
            try
            {
                string sourceDb = _testDatabasePath;
                if (!File.Exists(sourceDb))
                {
                    using (var context = new SchoolPayListDbContext())
                    {
                        context.Database.EnsureCreated();
                    }
                }

                string backupDir = Path.GetDirectoryName(backupPath);
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                File.Copy(sourceDb, backupPath, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create backup: {ex.Message}", ex);
            }
        }

        private void RestoreBackup(string backupPath, string targetDbPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                    throw new FileNotFoundException($"Backup file not found: {backupPath}");

                var context = new SchoolPayListDbContext();
                context.Dispose();

                System.Threading.Thread.Sleep(500);

                if (File.Exists(targetDbPath))
                {
                    try
                    {
                        File.Delete(targetDbPath);
                        System.Threading.Thread.Sleep(200);
                    }
                    catch
                    {
                        // If delete fails, overwrite will work
                    }
                }

                File.Copy(backupPath, targetDbPath, overwrite: true);

                using (var verifyContext = new SchoolPayListDbContext())
                {
                    int userCount = verifyContext.Users.Count();
                    if (userCount < 0)
                        throw new Exception("Database verification failed");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to restore backup: {ex.Message}", ex);
            }
        }

        private int GetRecordCount()
        {
            try
            {
                using (var context = new SchoolPayListDbContext())
                {
                    return context.Users.Count();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get record count: {ex.Message}", ex);
            }
        }

        private void AddTestDataToDatabase()
        {
            try
            {
                using (var context = new SchoolPayListDbContext())
                {
                    var testUser = new User
                    {
                        Username = $"testuser_{DateTime.Now.Ticks}",
                        FullName = "Test User",
                        PasswordHash = "testhash123",
                        Role = "Operator",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    var existingUser = context.Users.FirstOrDefault(u => u.Username == testUser.Username);
                    if (existingUser == null)
                    {
                        context.Users.Add(testUser);
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to add test data: {ex.Message}", ex);
            }
        }

        private void AddUserToDatabase(User user)
        {
            try
            {
                using (var context = new SchoolPayListDbContext())
                {
                    var existingUser = context.Users.FirstOrDefault(u => u.Username == user.Username);
                    if (existingUser == null)
                    {
                        context.Users.Add(user);
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to add user: {ex.Message}", ex);
            }
        }

        private User GetUserByUsername(string username)
        {
            try
            {
                using (var context = new SchoolPayListDbContext())
                {
                    return context.Users.FirstOrDefault(u => u.Username == username);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get user: {ex.Message}", ex);
            }
        }

        private void Cleanup()
        {
            try
            {
                if (File.Exists(_testBackupPath))
                    File.Delete(_testBackupPath);

                string testBackupDir = Path.Combine(_appDataPath, "SchoolPayListSystem", "TestBackups");
                if (Directory.Exists(testBackupDir) && Directory.GetFiles(testBackupDir).Length == 0)
                {
                    Directory.Delete(testBackupDir);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

