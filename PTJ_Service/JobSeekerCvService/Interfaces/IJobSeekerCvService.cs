using PTJ_Models.DTO.CvDTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Service.JobSeekerCvService.Interfaces
    {
    public interface IJobSeekerCvService
        {
        Task<JobSeekerCvResultDto?> GetByIdAsync(int id);
        Task<IEnumerable<JobSeekerCvResultDto>> GetByJobSeekerAsync(int jobSeekerId);
        Task<JobSeekerCvResultDto> CreateAsync(int jobSeekerId, JobSeekerCvCreateDto dto);
        Task<bool> UpdateAsync(int jobSeekerId, int cvId, JobSeekerCvUpdateDto dto);
        Task<bool> DeleteAsync(int jobSeekerId, int cvId);
        }
    }
