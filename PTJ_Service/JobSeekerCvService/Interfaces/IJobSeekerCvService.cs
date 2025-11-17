using PTJ_Models.DTO.CvDTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Service.JobSeekerCvService.Interfaces
    {
    public interface IJobSeekerCvService
        {
        // JobSeeker xem CV của chính mình
        Task<JobSeekerCvResultDto?> GetByIdAsync(int cvId, int jobSeekerId);

        // Employer xem CV theo CvId
        Task<JobSeekerCvResultDto?> GetByIdForEmployerAsync(int cvId);

        // Lấy danh sách CV của JobSeeker
        Task<IEnumerable<JobSeekerCvResultDto>> GetByJobSeekerAsync(int jobSeekerId);

        Task<JobSeekerCvResultDto> CreateAsync(int jobSeekerId, JobSeekerCvCreateDto dto);
        Task<bool> UpdateAsync(int jobSeekerId, int cvId, JobSeekerCvUpdateDto dto);
        Task<bool> DeleteAsync(int jobSeekerId, int cvId);
        }
    }
