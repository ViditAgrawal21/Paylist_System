using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;

namespace SchoolPayListSystem.Data.Repositories
{
    public interface IAdviceNumberMappingRepository : IRepository<AdviceNumberMapping>
    {
        Task<AdviceNumberMapping> GetByDateBranchSchoolTypeAsync(DateTime date, int branchId, int schoolTypeId);
        Task<int> GetNextSerialNumberAsync(DateTime date);
        Task<List<AdviceNumberMapping>> GetByDateAsync(DateTime date);
        Task<AdviceNumberMapping> GetByAdviceNumberAsync(string adviceNumber);
    }

    public class AdviceNumberMappingRepository : BaseRepository<AdviceNumberMapping>, IAdviceNumberMappingRepository
    {
        public AdviceNumberMappingRepository(SchoolPayListDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get existing advice number mapping for a specific (date + branch + school type) combination
        /// </summary>
        public async Task<AdviceNumberMapping> GetByDateBranchSchoolTypeAsync(DateTime date, int branchId, int schoolTypeId)
        {
            try
            {
                var mapping = await _dbSet
                    .Where(a => a.AdviceDate.Date == date.Date 
                        && a.BranchId == branchId 
                        && a.SchoolTypeId == schoolTypeId)
                    .FirstOrDefaultAsync();
                
                return mapping;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching advice number mapping: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get the next serial number for a given date (globally sequential across all branch/school type combos)
        /// </summary>
        public async Task<int> GetNextSerialNumberAsync(DateTime date)
        {
            try
            {
                string datePrefix = date.ToString("yyMMdd");
                
                // Get all advice numbers for this date
                var mappingsForDate = await _dbSet
                    .Where(a => a.AdviceDate.Date == date.Date)
                    .OrderByDescending(a => a.SerialNumber)
                    .FirstOrDefaultAsync();

                if (mappingsForDate == null)
                    return 1; // First advice number of the day starts at 01

                return mappingsForDate.SerialNumber + 1;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting next serial number: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all advice number mappings for a specific date
        /// </summary>
        public async Task<List<AdviceNumberMapping>> GetByDateAsync(DateTime date)
        {
            try
            {
                var mappings = await _dbSet
                    .Where(a => a.AdviceDate.Date == date.Date)
                    .OrderBy(a => a.SerialNumber)
                    .ToListAsync();
                
                return mappings;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching advice numbers for date: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get mapping by full advice number
        /// </summary>
        public async Task<AdviceNumberMapping> GetByAdviceNumberAsync(string adviceNumber)
        {
            try
            {
                var mapping = await _dbSet
                    .Where(a => a.AdviceNumber == adviceNumber)
                    .FirstOrDefaultAsync();
                
                return mapping;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching advice number mapping by advice number: {ex.Message}", ex);
            }
        }
    }
}
