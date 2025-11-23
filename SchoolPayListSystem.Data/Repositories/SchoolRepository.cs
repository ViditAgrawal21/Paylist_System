using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;

namespace SchoolPayListSystem.Data.Repositories
{
    public interface ISchoolRepository : IRepository<School> 
    {
        Task<List<School>> GetAllWithNavigationAsync();
    }

    public class SchoolRepository : BaseRepository<School>, ISchoolRepository
    {
        public SchoolRepository(SchoolPayListDbContext context) : base(context) { }

        public async Task<List<School>> GetAllWithNavigationAsync()
        {
            return await _dbSet.Include(s => s.SchoolType).Include(s => s.Branch).ToListAsync();
        }
    }
}
