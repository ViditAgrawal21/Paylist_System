using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SchoolPayListSystem.Core;
using SchoolPayListSystem.Core.DTOs;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SchoolPayListSystem.Services;

namespace SchoolPayListSystem.App
{
    public partial class SalaryEntryWindow : Window
    {
        private readonly SalaryService _salaryService;
        private readonly SchoolService _schoolService;
        private readonly BranchService _branchService;
        private readonly SchoolTypeService _schoolTypeService;
        private System.Collections.ObjectModel.ObservableCollection<FreshSalaryEntryDTO> _entries;  // Now stores FreshSalaryEntryDTO
        private System.Collections.ObjectModel.ObservableCollection<ImportedSalaryEntrySummaryDTO> _importedEntries;
        private System.Collections.ObjectModel.ObservableCollection<SchoolPayListSystem.Core.Models.School> _schools = new();
        private int _currentIndex = -1;
        private bool _isNewRecord = true;
        private bool _isEditMode = false;
        private int? _editingEntryId = null;

        public SalaryEntryWindow()
        {
            InitializeComponent();
            
            var context = new SchoolPayListDbContext();
            var schoolRepository = new SchoolRepository(context);
            var branchRepository = new BranchRepository(context);
            var schoolTypeRepository = new SchoolTypeRepository(context);
            var salaryRepository = new SalaryEntryRepository(context);
            
            _schoolService = new SchoolService(schoolRepository);
            _branchService = new BranchService(branchRepository);
            _schoolTypeService = new SchoolTypeService(schoolTypeRepository);
            _salaryService = new SalaryService(salaryRepository);
            
            // Initialize collections - fresh entries in main grid
            _entries = new System.Collections.ObjectModel.ObservableCollection<FreshSalaryEntryDTO>();
            _importedEntries = new System.Collections.ObjectModel.ObservableCollection<ImportedSalaryEntrySummaryDTO>();
            FreshEntriesDataGrid.ItemsSource = _entries;
            // ImportedEntriesDataGrid hidden from UI - no longer bound
            
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            try
            {
                // Display operator name
                var app = (App)Application.Current;
                if (app.LoggedInUser != null)
                {
                    var operatorDisplay = this.FindName("OperatorNameDisplay") as TextBlock;
                    if (operatorDisplay != null)
                    {
                        operatorDisplay.Text = app.LoggedInUser.FullName ?? app.LoggedInUser.Username;
                    }
                }

                await LoadSchools();
                await LoadBranches();
                await LoadSchoolTypes();
                
                // Load fresh entries (full details)
                var freshEntries = await _salaryService.GetFreshEntriesForDisplayAsync(app.LoggedInUser?.UserId ?? 0);
                _entries.Clear();
                foreach (var entry in freshEntries)
                {
                    _entries.Add(entry);
                }

                // Imported entries are stored in database but not displayed in UI
                // No need to load ImportedEntriesSummary for display
                
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadSchools()
        {
            try
            {
                _schools = new System.Collections.ObjectModel.ObservableCollection<SchoolPayListSystem.Core.Models.School>();
                var schools = await _schoolService.GetAllSchoolsAsync();
                foreach (var school in schools)
                {
                    _schools.Add(school);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading schools: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadBranches()
        {
            var branches = await _branchService.GetAllBranchesAsync();
            // Store for reference but don't bind to UI
        }

        private async System.Threading.Tasks.Task LoadSchoolTypes()
        {
            var types = await _schoolTypeService.GetAllTypesAsync();
            // Store for reference but don't bind to UI
        }

        private async System.Threading.Tasks.Task LoadEntries()
        {
            try
            {
                var app = (App)Application.Current;
                _entries.Clear();
                var freshEntries = await _salaryService.GetFreshEntriesForDisplayAsync(app.LoggedInUser?.UserId ?? 0);
                foreach (var entry in freshEntries)
                {
                    _entries.Add(entry);
                }
                
                // Also reload imported entries
                var importedEntries = await _salaryService.GetImportedEntriesSummaryAsync(app.LoggedInUser?.UserId ?? 0);
                _importedEntries.Clear();
                foreach (var entry in importedEntries)
                {
                    _importedEntries.Add(entry);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading entries: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EntryDatePicker_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                var entryDatePicker = this.FindName("EntryDatePicker") as DatePicker;
                if (entryDatePicker?.SelectedDate == null)
                {
                    return;
                }

                DateTime selectedDate = entryDatePicker.SelectedDate.Value.Date;
                var app = (App)Application.Current;

                // Load all fresh entries for the logged-in user
                var freshEntries = await _salaryService.GetFreshEntriesForDisplayAsync(app.LoggedInUser?.UserId ?? 0);

                // Filter entries for the selected date
                var filteredEntries = freshEntries
                    .Where(e => e.INDATE.Date == selectedDate)
                    .ToList();

                // Update the grid with filtered entries
                _entries.Clear();
                foreach (var entry in filteredEntries)
                {
                    _entries.Add(entry);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading entries for selected date: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Amount_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            var amount1TextBox = this.FindName("Amount1TextBox") as TextBox;
            var amount2TextBox = this.FindName("Amount2TextBox") as TextBox;
            var amount3TextBox = this.FindName("Amount3TextBox") as TextBox;
            var amount1Display = this.FindName("Amount1Display") as TextBlock;
            var amount2Display = this.FindName("Amount2Display") as TextBlock;
            var amount3Display = this.FindName("Amount3Display") as TextBlock;
            var totalAmountDisplay = this.FindName("TotalAmountDisplay") as TextBlock;

            decimal amount1 = amount1TextBox != null && decimal.TryParse(amount1TextBox.Text, out var a1) ? a1 : 0;
            decimal amount2 = amount2TextBox != null && decimal.TryParse(amount2TextBox.Text, out var a2) ? a2 : 0;
            decimal amount3 = amount3TextBox != null && decimal.TryParse(amount3TextBox.Text, out var a3) ? a3 : 0;
            
            decimal total = amount1 + amount2 + amount3;
            
            if (amount1Display != null) amount1Display.Text = $"₹{amount1:F2}";
            if (amount2Display != null) amount2Display.Text = $"₹{amount2:F2}";
            if (amount3Display != null) amount3Display.Text = $"₹{amount3:F2}";
            if (totalAmountDisplay != null) totalAmountDisplay.Text = $"₹{total:F2}";
        }

        private void ClearForm()
        {
            var entryDatePicker = this.FindName("EntryDatePicker") as DatePicker;
            var schoolCodeTextBox = this.FindName("SchoolCodeTextBox") as TextBox;
            var schoolNameTextBox = this.FindName("SchoolNameTextBox") as TextBox;
            var accountNoTextBox = this.FindName("AccountNoTextBox") as TextBox;
            var schoolTypeTextBox = this.FindName("SchoolTypeTextBox") as TextBox;
            var branchNameTextBox = this.FindName("BranchNameTextBox") as TextBox;
            var amount1TextBox = this.FindName("Amount1TextBox") as TextBox;
            var amount2TextBox = this.FindName("Amount2TextBox") as TextBox;
            var amount3TextBox = this.FindName("Amount3TextBox") as TextBox;
            var totalAmountDisplay = this.FindName("TotalAmountDisplay") as TextBlock;
            var editButton = this.FindName("EditButton") as Button;
            var suggestions = this.FindName("SchoolCodeSuggestions") as ListBox;

            if (entryDatePicker != null) entryDatePicker.SelectedDate = DateTime.Now;
            if (schoolCodeTextBox != null) schoolCodeTextBox.Clear();
            if (schoolNameTextBox != null) schoolNameTextBox.Clear();
            if (accountNoTextBox != null) accountNoTextBox.Clear();
            if (schoolTypeTextBox != null) schoolTypeTextBox.Clear();
            if (branchNameTextBox != null) branchNameTextBox.Clear();
            if (amount1TextBox != null) amount1TextBox.Clear();
            if (amount2TextBox != null) amount2TextBox.Clear();
            if (amount3TextBox != null) amount3TextBox.Clear();
            if (totalAmountDisplay != null) totalAmountDisplay.Text = "₹0.00";
            if (suggestions != null) suggestions.Visibility = Visibility.Collapsed;

            // Enable fields for new entry
            if (entryDatePicker != null) entryDatePicker.IsEnabled = true;
            if (schoolCodeTextBox != null) schoolCodeTextBox.IsEnabled = true;
            if (amount1TextBox != null) amount1TextBox.IsReadOnly = false;
            if (amount2TextBox != null) amount2TextBox.IsReadOnly = false;
            if (amount3TextBox != null) amount3TextBox.IsReadOnly = false;

            // Disable Edit button when adding new
            if (editButton != null) editButton.IsEnabled = false;

            _currentIndex = -1;
            _isNewRecord = true;
            _isEditMode = false;
        }

        private async void AddNew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var schoolCodeTextBox = this.FindName("SchoolCodeTextBox") as TextBox;

                if (schoolCodeTextBox == null || string.IsNullOrWhiteSpace(schoolCodeTextBox.Text))
                {
                    MessageBox.Show("Please enter school code.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Find school by code
                var schoolCode = schoolCodeTextBox.Text.Trim();
                var selectedSchool = _schools.FirstOrDefault(s => s.SchoolCode == schoolCode);
                
                if (selectedSchool == null)
                {
                    MessageBox.Show("School code not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var amount1TextBox = this.FindName("Amount1TextBox") as TextBox;
                var amount2TextBox = this.FindName("Amount2TextBox") as TextBox;
                var amount3TextBox = this.FindName("Amount3TextBox") as TextBox;

                // Parse amounts - allow empty values (default to 0)
                decimal amount1 = string.IsNullOrWhiteSpace(amount1TextBox?.Text) ? 0 : decimal.Parse(amount1TextBox.Text);
                decimal amount2 = string.IsNullOrWhiteSpace(amount2TextBox?.Text) ? 0 : decimal.Parse(amount2TextBox.Text);
                decimal amount3 = string.IsNullOrWhiteSpace(amount3TextBox?.Text) ? 0 : decimal.Parse(amount3TextBox.Text);

                // Get logged-in user
                var app = (App)Application.Current;
                int createdByUserId = app.LoggedInUser?.UserId ?? 0;

                var result = await _salaryService.AddSalaryEntryAsync(
                    EntryDatePicker.SelectedDate ?? DateTime.Now,
                    selectedSchool.SchoolId,
                    selectedSchool.BranchId,
                    selectedSchool.SchoolCode,
                    amount1,
                    amount2,
                    amount3,
                    createdByUserId);

                if (result.success)
                {
                    MessageBox.Show("Salary entry added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadEntries();
                    ClearForm();
                    _currentIndex = -1;
                    _isNewRecord = true;
                }
                else
                {
                    MessageBox.Show(result.message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding entry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_entries.Count == 0)
                {
                    MessageBox.Show("No entries available.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (_currentIndex <= 0)
                {
                    _currentIndex = _entries.Count - 1;
                }
                else
                {
                    _currentIndex--;
                }

                LoadEntryToForm(_currentIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_entries.Count == 0)
                {
                    MessageBox.Show("No entries available.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (_currentIndex >= _entries.Count - 1)
                {
                    _currentIndex = 0;
                }
                else
                {
                    _currentIndex++;
                }

                LoadEntryToForm(_currentIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEntryToForm(int index)
        {
            try
            {
                if (index < 0 || index >= _entries.Count)
                {
                    ClearForm();
                    return;
                }

                var entry = _entries[index];
                _currentIndex = index;
                _isNewRecord = false;
                _isEditMode = false;

                // Set school code and name directly from the entry DTO
                var schoolCodeTextBox = this.FindName("SchoolCodeTextBox") as TextBox;
                if (schoolCodeTextBox != null) schoolCodeTextBox.Text = entry.SchoolCode;

                var schoolNameTextBox = this.FindName("SchoolNameTextBox") as TextBox;
                if (schoolNameTextBox != null) schoolNameTextBox.Text = entry.SchoolName;

                var entryDatePicker = this.FindName("EntryDatePicker") as DatePicker;
                if (entryDatePicker != null) entryDatePicker.SelectedDate = entry.INDATE;

                var amount1TextBox = this.FindName("Amount1TextBox") as TextBox;
                var amount2TextBox = this.FindName("Amount2TextBox") as TextBox;
                var amount3TextBox = this.FindName("Amount3TextBox") as TextBox;

                if (amount1TextBox != null) amount1TextBox.Text = entry.AMOUNT1.ToString("F2");
                if (amount2TextBox != null) amount2TextBox.Text = entry.AMOUNT2.ToString("F2");
                if (amount3TextBox != null) amount3TextBox.Text = entry.AMOUNT2.ToString("F2");
                
                CalculateTotal();

                // Make amount fields read-only by default (enable Edit button to edit them)
                if (amount1TextBox != null) amount1TextBox.IsReadOnly = true;
                if (amount2TextBox != null) amount2TextBox.IsReadOnly = true;
                if (amount3TextBox != null) amount3TextBox.IsReadOnly = true;

                // Disable other fields when viewing
                if (schoolCodeTextBox != null) schoolCodeTextBox.IsEnabled = false;
                if (entryDatePicker != null) entryDatePicker.IsEnabled = false;

                // Enable Edit button
                var editButton = this.FindName("EditButton") as Button;
                if (editButton != null) editButton.IsEnabled = true;

                // Highlight the current entry in the grid
                var entriesDataGrid = this.FindName("FreshEntriesDataGrid") as DataGrid;
                if (entriesDataGrid != null)
                {
                    entriesDataGrid.SelectedIndex = index;
                    entriesDataGrid.ScrollIntoView(entry);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading entry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentIndex < 0 || _currentIndex >= _entries.Count || _isNewRecord)
                {
                    MessageBox.Show("Please select an entry to delete first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var entry = _entries[_currentIndex];
                
                var result = MessageBox.Show(
                    $"Are you sure you want to delete this salary entry?\n\n" +
                    $"Date: {entry.INDATE:dd/MM/yyyy}\n" +
                    $"Amount: ₹{entry.AMOUNT:F2}",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // TODO: Implement delete in SalaryService
                    // For now, we'll just show a message
                    MessageBox.Show("Delete functionality will be implemented in the service layer.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadEntries();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SchoolCode_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                var schoolCodeTextBox = this.FindName("SchoolCodeTextBox") as TextBox;
                var suggestions = this.FindName("SchoolCodeSuggestions") as ListBox;
                
                if (schoolCodeTextBox == null || suggestions == null)
                    return;

                string searchText = schoolCodeTextBox.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(searchText))
                {
                    // Clear fields if no school is selected
                    SchoolNameTextBox.Clear();
                    AccountNoTextBox.Clear();
                    SchoolTypeTextBox.Clear();
                    BranchNameTextBox.Clear();
                    suggestions.Visibility = Visibility.Collapsed;
                    return;
                }

                // Show matching schools
                var matching = _schools.Where(s => s.SchoolCode.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)).ToList();

                if (matching.Count > 0)
                {
                    suggestions.ItemsSource = matching;
                    suggestions.DisplayMemberPath = "SchoolCode";
                    suggestions.Visibility = Visibility.Visible;

                    // If exact match, populate the fields
                    var exactMatch = matching.FirstOrDefault(s => s.SchoolCode == searchText);
                    if (exactMatch != null)
                    {
                        PopulateSchoolDetails(exactMatch);
                        suggestions.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    SchoolNameTextBox.Clear();
                    AccountNoTextBox.Clear();
                    SchoolTypeTextBox.Clear();
                    BranchNameTextBox.Clear();
                    suggestions.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void PopulateSchoolDetails(School school)
        {
            try
            {
                // Populate School Name
                SchoolNameTextBox.Text = school.SchoolName ?? "";

                // Populate Account Number
                AccountNoTextBox.Text = school.BankAccountNumber ?? "";

                // Populate School Type
                var allTypes = await _schoolTypeService.GetAllTypesAsync();
                var schoolType = allTypes.FirstOrDefault(t => t.SchoolTypeId == school.SchoolTypeId);
                SchoolTypeTextBox.Text = schoolType?.TypeName ?? "";

                // Populate Branch
                var allBranches = await _branchService.GetAllBranchesAsync();
                var branch = allBranches.FirstOrDefault(b => b.BranchId == school.BranchId);
                BranchNameTextBox.Text = branch?.BranchName ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching school data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SchoolCodeTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                var suggestions = this.FindName("SchoolCodeSuggestions") as ListBox;
                
                if (suggestions?.Visibility == Visibility.Visible)
                {
                    if (e.Key == System.Windows.Input.Key.Down)
                    {
                        suggestions.Focus();
                        suggestions.SelectedIndex = 0;
                        e.Handled = true;
                    }
                }
                else if (e.Key == System.Windows.Input.Key.Tab || e.Key == System.Windows.Input.Key.Return)
                {
                    // After school code is selected, move focus to Amount1 field
                    var amount1TextBox = this.FindName("Amount1TextBox") as TextBox;
                    if (amount1TextBox != null)
                    {
                        amount1TextBox.Focus();
                        amount1TextBox.SelectAll();
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SchoolSuggestion_Selected(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var suggestions = this.FindName("SchoolCodeSuggestions") as ListBox;
                var schoolCodeTextBox = this.FindName("SchoolCodeTextBox") as TextBox;
                
                if (suggestions?.SelectedItem is School school && schoolCodeTextBox != null)
                {
                    schoolCodeTextBox.Text = school.SchoolCode;
                    PopulateSchoolDetails(school);
                    suggestions.Visibility = Visibility.Collapsed;
                    
                    // Auto-focus to Amount1 field after school selection
                    var amount1TextBox = this.FindName("Amount1TextBox") as TextBox;
                    if (amount1TextBox != null)
                    {
                        amount1TextBox.Focus();
                        amount1TextBox.SelectAll();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create and show School Reference Window
                var schoolReferenceWindow = new SchoolReferenceWindow(_schools);
                schoolReferenceWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening help: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AccountNo_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Allow only digits and prevent non-numeric input
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            // Prevent input if it exceeds 15 characters
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Text.Length >= 15)
            {
                e.Handled = true;
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentIndex < 0 || _currentIndex >= _entries.Count || _isNewRecord)
                {
                    MessageBox.Show("Please select an entry to edit first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _isEditMode = !_isEditMode;
                var editButton = this.FindName("EditButton") as Button;
                var amount1TextBox = this.FindName("Amount1TextBox") as TextBox;
                var amount2TextBox = this.FindName("Amount2TextBox") as TextBox;
                var amount3TextBox = this.FindName("Amount3TextBox") as TextBox;

                if (_isEditMode)
                {
                    // Enter edit mode - enable amount fields only
                    if (editButton != null) editButton.Content = "Save";
                    if (editButton != null) editButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                    
                    if (amount1TextBox != null) amount1TextBox.IsReadOnly = false;
                    if (amount2TextBox != null) amount2TextBox.IsReadOnly = false;
                    if (amount3TextBox != null) amount3TextBox.IsReadOnly = false;

                    var schoolCodeCombo = this.FindName("SchoolCodeCombo") as ComboBox;
                    var entryDatePicker = this.FindName("EntryDatePicker") as DatePicker;
                    if (schoolCodeCombo != null) schoolCodeCombo.IsEnabled = false;
                    if (entryDatePicker != null) entryDatePicker.IsEnabled = false;
                }
                else
                {
                    // Exit edit mode - save changes
                    if (editButton != null) editButton.Content = "Edit";
                    if (editButton != null) editButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                    
                    if (amount1TextBox != null) amount1TextBox.IsReadOnly = true;
                    if (amount2TextBox != null) amount2TextBox.IsReadOnly = true;
                    if (amount3TextBox != null) amount3TextBox.IsReadOnly = true;

                    // TODO: Implement update in service layer
                    MessageBox.Show("Changes saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    Title = "Select Salary Import Excel File"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var app = (App)Application.Current;
                    int createdByUserId = app.LoggedInUser?.UserId ?? 0;

                    if (createdByUserId == 0)
                    {
                        MessageBox.Show("Please log in first to import data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var result = await _salaryService.ImportSalariesFromExcelAsync(openFileDialog.FileName, createdByUserId);

                    if (result.success)
                    {
                        string message = $"Import completed!\n\n" +
                                       $"✓ Successfully imported {result.importedCount} salary entries";
                        
                        if (result.errors.Count > 0)
                        {
                            message += $"\n⚠ {result.errors.Count} errors encountered:\n\n";
                            // Show first 10 errors
                            for (int i = 0; i < Math.Min(10, result.errors.Count); i++)
                            {
                                message += $"  • {result.errors[i]}\n";
                            }
                            if (result.errors.Count > 10)
                            {
                                message += $"\n  ... and {result.errors.Count - 10} more errors";
                            }
                        }

                        MessageBox.Show(message, "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Refresh the grid with updated data
                        await LoadData();
                    }
                    else
                    {
                        string message = $"Import failed: {result.message}\n\n";
                        if (result.errors.Count > 0)
                        {
                            message += "Errors:\n";
                            foreach (var error in result.errors.Take(5))
                            {
                                message += $"  • {error}\n";
                            }
                        }
                        MessageBox.Show(message, "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
