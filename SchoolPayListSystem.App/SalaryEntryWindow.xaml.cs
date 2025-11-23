using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SchoolPayListSystem.Core;
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
        private System.Collections.ObjectModel.ObservableCollection<SchoolPayListSystem.Core.Models.SalaryEntry> _entries;
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
            
            _entries = new System.Collections.ObjectModel.ObservableCollection<SchoolPayListSystem.Core.Models.SalaryEntry>();
            EntriesDataGrid.ItemsSource = _entries;
            
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            try
            {
                await LoadSchools();
                await LoadBranches();
                await LoadSchoolTypes();
                await LoadEntries();
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
                SchoolCodeCombo.ItemsSource = _schools;
                SchoolCodeCombo.DisplayMemberPath = "SchoolCode";
                SchoolCodeCombo.SelectedValuePath = "SchoolId";
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
                _entries.Clear();
                var entries = await _salaryService.GetAllEntriesAsync();
                foreach (var entry in entries.OrderByDescending(x => x.EntryDate))
                {
                    _entries.Add(entry);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading entries: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Amount_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            decimal amount1 = decimal.TryParse(Amount1TextBox.Text, out var a1) ? a1 : 0;
            decimal amount2 = decimal.TryParse(Amount2TextBox.Text, out var a2) ? a2 : 0;
            decimal amount3 = decimal.TryParse(Amount3TextBox.Text, out var a3) ? a3 : 0;
            
            decimal total = amount1 + amount2 + amount3;
            
            Amount1Display.Text = $"₹{amount1:F2}";
            Amount2Display.Text = $"₹{amount2:F2}";
            Amount3Display.Text = $"₹{amount3:F2}";
            TotalAmountDisplay.Text = $"₹{total:F2}";
        }

        private void ClearForm()
        {
            var entryDatePicker = this.FindName("EntryDatePicker") as DatePicker;
            var schoolCodeCombo = this.FindName("SchoolCodeCombo") as ComboBox;
            var schoolNameTextBox = this.FindName("SchoolNameTextBox") as TextBox;
            var accountNoTextBox = this.FindName("AccountNoTextBox") as TextBox;
            var schoolTypeTextBox = this.FindName("SchoolTypeTextBox") as TextBox;
            var branchNameTextBox = this.FindName("BranchNameTextBox") as TextBox;
            var amount1TextBox = this.FindName("Amount1TextBox") as TextBox;
            var amount2TextBox = this.FindName("Amount2TextBox") as TextBox;
            var amount3TextBox = this.FindName("Amount3TextBox") as TextBox;
            var totalAmountDisplay = this.FindName("TotalAmountDisplay") as TextBlock;
            var editButton = this.FindName("EditButton") as Button;

            if (entryDatePicker != null) entryDatePicker.SelectedDate = DateTime.Now;
            if (schoolCodeCombo != null) schoolCodeCombo.SelectedIndex = -1;
            if (schoolNameTextBox != null) schoolNameTextBox.Clear();
            if (accountNoTextBox != null) accountNoTextBox.Clear();
            if (schoolTypeTextBox != null) schoolTypeTextBox.Clear();
            if (branchNameTextBox != null) branchNameTextBox.Clear();
            if (amount1TextBox != null) amount1TextBox.Clear();
            if (amount2TextBox != null) amount2TextBox.Clear();
            if (amount3TextBox != null) amount3TextBox.Clear();
            if (totalAmountDisplay != null) totalAmountDisplay.Text = "₹0.00";

            // Enable fields for new entry
            if (entryDatePicker != null) entryDatePicker.IsEnabled = true;
            if (schoolCodeCombo != null) schoolCodeCombo.IsEnabled = true;
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
                if (SchoolCodeCombo.SelectedIndex == -1 || 
                    string.IsNullOrWhiteSpace(Amount1TextBox.Text))
                {
                    MessageBox.Show("Please select a school and enter Amount 1.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal amount1 = decimal.Parse(Amount1TextBox.Text);
                decimal amount2 = decimal.Parse(Amount2TextBox.Text ?? "0");
                decimal amount3 = decimal.Parse(Amount3TextBox.Text ?? "0");

                var selectedSchool = SchoolCodeCombo.SelectedItem as SchoolPayListSystem.Core.Models.School;
                
                if (selectedSchool == null)
                {
                    MessageBox.Show("School not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = await _salaryService.AddSalaryEntryAsync(
                    EntryDatePicker.SelectedDate ?? DateTime.Now,
                    selectedSchool.SchoolId,
                    selectedSchool.BranchId,
                    selectedSchool.SchoolCode,
                    amount1,
                    amount2,
                    amount3);

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

                // Find and select the school
                var school = _schools.FirstOrDefault(s => s.SchoolId == entry.SchoolId);
                if (school != null)
                {
                    var schoolCodeCombo = this.FindName("SchoolCodeCombo") as ComboBox;
                    if (schoolCodeCombo != null) schoolCodeCombo.SelectedItem = school;
                }

                var entryDatePicker = this.FindName("EntryDatePicker") as DatePicker;
                if (entryDatePicker != null) entryDatePicker.SelectedDate = entry.EntryDate;

                var amount1TextBox = this.FindName("Amount1TextBox") as TextBox;
                var amount2TextBox = this.FindName("Amount2TextBox") as TextBox;
                var amount3TextBox = this.FindName("Amount3TextBox") as TextBox;

                if (amount1TextBox != null) amount1TextBox.Text = entry.Amount1.ToString("F2");
                if (amount2TextBox != null) amount2TextBox.Text = entry.Amount2.ToString("F2");
                if (amount3TextBox != null) amount3TextBox.Text = entry.Amount3.ToString("F2");
                
                CalculateTotal();

                // Make amount fields read-only by default (enable Edit button to edit them)
                if (amount1TextBox != null) amount1TextBox.IsReadOnly = true;
                if (amount2TextBox != null) amount2TextBox.IsReadOnly = true;
                if (amount3TextBox != null) amount3TextBox.IsReadOnly = true;

                // Disable other fields when viewing
                var schoolCodeComboDisable = this.FindName("SchoolCodeCombo") as ComboBox;
                if (schoolCodeComboDisable != null) schoolCodeComboDisable.IsEnabled = false;
                if (entryDatePicker != null) entryDatePicker.IsEnabled = false;

                // Enable Edit button
                var editButton = this.FindName("EditButton") as Button;
                if (editButton != null) editButton.IsEnabled = true;

                // Highlight the current entry in the grid
                var entriesDataGrid = this.FindName("EntriesDataGrid") as DataGrid;
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
                    $"Date: {entry.EntryDate:dd/MM/yyyy}\n" +
                    $"Amount: ₹{entry.TotalAmount:F2}",
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

        private async void SchoolCode_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (SchoolCodeCombo.SelectedItem == null)
                {
                    // Clear fields if no school is selected
                    SchoolNameTextBox.Clear();
                    AccountNoTextBox.Clear();
                    SchoolTypeTextBox.Clear();
                    BranchNameTextBox.Clear();
                    return;
                }

                var school = SchoolCodeCombo.SelectedItem as SchoolPayListSystem.Core.Models.School;

                if (school != null)
                {
                    // Populate School Name
                    SchoolNameTextBox.Text = school.SchoolName ?? "";

                    // Populate Account Number
                    AccountNoTextBox.Text = school.BankAccountNumber ?? "";

                    // Populate School Type - get the type name from SchoolTypeId
                    var allTypes = await _schoolTypeService.GetAllTypesAsync();
                    var schoolType = allTypes.FirstOrDefault(t => t.SchoolTypeId == school.SchoolTypeId);
                    SchoolTypeTextBox.Text = schoolType?.TypeName ?? "";

                    // Populate Branch - get the branch name from BranchId
                    var allBranches = await _branchService.GetAllBranchesAsync();
                    var branch = allBranches.FirstOrDefault(b => b.BranchId == school.BranchId);
                    BranchNameTextBox.Text = branch?.BranchName ?? "";
                }
                else
                {
                    // Clear fields if school not found
                    SchoolNameTextBox.Clear();
                    AccountNoTextBox.Clear();
                    SchoolTypeTextBox.Clear();
                    BranchNameTextBox.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching school data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
    }
}
