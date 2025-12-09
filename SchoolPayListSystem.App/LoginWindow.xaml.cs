using System;
using System.IO;
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
        private string _logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SchoolPayListSystem", "login_debug.log");

        private void LogDebug(string message)
        {
            try
            {
                string logDir = Path.GetDirectoryName(_logFile);
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);
                
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                File.AppendAllText(_logFile, logMessage + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
            catch { }
        }

        public LoginWindow()
        {
            try
            {
                LogDebug("=== LoginWindow Constructor Starting ===");
                
                InitializeComponent();
                LogDebug("InitializeComponent completed");
                
                // Initialize database if needed
                LogDebug("Calling LocalDbInitializer.Initialize()");
                LocalDbInitializer.Initialize();
                LogDebug("LocalDbInitializer.Initialize() completed");
                
                // Create database context with proper error handling
                LogDebug("Creating SchoolPayListDbContext");
                _context = new SchoolPayListDbContext();
                LogDebug("SchoolPayListDbContext created");
                
                _userRepository = new UserRepository(_context);
                _authService = new AuthenticationService(_userRepository);
                LogDebug("AuthenticationService initialized");
                
                // Check if this is first-time use
                LogDebug("Checking first-time use");
                CheckFirstTimeUse();
                LogDebug("First-time use check completed");
                
                this.Topmost = true;
                this.Focus();
                
                LogDebug("=== LoginWindow Constructor Complete ===");
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
                
                LogDebug($"EXCEPTION in constructor: {fullError}");
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

        private void UserId_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LogDebug($"=== TextChanged Event Fired ===");
            LogDebug($"UserIdTextBox.Text = '{UserIdTextBox.Text}'");
            
            try
            {
                string userId = UserIdTextBox.Text?.Trim() ?? string.Empty;
                LogDebug($"Trimmed userId = '{userId}'");
                
                if (string.IsNullOrEmpty(userId))
                {
                    LogDebug("UserId is empty, clearing username");
                    UserNameTextBox.Text = "";
                    _currentUser = null;
                    return;
                }

                LogDebug($"Querying database for userId: '{userId}'");
                
                // Check if context is valid
                if (_context == null)
                {
                    LogDebug("ERROR: _context is null!");
                    return;
                }

                try
                {
                    var allUsers = _context.Users.ToList();
                    LogDebug($"Database returned {allUsers.Count} users");
                    
                    foreach (var u in allUsers)
                    {
                        LogDebug($"  - User: '{u.Username}' -> '{u.FullName}'");
                    }
                    
                    var user = allUsers.FirstOrDefault(u => u.Username.Equals(userId, StringComparison.OrdinalIgnoreCase));
                    
                    if (user != null)
                    {
                        LogDebug($"FOUND: Matched user '{user.Username}' with FullName '{user.FullName}'");
                        LogDebug($"Setting UserNameTextBox.Text to '{user.FullName}'");
                        UserNameTextBox.Text = user.FullName ?? "N/A";
                        LogDebug($"UserNameTextBox.Text is now: '{UserNameTextBox.Text}'");
                        _currentUser = user;
                        MessageBlock.Text = "";
                    }
                    else
                    {
                        LogDebug($"NOT FOUND: No user matched '{userId}'");
                        UserNameTextBox.Text = "";
                        _currentUser = null;
                    }
                }
                catch (Exception dbEx)
                {
                    LogDebug($"Database Error: {dbEx.GetType().Name}: {dbEx.Message}");
                    LogDebug($"Stack: {dbEx.StackTrace}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                LogDebug($"Stack: {ex.StackTrace}");
                MessageBlock.Text = $"Error: {ex.Message}";
            }
            LogDebug($"=== End TextChanged Event ===");
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