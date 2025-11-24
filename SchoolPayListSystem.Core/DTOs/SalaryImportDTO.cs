using System;

namespace SchoolPayListSystem.Core.DTOs
{
    public class SalaryImportDTO
    {
        public string BankAccount { get; set; }
        public string SchoolName { get; set; }
        public string SchoolTypeCode { get; set; }
        public string SchoolType { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public decimal Amount { get; set; }
        public decimal Amount1 { get; set; }
        public decimal Amount2 { get; set; }
        public decimal Amount3 { get; set; }
        public string AdviceNumber { get; set; }
        public string OperatorName { get; set; }
        public DateTime StampDate { get; set; }
        public TimeSpan StampTime { get; set; }
        public int UserID { get; set; }
    }
}
