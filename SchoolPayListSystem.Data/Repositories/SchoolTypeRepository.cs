using System.Threading.Tasks;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;

namespace SchoolPayListSystem.Data.Repositories
{
    public interface ISchoolTypeRepository : IRepository<SchoolType> { }

    public class SchoolTypeRepository : BaseRepository<SchoolType>, ISchoolTypeRepository
    {
        public SchoolTypeRepository(SchoolPayListDbContext context) : base(context) { }
    }
}
