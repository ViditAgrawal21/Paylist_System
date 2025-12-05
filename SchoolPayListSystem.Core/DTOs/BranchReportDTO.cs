using System;
using System.Collections.Generic;

namespace SchoolPayListSystem.Core.DTOs
{
    public class SalaryEntryReportDTO
    {
        public DateTime INDATE { get; set; }
        public string SchoolCode { get; set; }
        public string SchoolName { get; set; }
        public string BankAccount { get; set; }
        public string SchoolTypeCode { get; set; }
        public string SchoolType { get; set; }
        public int BranchCode { get; set; }
        public string BranchName { get; set; }
        public decimal AMOUNT { get; set; }
        public decimal AMOUNT1 { get; set; }
        public decimal AMOUNT2 { get; set; }
        public string AdviceNumber { get; set; }
        public string OperatorName { get; set; }
        public string OperatorId { get; set; }
        public DateTime? STAMPDATE { get; set; }
        public TimeSpan? STAMPTIME { get; set; }
    }

    public class BranchReportDTO
    {
        public string BranchName { get; set; }
        public int BranchCode { get; set; }
        public decimal TotalAmount { get; set; }
        public string AdviceNumber { get; set; }  // Starting advice number for this branch
        public List<SalaryEntryReportDTO> Entries { get; set; } = new();
    }
}
