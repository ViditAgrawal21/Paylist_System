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
    public partial class SchoolTypeWindow : Window
    {
        private readonly SchoolTypeService _service;
        private readonly ExcelImportService _excelImportService;
        private System.Collections.ObjectModel.ObservableCollection<SchoolPayListSystem.Core.Models.SchoolType> _types;
        private int? _selectedTypeId = null;

        public SchoolTypeWindow()
        {
            InitializeComponent();
            var context = new SchoolPayListDbContext();
            var typeRepo = new SchoolTypeRepository(context);
            var schoolRepo = new SchoolRepository(context);
            var branchRepo = new BranchRepository(context);
            
            _service = new SchoolTypeService(typeRepo);
            
            var schoolService = new SchoolService(schoolRepo);
            var branchService = new BranchService(branchRepo);
            _excelImportService = new ExcelImportService(_service, schoolService, branchService);
            
            _types = new ObservableCollection<SchoolPayListSystem.Core.Models.SchoolType>();
            var typesDataGrid = this.FindName("TypesDataGrid") as DataGrid;
            if (typesDataGrid != null)
                typesDataGrid.ItemsSource = _types;
            
            Loaded += async (s, e) => await LoadTypes();
        }

        private async System.Threading.Tasks.Task LoadTypes()
        {
            try
            {
                _types.Clear();
                var types = await _service.GetAllTypesAsync();
                foreach (var type in types)
                {
                    _types.Add(type);
                }
                var statusText = this.FindName("StatusText") as TextBlock;
                if (statusText != null)
                    statusText.Text = $"Total: {_types.Count} school types";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading school types: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var typeCodeTextBox = this.FindName("TypeCodeTextBox") as TextBox;
            var typeNameTextBox = this.FindName("TypeNameTextBox") as TextBox;
            var statusText = this.FindName("StatusText") as TextBlock;
            
            try
            {
                if (string.IsNullOrWhiteSpace(typeCodeTextBox?.Text))
                {
                    MessageBox.Show("Please enter School Type Code.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(typeNameTextBox?.Text))
                {
                    MessageBox.Show("Please enter School Type Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                (bool success, string message) result;

                if (_selectedTypeId.HasValue)
                {
                    // Update existing school type (only name, not code)
                    result = await _service.UpdateTypeAsync(_selectedTypeId.Value, typeNameTextBox.Text);
                }
                else
                {
                    // Add new school type with both code and name
                    result = await _service.AddTypeAsync(typeCodeTextBox.Text, typeNameTextBox.Text);
                }

                if (result.success)
                {
                    MessageBox.Show(result.message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadTypes();
                    Clear_Click(sender, e);
                }
                else
                {
                    MessageBox.Show(result.message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving school type: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var typeCodeTextBox = this.FindName("TypeCodeTextBox") as TextBox;
            var typeNameTextBox = this.FindName("TypeNameTextBox") as TextBox;
            var saveButton = this.FindName("SaveButton") as Button;
            var statusText = this.FindName("StatusText") as TextBlock;

            typeCodeTextBox?.Clear();
            typeNameTextBox?.Clear();
            _selectedTypeId = null;
            
            if (typeCodeTextBox != null)
            {
                typeCodeTextBox.IsReadOnly = false;
                typeCodeTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            }
            if (saveButton != null) saveButton.Content = "Save";
            if (statusText != null) statusText.Text = "Ready to add new school type";
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int typeId)
            {
                var type = _types.Where(t => t.SchoolTypeId == typeId).FirstOrDefault();
                if (type != null)
                {
                    var typeCodeTextBox = this.FindName("TypeCodeTextBox") as TextBox;
                    var typeNameTextBox = this.FindName("TypeNameTextBox") as TextBox;
                    var saveButton = this.FindName("SaveButton") as Button;
                    var statusText = this.FindName("StatusText") as TextBlock;

                    if (typeCodeTextBox != null) 
                    {
                        typeCodeTextBox.Text = type.TypeCode ?? typeId.ToString();
                        typeCodeTextBox.IsReadOnly = true;
                        typeCodeTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
                    }
                    if (typeNameTextBox != null) typeNameTextBox.Text = type.TypeName;
                    _selectedTypeId = typeId;
                    if (saveButton != null) saveButton.Content = "Update";
                    if (statusText != null) statusText.Text = $"Editing school type: {type.TypeName}";
                    typeNameTextBox?.Focus();
                }
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int typeId)
            {
                var result = MessageBox.Show("Are you sure you want to delete this school type?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var deleteResult = await _service.DeleteTypeAsync(typeId);
                        if (deleteResult.success)
                        {
                            MessageBox.Show(deleteResult.message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadTypes();
                            // Clear form if the deleted item was being edited
                            if (_selectedTypeId == typeId)
                            {
                                Clear_Click(sender, e);
                            }
                        }
                        else
                        {
                            MessageBox.Show(deleteResult.message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting school type: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Excel File to Import School Types",
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var result = await _excelImportService.ImportSchoolTypesFromExcel(openFileDialog.FileName);
                    
                    if (result.success)
                    {
                        MessageBox.Show(result.message, "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadTypes();
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
                Title = "Save School Type Template",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "SchoolTypeTemplate.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    _excelImportService.GenerateSchoolTypeTemplate(saveFileDialog.FileName);
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
