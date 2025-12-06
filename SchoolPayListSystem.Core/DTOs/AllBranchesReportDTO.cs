using System;
using System.Collections.Generic;

namespace SchoolPayListSystem.Core.DTOs
{
    /// <summary>
    /// DTO for All Branches Report - Groups by School Type, then by Branches within each type
    /// </summary>
    public class AllBranchesReportDTO
    {
        public string SchoolTypeName { get; set; }
        public string SchoolTypeCode { get; set; }
        public DateTime ReportDate { get; set; }
        
        // Branches within this school type
        public List<BranchSectionDTO> BranchSections { get; set; } = new List<BranchSectionDTO>();
        
        // Totals for this school type
        public decimal SchoolTypeTotal { get; set; }
        public int TotalEntries { get; set; }
    }

    /// <summary>
    /// Represents a single branch section within a school type report
    /// </summary>
    public class BranchSectionDTO
    {
        public int BranchId { get; set; }
        public int BranchCode { get; set; }
        public string BranchName { get; set; }
        public string AdviceNumber { get; set; }
        public decimal BranchTotal { get; set; }
        public List<SalaryEntryReportDTO> Entries { get; set; } = new List<SalaryEntryReportDTO>();
    }
}
