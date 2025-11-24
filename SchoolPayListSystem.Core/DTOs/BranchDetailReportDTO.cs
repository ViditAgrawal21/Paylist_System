using System;
using System.Collections.Generic;

namespace SchoolPayListSystem.Core.DTOs
{
    /// <summary>
    /// DTO for branch-specific detailed report entry
    /// </summary>
    public class BranchDetailEntryDTO
    {
        public int SerialNumber { get; set; }
        public string SchoolCode { get; set; }
        public string AccountNumber { get; set; }
        public string SchoolName { get; set; }
        public decimal Amount { get; set; }
        public string AdviceNumber { get; set; }
    }

    /// <summary>
    /// DTO for branch-specific detailed report
    /// </summary>
    public class BranchDetailReportDTO
    {
        public string BranchName { get; set; }
        public int BranchCode { get; set; }
        public DateTime EntryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<BranchDetailEntryDTO> Entries { get; set; } = new();
    }
}
