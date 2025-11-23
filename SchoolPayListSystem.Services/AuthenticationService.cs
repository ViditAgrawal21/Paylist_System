using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Repositories;

namespace SchoolPayListSystem.Services
{
    public class AuthenticationService
    {
        private readonly IUserRepository _userRepository;

        public AuthenticationService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<(bool success, string message, User user)> LoginAsync(string username, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
                return (false, "User not found", null);

            if (!ValidatePassword(password, user.PasswordHash))
                return (false, "Invalid password", null);

            return (true, "Login successful", user);
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hash);
            }
        }

        public bool ValidatePassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hash);
        }
    }
}
