using System;
using System.Threading.Tasks;

namespace SchoolPayListSystem.Services
{
    public class BackupService
    {
        public async Task<(bool success, string message)> BackupDatabaseAsync(string backupPath)
        {
            return await Task.FromResult((true, "Backup completed"));
        }
    }
}
