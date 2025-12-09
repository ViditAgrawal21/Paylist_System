    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Windows;
    using Microsoft.Win32;
    using SchoolPayListSystem.Services;
    using SchoolPayListSystem.Data.Database;

    namespace SchoolPayListSystem.App
    {
        public partial class BackupWindow : Window
        {
            private readonly BackupService _backupService;

            public BackupWindow()
            {
                InitializeComponent();
                _backupService = new BackupService();
                
                // Set default backup location
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string defaultBackupPath = Path.Combine(appDataPath, "SchoolPayListSystem", "Backups");
                BackupPathTextBox.Text = defaultBackupPath;
            }

            private void BrowseBackup_Click(object sender, RoutedEventArgs e)
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Select Backup Location",
                    Filter = "ZIP Files (*.zip)|*.zip|All Files (*.*)|*.*",
                    DefaultExt = ".zip",
                    FileName = $"SchoolPayList_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip"
                };

                if (dialog.ShowDialog() == true)
                {
                    BackupPathTextBox.Text = dialog.FileName;
                }
            }

            private async void CreateBackup_Click(object sender, RoutedEventArgs e)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(BackupPathTextBox.Text))
                    {
                        MessageBox.Show("Please select a backup location.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Create directory if it doesn't exist
                    string backupDir = Path.GetDirectoryName(BackupPathTextBox.Text);
                    if (!Directory.Exists(backupDir))
                    {
                        Directory.CreateDirectory(backupDir);
                    }

                    BackupStatus.Text = "Creating backup...";
                    BackupStatus.Foreground = System.Windows.Media.Brushes.Orange;

                    // Get database file path
                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string dbPath = Path.Combine(appDataPath, "SchoolPayListSystem", "Database", "SchoolPayList.db");

                    if (!File.Exists(dbPath))
                    {
                        MessageBox.Show("Database file not found. Cannot create backup.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string backupFilePath = BackupPathTextBox.Text;
                    string tempDbFile = null;

                    try
                    {
                        // Create a temporary copy of the database file (handles locked files)
                        tempDbFile = Path.Combine(Path.GetTempPath(), $"SchoolPayList_temp_{Guid.NewGuid()}.db");
                        File.Copy(dbPath, tempDbFile, true);

                        // Check if backup should be created as zip file
                        if (backupFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            // Remove existing zip if it exists
                            if (File.Exists(backupFilePath))
                            {
                                File.Delete(backupFilePath);
                            }

                            using (var zipArchive = ZipFile.Open(backupFilePath, ZipArchiveMode.Create))
                            {
                                // Add temporary database file to zip
                                zipArchive.CreateEntryFromFile(tempDbFile, Path.GetFileName(dbPath));
                            }
                        }
                        else
                        {
                            // Copy temporary file to backup location
                            File.Copy(tempDbFile, backupFilePath, true);
                        }

                        BackupStatus.Text = "✓ Backup completed successfully!";
                        BackupStatus.Foreground = System.Windows.Media.Brushes.Green;
                        
                        MessageBox.Show($"Database backup created successfully at:\n{backupFilePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    finally
                    {
                        // Clean up temporary file
                        try
                        {
                            if (!string.IsNullOrEmpty(tempDbFile) && File.Exists(tempDbFile))
                            {
                                File.Delete(tempDbFile);
                            }
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }
                }
                catch (Exception ex)
                {
                    BackupStatus.Text = $"✗ Error: {ex.Message}";
                    BackupStatus.Foreground = System.Windows.Media.Brushes.Red;
                    MessageBox.Show($"Error creating backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            private void BrowseRestore_Click(object sender, RoutedEventArgs e)
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Backup File to Restore",
                    Filter = "Database Files (*.db)|*.db|ZIP Files (*.zip)|*.zip|All Files (*.*)|*.*",
                    DefaultExt = ".db"
                };

                if (dialog.ShowDialog() == true)
                {
                    RestorePathTextBox.Text = dialog.FileName;
                }
            }

            private async void RestoreDatabase_Click(object sender, RoutedEventArgs e)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(RestorePathTextBox.Text))
                    {
                        MessageBox.Show("Please select a backup file to restore.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!File.Exists(RestorePathTextBox.Text))
                    {
                        MessageBox.Show("Selected backup file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var confirmResult = MessageBox.Show(
                        "This will overwrite the current database with the backup file.\n\nDo you want to continue?",
                        "Confirm Database Restore",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (confirmResult != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    RestoreStatus.Text = "Preparing to restore database...";
                    RestoreStatus.Foreground = System.Windows.Media.Brushes.Orange;

                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string dbDir = Path.Combine(appDataPath, "SchoolPayListSystem", "Database");
                    string dbPath = Path.Combine(dbDir, "SchoolPayList.db");

                    if (!Directory.Exists(dbDir))
                    {
                        Directory.CreateDirectory(dbDir);
                    }

                    string selectedFile = RestorePathTextBox.Text;
                    string extractedDbFile = null;

                    try
                    {
                        RestoreStatus.Text = "Extracting backup file...";

                        // Extract or identify the database file
                        if (selectedFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            string tempDir = Path.Combine(dbDir, $"temp_extract_{DateTime.Now:yyyyMMdd_HHmmss}");
                            Directory.CreateDirectory(tempDir);

                            try
                            {
                                ZipFile.ExtractToDirectory(selectedFile, tempDir);
                                var dbFiles = Directory.GetFiles(tempDir, "*.db", SearchOption.AllDirectories);
                                
                                if (dbFiles.Length == 0)
                                {
                                    throw new Exception("No database file (.db) found in the zip archive.");
                                }

                                extractedDbFile = dbFiles[0];
                            }
                            catch (Exception ex)
                            {
                                if (Directory.Exists(tempDir))
                                    Directory.Delete(tempDir, true);
                                throw new Exception($"Failed to extract zip file: {ex.Message}", ex);
                            }
                        }
                        else if (selectedFile.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                        {
                            extractedDbFile = selectedFile;
                        }
                        else
                        {
                            throw new Exception("Selected file must be a .db or .zip file containing a database.");
                        }

                        RestoreStatus.Text = "Closing application connections...";

                        // Close all other windows to release database connections
                        var windowsToClose = Application.Current.Windows.Cast<Window>().Where(w => w != this).ToList();
                        foreach (var window in windowsToClose)
                        {
                            try
                            {
                                window.Close();
                            }
                            catch { }
                        }

                        // Wait for windows to close
                        System.Threading.Thread.Sleep(500);

                        // Force garbage collection
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        System.Threading.Thread.Sleep(500);

                        RestoreStatus.Text = "Restoring database file...";
                        
                        // Update UI
                        Dispatcher.Invoke(() => { });

                        // Overwrite the database file directly (no delete needed)
                        File.Copy(extractedDbFile, dbPath, true);

                        RestoreStatus.Text = "Verifying restored data...";
                        
                        // Update UI
                        Dispatcher.Invoke(() => { });

                        System.Threading.Thread.Sleep(500);

                        // Verify restored database
                        try
                        {
                            using (var verifyContext = new SchoolPayListDbContext())
                            {
                                var userCount = verifyContext.Users.Count();
                                System.Diagnostics.Debug.WriteLine($"Database verification: {userCount} users found");
                            }
                        }
                        catch (Exception verifyEx)
                        {
                            throw new Exception($"Database verification failed after restore: {verifyEx.Message}", verifyEx);
                        }

                        RestoreStatus.Text = "✓ Database restored successfully!";
                        RestoreStatus.Foreground = System.Windows.Media.Brushes.Green;

                        MessageBox.Show(
                            "✓ Database has been restored successfully!\n\nThe application will restart to load the restored data.",
                            "Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        RestartApplication();
                    }
                    finally
                    {
                        // Clean up temporary directory
                        if (!string.IsNullOrEmpty(extractedDbFile) && selectedFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                string tempDir = Path.GetDirectoryName(extractedDbFile);
                                if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
                                {
                                    Directory.Delete(tempDir, true);
                                }
                            }
                            catch
                            {
                                // Ignore cleanup errors
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    RestoreStatus.Text = $"✗ Error: {ex.Message}";
                    RestoreStatus.Foreground = System.Windows.Media.Brushes.Red;
                    MessageBox.Show($"Error restoring database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            private void RestartApplication()
            {
                try
                {
                    // Get the path to the current executable
                    string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    exePath = exePath.Replace(".dll", ".exe");

                    // Close all windows to release database connections
                    System.Threading.Thread.Sleep(200);

                    // Close this window
                    this.Close();
                    System.Threading.Thread.Sleep(100);

                    // Collect garbage to release any lingering connections
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(200);

                    // Close all other windows
                    var windowsToClose = Application.Current.Windows.Cast<Window>().ToList();
                    foreach (var window in windowsToClose)
                    {
                        try
                        {
                            window.Close();
                        }
                        catch { }
                    }

                    System.Threading.Thread.Sleep(500);

                    // Start new instance of the application
                    try
                    {
                        if (File.Exists(exePath))
                        {
                            System.Diagnostics.Process.Start(exePath);
                        }
                        else
                        {
                            // Fallback: try to restart via assembly code base
                            System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                        }
                    }
                    catch
                    {
                        // If automatic restart fails, just shutdown
                    }

                    // Shutdown current application
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Automatic restart failed. Please close and reopen the application manually to load restored data.\n\nError: {ex.Message}",
                        "Restart Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }

            private void Close_Click(object sender, RoutedEventArgs e)
            {
                this.Close();
            }
        }
    }
