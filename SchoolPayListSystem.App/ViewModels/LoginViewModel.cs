using System;
using System.Windows.Input;
using SchoolPayListSystem.App.Helpers;
using SchoolPayListSystem.Services;

namespace SchoolPayListSystem.App.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _username;
        private string _password;
        private string _message;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public ICommand LoginCommand { get; }

        private readonly AuthenticationService _authService;

        public LoginViewModel()
        {
            _authService = new AuthenticationService(null);
            LoginCommand = new RelayCommand(_ => LoginExecute());
        }

        private async void LoginExecute()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                Message = "Please enter username and password";
                return;
            }

            Message = "Logging in...";
        }
    }
}
