using System;

namespace SchoolPayListSystem.Core.Models
{
    /// <summary>
    /// Stores the mapping of advice numbers to (Branch + SchoolType + Date) combinations
    /// Ensures global sequential advice numbering across all branch/school type combinations
    /// Provides sync mechanism between Branch Reports and School Summary Reports
    /// </summary>
    public class AdviceNumberMapping
    {
        public int AdviceNumberMappingId { get; set; }
        
        /// <summary>
        /// Full advice number in format YYMMDD + 2-digit serial (e.g., 25120601)
        /// </summary>
        public string AdviceNumber { get; set; }
        
        /// <summary>
        /// Date for which advice number was generated
        /// </summary>
        public DateTime AdviceDate { get; set; }
        
        /// <summary>
        /// Branch ID part of the unique combination
        /// </summary>
        public int BranchId { get; set; }
        
        /// <summary>
        /// School Type ID part of the unique combination
        /// </summary>
        public int SchoolTypeId { get; set; }
        
        /// <summary>
        /// Module that generated this advice number (e.g., "BranchReport" or "SchoolSummary")
        /// </summary>
        public string GeneratedByModule { get; set; }
        
        /// <summary>
        /// Timestamp when this mapping was created
        /// </summary>
        public DateTime GeneratedTimestamp { get; set; }
        
        /// <summary>
        /// Serial number extracted from advice number (for querying convenience)
        /// </summary>
        public int SerialNumber { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Branch Branch { get; set; }
        public virtual SchoolType SchoolType { get; set; }
    }
}
