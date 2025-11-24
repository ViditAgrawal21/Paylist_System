using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SchoolPayListSystem.Core.Models;
using SQLitePCL;
using Microsoft.EntityFrameworkCore;

namespace SchoolPayListSystem.Data.Database
{
    public class LocalDbInitializer
    {
        static LocalDbInitializer()
        {
            // Initialize SQLite provider on first load
            try
            {
                raw.SetProvider(new SQLite3Provider_e_sqlite3());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SQLite provider initialization in LocalDbInitializer: {ex.Message}");
            }
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public static void Initialize()
        {
            try
            {
                // Ensure SQLite provider is initialized
                try
                {
                    raw.SetProvider(new SQLite3Provider_e_sqlite3());
                }
                catch
                {
                    // Already initialized, that's fine
                }

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dbFolder = Path.Combine(appData, "SchoolPayListSystem", "Database");
                
                // Create folder with error handling
                if (!Directory.Exists(dbFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(dbFolder);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to create database folder at '{dbFolder}': {ex.Message}", ex);
                    }
                }

                // Ensure folder is writable
                string testFile = Path.Combine(dbFolder, ".writetest");
                try
                {
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Database folder is not writable. Check folder permissions: {dbFolder}\n{ex.Message}", ex);
                }

                using (var context = new SchoolPayListDbContext())
                {
                    context.Database.EnsureCreated();

                    // Add new columns to SalaryEntry if they don't exist (for existing databases)
                    try
                    {
                        var connection = context.Database.GetDbConnection();
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            // Check if EntryTime column exists, if not add it
                            command.CommandText = "PRAGMA table_info(SalaryEntries)";
                            var reader = command.ExecuteReader();
                            bool hasEntryTime = false;
                            bool hasOperatorName = false;
                            
                            while (reader.Read())
                            {
                                string columnName = reader["name"].ToString();
                                if (columnName == "EntryTime") hasEntryTime = true;
                                if (columnName == "OperatorName") hasOperatorName = true;
                            }
                            reader.Close();

                            // Add missing columns
                            if (!hasEntryTime)
                            {
                                command.CommandText = "ALTER TABLE SalaryEntries ADD COLUMN EntryTime TEXT";
                                command.ExecuteNonQuery();
                            }

                            if (!hasOperatorName)
                            {
                                command.CommandText = "ALTER TABLE SalaryEntries ADD COLUMN OperatorName TEXT";
                                command.ExecuteNonQuery();
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Could not add new columns to SalaryEntries table: {ex.Message}");
                    }

                    if (!context.SchoolTypes.Any())
                    {
                        context.SchoolTypes.AddRange(
                            new SchoolType { TypeCode = "PS", TypeName = "Primary School", IsDefault = true, CreatedAt = DateTime.Now },
                            new SchoolType { TypeCode = "HS", TypeName = "High School", IsDefault = false, CreatedAt = DateTime.Now },
                            new SchoolType { TypeCode = "JC", TypeName = "Junior College", IsDefault = false, CreatedAt = DateTime.Now }
                        );
                        context.SaveChanges();
                    }

                    if (!context.Branches.Any())
                    {
                        context.Branches.Add(
                            new Branch { BranchCode = 1, BranchName = "Head Office", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
                        );
                        context.SaveChanges();
                    }

                    // Add hard-coded GCP admin user if no users exist
                    if (!context.Users.Any())
                    {
                        // Hard-coded GCP admin credentials
                        string gcpAdminUsername = "GCP";
                        string gcpAdminPassword = "GCP"; // Default GCP admin password
                        
                        // Hash the password
                        string hashedPassword = HashPassword(gcpAdminPassword);
                        
                        context.Users.Add(
                            new User
                            {
                                Username = gcpAdminUsername,
                                FullName = "GCP Administrator",
                                PasswordHash = hashedPassword,
                                CreatedAt = DateTime.Now,
                                IsActive = true,
                                Role = "Admin"
                            }
                        );
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database initialization failed: {ex.Message}", ex);
            }
        }
    }
}

