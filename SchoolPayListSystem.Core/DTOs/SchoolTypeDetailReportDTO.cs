namespace SchoolPayListSystem.Core.DTOs
{
    /// <summary>
    /// DTO for school type detail report entry
    /// Lists all individual schools of a school type with Amount1, Amount2, Amount3
    /// </summary>
    public class SchoolTypeDetailReportDTO
    {
        public int SerialNumber { get; set; }
        public string SchoolCode { get; set; }
        public string SchoolName { get; set; }
        public decimal Amount1 { get; set; }
        public decimal Amount2 { get; set; }
        public decimal Amount3 { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
