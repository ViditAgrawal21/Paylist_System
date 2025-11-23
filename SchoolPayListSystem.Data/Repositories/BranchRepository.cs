using System.Threading.Tasks;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;

namespace SchoolPayListSystem.Data.Repositories
{
    public interface IBranchRepository : IRepository<Branch> { }

    public class BranchRepository : BaseRepository<Branch>, IBranchRepository
    {
        public BranchRepository(SchoolPayListDbContext context) : base(context) { }
    }
}
