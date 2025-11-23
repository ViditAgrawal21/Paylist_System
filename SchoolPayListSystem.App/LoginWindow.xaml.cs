using System;
using System.Linq;
using System.Windows;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SchoolPayListSystem.Services;

namespace SchoolPayListSystem.App
{
    public partial class LoginWindow : Window
    {
        private AuthenticationService _authService;
        private SchoolPayListDbContext _context;
        private UserRepository _userRepository;

        public LoginWindow()
        {
            try
            {
                InitializeComponent();
                
                // Create database context with proper error handling
                _context = new SchoolPayListDbContext();
                _userRepository = new UserRepository(_context);
                _authService = new AuthenticationService(_userRepository);
                
                // Check if this is first-time use (no users besides GCP admin)
                CheckFirstTimeUse();
                
                this.Topmost = true;
                this.Focus();
            }
            catch (Exception ex)
            {
                string fullError = $"Login Window Initialization Error: {ex.Message}";
                if (ex.InnerException != null)
                {
                    fullError += $"\n\nInner Exception: {ex.InnerException.Message}";
                }
                if (ex.InnerException?.InnerException != null)
                {
                    fullError += $"\n\nRoot Cause: {ex.InnerException.InnerException.Message}";
                }
                
                System.Diagnostics.Debug.WriteLine(fullError);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                
                MessageBox.Show(fullError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void CheckFirstTimeUse()
        {
            try
            {
                // Count users (should be only GCP admin on first use)
                int userCount = _context.Users.Count();
                
                // If only 1 user (GCP admin) or 0, show create admin screen
                if (userCount <= 1)
                {
                    // Hide login controls, show create admin message
                    MessageBlock.Text = "First-time setup required. Please create your admin account.";
                    LoginButton.Visibility = Visibility.Collapsed;
                    ForgotPasswordButton.Visibility = Visibility.Collapsed;
                    CreateAdminButton.Content = "CREATE ADMIN ACCOUNT";
                    CreateAdminButton.Visibility = Visibility.Visible;
                }
                else
                {
                    // Hide create admin button for subsequent logins
                    CreateAdminButton.Visibility = Visibility.Collapsed;
                    LoginButton.Visibility = Visibility.Visible;
                    ForgotPasswordButton.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"First-time use check error: {ex.Message}");
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameTextBox.Text ?? string.Empty;
                string password = PasswordControl.Password ?? string.Empty;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBlock.Text = "Please enter username and password";
                    return;
                }

                var (success, message, user) = await _authService.LoginAsync(username, password);

                if (success)
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    Close();
                }
                else
                {
                    MessageBlock.Text = message;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateAdminButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateAdminWindow createAdmin = new CreateAdminWindow();
                bool? result = createAdmin.ShowDialog();
                
                // If admin was created successfully, automatically transition to login screen
                if (result == true)
                {
                    // Refresh the UI to show login screen
                    CheckFirstTimeUse();
                    MessageBlock.Text = "Admin account created successfully! Please login with your credentials.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Create Admin Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ForgotPasswordWindow forgotPassword = new ForgotPasswordWindow();
                bool? result = forgotPassword.ShowDialog();
                
                if (result == true)
                {
                    MessageBlock.Text = "Password reset successfully! Please login with your new password.";
                    UsernameTextBox.Clear();
                    PasswordControl.Clear();
                    UsernameTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Forgot Password Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}