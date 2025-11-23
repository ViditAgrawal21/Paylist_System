using System;
using System.Windows;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SchoolPayListSystem.Services;

namespace SchoolPayListSystem.App
{
    public partial class ForgotPasswordWindow : Window
    {
        private SchoolPayListDbContext _context;
        private UserRepository _userRepository;
        private AuthenticationService _authService;

        public ForgotPasswordWindow()
        {
            try
            {
                InitializeComponent();
                
                _context = new SchoolPayListDbContext();
                _userRepository = new UserRepository(_context);
                _authService = new AuthenticationService(_userRepository);

                this.Topmost = true;
                this.Focus();
            }
            catch (Exception ex)
            {
                string fullError = $"Forgot Password Window Initialization Error: {ex.Message}";
                if (ex.InnerException != null)
                {
                    fullError += $"\n\nInner Exception: {ex.InnerException.Message}";
                }
                MessageBox.Show(fullError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = UsernameTextBox.Text?.Trim() ?? string.Empty;
                string newPassword = NewPasswordControl.Password ?? string.Empty;
                string confirmPassword = ConfirmPasswordControl.Password ?? string.Empty;

                // Validation
                if (string.IsNullOrWhiteSpace(username))
                {
                    MessageBlock.Text = "Please enter your username";
                    MessageBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0x00, 0x00));
                    return;
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    MessageBlock.Text = "Please enter a new password";
                    MessageBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0x00, 0x00));
                    return;
                }

                if (newPassword.Length < 6)
                {
                    MessageBlock.Text = "Password must be at least 6 characters long";
                    MessageBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0x00, 0x00));
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    MessageBlock.Text = "Passwords do not match";
                    MessageBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0x00, 0x00));
                    return;
                }

                // Check if user exists
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    MessageBlock.Text = "Username not found";
                    MessageBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0x00, 0x00));
                    return;
                }

                // Update password
                user.PasswordHash = _authService.HashPassword(newPassword);
                await _userRepository.UpdateAsync(user);

                MessageBlock.Text = "Password reset successfully! Redirecting to login...";
                MessageBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xAA, 0x00));

                // Close after 1.5 seconds
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(1500);
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    this.DialogResult = true;
                    this.Close();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
