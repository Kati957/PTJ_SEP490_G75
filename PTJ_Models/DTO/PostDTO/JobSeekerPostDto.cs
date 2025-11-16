using System.ComponentModel.DataAnnotations;

namespace PTJ_Models.DTO.PostDTO
    {
    public class JobSeekerPostDto
        {
        public int UserID { get; set; }
        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [StringLength(120, MinimumLength = 5, ErrorMessage = "Tiêu đề phải từ 5–120 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả bản thân không được để trống.")]
        [StringLength(5000, MinimumLength = 20, ErrorMessage = "Mô tả phải từ 20 ký tự trở lên.")]
        public string? Description { get; set; }

        [Range(16, 60, ErrorMessage = "Tuổi phải từ 16 đến 60.")]
        public int? Age { get; set; }

        public string? Gender { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$",
       ErrorMessage = "Giờ làm phải có định dạng HH:mm.")]
        public string? PreferredWorkHourStart { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$",
            ErrorMessage = "Giờ làm phải có định dạng HH:mm.")]
        public string? PreferredWorkHourEnd { get; set; }


        [Range(1, int.MaxValue, ErrorMessage = "ProvinceId không hợp lệ.")]
        public int ProvinceId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "DistrictId không hợp lệ.")]
        public int DistrictId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "WardId không hợp lệ.")]
        public int WardId { get; set; }


        [Range(1, int.MaxValue, ErrorMessage = "CategoryID không hợp lệ.")]
        public int? CategoryID { get; set; }

        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$",
     ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam.")]
        public string? PhoneContact { get; set; }


        // 👇 NEW - thêm CV để JobSeekerPost dùng embedding từ CV
        [Range(1, int.MaxValue, ErrorMessage = "SelectedCvId không hợp lệ.")]
        public int? SelectedCvId { get; set; }

        }

    public class JobSeekerPostResultDto
        {
        public JobSeekerPostDtoOut Post { get; set; } = new();
        public List<AIResultDto> SuggestedJobs { get; set; } = new();
        }
    }
