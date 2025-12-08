using System.Windows;
using SchoolPayListSystem.Services;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;

namespace SchoolPayListSystem.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Show Export Data section if user is Admin (GCP)
            var loggedInUser = ((App)Application.Current).LoggedInUser;
            if (loggedInUser != null && (loggedInUser.Role == "Admin" || loggedInUser.Username == "GCP"))
            {
                ExportDataSection.Visibility = Visibility.Visible;
            }
        }

        private void AddBranch_Click(object sender, RoutedEventArgs e)
        {
            BranchWindow window = new BranchWindow();
            window.ShowDialog();
        }

        private void AddSchoolType_Click(object sender, RoutedEventArgs e)
        {
            SchoolTypeWindow window = new SchoolTypeWindow();
            window.ShowDialog();
        }

        private void AddSchool_Click(object sender, RoutedEventArgs e)
        {
            SchoolWindow window = new SchoolWindow();
            window.ShowDialog();
        }

        private void SalaryEntry_Click(object sender, RoutedEventArgs e)
        {
            SalaryEntryWindow window = new SalaryEntryWindow();
            window.ShowDialog();
        }

        private void BranchReport_Click(object sender, RoutedEventArgs e)
        {
            ReportWindow window = new ReportWindow();
            window.ShowDialog();
        }

        private void SchoolReport_Click(object sender, RoutedEventArgs e)
        {
            ReportWindow window = new ReportWindow();
            window.ShowDialog();
        }

        private void DateReport_Click(object sender, RoutedEventArgs e)
        {
            ReportWindow window = new ReportWindow();
            window.ShowDialog();
        }

        private void SchoolTypeSummary_Click(object sender, RoutedEventArgs e)
        {
            ReportWindow window = new ReportWindow();
            window.ReportType = "SchoolTypeSummary";
            window.ShowDialog();
        }

        private void BranchDetail_Click(object sender, RoutedEventArgs e)
        {
            ReportWindow window = new ReportWindow();
            window.ReportType = "BranchDetail";
            window.ShowDialog();
        }

        private void SchoolTypeDetail_Click(object sender, RoutedEventArgs e)
        {
            ReportWindow window = new ReportWindow();
            window.ReportType = "SchoolTypeDetail";
            window.ShowDialog();
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            BackupWindow window = new BackupWindow();
            window.ShowDialog();
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            BackupWindow window = new BackupWindow();
            window.ShowDialog();
        }

        // CleanDatabase_Click method removed - Clean Database button removed from UI
        
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            ExportDataWindow window = new ExportDataWindow();
            window.ShowDialog();
        }
    }
}
