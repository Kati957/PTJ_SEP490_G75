using PTJ_Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Repositories.Interfaces
    {
    public interface IJobSeekerProfileRepository
        {
        // 🔹 Lấy profile theo userId
        Task<JobSeekerProfile?> GetByUserIdAsync(int userId);

        // 🔹 Cập nhật thông tin + ảnh (service đã xử lý ảnh, nên repo chỉ cập nhật DB)
        Task UpdateAsync(JobSeekerProfile profile);

        // 🔹 Gỡ ảnh — chỉ thay link ảnh mặc định (không xóa trên cloud)
        Task DeleteProfilePictureAsync(int userId, string defaultPictureUrl, string defaultPublicId);
        }
    }
