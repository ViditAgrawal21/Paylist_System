using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SQLitePCL;

namespace SchoolPayListSystem.App
{
    public partial class CleanDatabaseWindow : Window
    {
        private const string AdminPassword = "ADCP@123";
        private bool _passwordValidated = false;

        public CleanDatabaseWindow()
        {
            InitializeComponent();
        }

        private void ValidatePassword_Click(object sender, RoutedEventArgs e)
        {
            string enteredPassword = PasswordBox.Password;

            if (string.IsNullOrEmpty(enteredPassword))
            {
                UpdateStatus("Error: Please enter a password", isError: true);
                return;
            }

            if (enteredPassword != AdminPassword)
            {
                UpdateStatus("Error: Invalid password. Please try again", isError: true);
                PasswordBox.Clear();
                return;
            }

            // Password is correct
            _passwordValidated = true;
            PasswordBorder.Visibility = Visibility.Collapsed;
            ConfirmationBorder.Visibility = Visibility.Visible;
            UpdateStatus("Password validated. Please review the warnings and confirm your action", isError: false);
            
            // Attach event handler to check confirmations
            Confirm1.Checked += ConfirmationCheckbox_Changed;
            Confirm1.Unchecked += ConfirmationCheckbox_Changed;
            Confirm2.Checked += ConfirmationCheckbox_Changed;
            Confirm2.Unchecked += ConfirmationCheckbox_Changed;
            Confirm3.Checked += ConfirmationCheckbox_Changed;
            Confirm3.Unchecked += ConfirmationCheckbox_Changed;
        }

        private void ConfirmationCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            // Enable delete button only if all three confirmations are checked
            if (Confirm1.IsChecked == true && Confirm2.IsChecked == true && Confirm3.IsChecked == true)
            {
                DeleteButton.IsEnabled = true;
            }
            else
            {
                DeleteButton.IsEnabled = false;
            }
        }

        private void DeleteDatabase_Click(object sender, RoutedEventArgs e)
        {
            // Final confirmation dialog
            MessageBoxResult result = MessageBox.Show(
                "This is your FINAL confirmation.\n\n" +
                "All data in the database will be permanently deleted and cannot be recovered.\n" +
                "The software will be reset to its initial state.\n\n" +
                "Click 'YES' to proceed with deletion, or 'NO' to cancel.",
                "FINAL CONFIRMATION - DELETE DATABASE",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                UpdateStatus("Database cleanup cancelled", isError: false);
                return;
            }

            // Proceed with database cleanup
            try
            {
                UpdateStatus("Cleaning database... Please wait...", isError: false);
                DeleteButton.IsEnabled = false;

                if (CleanDatabase())
                {
                    MessageBox.Show(
                        "Database has been successfully cleaned and reset to fresh state.\n\n" +
                        "The application will now restart.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Close all windows and restart application
                    RestartApplication();
                }
                else
                {
                    UpdateStatus("Error: Failed to clean database. Please try again or contact support", isError: true);
                    DeleteButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}", isError: true);
                DeleteButton.IsEnabled = true;
            }
        }

        private void CloseAllDatabaseConnections()
        {
            try
            {
                // Force SQLite to close all connections
                SQLitePCL.raw.sqlite3_shutdown();
                System.Threading.Thread.Sleep(500);
                
                // Force garbage collection multiple times
                for (int i = 0; i < 3; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(200);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing DB connections: {ex.Message}");
            }
        }

        private bool CleanDatabase()
        {
            try
            {
                // Close all database connections FIRST - this is critical
                CloseAllDatabaseConnections();

                // Get database file path
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SchoolPayListSystem", "Database");
                string dbPath = Path.Combine(appDataPath, "SchoolPayList.db");

                UpdateStatus($"Closing all SQLite connections...", isError: false);
                System.Threading.Thread.Sleep(1000);

                // Delete database and associated files
                if (File.Exists(dbPath))
                {
                    try
                    {
                        // Remove file attributes (read-only, system, hidden, etc)
                        FileInfo fileInfo = new FileInfo(dbPath);
                        fileInfo.Attributes = FileAttributes.Normal;
                        
                        // Delete the main database file
                        File.Delete(dbPath);
                        UpdateStatus($"Deleted main database file...", isError: false);
                    }
                    catch (IOException)
                    {
                        UpdateStatus($"Direct delete failed, attempting force delete via temp...", isError: false);
                        System.Threading.Thread.Sleep(500);

                        try
                        {
                            // Move to temp and delete
                            string tempFile = Path.Combine(Path.GetTempPath(), "SchoolPayList_" + Guid.NewGuid() + ".db");
                            File.SetAttributes(dbPath, FileAttributes.Normal);
                            File.Move(dbPath, tempFile, overwrite: true);
                            System.Threading.Thread.Sleep(200);
                            File.Delete(tempFile);
                            UpdateStatus($"Force deleted database file via temp...", isError: false);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Failed to delete database file: {ex.Message}");
                        }
                    }
                }

                // Delete SQLite lock files
                string[] lockFiles = { dbPath + "-wal", dbPath + "-shm", dbPath + "-journal" };
                foreach (var lockFile in lockFiles)
                {
                    if (File.Exists(lockFile))
                    {
                        try
                        {
                            File.SetAttributes(lockFile, FileAttributes.Normal);
                            File.Delete(lockFile);
                            UpdateStatus($"Deleted lock file: {Path.GetFileName(lockFile)}...", isError: false);
                        }
                        catch { }
                    }
                }

                // Delete backup files
                try
                {
                    if (Directory.Exists(appDataPath))
                    {
                        string[] backupPatterns = { "*.bak", "*.backup" };
                        foreach (var pattern in backupPatterns)
                        {
                            foreach (var backup in Directory.GetFiles(appDataPath, pattern))
                            {
                                try
                                {
                                    File.SetAttributes(backup, FileAttributes.Normal);
                                    File.Delete(backup);
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }

                UpdateStatus($"All database files deleted. Reinitializing fresh database...", isError: false);
                System.Threading.Thread.Sleep(500);

                // Initialize fresh database
                DatabaseInitializer.InitializeDatabase();

                UpdateStatus($"Fresh database created successfully!", isError: false);
                return true;
            }
            catch (Exception ex)
            {
                string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SchoolPayListSystem", "Database", "SchoolPayList.db");
                MessageBox.Show($"Error cleaning database: {ex.Message}\n\n" +
                    $"To manually clean:\n\n" +
                    $"1. Close this application completely\n" +
                    $"2. Delete the file:\n{dbPath}\n\n" +
                    $"3. Restart the application (database will be recreated)",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void RestartApplication()
        {
            // Close all windows
            foreach (Window window in Application.Current.Windows)
            {
                if (window != this)
                {
                    window.Close();
                }
            }

            // Close this window and application
            this.Close();
            Application.Current.Shutdown();

            // Optionally restart the application
            System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Ask for confirmation before closing
            if (_passwordValidated)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Are you sure you want to cancel? Any unsaved work will be lost.",
                    "Confirm Cancel",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.No)
                    return;
            }

            this.Close();
        }

        private void UpdateStatus(string message, bool isError)
        {
            StatusMessage.Text = message;
            StatusMessage.Foreground = new System.Windows.Media.SolidColorBrush(
                isError ? System.Windows.Media.Colors.Red : System.Windows.Media.Colors.Green
            );
        }
    }
}
