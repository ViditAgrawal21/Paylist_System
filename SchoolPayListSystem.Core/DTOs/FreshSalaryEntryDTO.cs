using System;

namespace SchoolPayListSystem.Core.DTOs
{
    /// <summary>
    /// DTO for displaying fresh (manually created) salary entries with full details
    /// </summary>
    public class FreshSalaryEntryDTO
    {
        public int SalaryEntryId { get; set; }
        public DateTime INDATE { get; set; }
        public string SchoolCode { get; set; }
        public string SchoolName { get; set; }
        public string BankAccount { get; set; }
        public string SchoolTypeCode { get; set; }
        public string SchoolType { get; set; }
        public int BranchCode { get; set; }
        public string Branch { get; set; }
        public decimal AMOUNT { get; set; }  // Total Amount
        public decimal AMOUNT1 { get; set; }
        public decimal AMOUNT2 { get; set; }
        public decimal AMOUNT3 { get; set; }
        public string OperatorName { get; set; }
        public string OperatorId { get; set; }
        public int UserId { get; set; }
        public DateTime? STAMPDATE { get; set; }
        public TimeSpan? STAMPTIME { get; set; }
    }
}
