using System;
using System.Linq;
using System.Threading.Tasks;
using SchoolPayListSystem.Core.Models;
using SchoolPayListSystem.Data.Database;

namespace SchoolPayListSystem.Data.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetByUsernameAsync(string username);
    }

    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(SchoolPayListDbContext context) : base(context) { }

        public async Task<User> GetByUsernameAsync(string username)
        {
            return await Task.FromResult(_dbSet.FirstOrDefault(u => u.Username == username));
        }
    }
}
