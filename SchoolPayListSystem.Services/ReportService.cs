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

                // Build report DTOs
                foreach (var branchId in groupedByBranch.Keys)
                {
                    var branch = _context.Branches.FirstOrDefault(b => b.BranchId == branchId);
                    if (branch == null) continue;

                    var branchEntries = groupedByBranch[branchId];
                    var branchReport = new BranchReportDTO
                    {
                        BranchName = branch.BranchName,
                        BranchCode = branch.BranchCode,
                        TotalAmount = branchEntries.Sum(e => e.TotalAmount),
                        Entries = branchEntries.Select(e => new SalaryEntryReportDTO
                        {
                            EntryDate = e.EntryDate,
                            SchoolName = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId)?.SchoolName,
                            AccountNumber = e.AccountNumber,
                            Amount1 = e.Amount1,
                            Amount2 = e.Amount2,
                            Amount3 = e.Amount3,
                            TotalAmount = e.TotalAmount,
                            AdviceNumber = e.AdviceNumber
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
        /// Generate Branch-Specific Report (Details for a particular branch)
        /// Filtered by currently logged-in user
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

                var branchReport = new BranchDetailReportDTO
                {
                    BranchName = branch.BranchName,
                    BranchCode = branch.BranchCode,
                    EntryDate = reportDate,
                    TotalAmount = entries.Sum(e => e.TotalAmount),
                    Entries = entries.Select((e, index) => new BranchDetailEntryDTO
                    {
                        SerialNumber = index + 1,
                        SchoolCode = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId)?.SchoolCode,
                        AccountNumber = e.AccountNumber,
                        SchoolName = _context.Schools.FirstOrDefault(s => s.SchoolId == e.SchoolId)?.SchoolName,
                        Amount = e.TotalAmount,
                        AdviceNumber = e.AdviceNumber
                    }).ToList()
                };

                return await Task.FromResult(branchReport);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating branch-specific report: {ex.Message}", ex);
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

        public async Task<string> GenerateReportAsync()
        {
            return await Task.FromResult("Report generated");
        }
    }
}
