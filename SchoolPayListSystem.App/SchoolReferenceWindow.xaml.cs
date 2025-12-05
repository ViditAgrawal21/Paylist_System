using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using SchoolPayListSystem.Core.Models;

namespace SchoolPayListSystem.App
{
    public partial class SchoolReferenceWindow : Window
    {
        private ObservableCollection<School> _allSchools;  // Store all schools for filtering
        private ObservableCollection<School> _filteredSchools;  // Store filtered schools for display

        public SchoolReferenceWindow(ObservableCollection<School> schools)
        {
            InitializeComponent();
            
            try
            {
                // Store original schools list
                _allSchools = new ObservableCollection<School>(schools);
                _filteredSchools = new ObservableCollection<School>(schools);
                
                // Bind filtered schools to DataGrid
                SchoolsDataGrid.ItemsSource = _filteredSchools;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading schools: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SchoolCodeSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SchoolNameSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string codeSearch = SchoolCodeSearchBox.Text.ToLower().Trim();
            string nameSearch = SchoolNameSearchBox.Text.ToLower().Trim();

            var filtered = _allSchools.Where(s =>
                (string.IsNullOrEmpty(codeSearch) || s.SchoolCode.ToLower().Contains(codeSearch)) &&
                (string.IsNullOrEmpty(nameSearch) || s.SchoolName.ToLower().Contains(nameSearch))
            ).ToList();

            _filteredSchools.Clear();
            foreach (var school in filtered)
            {
                _filteredSchools.Add(school);
            }
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SchoolCodeSearchBox.Clear();
            SchoolNameSearchBox.Clear();
            _filteredSchools.Clear();
            foreach (var school in _allSchools)
            {
                _filteredSchools.Add(school);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
