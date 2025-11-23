using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoolPayListSystem.Services
{
    public class ReportService
    {
        public async Task<string> GenerateReportAsync()
        {
            return await Task.FromResult("Report generated");
        }
    }
}
