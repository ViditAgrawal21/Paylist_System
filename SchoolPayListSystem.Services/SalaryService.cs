using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using SchoolPayListSystem.Core.DTOs;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace SchoolPayListSystem.Services
{
    public class SalaryService
    {
        private readonly ISalaryEntryRepository _salaryRepository;
        private readonly SchoolPayListDbContext _context;
        private readonly ISchoolRepository _schoolRepository;
        private readonly IBranchRepository _branchRepository;

        public SalaryService(ISalaryEntryRepository salaryRepository, ISchoolRepository schoolRepository = null, IBranchRepository branchRepository = null)
        {
            _salaryRepository = salaryRepository;
            _schoolRepository = schoolRepository;
            _branchRepository = branchRepository;
            _context = new SchoolPayListDbContext();
        }

        public async Task<List<SalaryEntry>> GetAllEntriesAsync()
        {
            return await _salaryRepository.GetAllWithNavigationAsync();
        }

        public async Task<List<SalaryEntry>> GetEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _salaryRepository.GetByDateRangeAsync(startDate, endDate);
        }

        /// <summary>
        /// Get all salary entries for a specific user (operator)
        /// Filters by CreatedByUserId which links to the operator's UserId
        /// </summary>
        public async Task<List<SalaryEntry>> GetEntriesByUserIdAsync(int userId)
        {
            return await _salaryRepository.GetByCreatedByUserIdAsync(userId);
        }

        /// <summary>
        /// Get salary entries for a user within a date range
        /// </summary>
        public async Task<List<SalaryEntry>> GetEntriesByUserIdAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var entries = await _salaryRepository.GetByCreatedByUserIdAsync(userId);
            return entries.Where(e => e.EntryDate >= startDate && e.EntryDate <= endDate).ToList();
        }

        /// <summary>
        /// Add salary entry with advice number generation and user tracking
        /// Advice number is unique per school type per day - all branches of same school type share sequential advice numbers
        /// </summary>
        public async Task<(bool success, string message, SalaryEntry entry)> AddSalaryEntryAsync(
            DateTime entryDate, 
            int schoolId, 
            int branchId, 
            string accountNumber, 
            decimal amount1, 
            decimal amount2, 
            decimal amount3,
            int createdByUserId)
        {
            try
            {
                // Get the school to retrieve its school type
                var school = _context.Schools.FirstOrDefault(s => s.SchoolId == schoolId);
                if (school == null)
                    throw new Exception("School not found");

                // Generate advice number unique to this school type and date
                var adviceNumberService = new AdviceNumberService(_context);
                string adviceNumber = adviceNumberService.GenerateAdviceNumber(entryDate, branchId, school.SchoolTypeId);

                decimal total = amount1 + amount2 + amount3;
                var entry = new SalaryEntry
                {
                    EntryDate = entryDate,
                    SchoolId = schoolId,
                    BranchId = branchId,
                    AccountNumber = accountNumber,
                    Amount1 = amount1,
                    Amount2 = amount2,
                    Amount3 = amount3,
                    TotalAmount = total,
                    AdviceNumber = adviceNumber,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                await _salaryRepository.AddAsync(entry);
                await _salaryRepository.SaveChangesAsync();
                return (true, "Salary entry added successfully", entry);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Backward compatibility - for existing code that doesn't pass createdByUserId
        /// </summary>
        public async Task<(bool success, string message)> AddSalaryEntryAsync(
            DateTime entryDate, 
            int schoolId, 
            int branchId, 
            string accountNumber, 
            decimal amount1, 
            decimal amount2, 
            decimal amount3)
        {
            try
            {
                var result = await AddSalaryEntryAsync(entryDate, schoolId, branchId, accountNumber, amount1, amount2, amount3, 0);
                return (result.success, result.message);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Import salary entries from Excel file with BankAccount to School lookup
        /// </summary>
        public async Task<(bool success, string message, int importedCount, List<string> errors)> ImportSalariesFromExcelAsync(string filePath, int createdByUserId)
        {
            var errors = new List<string>();
            int importedCount = 0;

            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    return (false, "File not found", 0, new List<string> { "Excel file not found" });
                }

                // Set EPPlus license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new System.IO.FileInfo(filePath)))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        return (false, "No worksheets found in Excel file", 0, new List<string> { "Workbook is empty" });
                    }

                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension?.Rows ?? 0;

                    if (rowCount < 2)
                    {
                        return (false, "Excel file has no data rows", 0, new List<string> { "No data found after headers" });
                    }

                    // Start from row 2 (row 1 is headers)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            // Read Excel columns according to format:
                            // D: BankAccount, E: SchoolTypeCode, F: SchoolType, G: BranchCode, H: Branch
                            // I: AMOUNT, J: AMOUNT1, K: AMOUNT2, L: AdviceNumber, M: OperatorName
                            // N: STAMPDATE, O: STAMPTIME, P: UserID

                            string bankAccount = worksheet.Cells[$"D{row}"].Value?.ToString()?.Trim() ?? "";
                            string stampDateStr = worksheet.Cells[$"N{row}"].Value?.ToString()?.Trim() ?? "";
                            string stampTimeStr = worksheet.Cells[$"O{row}"].Value?.ToString()?.Trim() ?? "";
                            string operatorName = worksheet.Cells[$"M{row}"].Value?.ToString()?.Trim() ?? "";
                            string adviceNumber = worksheet.Cells[$"L{row}"].Value?.ToString()?.Trim() ?? "";
                            string userIdFromExcel = worksheet.Cells[$"P{row}"].Value?.ToString()?.Trim() ?? "";

                            // Parse amounts
                            if (!decimal.TryParse(worksheet.Cells[$"I{row}"].Value?.ToString() ?? "0", out decimal amount))
                                amount = 0;
                            if (!decimal.TryParse(worksheet.Cells[$"J{row}"].Value?.ToString() ?? "0", out decimal amount1))
                                amount1 = 0;
                            if (!decimal.TryParse(worksheet.Cells[$"K{row}"].Value?.ToString() ?? "0", out decimal amount2))
                                amount2 = 0;

                            // Amount3 is calculated as Amount - Amount1 - Amount2
                            decimal amount3 = amount - amount1 - amount2;
                            if (amount3 < 0) amount3 = 0;

                            // Parse date/time
                            if (!DateTime.TryParse(stampDateStr, out DateTime entryDate))
                            {
                                errors.Add($"Row {row}: Invalid date '{stampDateStr}'");
                                continue;
                            }

                            TimeSpan entryTime = TimeSpan.Zero;
                            if (!string.IsNullOrEmpty(stampTimeStr))
                            {
                                if (!TimeSpan.TryParse(stampTimeStr, out entryTime))
                                {
                                    errors.Add($"Row {row}: Invalid time '{stampTimeStr}', using 00:00:00");
                                }
                            }

                            // Skip if no bank account
                            if (string.IsNullOrEmpty(bankAccount))
                            {
                                errors.Add($"Row {row}: Bank account is required");
                                continue;
                            }

                            // Determine CreatedByUserId: use UserID from Excel if present, otherwise use passed parameter
                            int actualCreatedByUserId = createdByUserId;
                            if (!string.IsNullOrEmpty(userIdFromExcel) && int.TryParse(userIdFromExcel, out int userIdFromExcelInt))
                            {
                                // Verify this user exists in database
                                var userFromExcel = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userIdFromExcelInt);
                                if (userFromExcel != null)
                                {
                                    actualCreatedByUserId = userIdFromExcelInt;
                                }
                                else
                                {
                                    errors.Add($"Row {row}: User ID '{userIdFromExcel}' not found in database, using current operator");
                                }
                            }

                            // Find School by BankAccount
                            var school = await _context.Schools.FirstOrDefaultAsync(s => s.BankAccountNumber == bankAccount);
                            if (school == null)
                            {
                                errors.Add($"Row {row}: School with bank account '{bankAccount}' not found");
                                continue;
                            }

                            // Validate amount
                            if (amount <= 0)
                            {
                                errors.Add($"Row {row}: Amount must be greater than 0");
                                continue;
                            }

                            // Create salary entry - marked as imported
                            var entry = new SalaryEntry
                            {
                                EntryDate = entryDate,
                                SchoolId = school.SchoolId,
                                BranchId = school.BranchId,
                                AccountNumber = bankAccount,
                                Amount1 = amount1,
                                Amount2 = amount2,
                                Amount3 = amount3,
                                TotalAmount = amount,
                                AdviceNumber = adviceNumber,
                                OperatorName = operatorName,
                                EntryTime = entryTime,
                                CreatedByUserId = actualCreatedByUserId,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now,
                                IsImported = true  // Mark as imported from Excel
                            };

                            await _salaryRepository.AddAsync(entry);
                            importedCount++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Row {row}: Error - {ex.Message}");
                        }
                    }

                    // Save all entries at once
                    if (importedCount > 0)
                    {
                        await _salaryRepository.SaveChangesAsync();
                    }

                    string message = $"Successfully imported {importedCount} salary entries";
                    if (errors.Count > 0)
                        message += $" ({errors.Count} errors)";

                    return (true, message, importedCount, errors);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Import failed: {ex.Message}");
                return (false, $"Import error: {ex.Message}", importedCount, errors);
            }
        }

        /// <summary>
        /// Get salary entries created by specific user (operator)
        /// </summary>
        public async Task<List<SalaryEntry>> GetEntriesByOperatorAsync(int createdByUserId)
        {
            return await _salaryRepository.GetByCreatedByUserIdAsync(createdByUserId);
        }

        /// <summary>
        /// Get fresh (manually created) salary entries for display with full details
        /// </summary>
        public async Task<List<FreshSalaryEntryDTO>> GetFreshEntriesForDisplayAsync(int createdByUserId)
        {
            try
            {
                var entries = await _context.SalaryEntries
                    .Where(se => se.CreatedByUserId == createdByUserId && !se.IsImported)
                    .Join(_context.Schools, se => se.SchoolId, s => s.SchoolId, (se, s) => new { se, s })
                    .Join(_context.Branches, x => x.se.BranchId, b => b.BranchId, (x, b) => new { x.se, x.s, b })
                    .Join(_context.SchoolTypes, x => x.s.SchoolTypeId, st => st.SchoolTypeId, (x, st) => new { x.se, x.s, x.b, st })
                    .Join(_context.Users, x => x.se.CreatedByUserId, u => u.UserId, (x, u) => new { x.se, x.s, x.b, x.st, u })
                    .OrderByDescending(x => x.se.EntryDate)
                    .Select(x => new FreshSalaryEntryDTO
                    {
                        SalaryEntryId = x.se.SalaryEntryId,
                        INDATE = x.se.EntryDate,
                        SchoolCode = x.s.SchoolCode,
                        SchoolName = x.s.SchoolName,
                        BankAccount = x.se.AccountNumber,
                        SchoolTypeCode = x.st.TypeCode,
                        SchoolType = x.st.TypeName,
                        BranchCode = x.b.BranchId,
                        Branch = x.b.BranchName,
                        AMOUNT = x.se.TotalAmount,
                        AMOUNT1 = x.se.Amount1,
                        AMOUNT2 = x.se.Amount2,
                        AMOUNT3 = x.se.Amount3,
                        OperatorName = x.u.FullName,
                        OperatorId = x.u.Username,
                        UserId = x.u.UserId,
                        STAMPDATE = x.se.CreatedAt,
                        STAMPTIME = x.se.CreatedAt.TimeOfDay
                    })
                    .ToListAsync();

                return entries;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving fresh entries: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get imported salary entries as summary by date, school type, and branch
        /// </summary>
        public async Task<List<ImportedSalaryEntrySummaryDTO>> GetImportedEntriesSummaryAsync(int createdByUserId)
        {
            try
            {
                var importedEntries = await _context.SalaryEntries
                    .Where(se => se.CreatedByUserId == createdByUserId && se.IsImported)
                    .Join(_context.Schools, se => se.SchoolId, s => s.SchoolId, (se, s) => new { se, s })
                    .Join(_context.Branches, x => x.se.BranchId, b => b.BranchId, (x, b) => new { x.se, x.s, b })
                    .Join(_context.SchoolTypes, x => x.s.SchoolTypeId, st => st.SchoolTypeId, (x, st) => new { x.se, x.s, x.b, st })
                    .ToListAsync();  // Fetch data to client first

                // Group and summarize on client side (LINQ to Objects)
                var summary = importedEntries
                    .GroupBy(x => new { x.se.EntryDate, x.st.TypeCode, x.st.TypeName, x.b.BranchName })
                    .OrderByDescending(g => g.Key.EntryDate)
                    .Select(g => new ImportedSalaryEntrySummaryDTO
                    {
                        EntryDate = g.Key.EntryDate,
                        SchoolTypeCode = g.Key.TypeCode,
                        SchoolTypeName = g.Key.TypeName,
                        BranchName = g.Key.BranchName,
                        Count = g.Count(),
                        TotalAmount = g.Sum(x => x.se.TotalAmount)  // Sum on client side
                    })
                    .ToList();

                return summary;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving imported entries summary: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get export summary - count of entries per operator (for GCP admin view)
        /// Shows data only for the 4 configured operator IDs: SNC, RRS, RRS915, ratesh
        /// </summary>
        public async Task<List<ExportSummaryDTO>> GetExportSummaryAsync()
        {
            try
            {
                // List of 4 operator IDs to show
                var operatorIds = new[] { "SNC", "RRS", "RRS915", "ratesh" };

                // Load all entries and users to memory first
                var entries = await _context.SalaryEntries.ToListAsync();
                var users = await _context.Users
                    .Where(u => operatorIds.Contains(u.Username))
                    .ToListAsync();

                // Group entries by user and create summary - only for our 4 operators
                var summary = entries
                    .Where(se => operatorIds.Contains(_context.Users.FirstOrDefault(u => u.UserId == se.CreatedByUserId)?.Username ?? ""))
                    .GroupBy(se => se.CreatedByUserId)
                    .Select(g => new ExportSummaryDTO
                    {
                        UserId = g.Key,
                        OperatorId = users.FirstOrDefault(u => u.UserId == g.Key)?.Username ?? "Unknown",
                        OperatorName = users.FirstOrDefault(u => u.UserId == g.Key)?.FullName ?? "Unknown",
                        FreshEntryCount = g.Count(se => !se.IsImported),
                        ImportedEntryCount = g.Count(se => se.IsImported),
                        TotalEntryCount = g.Count(),
                        TotalAmount = g.Sum(se => se.TotalAmount),
                        LastEntryDate = g.Max(se => se.EntryDate)
                    })
                    .OrderBy(x => x.OperatorId)
                    .ToList();

                // Add entries for operators with no data yet (count = 0)
                foreach (var opId in operatorIds)
                {
                    if (!summary.Any(s => s.OperatorId == opId))
                    {
                        var user = users.FirstOrDefault(u => u.Username == opId);
                        if (user != null)
                        {
                            summary.Add(new ExportSummaryDTO
                            {
                                UserId = user.UserId,
                                OperatorId = user.Username,
                                OperatorName = user.FullName,
                                FreshEntryCount = 0,
                                ImportedEntryCount = 0,
                                TotalEntryCount = 0,
                                TotalAmount = 0,
                                LastEntryDate = null
                            });
                        }
                    }
                }

                return summary.OrderBy(x => x.OperatorId).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving export summary: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get data count for a specific user (shown after login)
        /// </summary>
        public async Task<ExportSummaryDTO> GetUserDataSummaryAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                    return null;

                var entries = _context.SalaryEntries.Where(se => se.CreatedByUserId == userId).ToList();

                var summary = new ExportSummaryDTO
                {
                    UserId = user.UserId,
                    OperatorId = user.Username,
                    OperatorName = user.FullName,
                    FreshEntryCount = entries.Count(se => !se.IsImported),
                    ImportedEntryCount = entries.Count(se => se.IsImported),
                    TotalEntryCount = entries.Count,
                    TotalAmount = entries.Sum(se => se.TotalAmount),
                    LastEntryDate = entries.Any() ? entries.Max(se => se.EntryDate) : null
                };

                return await Task.FromResult(summary);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving user data summary: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete all salary entries (both imported and manually added)
        /// </summary>
        public async Task<(bool success, int deletedCount, string message)> ClearAllSalaryEntriesAsync()
        {
            try
            {
                var allEntries = await _context.SalaryEntries.ToListAsync();
                int count = allEntries.Count;

                if (count == 0)
                {
                    return (true, 0, "Database is already empty. No entries to delete.");
                }

                _context.SalaryEntries.RemoveRange(allEntries);
                await _context.SaveChangesAsync();

                return (true, count, $"Successfully deleted {count} salary entries from the database.");
            }
            catch (Exception ex)
            {
                return (false, 0, $"Error clearing salary entries: {ex.Message}");
            }
        }

        /// <summary>
        /// Seed sample data for testing (only if database is empty)
        /// </summary>
        public async Task<(bool success, string message)> SeedSampleDataAsync()
        {
            try
            {
                var existingEntries = await _context.SalaryEntries.CountAsync();
                if (existingEntries > 0)
                {
                    return (false, "Database already contains data. Clear it first if you want to seed fresh sample data.");
                }

                // Get first 4 users (the operators)
                var users = await _context.Users
                    .Where(u => new[] { "SNC", "RRS", "RRS915", "ratesh" }.Contains(u.Username))
                    .ToListAsync();

                if (users.Count < 4)
                {
                    return (false, "Not all 4 operators found in database");
                }

                // Get sample school and branch
                var school = await _context.Schools.FirstOrDefaultAsync();
                var branch = await _context.Branches.FirstOrDefaultAsync();

                if (school == null || branch == null)
                {
                    return (false, "No schools or branches found in database");
                }

                var random = new Random();
                int sampleCount = 0;

                // Create sample entries for each operator
                foreach (var user in users)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var entry = new SalaryEntry
                        {
                            EntryDate = DateTime.Now.AddDays(-random.Next(0, 10)),
                            SchoolId = school.SchoolId,
                            BranchId = branch.BranchId,
                            AccountNumber = school.BankAccountNumber,
                            Amount1 = random.Next(10000, 50000),
                            Amount2 = random.Next(5000, 20000),
                            Amount3 = random.Next(2000, 10000),
                            TotalAmount = random.Next(20000, 80000),
                            AdviceNumber = $"{DateTime.Now:ddMMyy}{random.Next(10, 99):D2}",
                            CreatedByUserId = user.UserId,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            IsImported = i % 2 == 0  // Alternate between fresh and imported
                        };

                        await _context.SalaryEntries.AddAsync(entry);
                        sampleCount++;
                    }
                }

                await _context.SaveChangesAsync();
                return (true, $"Successfully seeded {sampleCount} sample entries for testing.");
            }
            catch (Exception ex)
            {
                return (false, $"Error seeding sample data: {ex.Message}");
            }
        }
    }
}
