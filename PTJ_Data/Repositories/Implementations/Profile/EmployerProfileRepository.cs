using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models;
using PTJ_Models.Models;
using PTJ_Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_Repositories.Implementations
    {
    public class EmployerProfileRepository : IEmployerProfileRepository
        {
        private readonly JobMatchingDbContext _context;

        public EmployerProfileRepository(JobMatchingDbContext context)
            {
            _context = context;
            }

        public async Task<EmployerProfile?> GetByUserIdAsync(int userId)
            {
            return await _context.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);
            }

        public async Task UpdateAsync(EmployerProfile profile)
            {
            _context.EmployerProfiles.Update(profile);
            await _context.SaveChangesAsync();
            }

        public async Task DeleteAvatarAsync(int userId, string defaultAvatarUrl, string defaultPublicId)
            {
            var existing = await _context.EmployerProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existing != null)
                {
                existing.AvatarUrl = defaultAvatarUrl;
                existing.AvatarPublicId = defaultPublicId;
                existing.IsAvatarHidden = false;

                _context.EmployerProfiles.Update(existing);
                await _context.SaveChangesAsync();
                }
            }
        }
    }
