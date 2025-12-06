using System;
using Microsoft.Data.Sqlite;
using System.IO;

namespace SchoolPayListSystem.Data.Database
{
    /// <summary>
    /// Helper class to initialize database tables if they don't exist
    /// </summary>
    public static class DatabaseInitializer
    {
        public static void InitializeDatabase()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dbFolder = Path.Combine(appData, "SchoolPayListSystem", "Database");
                string dbPath = Path.Combine(dbFolder, "SchoolPayList.db");

                if (!Directory.Exists(dbFolder))
                    Directory.CreateDirectory(dbFolder);

                string connectionString = $"Data Source={dbPath}";

                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    
                    // Create Branches and SchoolTypes tables FIRST (before AdviceNumberMappings which has FKs)
                    CreateBranchesTable(connection);
                    CreateSchoolTypesTable(connection);
                    
                    // Then create AdviceNumberMappings table
                    CreateAdviceNumberMappingsTable(connection);
                    
                    // Then ensure test data exists
                    EnsureTestBranches(connection);
                    EnsureTestSchoolTypes(connection);
                    
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
            }
        }

        private static void CreateBranchesTable(SqliteConnection connection)
        {
            const string createTableSql = @"
                CREATE TABLE IF NOT EXISTS Branches (
                    BranchId INTEGER PRIMARY KEY AUTOINCREMENT,
                    BranchName TEXT NOT NULL,
                    Address TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createTableSql;
                command.ExecuteNonQuery();
            }
        }

        private static void CreateSchoolTypesTable(SqliteConnection connection)
        {
            const string createTableSql = @"
                CREATE TABLE IF NOT EXISTS SchoolTypes (
                    SchoolTypeId INTEGER PRIMARY KEY AUTOINCREMENT,
                    SchoolTypeName TEXT NOT NULL,
                    Description TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createTableSql;
                command.ExecuteNonQuery();
            }
        }

        private static void EnsureTestBranches(SqliteConnection connection)
        {
            // Check if branches table exists
            const string checkTableSql = "SELECT name FROM sqlite_master WHERE type='table' AND name='Branches';";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = checkTableSql;
                var result = command.ExecuteScalar();
                if (result == null) return; // Table doesn't exist, skip
            }

            // Create branches if they don't exist
            for (int i = 1; i <= 3; i++)
            {
                const string checkSql = "SELECT COUNT(*) FROM Branches WHERE BranchId = @id;";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = checkSql;
                    command.Parameters.AddWithValue("@id", i);
                    var count = (long)command.ExecuteScalar();
                    if (count == 0)
                    {
                        const string insertSql = "INSERT INTO Branches (BranchId, BranchName, CreatedAt, UpdatedAt) VALUES (@id, @name, @now, @now);";
                        using (var insertCmd = connection.CreateCommand())
                        {
                            insertCmd.CommandText = insertSql;
                            insertCmd.Parameters.AddWithValue("@id", i);
                            insertCmd.Parameters.AddWithValue("@name", $"Branch{i}");
                            insertCmd.Parameters.AddWithValue("@now", DateTime.Now);
                            try { insertCmd.ExecuteNonQuery(); } catch { }
                        }
                    }
                }
            }
        }

        private static void EnsureTestSchoolTypes(SqliteConnection connection)
        {
            // Check if SchoolTypes table exists
            const string checkTableSql = "SELECT name FROM sqlite_master WHERE type='table' AND name='SchoolTypes';";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = checkTableSql;
                var result = command.ExecuteScalar();
                if (result == null) return; // Table doesn't exist, skip
            }

            // Create school types if they don't exist
            var types = new[] { ("HighSchool", "High School"), ("JuniorCollege", "Junior College"), ("Others", "Others") };
            for (int i = 1; i <= types.Length; i++)
            {
                const string checkSql = "SELECT COUNT(*) FROM SchoolTypes WHERE SchoolTypeId = @id;";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = checkSql;
                    command.Parameters.AddWithValue("@id", i);
                    var count = (long)command.ExecuteScalar();
                    if (count == 0)
                    {
                        const string insertSql = "INSERT INTO SchoolTypes (SchoolTypeId, SchoolTypeName, CreatedAt, UpdatedAt) VALUES (@id, @name, @now, @now);";
                        using (var insertCmd = connection.CreateCommand())
                        {
                            insertCmd.CommandText = insertSql;
                            insertCmd.Parameters.AddWithValue("@id", i);
                            insertCmd.Parameters.AddWithValue("@name", types[i - 1].Item2);
                            insertCmd.Parameters.AddWithValue("@now", DateTime.Now);
                            try { insertCmd.ExecuteNonQuery(); } catch { }
                        }
                    }
                }
            }
        }

        private static void CreateAdviceNumberMappingsTable(SqliteConnection connection)
        {
            const string createTableSql = @"
                CREATE TABLE IF NOT EXISTS AdviceNumberMappings (
                    AdviceNumberMappingId INTEGER PRIMARY KEY AUTOINCREMENT,
                    AdviceNumber TEXT NOT NULL,
                    AdviceDate DATE NOT NULL,
                    BranchId INTEGER NOT NULL,
                    SchoolTypeId INTEGER NOT NULL,
                    SerialNumber INTEGER NOT NULL,
                    GeneratedByModule TEXT,
                    GeneratedTimestamp DATETIME,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE (AdviceDate, BranchId, SchoolTypeId),
                    FOREIGN KEY (BranchId) REFERENCES Branches(BranchId) ON DELETE RESTRICT,
                    FOREIGN KEY (SchoolTypeId) REFERENCES SchoolTypes(SchoolTypeId) ON DELETE RESTRICT
                );
            ";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createTableSql;
                command.ExecuteNonQuery();
            }

            // Create index if it doesn't exist
            const string createIndexSql = @"
                CREATE UNIQUE INDEX IF NOT EXISTS IX_AdviceNumberMappings_Date_Branch_SchoolType
                ON AdviceNumberMappings(AdviceDate, BranchId, SchoolTypeId);
            ";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createIndexSql;
                command.ExecuteNonQuery();
            }
        }
    }
}
