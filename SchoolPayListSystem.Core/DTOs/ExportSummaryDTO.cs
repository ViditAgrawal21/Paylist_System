using System;

namespace SchoolPayListSystem.Core.DTOs
{
    /// <summary>
    /// DTO for displaying export summary - shows count of entries per operator
    /// Used by GCP admin to see how much data each operator has created
    /// </summary>
    public class ExportSummaryDTO
    {
        public int UserId { get; set; }
        public string OperatorId { get; set; }      // Username
        public string OperatorName { get; set; }    // Full Name
        public int FreshEntryCount { get; set; }
        public int ImportedEntryCount { get; set; }
        public int TotalEntryCount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime? LastEntryDate { get; set; }
    }
}
