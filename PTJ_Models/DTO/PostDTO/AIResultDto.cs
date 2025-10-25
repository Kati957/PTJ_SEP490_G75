using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.PostDTO
{
    public class AIResultDto
    {
        public string Id { get; set; } = string.Empty;
        public double Score { get; set; } // phần trăm tương đồng
        public object? ExtraInfo { get; set; } // chứa thông tin ứng viên hoặc bài đăng
    }
}
