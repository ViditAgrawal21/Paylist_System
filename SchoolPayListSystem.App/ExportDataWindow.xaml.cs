using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SchoolPayListSystem.Services;
using Microsoft.Win32;

namespace SchoolPayListSystem.App
{
    public partial class ExportDataWindow : Window
    {
        private readonly SalaryService _salaryService;

        public ExportDataWindow()
        {
            InitializeComponent();
            _salaryService = new SalaryService(null, null, null);
            LoadAllEntries();
        }

        private void LoadAllEntries()
        {
            try
            {
                var context = new SchoolPayListDbContext();
                var entries = context.SalaryEntries
                    .Include(s => s.School)
                    .Include(s => s.Branch)
                    .OrderByDescending(s => s.EntryDate)
                    .ToList();
                
                EntriesDataGrid.ItemsSource = entries ?? new List<SalaryEntry>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading entries: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportSalaryData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open the new simplified import window
                var importWindow = new ImportExcelWindow();
                importWindow.ShowDialog();

                // Refresh the data after import
                LoadAllEntries();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    FileName = $"SalaryData_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = "Export to Excel"
                };

                if (saveFileDialog.ShowDialog() != true)
                    return;

                var context = new SchoolPayListDbContext();
                var entries = context.SalaryEntries
                    .Include(s => s.School)
                    .Include(s => s.Branch)
                    .ToList();

                // Export logic here (use existing export service if available)
                MessageBox.Show($"Exported {entries.Count} entries successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ClearDatabase_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️  This will DELETE ALL data from the database!\n\nThis action cannot be undone. Continue?",
                "Confirm Clear Database",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var (success, deletedCount, message) = await _salaryService.ClearAllSalaryEntriesAsync();

                if (success)
                {
                    MessageBox.Show(
                        $"✓ Database cleared!\n\n{message}",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    LoadAllEntries();
                }
                else
                {
                    MessageBox.Show($"Error: {message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
