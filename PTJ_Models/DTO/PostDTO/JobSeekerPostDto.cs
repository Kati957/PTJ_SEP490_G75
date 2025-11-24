using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PTJ_Models.DTO.PostDTO
{
    public class JobSeekerPostCreateDto
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

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$")]
        public string? PreferredWorkHourStart { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$")]
        public string? PreferredWorkHourEnd { get; set; }

        [Range(1, int.MaxValue)]
        public int ProvinceId { get; set; }

        [Range(1, int.MaxValue)]
        public int DistrictId { get; set; }

        [Range(1, int.MaxValue)]
        public int WardId { get; set; }

        [Range(1, int.MaxValue)]
        public int? CategoryID { get; set; }

        public int? SubCategoryId { get; set; }

        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$")]
        public string? PhoneContact { get; set; }

        [Range(1, int.MaxValue)]
        public int? SelectedCvId { get; set; }

        public List<IFormFile>? Images { get; set; }
        }

    public class JobSeekerPostUpdateDto
        {
        [StringLength(120, MinimumLength = 5)]
        public string? Title { get; set; }

        [StringLength(5000, MinimumLength = 20)]
        public string? Description { get; set; }

        [Range(16, 60)]
        public int? Age { get; set; }

        public string? Gender { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$")]
        public string? PreferredWorkHourStart { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$")]
        public string? PreferredWorkHourEnd { get; set; }

        [Range(1, int.MaxValue)]
        public int? ProvinceId { get; set; }

        [Range(1, int.MaxValue)]
        public int? DistrictId { get; set; }

        [Range(1, int.MaxValue)]
        public int? WardId { get; set; }

        [Range(1, int.MaxValue)]
        public int? CategoryID { get; set; }

        public int? SubCategoryId { get; set; }

        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$")]
        public string? PhoneContact { get; set; }

        [Range(1, int.MaxValue)]
        public int? SelectedCvId { get; set; }

        public List<IFormFile>? Images { get; set; }
        public List<int>? DeleteImageIds { get; set; }
        }


    public class JobSeekerPostResultDto
    {
        public JobSeekerPostDtoOut Post { get; set; } = new();
        public List<AIResultDto> SuggestedJobs { get; set; } = new();
    }
}
