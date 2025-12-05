using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Repositories;

namespace SchoolPayListSystem.Services
{
    public class ExcelImportService
    {
        private readonly SchoolTypeService _schoolTypeService;
        private readonly SchoolService _schoolService;
        private readonly BranchService _branchService;
        private readonly ISalaryEntryRepository _salaryRepository;

        public ExcelImportService(
            SchoolTypeService schoolTypeService, 
            SchoolService schoolService, 
            BranchService branchService,
            ISalaryEntryRepository salaryRepository = null)
        {
            _schoolTypeService = schoolTypeService;
            _schoolService = schoolService;
            _branchService = branchService;
            _salaryRepository = salaryRepository;
            
            // Set EPPlus license context for non-commercial use
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<(bool success, string message, List<string> errors)> ImportSchoolTypesFromExcel(string filePath)
        {
            var errors = new List<string>();
            var successCount = 0;

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0]; // First worksheet
                
                // Expected columns: TypeCode, TypeName
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                
                if (rowCount < 2)
                {
                    return (false, "Excel file must contain at least one data row (plus header)", errors);
                }

                // Validate headers
                var expectedHeaders = new[] { "typecode", "typename" };
                for (int col = 1; col <= expectedHeaders.Length; col++)
                {
                    var header = worksheet.Cells[1, col].Text?.Trim().ToLower();
                    if (header != expectedHeaders[col - 1])
                    {
                        return (false, $"Column {col} must have '{expectedHeaders[col - 1]}' as header", errors);
                    }
                }

                // Process each row starting from row 2 (skip header)
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var typeCode = worksheet.Cells[row, 1].Text?.Trim();
                        var typeName = worksheet.Cells[row, 2].Text?.Trim();

                        if (string.IsNullOrWhiteSpace(typeCode))
                        {
                            errors.Add($"Row {row}: Type Code cannot be empty");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(typeName))
                        {
                            errors.Add($"Row {row}: Type Name cannot be empty");
                            continue;
                        }

                        var result = await _schoolTypeService.AddTypeAsync(typeCode, typeName);
                        if (result.success)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"Row {row}: {result.message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {row}: Error processing - {ex.Message}");
                    }
                }

                var message = $"Import completed. {successCount} school types imported successfully.";
                if (errors.Count > 0)
                {
                    message += $" {errors.Count} errors occurred.";
                }

                return (successCount > 0, message, errors);
            }
            catch (Exception ex)
            {
                return (false, $"Error reading Excel file: {ex.Message}", errors);
            }
        }

        public async Task<(bool success, string message, List<string> errors)> ImportBranchesFromExcel(string filePath)
        {
            var errors = new List<string>();
            var successCount = 0;

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0];
                
                // Expected columns: BranchCode, BranchName
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                
                if (rowCount < 2)
                {
                    return (false, "Excel file must contain at least one data row (plus header)", errors);
                }

                // Validate headers
                var expectedHeaders = new[] { "branchcode", "branchname" };
                for (int col = 1; col <= expectedHeaders.Length; col++)
                {
                    var header = worksheet.Cells[1, col].Text?.Trim().ToLower();
                    if (header != expectedHeaders[col - 1])
                    {
                        return (false, $"Column {col} must have '{expectedHeaders[col - 1]}' as header", errors);
                    }
                }

                // Process each row starting from row 2
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var branchCodeText = worksheet.Cells[row, 1].Text?.Trim();
                        var branchName = worksheet.Cells[row, 2].Text?.Trim();

                        if (string.IsNullOrWhiteSpace(branchCodeText))
                        {
                            errors.Add($"Row {row}: Branch Code cannot be empty");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(branchName))
                        {
                            errors.Add($"Row {row}: Branch Name cannot be empty");
                            continue;
                        }

                        if (!int.TryParse(branchCodeText, out int branchCode))
                        {
                            errors.Add($"Row {row}: Branch Code must be a valid number");
                            continue;
                        }

                        var result = await _branchService.AddBranchAsync(branchCode, branchName);
                        if (result.success)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"Row {row}: {result.message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {row}: Error processing - {ex.Message}");
                    }
                }

                var message = $"Import completed. {successCount} branches imported successfully.";
                if (errors.Count > 0)
                {
                    message += $" {errors.Count} errors occurred.";
                }

                return (successCount > 0, message, errors);
            }
            catch (Exception ex)
            {
                return (false, $"Error reading Excel file: {ex.Message}", errors);
            }
        }

        public async Task<(bool success, string message, List<string> errors)> ImportSchoolsFromExcel(string filePath)
        {
            var errors = new List<string>();
            var successCount = 0;

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0];
                
                // Multiple formats supported:
                // New format: SchoolCode, SchoolName, SchoolTypeCode, SchoolType, BranchCode, Branch, BankAccount
                // Old format: SchoolCode, SchoolName, SchoolType, BranchCode, Branch, BankAccount
                // Legacy: SchoolCode, SchoolName, SchoolType, Branch, BankAccount
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                
                if (rowCount < 2)
                {
                    return (false, "Excel file must contain at least one data row (plus header)", errors);
                }

                // Detect format by checking headers
                var headerRow1 = worksheet.Cells[1, 1].Text?.Trim().ToLower() ?? "";
                var headerRow2 = worksheet.Cells[1, 2].Text?.Trim().ToLower() ?? "";
                var headerRow3 = worksheet.Cells[1, 3].Text?.Trim().ToLower() ?? "";
                var headerRow4 = worksheet.Cells[1, 4].Text?.Trim().ToLower() ?? "";
                var headerRow5 = worksheet.Cells[1, 5].Text?.Trim().ToLower() ?? "";
                var headerRow6 = worksheet.Cells[1, 6].Text?.Trim().ToLower() ?? "";
                var headerRow7 = worksheet.Cells[1, 7].Text?.Trim().ToLower() ?? "";

                bool hasSchoolTypeCode = headerRow3 == "schooltypecode";
                bool hasBranchCode = hasSchoolTypeCode ? headerRow5 == "branchcode" : headerRow4 == "branchcode";
                
                int expectedColumns;
                string[] expectedHeaders;

                if (hasSchoolTypeCode)
                {
                    expectedColumns = 7;
                    expectedHeaders = new[] { "schoolcode", "schoolname", "schooltypecode", "schooltype", "branchcode", "branch", "bankaccount" };
                }
                else if (hasBranchCode)
                {
                    expectedColumns = 6;
                    expectedHeaders = new[] { "schoolcode", "schoolname", "schooltype", "branchcode", "branch", "bankaccount" };
                }
                else
                {
                    expectedColumns = 5;
                    expectedHeaders = new[] { "schoolcode", "schoolname", "schooltype", "branch", "bankaccount" };
                }

                // Validate headers
                for (int col = 1; col <= expectedColumns; col++)
                {
                    var header = worksheet.Cells[1, col].Text?.Trim().ToLower();
                    if (header != expectedHeaders[col - 1])
                    {
                        return (false, $"Column {col} must have '{expectedHeaders[col - 1]}' as header", errors);
                    }
                }

                // Get reference data for lookups
                var schoolTypes = await _schoolTypeService.GetAllTypesAsync();
                var branches = await _branchService.GetAllBranchesAsync();

                // Process each row starting from row 2
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        string schoolCode, schoolName, schoolTypeIdentifier, branchIdentifier, bankAccount;
                        
                        if (hasSchoolTypeCode)
                        {
                            schoolCode = worksheet.Cells[row, 1].Text?.Trim();
                            schoolName = worksheet.Cells[row, 2].Text?.Trim();
                            var schoolTypeCodeStr = worksheet.Cells[row, 3].Text?.Trim();
                            var schoolTypeStr = worksheet.Cells[row, 4].Text?.Trim();
                            schoolTypeIdentifier = !string.IsNullOrWhiteSpace(schoolTypeCodeStr) ? schoolTypeCodeStr : schoolTypeStr;
                            
                            var branchCodeStr = worksheet.Cells[row, 5].Text?.Trim();
                            var branchStr = worksheet.Cells[row, 6].Text?.Trim();
                            branchIdentifier = !string.IsNullOrWhiteSpace(branchCodeStr) ? branchCodeStr : branchStr;
                            
                            bankAccount = worksheet.Cells[row, 7].Text?.Trim();
                        }
                        else if (hasBranchCode)
                        {
                            schoolCode = worksheet.Cells[row, 1].Text?.Trim();
                            schoolName = worksheet.Cells[row, 2].Text?.Trim();
                            schoolTypeIdentifier = worksheet.Cells[row, 3].Text?.Trim();
                            
                            var branchCodeStr = worksheet.Cells[row, 4].Text?.Trim();
                            var branchStr = worksheet.Cells[row, 5].Text?.Trim();
                            branchIdentifier = !string.IsNullOrWhiteSpace(branchCodeStr) ? branchCodeStr : branchStr;
                            
                            bankAccount = worksheet.Cells[row, 6].Text?.Trim();
                        }
                        else
                        {
                            schoolCode = worksheet.Cells[row, 1].Text?.Trim();
                            schoolName = worksheet.Cells[row, 2].Text?.Trim();
                            schoolTypeIdentifier = worksheet.Cells[row, 3].Text?.Trim();
                            branchIdentifier = worksheet.Cells[row, 4].Text?.Trim();
                            bankAccount = worksheet.Cells[row, 5].Text?.Trim();
                        }

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(schoolCode))
                        {
                            errors.Add($"Row {row}: School Code cannot be empty");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(schoolName))
                        {
                            errors.Add($"Row {row}: School Name cannot be empty");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(schoolTypeIdentifier))
                        {
                            errors.Add($"Row {row}: School Type cannot be empty");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(branchIdentifier))
                        {
                            errors.Add($"Row {row}: Branch cannot be empty");
                            continue;
                        }

                        // Find school type - first try by code, then by name
                        SchoolType schoolTypeObj = null;
                        schoolTypeObj = schoolTypes.Find(st => st.TypeCode?.Equals(schoolTypeIdentifier, StringComparison.OrdinalIgnoreCase) ?? false);
                        
                        if (schoolTypeObj == null)
                        {
                            schoolTypeObj = schoolTypes.Find(st => st.TypeName.Equals(schoolTypeIdentifier, StringComparison.OrdinalIgnoreCase));
                        }

                        if (schoolTypeObj == null)
                        {
                            errors.Add($"Row {row}: School Type '{schoolTypeIdentifier}' not found. Please add it first.");
                            continue;
                        }

                        // Find branch - first try by code (if numeric), then by name
                        Branch branchObj = null;
                        if (int.TryParse(branchIdentifier, out int branchCode))
                        {
                            branchObj = branches.Find(b => b.BranchCode == branchCode);
                        }

                        if (branchObj == null)
                        {
                            // Try by name
                            branchObj = branches.Find(b => b.BranchName.Equals(branchIdentifier, StringComparison.OrdinalIgnoreCase));
                        }

                        if (branchObj == null)
                        {
                            errors.Add($"Row {row}: Branch '{branchIdentifier}' not found. Please add it first.");
                            continue;
                        }

                        var result = await _schoolService.AddSchoolAsync(schoolCode, schoolName, schoolTypeObj.SchoolTypeId, branchObj.BranchId, bankAccount ?? "");
                        if (result.success)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"Row {row}: {result.message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {row}: Error processing - {ex.Message}");
                    }
                }

                var message = $"Import completed. {successCount} schools imported successfully.";
                if (errors.Count > 0)
                {
                    message += $" {errors.Count} errors occurred.";
                }

                return (successCount > 0, message, errors);
            }
            catch (Exception ex)
            {
                return (false, $"Error reading Excel file: {ex.Message}", errors);
            }
        }

        public void GenerateSchoolTypeTemplate(string filePath)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("SchoolTypes");
            
            // Headers
            worksheet.Cells[1, 1].Value = "TypeCode";
            worksheet.Cells[1, 2].Value = "TypeName";
            
            // Sample data
            worksheet.Cells[2, 1].Value = "PS";
            worksheet.Cells[2, 2].Value = "Primary School";
            worksheet.Cells[3, 1].Value = "HS";
            worksheet.Cells[3, 2].Value = "High School";
            worksheet.Cells[4, 1].Value = "JC";
            worksheet.Cells[4, 2].Value = "Junior College";
            
            // Format headers
            worksheet.Cells[1, 1, 1, 2].Style.Font.Bold = true;
            worksheet.Column(1).AutoFit();
            worksheet.Column(2).AutoFit();
            
            package.SaveAs(new FileInfo(filePath));
        }

        public void GenerateBranchTemplate(string filePath)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Branches");
            
            // Headers
            worksheet.Cells[1, 1].Value = "BranchCode";
            worksheet.Cells[1, 2].Value = "BranchName";
            
            // Sample data
            worksheet.Cells[2, 1].Value = 101;
            worksheet.Cells[2, 2].Value = "Main Branch";
            worksheet.Cells[3, 1].Value = 102;
            worksheet.Cells[3, 2].Value = "East Branch";
            
            // Format headers
            worksheet.Cells[1, 1, 1, 2].Style.Font.Bold = true;
            worksheet.Column(1).AutoFit();
            worksheet.Column(2).AutoFit();
            
            package.SaveAs(new FileInfo(filePath));
        }

        public void GenerateSchoolTemplate(string filePath)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Schools");
            
            // Headers
            worksheet.Cells[1, 1].Value = "SchoolCode";
            worksheet.Cells[1, 2].Value = "SchoolName";
            worksheet.Cells[1, 3].Value = "SchoolTypeCode";
            worksheet.Cells[1, 4].Value = "SchoolType";
            worksheet.Cells[1, 5].Value = "BranchCode";
            worksheet.Cells[1, 6].Value = "Branch";
            worksheet.Cells[1, 7].Value = "BankAccount";
            
            // Sample data
            worksheet.Cells[2, 1].Value = "SCH001";
            worksheet.Cells[2, 2].Value = "ABC Primary School";
            worksheet.Cells[2, 3].Value = "PS";
            worksheet.Cells[2, 4].Value = "Primary School";
            worksheet.Cells[2, 5].Value = 101;
            worksheet.Cells[2, 6].Value = "Main Branch";
            worksheet.Cells[2, 7].Value = "1234567890";
            
            worksheet.Cells[3, 1].Value = "SCH002";
            worksheet.Cells[3, 2].Value = "XYZ High School";
            worksheet.Cells[3, 3].Value = "HS";
            worksheet.Cells[3, 4].Value = "High School";
            worksheet.Cells[3, 5].Value = 102;
            worksheet.Cells[3, 6].Value = "East Branch";
            worksheet.Cells[3, 7].Value = "0987654321";
            
            worksheet.Cells[4, 1].Value = "SCH003";
            worksheet.Cells[4, 2].Value = "PQR Junior College";
            worksheet.Cells[4, 3].Value = "JC";
            worksheet.Cells[4, 4].Value = "Junior College";
            worksheet.Cells[4, 5].Value = 101;
            worksheet.Cells[4, 6].Value = "Main Branch";
            worksheet.Cells[4, 7].Value = "1122334455";
            
            // Format headers
            worksheet.Cells[1, 1, 1, 7].Style.Font.Bold = true;
            worksheet.Column(1).AutoFit();
            worksheet.Column(2).AutoFit();
            worksheet.Column(3).AutoFit();
            worksheet.Column(4).AutoFit();
            worksheet.Column(5).AutoFit();
            worksheet.Column(6).AutoFit();
            worksheet.Column(7).AutoFit();
            
            package.SaveAs(new FileInfo(filePath));
        }

        /// <summary>
        /// Import salary entries from Excel file as-is
        /// Columns expected: InDate, SchoolCode, SchoolName, BankAccount, SchoolTypeCode, SchoolType, 
        /// BranchCode, Branch, Amount, Amount1, Amount2, AdviceNumber, OperatorName, StampDate, StampTime, UserID
        /// Imported entries are marked with IsImported=true and linked to the UserID provided in the Excel
        /// </summary>
        public async Task<(bool success, string message, List<string> errors)> ImportSalaryEntriesFromExcel(string filePath)
        {
            var errors = new List<string>();
            int successCount = 0;

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets[0];
                
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                
                if (rowCount < 2)
                {
                    return (false, "Excel file must contain at least one data row (plus header)", errors);
                }

                // Expected columns in the Excel file:
                // A: InDate, B: SchoolCode, C: SchoolName, D: BankAccount, E: SchoolTypeCode, 
                // F: SchoolType, G: BranchCode, H: Branch, I: Amount, J: Amount1, K: Amount2, L: Amount3,
                // M: AdviceNumber, N: OperatorName, O: StampDate, P: StampTime, Q: UserID

                // Process each row starting from row 2 (skip header)
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        // Read all columns as-is from Excel
                        var inDateStr = worksheet.Cells[row, 1].Text?.Trim();
                        var schoolCode = worksheet.Cells[row, 2].Text?.Trim();
                        var schoolName = worksheet.Cells[row, 3].Text?.Trim();
                        var bankAccount = worksheet.Cells[row, 4].Text?.Trim();
                        var schoolTypeIdentifier = worksheet.Cells[row, 5].Text?.Trim() ?? worksheet.Cells[row, 6].Text?.Trim();
                        var branchIdentifier = worksheet.Cells[row, 7].Text?.Trim() ?? worksheet.Cells[row, 8].Text?.Trim();
                        var amountStr = worksheet.Cells[row, 9].Text?.Trim();
                        var amount1Str = worksheet.Cells[row, 10].Text?.Trim();
                        var amount2Str = worksheet.Cells[row, 11].Text?.Trim();
                        var amount3Str = worksheet.Cells[row, 12].Text?.Trim();
                        var adviceNumber = worksheet.Cells[row, 13].Text?.Trim();
                        var operatorName = worksheet.Cells[row, 14].Text?.Trim();
                        var stampDateStr = worksheet.Cells[row, 15].Text?.Trim();
                        var stampTimeStr = worksheet.Cells[row, 16].Text?.Trim();
                        var userIdStr = worksheet.Cells[row, 17].Text?.Trim();

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(inDateStr))
                        {
                            errors.Add($"Row {row}: InDate cannot be empty");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(schoolCode))
                        {
                            errors.Add($"Row {row}: SchoolCode cannot be empty");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                        {
                            errors.Add($"Row {row}: UserID must be a valid number");
                            continue;
                        }

                        // Parse amounts
                        if (!decimal.TryParse(amountStr, out decimal amount))
                        {
                            errors.Add($"Row {row}: Amount must be a valid decimal");
                            continue;
                        }

                        if (!decimal.TryParse(amount1Str, out decimal amount1))
                        {
                            errors.Add($"Row {row}: Amount1 must be a valid decimal");
                            continue;
                        }

                        if (!decimal.TryParse(amount2Str, out decimal amount2))
                        {
                            errors.Add($"Row {row}: Amount2 must be a valid decimal");
                            continue;
                        }

                        if (!decimal.TryParse(amount3Str, out decimal amount3))
                        {
                            errors.Add($"Row {row}: Amount3 must be a valid decimal");
                            continue;
                        }

                        // Parse dates
                        if (!DateTime.TryParse(inDateStr, out DateTime entryDate))
                        {
                            errors.Add($"Row {row}: InDate '{inDateStr}' is not a valid date");
                            continue;
                        }

                        // Parse time
                        TimeSpan? entryTime = null;
                        if (!string.IsNullOrWhiteSpace(stampTimeStr) && TimeSpan.TryParse(stampTimeStr, out TimeSpan time))
                        {
                            entryTime = time;
                        }

                        // Find school and branch
                        var schoolTypes = await _schoolTypeService.GetAllTypesAsync();
                        var branches = await _branchService.GetAllBranchesAsync();
                        var school = (await _schoolService.GetAllSchoolsAsync()).FirstOrDefault(s => s.SchoolCode == schoolCode);

                        if (school == null)
                        {
                            errors.Add($"Row {row}: School with code '{schoolCode}' not found");
                            continue;
                        }

                        // Create salary entry - import as-is with the UserID from Excel
                        var entry = new SalaryEntry
                        {
                            EntryDate = entryDate,
                            EntryTime = entryTime,
                            SchoolId = school.SchoolId,
                            BranchId = school.BranchId,
                            AccountNumber = bankAccount,
                            Amount1 = amount1,
                            Amount2 = amount2,
                            Amount3 = amount3,  // Store Amount3 correctly from Excel
                            TotalAmount = amount1 + amount2 + amount3,
                            AdviceNumber = adviceNumber,
                            OperatorName = operatorName,
                            CreatedByUserId = userId,  // Use the UserID from Excel
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            IsImported = true  // Mark as imported
                        };

                        await _salaryRepository.AddAsync(entry);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {row}: Error processing - {ex.Message}");
                    }
                }

                var message = $"Import completed. {successCount} salary entries imported successfully.";
                if (errors.Count > 0)
                {
                    message += $" {errors.Count} errors occurred.";
                }

                return (successCount > 0, message, errors);
            }
            catch (Exception ex)
            {
                return (false, $"Error reading Excel file: {ex.Message}", errors);
            }
        }

        /// <summary>
        /// Generate salary import template Excel file
        /// Columns: A-C (not used), D: BankAccount, E: SchoolTypeCode, F: SchoolType, G: BranchCode, H: Branch
        /// I: AMOUNT, J: AMOUNT1, K: AMOUNT2, L: AdviceNumber, M: OperatorName, N: STAMPDATE, O: STAMPTIME, P: UserID
        /// </summary>
        public void GenerateSalaryImportTemplate(string filePath)
        {
            var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("SalaryImport");

            // Set headers in row 1
            // Columns A-C are empty (padding for alignment)
            worksheet.Cells[1, 4].Value = "BankAccount";
            worksheet.Cells[1, 5].Value = "SchoolTypeCode";
            worksheet.Cells[1, 6].Value = "SchoolType";
            worksheet.Cells[1, 7].Value = "BranchCode";
            worksheet.Cells[1, 8].Value = "Branch";
            worksheet.Cells[1, 9].Value = "AMOUNT";
            worksheet.Cells[1, 10].Value = "AMOUNT1";
            worksheet.Cells[1, 11].Value = "AMOUNT2";
            worksheet.Cells[1, 12].Value = "AdviceNumber";
            worksheet.Cells[1, 13].Value = "OperatorName";
            worksheet.Cells[1, 14].Value = "STAMPDATE";
            worksheet.Cells[1, 15].Value = "STAMPTIME";
            worksheet.Cells[1, 16].Value = "UserID";

            // Sample data row 1
            worksheet.Cells[2, 4].Value = "1234567890";
            worksheet.Cells[2, 5].Value = "PS";
            worksheet.Cells[2, 6].Value = "Primary School";
            worksheet.Cells[2, 7].Value = 101;
            worksheet.Cells[2, 8].Value = "Main Branch";
            worksheet.Cells[2, 9].Value = 25000;
            worksheet.Cells[2, 10].Value = 10000;
            worksheet.Cells[2, 11].Value = 8000;
            worksheet.Cells[2, 12].Value = "250101001";
            worksheet.Cells[2, 13].Value = "Operator1";
            worksheet.Cells[2, 14].Value = DateTime.Now.ToString("yyyy-MM-dd");
            worksheet.Cells[2, 15].Value = "09:30:00";
            worksheet.Cells[2, 16].Value = 1;

            // Sample data row 2
            worksheet.Cells[3, 4].Value = "0987654321";
            worksheet.Cells[3, 5].Value = "HS";
            worksheet.Cells[3, 6].Value = "High School";
            worksheet.Cells[3, 7].Value = 102;
            worksheet.Cells[3, 8].Value = "East Branch";
            worksheet.Cells[3, 9].Value = 35000;
            worksheet.Cells[3, 10].Value = 15000;
            worksheet.Cells[3, 11].Value = 12000;
            worksheet.Cells[3, 12].Value = "250101002";
            worksheet.Cells[3, 13].Value = "Operator2";
            worksheet.Cells[3, 14].Value = DateTime.Now.ToString("yyyy-MM-dd");
            worksheet.Cells[3, 15].Value = "10:15:00";
            worksheet.Cells[3, 16].Value = 2;

            // Format headers
            worksheet.Cells[1, 4, 1, 16].Style.Font.Bold = true;
            worksheet.Cells[1, 4, 1, 16].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[1, 4, 1, 16].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            // Auto-fit columns
            for (int col = 4; col <= 16; col++)
            {
                worksheet.Column(col).AutoFit();
            }

            package.SaveAs(new FileInfo(filePath));
        }
    }
}