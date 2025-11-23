using System;

namespace SchoolPayListSystem.Core.Models
{
    public class SchoolType
    {
        public int SchoolTypeId { get; set; }
        public string TypeCode { get; set; }
        public string TypeName { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
