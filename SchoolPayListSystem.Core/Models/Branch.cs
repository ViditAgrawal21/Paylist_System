using System;

namespace SchoolPayListSystem.Core.Models
{
    public class Branch
    {
        public int BranchId { get; set; }
        public int BranchCode { get; set; }
        public string BranchName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
