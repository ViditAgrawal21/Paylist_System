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

        /// <summary>
        /// Login with ID only (no password required)
        /// </summary>
        public async Task<(bool success, string message, User user)> LoginAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return (false, "User ID is required", null);

                var user = await _userRepository.GetByUsernameAsync(userId);
                if (user == null)
                    return (false, "User not found", null);

                if (!user.IsActive)
                    return (false, "User account is inactive", null);

                return (true, "Login successful", user);
            }
            catch (Exception ex)
            {
                return (false, $"Login error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Create a new user with ID and Name (no password)
        /// </summary>
        public async Task<(bool success, string message, User user)> CreateUserAsync(string userId, string fullName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return (false, "User ID is required", null);

                if (string.IsNullOrWhiteSpace(fullName))
                    return (false, "Full Name is required", null);

                var existingUser = await _userRepository.GetByUsernameAsync(userId);
                if (existingUser != null)
                    return (false, "User ID already exists", null);

                var newUser = new User
                {
                    Username = userId,
                    FullName = fullName,
                    PasswordHash = "", // No password needed
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    Role = "User"
                };

                await _userRepository.AddAsync(newUser);
                await _userRepository.SaveChangesAsync();

                return (true, "User created successfully", newUser);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating user: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Get user by ID (username)
        /// </summary>
        public async Task<User> GetUserByIdAsync(string userId)
        {
            return await _userRepository.GetByUsernameAsync(userId);
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
