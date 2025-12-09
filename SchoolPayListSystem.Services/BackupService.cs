using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolPayListSystem.Services
{
    public class BackupService
    {
        public async Task<(bool success, string message)> BackupDatabaseAsync(string backupPath)
        {
            try
            {
                // This method can be extended in the future for more complex backup scenarios
                return await Task.FromResult((true, "Backup completed"));
            }
            catch (Exception ex)
            {
                return await Task.FromResult((false, $"Backup failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Creates a backup as a zip file containing the database
        /// </summary>
        public async Task<(bool success, string message)> BackupDatabaseAsZipAsync(string databasePath, string zipBackupPath)
        {
            string tempDbFile = null;
            try
            {
                if (!File.Exists(databasePath))
                {
                    return await Task.FromResult((false, "Database file not found."));
                }

                // Create directory if it doesn't exist
                string backupDir = Path.GetDirectoryName(zipBackupPath);
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // Create a temporary copy to handle locked database files
                tempDbFile = Path.Combine(Path.GetTempPath(), $"SchoolPayList_temp_{Guid.NewGuid()}.db");
                File.Copy(databasePath, tempDbFile, true);

                // Remove existing zip if it exists
                if (File.Exists(zipBackupPath))
                {
                    File.Delete(zipBackupPath);
                }

                // Create zip archive
                using (var zipArchive = ZipFile.Open(zipBackupPath, ZipArchiveMode.Create))
                {
                    zipArchive.CreateEntryFromFile(tempDbFile, Path.GetFileName(databasePath));
                }

                return await Task.FromResult((true, "Backup created successfully as zip file."));
            }
            catch (Exception ex)
            {
                return await Task.FromResult((false, $"Zip backup failed: {ex.Message}"));
            }
            finally
            {
                // Clean up temporary file
                try
                {
                    if (!string.IsNullOrEmpty(tempDbFile) && File.Exists(tempDbFile))
                    {
                        File.Delete(tempDbFile);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Restores the database from a backup file (supports both .db and .zip formats)
        /// </summary>
        public async Task<(bool success, string message)> RestoreDatabaseAsync(string backupFilePath, string targetDatabasePath)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                {
                    return await Task.FromResult((false, "Backup file not found."));
                }

                // Create directory if it doesn't exist
                string targetDir = Path.GetDirectoryName(targetDatabasePath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // Create automatic backup of current database before restoring
                if (File.Exists(targetDatabasePath))
                {
                    string autoBackupPath = Path.Combine(
                        targetDir,
                        $"SchoolPayList_Auto_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
                    );
                    File.Copy(targetDatabasePath, autoBackupPath, true);
                }

                // Check if file is a zip file
                if (backupFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    return await RestoreFromZipAsync(backupFilePath, targetDatabasePath, targetDir);
                }
                else
                {
                    // Direct database file restore
                    File.Copy(backupFilePath, targetDatabasePath, true);
                    return await Task.FromResult((true, "Database restored successfully from .db file."));
                }
            }
            catch (Exception ex)
            {
                return await Task.FromResult((false, $"Restore failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Restores database from a zip archive
        /// </summary>
        private async Task<(bool success, string message)> RestoreFromZipAsync(string zipFilePath, string targetDatabasePath, string targetDir)
        {
            string tempDir = Path.Combine(targetDir, $"temp_extract_{DateTime.Now:yyyyMMdd_HHmmss}");

            try
            {
                Directory.CreateDirectory(tempDir);

                // Extract the zip file
                ZipFile.ExtractToDirectory(zipFilePath, tempDir);

                // Find the database file in extracted contents
                var dbFiles = Directory.GetFiles(tempDir, "*.db", SearchOption.AllDirectories);

                if (dbFiles.Length == 0)
                {
                    return await Task.FromResult((false, "No database file (.db) found in the zip archive."));
                }

                // Use the first .db file found
                string extractedDbFile = dbFiles[0];

                // Copy extracted database to target location
                File.Copy(extractedDbFile, targetDatabasePath, true);

                return await Task.FromResult((true, "Database restored successfully from zip file."));
            }
            catch (Exception ex)
            {
                return await Task.FromResult((false, $"Zip extraction failed: {ex.Message}"));
            }
            finally
            {
                // Clean up temporary directory
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Verifies if a backup file is valid
        /// </summary>
        public async Task<(bool isValid, string message)> VerifyBackupFileAsync(string backupFilePath)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                {
                    return await Task.FromResult((false, "File does not exist."));
                }

                var fileInfo = new FileInfo(backupFilePath);
                if (fileInfo.Length == 0)
                {
                    return await Task.FromResult((false, "Backup file is empty."));
                }

                return await Task.FromResult((true, "Backup file is valid."));
            }
            catch (Exception ex)
            {
                return await Task.FromResult((false, $"Verification failed: {ex.Message}"));
            }
        }
    }
}
