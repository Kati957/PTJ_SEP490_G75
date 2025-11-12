using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.CvDTO;
using PTJ_Models.Models;
using PTJ_Service.JobSeekerCvService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTJ_Service.JobSeekerCvService.Implementations
    {
    public class JobSeekerCvService : IJobSeekerCvService
        {
        private readonly IJobSeekerCvRepository _repo;

        public JobSeekerCvService(IJobSeekerCvRepository repo)
            {
            _repo = repo;
            }

        public async Task<JobSeekerCvResultDto?> GetByIdAsync(int id)
            {
            var cv = await _repo.GetByIdAsync(id);
            if (cv == null)
                return null;

            return new JobSeekerCvResultDto
                {
                Cvid = cv.Cvid,
                JobSeekerId = cv.JobSeekerId,
                CvTitle = cv.Cvtitle,
                SkillSummary = cv.SkillSummary,
                Skills = cv.Skills,
                PreferredJobType = cv.PreferredJobType,
                PreferredLocation = cv.PreferredLocation,
                ContactPhone = cv.ContactPhone,
                CreatedAt = cv.CreatedAt,
                UpdatedAt = cv.UpdatedAt
                };
            }

        public async Task<IEnumerable<JobSeekerCvResultDto>> GetByJobSeekerAsync(int jobSeekerId)
            {
            var list = await _repo.GetByJobSeekerAsync(jobSeekerId);
            return list.Select(cv => new JobSeekerCvResultDto
                {
                Cvid = cv.Cvid,
                JobSeekerId = cv.JobSeekerId,
                CvTitle = cv.Cvtitle,
                SkillSummary = cv.SkillSummary,
                Skills = cv.Skills,
                PreferredJobType = cv.PreferredJobType,
                PreferredLocation = cv.PreferredLocation,
                ContactPhone = cv.ContactPhone,
                CreatedAt = cv.CreatedAt,
                UpdatedAt = cv.UpdatedAt
                });
            }

        public async Task<JobSeekerCvResultDto> CreateAsync(int jobSeekerId, JobSeekerCvCreateDto dto)
            {
            var cv = new JobSeekerCv
                {
                JobSeekerId = jobSeekerId,
                Cvtitle = dto.CvTitle,
                SkillSummary = dto.SkillSummary,
                Skills = dto.Skills,
                PreferredJobType = dto.PreferredJobType,
                PreferredLocation = dto.PreferredLocation,
                ContactPhone = dto.ContactPhone,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
                };

            await _repo.AddAsync(cv);

            return new JobSeekerCvResultDto
                {
                Cvid = cv.Cvid,
                JobSeekerId = cv.JobSeekerId,
                CvTitle = cv.Cvtitle,
                SkillSummary = cv.SkillSummary,
                Skills = cv.Skills,
                PreferredJobType = cv.PreferredJobType,
                PreferredLocation = cv.PreferredLocation,
                ContactPhone = cv.ContactPhone,
                CreatedAt = cv.CreatedAt,
                UpdatedAt = cv.UpdatedAt
                };
            }

        public async Task<bool> UpdateAsync(int jobSeekerId, int cvId, JobSeekerCvUpdateDto dto)
            {
            var cv = await _repo.GetByIdAsync(cvId);
            if (cv == null || cv.JobSeekerId != jobSeekerId)
                return false;

            cv.Cvtitle = dto.CvTitle;
            cv.SkillSummary = dto.SkillSummary;
            cv.Skills = dto.Skills;
            cv.PreferredJobType = dto.PreferredJobType;
            cv.PreferredLocation = dto.PreferredLocation;
            cv.ContactPhone = dto.ContactPhone;
            cv.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(cv);
            return true;
            }

        public async Task<bool> DeleteAsync(int jobSeekerId, int cvId)
            {
            var cv = await _repo.GetByIdAsync(cvId);
            if (cv == null || cv.JobSeekerId != jobSeekerId)
                return false;

            await _repo.DeleteAsync(cv);
            return true;
            }
        }
    }
