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
            string username = UsernameTextBox.Text;
            string password = PasswordControl.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBlock.Text = "Username and password required";
                return;
            }

            try
            {
                using (var context = new SchoolPayListDbContext())
                {
                    var userRepo = new UserRepository(context);
                    var existing = await userRepo.GetByUsernameAsync(username);

                    if (existing != null)
                    {
                        MessageBlock.Text = "Username already exists";
                        return;
                    }

                    var admin = new User
                    {
                        Username = username,
                        PasswordHash = _authService.HashPassword(password),
                        CreatedAt = DateTime.Now,
                        IsActive = true,
                        Role = "Admin"
                    };

                    await userRepo.AddAsync(admin);
                    await userRepo.SaveChangesAsync();

                    MessageBlock.Foreground = System.Windows.Media.Brushes.Green;
                    MessageBlock.Text = "Admin created successfully!";
                    System.Threading.Thread.Sleep(1500);
                    
                    // Open Login window after successful creation
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBlock.Text = $"Error: {ex.Message}";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
