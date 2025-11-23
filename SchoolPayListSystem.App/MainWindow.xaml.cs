using System.Windows;

namespace SchoolPayListSystem.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            BackupWindow window = new BackupWindow();
            window.ShowDialog();
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Restore functionality coming soon", "Restore");
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}
