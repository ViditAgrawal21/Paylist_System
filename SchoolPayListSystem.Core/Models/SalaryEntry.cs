using System;

namespace SchoolPayListSystem.Core.Models
{
    public class SalaryEntry
    {
        public int SalaryEntryId { get; set; }
        public DateTime EntryDate { get; set; }
        public TimeSpan? EntryTime { get; set; }
        public int SchoolId { get; set; }
        public int BranchId { get; set; }
        public string AccountNumber { get; set; }
        public decimal Amount1 { get; set; }
        public decimal Amount2 { get; set; }
        public decimal Amount3 { get; set; }
        public decimal TotalAmount { get; set; }
        public string AdviceNumber { get; set; }
        public string OperatorName { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsImported { get; set; } = false; // false = fresh entry, true = imported from Excel

        public virtual School School { get; set; }
        public virtual Branch Branch { get; set; }
        public virtual User CreatedByUser { get; set; }
    }
}
