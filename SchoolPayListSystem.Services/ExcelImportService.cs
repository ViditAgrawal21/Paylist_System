using System;
using System.Collections.Generic;
using System.IO;
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

        public ExcelImportService(
            SchoolTypeService schoolTypeService, 
            SchoolService schoolService, 
            BranchService branchService)
        {
            _schoolTypeService = schoolTypeService;
            _schoolService = schoolService;
            _branchService = branchService;
            
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
                
                // Expected columns: SchoolCode, SchoolName, SchoolType, Branch, BankAccount
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                
                if (rowCount < 2)
                {
                    return (false, "Excel file must contain at least one data row (plus header)", errors);
                }

                // Validate headers
                var expectedHeaders = new[] { "schoolcode", "schoolname", "schooltype", "branch", "bankaccount" };
                for (int col = 1; col <= expectedHeaders.Length; col++)
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
                        var schoolCode = worksheet.Cells[row, 1].Text?.Trim();
                        var schoolName = worksheet.Cells[row, 2].Text?.Trim();
                        var schoolType = worksheet.Cells[row, 3].Text?.Trim();
                        var branch = worksheet.Cells[row, 4].Text?.Trim();
                        var bankAccount = worksheet.Cells[row, 5].Text?.Trim();

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

                        if (string.IsNullOrWhiteSpace(schoolType))
                        {
                            errors.Add($"Row {row}: School Type cannot be empty");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(branch))
                        {
                            errors.Add($"Row {row}: Branch cannot be empty");
                            continue;
                        }

                        // Find school type ID by name
                        var schoolTypeObj = schoolTypes.Find(st => st.TypeName.Equals(schoolType, StringComparison.OrdinalIgnoreCase));
                        if (schoolTypeObj == null)
                        {
                            errors.Add($"Row {row}: School Type '{schoolType}' not found. Please add it first.");
                            continue;
                        }

                        // Find branch ID by name
                        var branchObj = branches.Find(b => b.BranchName.Equals(branch, StringComparison.OrdinalIgnoreCase));
                        if (branchObj == null)
                        {
                            errors.Add($"Row {row}: Branch '{branch}' not found. Please add it first.");
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
            worksheet.Cells[1, 3].Value = "SchoolType";
            worksheet.Cells[1, 4].Value = "Branch";
            worksheet.Cells[1, 5].Value = "BankAccount";
            
            // Sample data
            worksheet.Cells[2, 1].Value = "SCH001";
            worksheet.Cells[2, 2].Value = "ABC Primary School";
            worksheet.Cells[2, 3].Value = "Primary School";
            worksheet.Cells[2, 4].Value = "Main Branch";
            worksheet.Cells[2, 5].Value = "1234567890";
            
            worksheet.Cells[3, 1].Value = "SCH002";
            worksheet.Cells[3, 2].Value = "XYZ High School";
            worksheet.Cells[3, 3].Value = "High School";
            worksheet.Cells[3, 4].Value = "East Branch";
            worksheet.Cells[3, 5].Value = "0987654321";
            
            // Format headers
            worksheet.Cells[1, 1, 1, 5].Style.Font.Bold = true;
            worksheet.Column(1).AutoFit();
            worksheet.Column(2).AutoFit();
            worksheet.Column(3).AutoFit();
            worksheet.Column(4).AutoFit();
            worksheet.Column(5).AutoFit();
            
            package.SaveAs(new FileInfo(filePath));
        }
    }
}