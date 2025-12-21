using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.Home;
using PTJ_Models.DTO.HomePageDTO;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations.Home
{
    public class HomeRepository : IHomeRepository
    {
        private readonly JobMatchingOpenAiDbContext _context;

        public HomeRepository(JobMatchingOpenAiDbContext context)
        {
            _context = context;
        }

        public async Task<HomeStatisticsDto> GetHomeStatisticsAsync()
        {
            return new HomeStatisticsDto
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveEmployerPosts = await _context.EmployerPosts
                    .Where(p => p.Status == "Active")
                    .CountAsync(),
                ActiveJobSeekerPosts = await _context.JobSeekerPosts
                    .Where(p => p.Status == "Active")
                    .CountAsync(),
                TotalCategories = await _context.Categories
                    .Where(c => c.IsActive)
                    .CountAsync()
            };
        }
    }

}
