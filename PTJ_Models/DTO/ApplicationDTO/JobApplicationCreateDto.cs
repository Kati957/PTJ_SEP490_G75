using System.ComponentModel.DataAnnotations;

namespace PTJ_Models.DTO.ApplicationDTO
    {
    public class JobApplicationCreateDto
        {
        [Required(ErrorMessage = "Thiếu thông tin ứng viên.")]
        [Range(1, int.MaxValue, ErrorMessage = "JobSeekerId không hợp lệ.")]
        public int JobSeekerId { get; set; }

        [Required(ErrorMessage = "Thiếu thông tin bài đăng.")]
        [Range(1, int.MaxValue, ErrorMessage = "EmployerPostId không hợp lệ.")]
        public int EmployerPostId { get; set; }

        //  Thêm Cvid để chọn CV khi ứng tuyển
        [Range(1, int.MaxValue, ErrorMessage = "CV không hợp lệ.")]
        public int? Cvid { get; set; }

        [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
        public string? Note { get; set; }
        }
    }
