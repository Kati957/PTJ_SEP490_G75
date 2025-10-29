﻿using PTJ_Models.DTO.PostDTO;
using PTJ_Models.Models;

namespace PTJ_Service.JobSeekerPostService
    {
    public interface IJobSeekerPostService
        {
        // CRUD
        Task<IEnumerable<JobSeekerPostDtoOut>> GetAllAsync();
        Task<IEnumerable<JobSeekerPostDtoOut>> GetByUserAsync(int userId);
        Task<JobSeekerPostDtoOut?> GetByIdAsync(int id);
        Task<bool> DeleteAsync(int id);

        // CREATE + REFRESH
        Task<JobSeekerPostResultDto> CreateJobSeekerPostAsync(JobSeekerPostDto dto);
        Task<JobSeekerPostResultDto> RefreshSuggestionsAsync(int jobSeekerPostId);
        Task<JobSeekerPostDtoOut?> UpdateAsync(int id, JobSeekerPostDto dto);

        // SHORTLIST
        Task SaveJobAsync(SaveJobDto dto);
        Task UnsaveJobAsync(SaveJobDto dto);
        Task<IEnumerable<object>> GetSavedJobsAsync(int jobSeekerId);
        Task<IEnumerable<JobSeekerJobSuggestionDto>> GetSuggestionsByPostAsync(int jobSeekerPostId, int take = 10, int skip = 0);
        }
    }
