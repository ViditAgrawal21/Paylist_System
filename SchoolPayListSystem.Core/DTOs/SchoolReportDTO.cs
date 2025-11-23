using System;
using System.Collections.Generic;

namespace SchoolPayListSystem.Core.DTOs
{
    public class SchoolReportDTO
    {
        public string SchoolName { get; set; }
        public string SchoolCode { get; set; }
        public string BankAccountNumber { get; set; }
        public string BranchName { get; set; }
        public decimal TotalAmount { get; set; }
        public List<SalaryEntryReportDTO> Entries { get; set; } = new();
    }
}
