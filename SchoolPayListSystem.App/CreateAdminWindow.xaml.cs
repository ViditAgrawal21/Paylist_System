using System;
using System.Windows;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using SchoolPayListSystem.Services;

namespace SchoolPayListSystem.App
{
    public partial class CreateAdminWindow : Window
    {
        private AuthenticationService _authService;

        public CreateAdminWindow()
        {
            InitializeComponent();
            _authService = new AuthenticationService(new UserRepository(new SchoolPayListDbContext()));
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string userId = UsernameTextBox.Text?.Trim() ?? string.Empty;
            string fullName = FullNameTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(fullName))
            {
                MessageBlock.Text = "User ID and Full Name are required";
                return;
            }

            try
            {
                var (success, message, user) = await _authService.CreateUserAsync(userId, fullName);

                if (success)
                {
                    MessageBlock.Foreground = System.Windows.Media.Brushes.Green;
                    MessageBlock.Text = "User created successfully!";
                    System.Threading.Thread.Sleep(1500);
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBlock.Text = message;
                }
            }
            catch (Exception ex)
            {
                MessageBlock.Text = $"Error: {ex.Message}";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
