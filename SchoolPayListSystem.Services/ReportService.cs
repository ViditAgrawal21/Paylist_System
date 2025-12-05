using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SchoolPayListSystem.Core.DTOs;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;
using SchoolPayListSystem.Data.Repositories;

namespace SchoolPayListSystem.Services
{
    public class ReportService
    {
        private readonly SchoolPayListDbContext _context;
        private readonly SalaryEntryRepository _salaryEntryRepository;

        public ReportService(SchoolPayListDbContext context, SalaryEntryRepository salaryEntryRepository)
        {
            _context = context;
            _salaryEntryRepository = salaryEntryRepository;
        }

        /// <summary>
        /// Generate School Type Summary Report (All branches for a school type)
        /// Filtered by currently logged-in user
        /// Displays advice numbers that were already assigned in branch reports
        /// Advice numbers are unique per BRANCH, not per school type
        /// </summary>
        public async Task<List<BranchReportDTO>> GenerateSchoolTypeSummaryReportAsync(int schoolTypeId, int createdByUserId, DateTime reportDate)
        {
            try
            {
                var report = new List<BranchReportDTO>();

                // Get all schools of this type
                var schoolsOfType = _context.Schools
                    .Where(s => s.SchoolTypeId == schoolTypeId)
                    .ToList();

                // Group entries by branch
                var groupedByBranch = new Dictionary<int, List<SalaryEntry>>();

                foreach (var school in schoolsOfType)
                {
                    var entries = _context.SalaryEntries
                        .Where(se => se.SchoolId == school.SchoolId 
                            && se.CreatedByUserId == createdByUserId
                            && se.EntryDate.Date == reportDate.Date)
                        .ToList();

                    if (entries.Any())
                    {
                        if (!groupedByBranch.ContainsKey(school.BranchId))
                            groupedByBranch[school.BranchId] = new List<SalaryEntry>();

                        groupedByBranch[school.BranchId].AddRange(entries);
                    }
                }

                // Build report DTOs - use advice numbers from database if they exist, otherwise use first entry's advice number
                // Sort by Branch Code in ascending order
                var sortedBranchIds = groupedByBranch.Keys
                    .Select(branchId => new { BranchId = branchId, Branch = _context.Branches.FirstOrDefault(b => b.BranchId == branchId) })
                    .Where(x => x.Branch != null)
                    .OrderBy(x => x.Branch.BranchCode)
                    .Select(x => x.BranchId)
                    .ToList();

                foreach (var branchId in sortedBranchIds)
                {
                    var branch = _context.Branches.FirstOrDefault(b => b.BranchId == branchId);
                    if (branch == null) continue;

                    var branchEntries = groupedByBranch[branchId];
                    
                    // Get the advice number from the first entry (should be consistent within branch for a day)
                    // or get the minimum advice number for this branch (which represents the advice number for this branch report)
                    var firstEntryAdviceNumber = branchEntries.FirstOrDefault()?.AdviceNumber ?? "";
                    
                    var branchReport = new BranchReportDTO
                    {
                        BranchName = branch.BranchName,
                        BranchCode = branch.BranchCode,  // Use BranchCode (actual code), not BranchId
                        TotalAmount = branchEntries.Sum(e => e.TotalAmount),
                        AdviceNumber = firstEntryAdviceNumber,  // Use the advice number from database
                        Entries = branchEntries.Select(e => 
                        {
                            var school = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId);
                            return new SalaryEntryReportDTO
                            {
                                INDATE = e.EntryDate,
                                SchoolCode = school?.SchoolCode ?? "",
                                SchoolName = school?.SchoolName ?? "",
                                BankAccount = school?.BankAccountNumber ?? "",  // Fetch from School, not SalaryEntry
                                SchoolTypeCode = school?.SchoolType?.TypeCode ?? "",
                                SchoolType = school?.SchoolType?.TypeName ?? "",
                                BranchCode = branch.BranchCode,  // Use BranchCode (actual code), not BranchId
                                BranchName = branch.BranchName,
                                AMOUNT = e.TotalAmount,
                                AMOUNT1 = e.Amount1,
                                AMOUNT2 = e.Amount2,
                                OperatorName = _context.Users.FirstOrDefault(u => u.UserId == e.CreatedByUserId)?.FullName ?? "",
                                OperatorId = _context.Users.FirstOrDefault(u => u.UserId == e.CreatedByUserId)?.Username ?? "",
                                STAMPDATE = e.CreatedAt,
                                STAMPTIME = e.CreatedAt.TimeOfDay
                            };
                        }).ToList()
                    };
                    
                    report.Add(branchReport);
                }

                return await Task.FromResult(report);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating school type summary report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// <summary>
        /// Generate Branch-Specific Report (Details for a particular branch)
        /// Filtered by currently logged-in user
        /// Advice numbers are unique per BRANCH per day (all school types in branch share same sequence)
        /// </summary>
        public async Task<BranchDetailReportDTO> GenerateBranchSpecificReportAsync(int branchId, int createdByUserId, DateTime reportDate)
        {
            try
            {
                var branch = _context.Branches.FirstOrDefault(b => b.BranchId == branchId);
                if (branch == null)
                    throw new Exception("Branch not found");

                var entries = _context.SalaryEntries
                    .Where(se => se.BranchId == branchId 
                        && se.CreatedByUserId == createdByUserId
                        && se.EntryDate.Date == reportDate.Date)
                    .ToList();

                string datePrefix = reportDate.ToString("yyMMdd");
                var todayStart = reportDate.Date;
                var todayEnd = todayStart.AddDays(1);

                // Get all entries from this branch for all school types today
                // and find the max serial already used
                var allBranchEntriesForDate = _context.SalaryEntries
                    .Where(se => se.BranchId == branchId
                        && se.CreatedByUserId == createdByUserId
                        && se.EntryDate >= todayStart
                        && se.EntryDate < todayEnd
                        && se.AdviceNumber != null
                        && se.AdviceNumber != "")
                    .Select(se => se.AdviceNumber)
                    .Distinct()
                    .ToList();

                int maxSerial = 0;
                foreach (var adviceNo in allBranchEntriesForDate)
                {
                    if (adviceNo.Length >= 8 && adviceNo.StartsWith(datePrefix))
                    {
                        string serialPart = adviceNo.Substring(6);
                        if (int.TryParse(serialPart, out int serial))
                        {
                            if (serial > maxSerial)
                                maxSerial = serial;
                        }
                    }
                }

                int nextSerial = maxSerial + 1;
                bool hasNewAdviceNumbers = false;
                var adviceNumberService = new AdviceNumberService(_context);

                var branchReport = new BranchDetailReportDTO
                {
                    BranchName = branch.BranchName,
                    BranchCode = branch.BranchCode,
                    EntryDate = reportDate,
                    TotalAmount = entries.Sum(e => e.TotalAmount),
                    Entries = entries.Select((e, index) => 
                    {
                        // Use stored advice number, only generate if missing
                        string adviceNumber = e.AdviceNumber;
                        if (string.IsNullOrEmpty(adviceNumber))
                        {
                            adviceNumber = adviceNumberService.GenerateAdviceNumber(reportDate, branchId, 0);
                            e.AdviceNumber = adviceNumber;
                            hasNewAdviceNumbers = true;
                        }

                        var school = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId);
                        return new BranchDetailEntryDTO
                        {
                            SerialNumber = index + 1,
                            SchoolCode = school?.SchoolCode,
                            AccountNumber = school?.BankAccountNumber,  // Fetch from School, not SalaryEntry
                            SchoolName = school?.SchoolName,
                            Amount = e.TotalAmount,
                            AdviceNumber = adviceNumber
                        };
                    }).ToList()
                };

                // Save ONLY if new advice numbers were generated
                if (hasNewAdviceNumbers)
                {
                    var salaryEntryRepo = new SalaryEntryRepository(_context);
                    salaryEntryRepo.SaveChangesAsync().Wait();
                }

                return await Task.FromResult(branchReport);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating branch-specific report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate Branch-Wise Report with School Type Filter
        /// Filtered by branch + school type combination for a specific date
        /// Advice numbers are unique per BRANCH per day (all school types in branch share same sequence)
        /// Each entry gets a UNIQUE advice number per branch per day
        /// Advice numbers are persisted to SalaryEntry records
        /// </summary>
        public async Task<BranchDetailReportDTO> GenerateBranchWithSchoolTypeReportAsync(
            int branchId, 
            int schoolTypeId, 
            int createdByUserId, 
            DateTime reportDate)
        {
            try
            {
                var branch = _context.Branches.FirstOrDefault(b => b.BranchId == branchId);
                if (branch == null)
                    throw new Exception("Branch not found");

                var schoolType = _context.SchoolTypes.FirstOrDefault(st => st.SchoolTypeId == schoolTypeId);
                if (schoolType == null)
                    throw new Exception("School type not found");

                // Get all schools of this type in this branch
                var schoolsOfTypeInBranch = _context.Schools
                    .Where(s => s.BranchId == branchId && s.SchoolTypeId == schoolTypeId)
                    .Select(s => s.SchoolId)
                    .ToList();

                // Get entries for schools of this type in this branch for this date
                var entries = _context.SalaryEntries
                    .Where(se => se.BranchId == branchId 
                        && se.CreatedByUserId == createdByUserId
                        && se.EntryDate.Date == reportDate.Date
                        && schoolsOfTypeInBranch.Contains(se.SchoolId))
                    .ToList();

                // Generate advice numbers for this (Branch + SchoolType) combination
                var adviceNumberService = new AdviceNumberService(_context);
                
                // Build report entries with advice numbers
                var reportEntries = new List<BranchDetailEntryDTO>();
                int serialNumber = 1;
                bool hasNewAdviceNumbers = false;

                foreach (var entry in entries)
                {
                    // ONLY use existing advice number - NEVER regenerate
                    string adviceNumber = entry.AdviceNumber ?? "";
                    
                    // If advice number is missing AND this is a fresh entry (not imported), generate only then
                    if (string.IsNullOrEmpty(adviceNumber))
                    {
                        adviceNumber = adviceNumberService.GenerateAdviceNumber(reportDate, branchId, schoolTypeId);
                        entry.AdviceNumber = adviceNumber;
                        hasNewAdviceNumbers = true;
                    }

                    var school = _context.Schools.FirstOrDefault(s => s.SchoolId == entry.SchoolId);
                    var reportEntry = new BranchDetailEntryDTO
                    {
                        SerialNumber = serialNumber++,
                        SchoolCode = school?.SchoolCode,
                        AccountNumber = school?.BankAccountNumber,  // Fetch from School, not SalaryEntry
                        SchoolName = school?.SchoolName,
                        Amount = entry.TotalAmount,
                        AdviceNumber = adviceNumber
                    };

                    reportEntries.Add(reportEntry);
                }

                // Save ONLY if new advice numbers were generated (not for existing ones)
                if (hasNewAdviceNumbers && entries.Any())
                {
                    await _salaryEntryRepository.SaveChangesAsync();
                }

                var branchReport = new BranchDetailReportDTO
                {
                    BranchName = branch.BranchName,
                    BranchCode = branch.BranchCode,
                    EntryDate = reportDate,
                    TotalAmount = entries.Sum(e => e.TotalAmount),
                    Entries = reportEntries
                };

                return await Task.FromResult(branchReport);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating branch-wise report with school type filter: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate School Type Detail Report (All individual schools of a school type)
        /// Filtered by currently logged-in user
        /// </summary>
        public async Task<List<SchoolTypeDetailReportDTO>> GenerateSchoolTypeDetailReportAsync(int schoolTypeId, int createdByUserId, DateTime reportDate)
        {
            try
            {
                var report = new List<SchoolTypeDetailReportDTO>();

                // Get all schools of this type
                var schoolsOfType = _context.Schools
                    .Where(s => s.SchoolTypeId == schoolTypeId)
                    .ToList();

                // Get all entries for schools of this type
                int serialNumber = 1;
                foreach (var school in schoolsOfType)
                {
                    var entries = _context.SalaryEntries
                        .Where(se => se.SchoolId == school.SchoolId 
                            && se.CreatedByUserId == createdByUserId
                            && se.EntryDate.Date == reportDate.Date)
                        .ToList();

                    foreach (var entry in entries)
                    {
                        var reportEntry = new SchoolTypeDetailReportDTO
                        {
                            SerialNumber = serialNumber++,
                            SchoolCode = school.SchoolCode,
                            SchoolName = school.SchoolName,
                            Amount1 = entry.Amount1,
                            Amount2 = entry.Amount2,
                            Amount3 = entry.Amount3,
                            TotalAmount = entry.TotalAmount
                        };

                        report.Add(reportEntry);
                    }
                }

                return await Task.FromResult(report);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating school type detail report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate School Type Summary Report for a Date Range
        /// Shows all entries within the specified date range
        /// </summary>
        public async Task<List<BranchReportDTO>> GenerateSchoolTypeSummaryReportByDateRangeAsync(int schoolTypeId, int createdByUserId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var report = new List<BranchReportDTO>();

                // Get all schools of this type
                var schoolsOfType = _context.Schools
                    .Where(s => s.SchoolTypeId == schoolTypeId)
                    .ToList();

                // Group entries by branch
                var groupedByBranch = new Dictionary<int, List<SalaryEntry>>();

                foreach (var school in schoolsOfType)
                {
                    var entries = _context.SalaryEntries
                        .Where(se => se.SchoolId == school.SchoolId 
                            && se.CreatedByUserId == createdByUserId
                            && se.EntryDate.Date >= startDate.Date
                            && se.EntryDate.Date <= endDate.Date)
                        .ToList();

                    if (entries.Any())
                    {
                        if (!groupedByBranch.ContainsKey(school.BranchId))
                            groupedByBranch[school.BranchId] = new List<SalaryEntry>();
                        groupedByBranch[school.BranchId].AddRange(entries);
                    }
                }

                // Build report
                foreach (var branchGroup in groupedByBranch)
                {
                    var branch = _context.Branches.FirstOrDefault(b => b.BranchId == branchGroup.Key);
                    if (branch == null) continue;

                    var branchEntries = branchGroup.Value.OrderBy(e => e.EntryDate).ToList();
                    
                    // Get first entry to determine advice number
                    var firstEntry = branchEntries.FirstOrDefault();
                    string adviceNumber = "";
                    if (firstEntry?.AdviceNumber != null)
                        adviceNumber = firstEntry.AdviceNumber;

                    var branchReport = new BranchReportDTO
                    {
                        BranchCode = branch.BranchId,
                        BranchName = branch.BranchName,
                        TotalAmount = branchEntries.Sum(e => e.TotalAmount),
                        AdviceNumber = adviceNumber,
                        Entries = branchEntries.Select(e => new SalaryEntryReportDTO
                        {
                            INDATE = e.EntryDate,
                            SchoolCode = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId)?.SchoolCode ?? "",
                            SchoolName = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId)?.SchoolName ?? "",
                            BankAccount = e.AccountNumber,
                            SchoolTypeCode = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId)?.SchoolType?.TypeCode ?? "",
                            SchoolType = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId)?.SchoolType?.TypeName ?? "",
                            BranchCode = branch.BranchId,
                            BranchName = branch.BranchName,
                            AMOUNT = e.TotalAmount,
                            AMOUNT1 = e.Amount1,
                            AMOUNT2 = e.Amount2,
                            OperatorName = _context.Users.FirstOrDefault(u => u.UserId == e.CreatedByUserId)?.FullName ?? "",
                            OperatorId = _context.Users.FirstOrDefault(u => u.UserId == e.CreatedByUserId)?.Username ?? "",
                            STAMPDATE = e.CreatedAt,
                            STAMPTIME = e.CreatedAt.TimeOfDay
                        }).ToList()
                    };

                    report.Add(branchReport);
                }

                return await Task.FromResult(report);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating date range report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate All-Time Report for a School Type
        /// Shows all entries ever created for this school type
        /// </summary>
        public async Task<List<BranchReportDTO>> GenerateSchoolTypeAllTimeReportAsync(int schoolTypeId, int createdByUserId)
        {
            try
            {
                var report = new List<BranchReportDTO>();

                // Get all schools of this type
                var schoolsOfType = _context.Schools
                    .Where(s => s.SchoolTypeId == schoolTypeId)
                    .ToList();

                // Group entries by branch
                var groupedByBranch = new Dictionary<int, List<SalaryEntry>>();

                foreach (var school in schoolsOfType)
                {
                    var entries = _context.SalaryEntries
                        .Where(se => se.SchoolId == school.SchoolId 
                            && se.CreatedByUserId == createdByUserId)
                        .ToList();

                    if (entries.Any())
                    {
                        if (!groupedByBranch.ContainsKey(school.BranchId))
                            groupedByBranch[school.BranchId] = new List<SalaryEntry>();
                        groupedByBranch[school.BranchId].AddRange(entries);
                    }
                }

                // Build report
                foreach (var branchGroup in groupedByBranch)
                {
                    var branch = _context.Branches.FirstOrDefault(b => b.BranchId == branchGroup.Key);
                    if (branch == null) continue;

                    var branchEntries = branchGroup.Value.OrderByDescending(e => e.EntryDate).ToList();
                    
                    // Get first entry to determine advice number
                    var firstEntry = branchEntries.FirstOrDefault();
                    string adviceNumber = "";
                    if (firstEntry?.AdviceNumber != null)
                        adviceNumber = firstEntry.AdviceNumber;

                    var branchReport = new BranchReportDTO
                    {
                        BranchCode = branch.BranchId,
                        BranchName = branch.BranchName,
                        TotalAmount = branchEntries.Sum(e => e.TotalAmount),
                        AdviceNumber = adviceNumber,
                        Entries = branchEntries.Select(e => new SalaryEntryReportDTO
                        {
                            INDATE = e.EntryDate,
                            SchoolCode = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId)?.SchoolCode ?? "",
                            SchoolName = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId)?.SchoolName ?? "",
                            BankAccount = e.AccountNumber,
                            SchoolTypeCode = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId)?.SchoolType?.TypeCode ?? "",
                            SchoolType = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId)?.SchoolType?.TypeName ?? "",
                            BranchCode = branch.BranchId,
                            BranchName = branch.BranchName,
                            AMOUNT = e.TotalAmount,
                            AMOUNT1 = e.Amount1,
                            AMOUNT2 = e.Amount2,
                            OperatorName = _context.Users.FirstOrDefault(u => u.UserId == e.CreatedByUserId)?.FullName ?? "",
                            OperatorId = _context.Users.FirstOrDefault(u => u.UserId == e.CreatedByUserId)?.Username ?? "",
                            STAMPDATE = e.CreatedAt,
                            STAMPTIME = e.CreatedAt.TimeOfDay
                        }).ToList()
                    };

                    report.Add(branchReport);
                }

                return await Task.FromResult(report);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating all-time report: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerateReportAsync()
        {
            return await Task.FromResult("Report generated");
        }
    }
}
