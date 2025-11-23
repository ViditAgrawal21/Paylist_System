using System;
using System.Windows;
using System.Windows.Controls;

namespace SchoolPayListSystem.App.Controls
{
    public partial class PasswordBoxWithToggle : UserControl
    {
        private bool _isPasswordVisible = false;

        public string Password
        {
            get
            {
                return _isPasswordVisible ? PasswordTextBox.Text : PasswordBoxControl.Password;
            }
            set
            {
                if (_isPasswordVisible)
                {
                    PasswordTextBox.Text = value;
                }
                else
                {
                    PasswordBoxControl.Password = value;
                }
            }
        }

        public void Clear()
        {
            PasswordBoxControl.Clear();
            PasswordTextBox.Clear();
            _isPasswordVisible = false;
            PasswordBoxControl.Visibility = Visibility.Visible;
            PasswordTextBox.Visibility = Visibility.Collapsed;
            EyeIcon.Text = "👁️";
        }

        public PasswordBoxWithToggle()
        {
            InitializeComponent();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                // Show password as TextBox
                PasswordTextBox.Text = PasswordBoxControl.Password;
                PasswordBoxControl.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                PasswordTextBox.Focus();
                EyeIcon.Text = "👁‍🗨️";
            }
            else
            {
                // Hide password in PasswordBox
                PasswordBoxControl.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBoxControl.Visibility = Visibility.Visible;
                PasswordBoxControl.Focus();
                EyeIcon.Text = "👁️";
            }
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Keep PasswordBox in sync if needed
        }
    }
}
