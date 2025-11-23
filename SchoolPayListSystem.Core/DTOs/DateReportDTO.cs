using System;
using System.Collections.Generic;

namespace SchoolPayListSystem.Core.DTOs
{
    public class DateReportDTO
    {
        public DateTime ReportDate { get; set; }
        public string DateRangeLabel { get; set; }
        public decimal TotalAmount { get; set; }
        public List<SalaryEntryReportDTO> Entries { get; set; } = new();
    }
}
