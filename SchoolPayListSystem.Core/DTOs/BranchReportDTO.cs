using System;
using System.Collections.Generic;

namespace SchoolPayListSystem.Core.DTOs
{
    public class SalaryEntryReportDTO
    {
        public DateTime EntryDate { get; set; }
        public string SchoolName { get; set; }
        public string AccountNumber { get; set; }
        public decimal Amount1 { get; set; }
        public decimal Amount2 { get; set; }
        public decimal Amount3 { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class BranchReportDTO
    {
        public string BranchName { get; set; }
        public int BranchCode { get; set; }
        public decimal TotalAmount { get; set; }
        public List<SalaryEntryReportDTO> Entries { get; set; } = new();
    }
}
