using System;

namespace SchoolPayListSystem.Core.Models
{
    /// <summary>
    /// Represents a user account in the system
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string Role { get; set; } = "User";
    }
}
