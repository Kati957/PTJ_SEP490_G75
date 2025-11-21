using System.ComponentModel.DataAnnotations;

namespace PTJ_Models.DTO.PostDTO
{
    public class EmployerPostDto
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [StringLength(120, MinimumLength = 5, ErrorMessage = "Tiêu đề phải từ 5–120 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả công việc không được để trống.")]
        [StringLength(5000, MinimumLength = 20, ErrorMessage = "Mô tả phải từ 20 ký tự trở lên.")]
        public string? Description { get; set; }


        [Range(0, double.MaxValue, ErrorMessage = "Mức lương phải >= 0.")]
        public decimal? Salary { get; set; }

        [StringLength(50)]
        public string? SalaryText { get; set; }
        // FE gửi "thỏa thuận"

        public string? Requirements { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$",
       ErrorMessage = "Giờ làm phải có định dạng HH:mm.")]
        public string? WorkHourStart { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$",
            ErrorMessage = "Giờ làm phải có định dạng HH:mm.")]
        public string? WorkHourEnd { get; set; }


        [Range(1, int.MaxValue, ErrorMessage = "ProvinceId không hợp lệ.")]
        public int ProvinceId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "DistrictId không hợp lệ.")]
        public int DistrictId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "WardId không hợp lệ.")]
        public int WardId { get; set; }

        [Required(ErrorMessage = "Địa chỉ chi tiết không được để trống.")]
        public string? DetailAddress { get; set; }


        [Range(1, int.MaxValue, ErrorMessage = "CategoryID không hợp lệ.")]
        public int? CategoryID { get; set; }

        public int? SubCategoryId { get; set; }

        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$",
    ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam.")]
        public string? PhoneContact { get; set; }

    }

    public class EmployerPostResultDto
    {
        public EmployerPostDtoOut Post { get; set; } = new();
        public List<AIResultDto> SuggestedCandidates { get; set; } = new();
    }
}
