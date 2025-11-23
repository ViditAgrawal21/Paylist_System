using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using SchoolPayListSystem.Services;

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

        private void BrowseRestore_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Backup File to Restore",
                Filter = "ZIP Files (*.zip)|*.zip|All Files (*.*)|*.*",
                DefaultExt = ".zip"
            };

            if (dialog.ShowDialog() == true)
            {
                RestorePathTextBox.Text = dialog.FileName;
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

                // Copy database file to backup location
                File.Copy(dbPath, BackupPathTextBox.Text, true);

                BackupStatus.Text = "✓ Backup completed successfully!";
                BackupStatus.Foreground = System.Windows.Media.Brushes.Green;
                
                MessageBox.Show($"Database backup created successfully at:\n{BackupPathTextBox.Text}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                BackupStatus.Text = $"✗ Error: {ex.Message}";
                BackupStatus.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"Error creating backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RestoreBackup_Click(object sender, RoutedEventArgs e)
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
                    MessageBox.Show("Backup file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show("This will overwrite the current database. Continue?", "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                RestoreStatus.Text = "Restoring database...";
                RestoreStatus.Foreground = System.Windows.Media.Brushes.Orange;

                // Get database file path
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dbPath = Path.Combine(appDataPath, "SchoolPayListSystem", "Database", "SchoolPayList.db");

                // Ensure directory exists
                string? dbDir = Path.GetDirectoryName(dbPath);
                if (dbDir != null && !Directory.Exists(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                }

                // Restore from backup
                File.Copy(RestorePathTextBox.Text, dbPath, true);

                RestoreStatus.Text = "✓ Database restored successfully!";
                RestoreStatus.Foreground = System.Windows.Media.Brushes.Green;
                
                MessageBox.Show("Database restored successfully. The application may need to be restarted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                RestoreStatus.Text = $"✗ Error: {ex.Message}";
                RestoreStatus.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"Error restoring backup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
