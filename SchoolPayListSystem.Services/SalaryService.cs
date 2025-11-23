using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Repositories;

namespace SchoolPayListSystem.Services
{
    public class SalaryService
    {
        private readonly ISalaryEntryRepository _salaryRepository;

        public SalaryService(ISalaryEntryRepository salaryRepository)
        {
            _salaryRepository = salaryRepository;
        }

        public async Task<List<SalaryEntry>> GetAllEntriesAsync()
        {
            return await _salaryRepository.GetAllWithNavigationAsync();
        }

        public async Task<List<SalaryEntry>> GetEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _salaryRepository.GetByDateRangeAsync(startDate, endDate);
        }

        public async Task<(bool success, string message)> AddSalaryEntryAsync(DateTime entryDate, int schoolId, 
            int branchId, string accountNumber, decimal amount1, decimal amount2, decimal amount3)
        {
            try
            {
                decimal total = amount1 + amount2 + amount3;
                var entry = new SalaryEntry
                {
                    EntryDate = entryDate,
                    SchoolId = schoolId,
                    BranchId = branchId,
                    AccountNumber = accountNumber,
                    Amount1 = amount1,
                    Amount2 = amount2,
                    Amount3 = amount3,
                    TotalAmount = total,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                await _salaryRepository.AddAsync(entry);
                await _salaryRepository.SaveChangesAsync();
                return (true, "Salary entry added successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
    }
}
