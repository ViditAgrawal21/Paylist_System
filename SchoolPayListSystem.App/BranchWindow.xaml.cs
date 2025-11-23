using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SchoolPayListSystem.Services;

namespace SchoolPayListSystem.App
{
    public partial class BranchWindow : Window
    {
        private BranchService _branchService;
        private ExcelImportService _excelImportService;
        private Branch? _selectedBranch;

        public BranchWindow()
        {
            InitializeComponent();
            var context = new SchoolPayListDbContext();
            var branchRepo = new BranchRepository(context);
            var schoolRepo = new SchoolRepository(context);
            var typeRepo = new SchoolTypeRepository(context);
            var salaryRepo = new SalaryEntryRepository(context);
            
            _branchService = new BranchService(branchRepo, salaryRepo);
            var schoolService = new SchoolService(schoolRepo, salaryRepo);
            var schoolTypeService = new SchoolTypeService(typeRepo);
            
            _excelImportService = new ExcelImportService(schoolTypeService, schoolService, _branchService);
            
            LoadBranches();
        }

        private async void LoadBranches()
        {
            try
            {
                var branches = await _branchService.GetAllBranchesAsync();
                BranchesDataGrid.ItemsSource = branches;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading branches: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string codeText = BranchCodeTextBox.Text?.Trim();
                string nameText = BranchNameTextBox.Text?.Trim();

                if (string.IsNullOrEmpty(codeText))
                {
                    MessageBlock.Text = "Branch Code is required";
                    MessageBlock.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                if (string.IsNullOrEmpty(nameText))
                {
                    MessageBlock.Text = "Branch Name is required";
                    MessageBlock.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                if (!int.TryParse(codeText, out int code))
                {
                    MessageBlock.Text = "Branch Code must be a number";
                    MessageBlock.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                var (success, message) = await _branchService.AddBranchAsync(code, nameText);
                MessageBlock.Text = message;
                MessageBlock.Foreground = success ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;

                if (success)
                {
                    BranchCodeTextBox.Clear();
                    BranchNameTextBox.Clear();
                    MessageBlock.Text = "";
                    _selectedBranch = null;
                    LoadBranches();
                }
            }
            catch (Exception ex)
            {
                MessageBlock.Text = $"Error: {ex.Message}";
                MessageBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var branchCodeTextBox = this.FindName("BranchCodeTextBox") as TextBox;
            var branchNameTextBox = this.FindName("BranchNameTextBox") as TextBox;
            var messageBlock = this.FindName("MessageBlock") as TextBlock;
            
            branchCodeTextBox?.Clear();
            branchNameTextBox?.Clear();
            if (messageBlock != null) messageBlock.Text = "";
            _selectedBranch = null;
            
            // Enable code field for new entry
            if (branchCodeTextBox != null)
            {
                branchCodeTextBox.IsReadOnly = false;
                branchCodeTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            }
        }

        private void LockCodeField()
        {
            var branchCodeTextBox = this.FindName("BranchCodeTextBox") as TextBox;
            if (branchCodeTextBox != null)
            {
                branchCodeTextBox.IsReadOnly = true;
                branchCodeTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var branch = button?.DataContext as Branch;
                if (branch == null)
                {
                    MessageBox.Show("Could not get branch data", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                var branchCodeTextBox = this.FindName("BranchCodeTextBox") as TextBox;
                var branchNameTextBox = this.FindName("BranchNameTextBox") as TextBox;
                
                if (branchCodeTextBox != null) branchCodeTextBox.Text = branch.BranchCode.ToString();
                if (branchNameTextBox != null) branchNameTextBox.Text = branch.BranchName;
                
                _selectedBranch = branch;
                
                // Lock the code field for editing
                LockCodeField();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = (Button)sender;
                var branch = button.DataContext as Branch;
                if (branch == null)
                {
                    MessageBox.Show("Could not get branch data", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Check for associated salary entries
                var associatedEntries = await _branchService.GetAssociatedSalaryEntriesAsync(branch.BranchId);
                
                if (associatedEntries.Count > 0)
                {
                    // Show warning message with associated data info
                    string warningMessage = $"⚠️ WARNING - ASSOCIATED DATA FOUND!\n\n" +
                        $"This branch has {associatedEntries.Count} salary entry/entries associated with it.\n\n" +
                        $"If you proceed with deletion:\n" +
                        $"• All {associatedEntries.Count} associated salary entry/entries WILL BE DELETED\n" +
                        $"• This action CANNOT BE UNDONE\n\n" +
                        $"📌 RECOMMENDATION: Please backup your data before proceeding.\n\n" +
                        $"Do you want to proceed with the deletion?";

                    var result = MessageBox.Show(warningMessage, "⚠️ WARNING: Associated Data Will Be Deleted", 
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                        return;
                }
                else
                {
                    // No associated data, just ask for confirmation
                    if (MessageBox.Show("Delete this branch?", "Confirm", MessageBoxButton.YesNo, 
                        MessageBoxImage.Question) == MessageBoxResult.No)
                        return;
                }

                var (success, message, _) = await _branchService.DeleteBranchAsync(branch.BranchId);
                var messageBlock = this.FindName("MessageBlock") as TextBlock;
                if (messageBlock != null) messageBlock.Text = message;
                if (success)
                {
                    LoadBranches();
                }
                else
                {
                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Excel File to Import Branches",
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var result = await _excelImportService.ImportBranchesFromExcel(openFileDialog.FileName);
                    
                    if (result.success)
                    {
                        MessageBox.Show(result.message, "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadBranches();
                    }
                    else
                    {
                        var errorMessage = result.message;
                        if (result.errors.Count > 0)
                        {
                            errorMessage += "\n\nErrors:\n" + string.Join("\n", result.errors.Take(10));
                            if (result.errors.Count > 10)
                                errorMessage += $"\n... and {result.errors.Count - 10} more errors";
                        }
                        MessageBox.Show(errorMessage, "Import Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing Excel file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Branch Template",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "BranchTemplate.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    _excelImportService.GenerateBranchTemplate(saveFileDialog.FileName);
                    MessageBox.Show($"Template saved successfully to:\n{saveFileDialog.FileName}", "Template Downloaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
