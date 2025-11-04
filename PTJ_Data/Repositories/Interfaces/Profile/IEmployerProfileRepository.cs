using PTJ_Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTJ_Repositories.Interfaces
    {
    public interface IEmployerProfileRepository
        {
        // 🔹 Lấy profile theo userId
        Task<EmployerProfile?> GetByUserIdAsync(int userId);

        // 🔹 Cập nhật thông tin + ảnh (service đã xử lý ảnh upload rồi)
        Task UpdateAsync(EmployerProfile profile);

        // 🔹 Gỡ ảnh — chuyển sang ảnh mặc định (không xóa Cloud)
        Task DeleteAvatarAsync(int userId, string defaultAvatarUrl, string defaultPublicId);
        }
    }
