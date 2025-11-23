using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SchoolPayListSystem.Core;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SchoolPayListSystem.Services;

namespace SchoolPayListSystem.App
{
    public partial class SchoolWindow : Window
    {
        private readonly SchoolService _schoolService;
        private readonly SchoolTypeService _schoolTypeService;
        private readonly BranchService _branchService;
        private readonly ExcelImportService _excelImportService;
        private System.Collections.ObjectModel.ObservableCollection<SchoolPayListSystem.Core.Models.School> _schools;
        private int? _selectedSchoolId = null;

        public SchoolWindow()
        {
            InitializeComponent();
            
            var context = new SchoolPayListDbContext();
            var schoolRepo = new SchoolRepository(context);
            var typeRepo = new SchoolTypeRepository(context);
            var branchRepo = new BranchRepository(context);
            var salaryRepo = new SalaryEntryRepository(context);
            
            _schoolService = new SchoolService(schoolRepo, salaryRepo);
            _schoolTypeService = new SchoolTypeService(typeRepo);
            _branchService = new BranchService(branchRepo, salaryRepo);
            _excelImportService = new ExcelImportService(_schoolTypeService, _schoolService, _branchService);
            
            _schools = new System.Collections.ObjectModel.ObservableCollection<SchoolPayListSystem.Core.Models.School>();
            var schoolsDataGrid = this.FindName("SchoolsDataGrid") as DataGrid;
            if (schoolsDataGrid != null)
                schoolsDataGrid.ItemsSource = _schools;
            
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            try
            {
                await LoadSchoolTypes();
                await LoadBranches();
                await LoadSchools();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadSchoolTypes()
        {
            try
            {
                var types = await _schoolTypeService.GetAllTypesAsync();
                SchoolTypeCombo.ItemsSource = types;
                SchoolTypeCombo.DisplayMemberPath = "TypeName";
                SchoolTypeCombo.SelectedValuePath = "TypeId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading school types: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadBranches()
        {
            try
            {
                var branches = await _branchService.GetAllBranchesAsync();
                BranchCombo.ItemsSource = branches;
                BranchCombo.DisplayMemberPath = "BranchName";
                BranchCombo.SelectedValuePath = "BranchId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading branches: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadSchools()
        {
            try
            {
                _schools.Clear();
                var schools = await _schoolService.GetAllSchoolsAsync();
                foreach (var school in schools)
                {
                    _schools.Add(school);
                }
                StatusText.Text = $"Total: {_schools.Count} schools";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading schools: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SchoolCodeTextBox.Text) || 
                    string.IsNullOrWhiteSpace(SchoolNameTextBox.Text))
                {
                    MessageBox.Show("Please fill all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SchoolTypeCombo.SelectedItem == null || BranchCombo.SelectedItem == null)
                {
                    MessageBox.Show("Please select valid School Type and Branch.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedSchoolType = SchoolTypeCombo.SelectedItem as SchoolType;
                var selectedBranch = BranchCombo.SelectedItem as Branch;

                if (selectedSchoolType == null || selectedBranch == null)
                {
                    MessageBox.Show("Please select valid School Type and Branch.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int schoolTypeId = selectedSchoolType.SchoolTypeId;
                int branchId = selectedBranch.BranchId;

                var result = await _schoolService.AddSchoolAsync(
                    SchoolCodeTextBox.Text.Trim(),
                    SchoolNameTextBox.Text.Trim(),
                    schoolTypeId,
                    branchId,
                    AccountNumberTextBox.Text?.Trim() ?? "");
                
                if (result.success)
                {
                    MessageBox.Show("School saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    Clear_Click(null, null);
                    await LoadSchools();
                }
                else
                {
                    MessageBox.Show(result.message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving school: {ex.Message}\n\nDetails: {ex.InnerException?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var schoolCodeTextBox = this.FindName("SchoolCodeTextBox") as TextBox;
                schoolCodeTextBox?.Clear();
                var schoolNameTextBox = this.FindName("SchoolNameTextBox") as TextBox;
                schoolNameTextBox?.Clear();
                var accountNumberTextBox = this.FindName("AccountNumberTextBox") as TextBox;
                accountNumberTextBox?.Clear();
                var schoolTypeCombo = this.FindName("SchoolTypeCombo") as ComboBox;
                if (schoolTypeCombo != null) schoolTypeCombo.SelectedIndex = -1;
                var branchCombo = this.FindName("BranchCombo") as ComboBox;
                if (branchCombo != null) branchCombo.SelectedIndex = -1;
                
                _selectedSchoolId = null;
                
                // Enable code field for new entry
                if (schoolCodeTextBox != null)
                {
                    schoolCodeTextBox.IsReadOnly = false;
                    schoolCodeTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing form: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LockCodeField()
        {
            var schoolCodeTextBox = this.FindName("SchoolCodeTextBox") as TextBox;
            if (schoolCodeTextBox != null)
            {
                schoolCodeTextBox.IsReadOnly = true;
                schoolCodeTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
            }
        }

        private void UnlockCodeField()
        {
            var schoolCodeTextBox = this.FindName("SchoolCodeTextBox") as TextBox;
            if (schoolCodeTextBox != null)
            {
                schoolCodeTextBox.IsReadOnly = false;
                schoolCodeTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var school = button?.DataContext as SchoolPayListSystem.Core.Models.School;
                if (school == null)
                {
                    MessageBox.Show("Could not get school data", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                var schoolCodeTextBox = this.FindName("SchoolCodeTextBox") as TextBox;
                var schoolNameTextBox = this.FindName("SchoolNameTextBox") as TextBox;
                var accountNumberTextBox = this.FindName("AccountNumberTextBox") as TextBox;
                var schoolTypeCombo = this.FindName("SchoolTypeCombo") as ComboBox;
                var branchCombo = this.FindName("BranchCombo") as ComboBox;
                
                if (schoolCodeTextBox != null) schoolCodeTextBox.Text = school.SchoolCode;
                if (schoolNameTextBox != null) schoolNameTextBox.Text = school.SchoolName;
                if (accountNumberTextBox != null) accountNumberTextBox.Text = school.BankAccountNumber ?? "";
                if (schoolTypeCombo != null) schoolTypeCombo.SelectedValue = school.SchoolTypeId;
                if (branchCombo != null) branchCombo.SelectedValue = school.BranchId;
                
                _selectedSchoolId = school.SchoolId;
                
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
                Button button = sender as Button;
                var school = button?.DataContext as SchoolPayListSystem.Core.Models.School;
                if (school == null)
                {
                    MessageBox.Show("Could not get school data", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Check for associated salary entries
                var associatedEntries = await _schoolService.GetAssociatedSalaryEntriesAsync(school.SchoolId);
                
                if (associatedEntries.Count > 0)
                {
                    // Show warning message with associated data info
                    string warningMessage = $"⚠️ WARNING - ASSOCIATED DATA FOUND!\n\n" +
                        $"This school has {associatedEntries.Count} salary entry/entries associated with it.\n\n" +
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
                    var result = MessageBox.Show("Are you sure you want to delete this school?", "Confirm Delete", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                        return;
                }

                var deleteResult = await _schoolService.DeleteSchoolAsync(school.SchoolId);
                if (deleteResult.success)
                {
                    MessageBox.Show("School deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadSchools();
                }
                else
                {
                    MessageBox.Show(deleteResult.message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting school: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Excel File to Import Schools",
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var result = await _excelImportService.ImportSchoolsFromExcel(openFileDialog.FileName);
                    
                    if (result.success)
                    {
                        MessageBox.Show(result.message, "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadSchools();
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
                Title = "Save School Template",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "SchoolTemplate.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    _excelImportService.GenerateSchoolTemplate(saveFileDialog.FileName);
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
