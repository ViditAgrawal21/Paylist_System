using System;
using System.Collections.ObjectModel;
using System.Windows;
using SchoolPayListSystem.Core.Models;

namespace SchoolPayListSystem.App
{
    public partial class SchoolReferenceWindow : Window
    {
        public SchoolReferenceWindow(ObservableCollection<School> schools)
        {
            InitializeComponent();
            
            try
            {
                // Bind schools to DataGrid
                SchoolsDataGrid.ItemsSource = schools;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading schools: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
