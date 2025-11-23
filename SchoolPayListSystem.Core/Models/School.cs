using System;

namespace SchoolPayListSystem.Core.Models
{
    public class School
    {
        public int SchoolId { get; set; }
        public string SchoolCode { get; set; }
        public string SchoolName { get; set; }
        public int SchoolTypeId { get; set; }
        public int BranchId { get; set; }
        public string BankAccountNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual SchoolType SchoolType { get; set; }
        public virtual Branch Branch { get; set; }
    }
}
