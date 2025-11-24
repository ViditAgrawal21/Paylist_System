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
        private User _currentUser;

        public LoginWindow()
        {
            try
            {
                InitializeComponent();
                
                // Create database context with proper error handling
                _context = new SchoolPayListDbContext();
                _userRepository = new UserRepository(_context);
                _authService = new AuthenticationService(_userRepository);
                
                // Check if this is first-time use
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
                    MessageBlock.Text = "First-time setup required. Please create your admin account.";
                    LoginButton.Visibility = Visibility.Collapsed;
                    CreateUserButton.Visibility = Visibility.Collapsed;
                    CreateAdminButton.Visibility = Visibility.Visible;
                }
                else
                {
                    CreateAdminButton.Visibility = Visibility.Collapsed;
                    LoginButton.Visibility = Visibility.Visible;
                    CreateUserButton.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"First-time use check error: {ex.Message}");
            }
        }

        private async void UserId_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                string userId = UserIdTextBox.Text?.Trim() ?? string.Empty;
                
                if (string.IsNullOrEmpty(userId))
                {
                    UserNameTextBox.Clear();
                    _currentUser = null;
                    return;
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    UserNameTextBox.Text = user.FullName;
                    _currentUser = user;
                    MessageBlock.Text = "";
                }
                else
                {
                    UserNameTextBox.Clear();
                    _currentUser = null;
                }
            }
            catch (Exception ex)
            {
                MessageBlock.Text = $"Error: {ex.Message}";
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string userId = UserIdTextBox.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    MessageBlock.Text = "Please enter your User ID";
                    return;
                }

                var (success, message, user) = await _authService.LoginAsync(userId);

                if (success)
                {
                    // Store logged-in user in App for access throughout the application
                    ((App)Application.Current).LoggedInUser = user;
                    
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

        private void CreateUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateAdminWindow createUserWindow = new CreateAdminWindow();
                createUserWindow.Owner = this;
                bool? result = createUserWindow.ShowDialog();
                
                if (result == true)
                {
                    MessageBlock.Foreground = System.Windows.Media.Brushes.Green;
                    MessageBlock.Text = "User created successfully! Please login with your User ID.";
                    UserIdTextBox.Clear();
                    UserNameTextBox.Clear();
                    
                    // Bring login window to front and set focus
                    this.Activate();
                    this.Focus();
                    UserIdTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Create User Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateAdminButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateAdminWindow createAdmin = new CreateAdminWindow();
                createAdmin.Owner = this;
                bool? result = createAdmin.ShowDialog();
                
                if (result == true)
                {
                    CheckFirstTimeUse();
                    MessageBlock.Foreground = System.Windows.Media.Brushes.Green;
                    MessageBlock.Text = "Admin account created successfully! Please login with your User ID.";
                    UserIdTextBox.Clear();
                    UserNameTextBox.Clear();
                    
                    // Bring login window to front and set focus
                    this.Activate();
                    this.Focus();
                    UserIdTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Create Admin Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}