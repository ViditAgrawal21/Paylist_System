using System;
using System.Windows;
using Microsoft.Win32;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SchoolPayListSystem.Services;

namespace SchoolPayListSystem.App
{
    public partial class ImportExcelWindow : Window
    {
        private string? _selectedFilePath = null;

        public ImportExcelWindow()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                Title = "Select Paylist Excel File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                FilePathText.Text = System.IO.Path.GetFileName(_selectedFilePath);
                ResultBorder.Visibility = Visibility.Collapsed;
                ErrorBorder.Visibility = Visibility.Collapsed;
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                ShowError("Please select an Excel file first");
                return;
            }

            try
            {
                ImportButton.IsEnabled = false;
                ImportButton.Content = "Importing...";

                // Create services
                var context = new SchoolPayListDbContext();
                var schoolTypeService = new SchoolTypeService(new SchoolTypeRepository(context));
                var schoolService = new SchoolService(new SchoolRepository(context));
                var branchService = new BranchService(new BranchRepository(context));
                var salaryRepository = new SalaryEntryRepository(context);

                var importService = new ExcelImportService(schoolTypeService, schoolService, branchService, salaryRepository);

                // Perform import
                var (success, message, errors) = await importService.ImportSalaryEntriesFromExcel(_selectedFilePath);

                if (success)
                {
                    // Extract count from message
                    string countStr = message.Split(' ')[2]; // "Import completed. X salary entries..."
                    int count = int.TryParse(countStr, out int c) ? c : 0;

                    ImportedCountText.Text = $"{count} entries imported";
                    ImportDateText.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    ResultBorder.Visibility = Visibility.Visible;
                    ErrorBorder.Visibility = Visibility.Collapsed;

                    if (errors.Count > 0)
                    {
                        MessageBox.Show(
                            $"Import completed with {errors.Count} errors.\n\nFirst few errors:\n{string.Join("\n", errors.GetRange(0, Math.Min(5, errors.Count)))}",
                            "Import Completed with Warnings",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                else
                {
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error during import: {ex.Message}");
            }
            finally
            {
                ImportButton.IsEnabled = true;
                ImportButton.Content = "Import Data";
            }
        }

        private void ShowError(string errorMessage)
        {
            ErrorMessageText.Text = errorMessage;
            ErrorBorder.Visibility = Visibility.Visible;
            ResultBorder.Visibility = Visibility.Collapsed;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
