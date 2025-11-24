using System;
using System.Collections.Generic;
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
        /// Add salary entry with advice number generation and user tracking
        /// Advice number is unique per branch per day - all schools in same branch share same advice number
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
                // Generate advice number unique to this branch and date
                var adviceNumberService = new AdviceNumberService(_context);
                string adviceNumber = adviceNumberService.GenerateAdviceNumber(entryDate, branchId);

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

                            // Create salary entry
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
                                CreatedByUserId = createdByUserId,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
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
    }
}
