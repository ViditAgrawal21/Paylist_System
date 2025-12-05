using System;

namespace SchoolPayListSystem.Core.DTOs
{
    /// <summary>
    /// DTO for displaying imported salary entries as summary/count
    /// </summary>
    public class ImportedSalaryEntrySummaryDTO
    {
        public DateTime EntryDate { get; set; }
        public string SchoolTypeCode { get; set; }
        public string SchoolTypeName { get; set; }
        public string BranchName { get; set; }
        public int Count { get; set; }  // Number of imported entries
        public decimal TotalAmount { get; set; }  // Sum of all amounts
    }
}
