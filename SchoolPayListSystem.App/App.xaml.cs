using System;
using System.Linq;
using System.Windows;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;
using SQLitePCL;

namespace SchoolPayListSystem.App
{
    public partial class App : Application
    {
        /// <summary>
        /// Currently logged-in user - accessible throughout the application
        /// </summary>
        public User LoggedInUser { get; set; }

        static App()
        {
            // Initialize SQLite FIRST - before anything else
            try
            {
                raw.SetProvider(new SQLite3Provider_e_sqlite3());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SQLite provider setup: {ex.Message}");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                // Initialize database on startup
                LocalDbInitializer.Initialize();
                
                // Check if this is first-time use
                var context = new SchoolPayListDbContext();
                int userCount = context.Users.Count();
                
                // If only GCP admin (1 user), show Login window (will prompt for create admin if needed)
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                string errorMessage = $"Application Startup Error: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
                }
                
                System.Diagnostics.Debug.WriteLine(errorMessage);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                MessageBox.Show(errorMessage, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
            
            this.DispatcherUnhandledException += (s, args) =>
            {
                string errorMessage = $"Unhandled Error: {args.Exception.Message}\n\n{args.Exception.StackTrace}";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}
