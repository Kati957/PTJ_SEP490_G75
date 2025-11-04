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
    public class JobSeekerProfileRepository : IJobSeekerProfileRepository
        {
        private readonly JobMatchingDbContext _context;

        public JobSeekerProfileRepository(JobMatchingDbContext context)
            {
            _context = context;
            }

        public async Task<JobSeekerProfile?> GetByUserIdAsync(int userId)
            {
            return await _context.JobSeekerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);
            }

        public async Task UpdateAsync(JobSeekerProfile profile)
            {
            _context.JobSeekerProfiles.Update(profile);
            await _context.SaveChangesAsync();
            }

        public async Task DeleteProfilePictureAsync(int userId, string defaultPictureUrl, string defaultPublicId)
            {
            var existing = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existing != null)
                {
                existing.ProfilePicture = defaultPictureUrl;
                existing.ProfilePicturePublicId = defaultPublicId;
                existing.IsPictureHidden = false;

                _context.JobSeekerProfiles.Update(existing);
                await _context.SaveChangesAsync();
                }
            }
        }
    }
