using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Models.DTO.Admin;
using PTJ_Models.Models;

namespace PTJ_Data.Repositories.Implementations.Admin
{
    public class AdminStatisticsRepository : IAdminStatisticsRepository
    {
        private readonly JobMatchingDbContext _context;

        public AdminStatisticsRepository(JobMatchingDbContext context)
        {
            _context = context;
        }

        public async Task<AdminStatisticsDto> GetAdminStatisticsAsync()
        {
            return new AdminStatisticsDto
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalEmployers = await _context.Users
                .Where(u => u.Roles.Any(r => r.RoleName == "Employer"))
                .CountAsync(),


                TotalJobSeekers = await _context.Users
                .Where(u => u.Roles.Any(r => r.RoleName == "JobSeeker"))
                .CountAsync(),

                TotalEmployerPosts = await _context.EmployerPosts.CountAsync(),

                TotalJobSeekerPosts = await _context.JobSeekerPosts.CountAsync(),

                TotalReports = await _context.PostReports.CountAsync(),

                TotalNews = await _context.News.CountAsync()
            };
        }
    }

}
