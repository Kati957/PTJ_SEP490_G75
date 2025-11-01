using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.Models;
using Microsoft.EntityFrameworkCore;
using PTJ_Models;

namespace PTJ_Data.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly JobMatchingDbContext _context;
        public UserRepository(JobMatchingDbContext context) => _context = context;

        public Task<User?> GetByUsernameAsync(string username)
            => _context.Users.FirstOrDefaultAsync(x => x.Username == username);

        public Task<User?> GetByEmailAsync(string email)
            => _context.Users.FirstOrDefaultAsync(x => x.Email == email);

        public Task<User?> GetByIdAsync(int id)
            => _context.Users.FindAsync(id).AsTask();

        public async Task AddAsync(User user)
            => await _context.Users.AddAsync(user);

        public Task SaveAsync()
            => _context.SaveChangesAsync();
    }
}

