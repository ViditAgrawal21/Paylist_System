using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;

namespace SchoolPayListSystem.Data.Repositories
{
    public interface ISalaryEntryRepository : IRepository<SalaryEntry>
    {
        Task<List<SalaryEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<SalaryEntry>> GetAllWithNavigationAsync();
        Task<List<SalaryEntry>> GetByCreatedByUserIdAsync(int createdByUserId);
    }

    public class SalaryEntryRepository : BaseRepository<SalaryEntry>, ISalaryEntryRepository
    {
        public SalaryEntryRepository(SchoolPayListDbContext context) : base(context) { }

        public async Task<List<SalaryEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(s => s.EntryDate >= startDate && s.EntryDate <= endDate)
                .Include(s => s.School)
                .Include(s => s.Branch)
                .ToListAsync();
        }

        public async Task<List<SalaryEntry>> GetAllWithNavigationAsync()
        {
            return await _dbSet.Include(s => s.School).Include(s => s.Branch).ToListAsync();
        }

        public async Task<List<SalaryEntry>> GetByCreatedByUserIdAsync(int createdByUserId)
        {
            return await _dbSet
                .Where(s => s.CreatedByUserId == createdByUserId)
                .Include(s => s.School)
                .Include(s => s.Branch)
                .OrderByDescending(s => s.EntryDate)
                .ToListAsync();
        }
    }
}
