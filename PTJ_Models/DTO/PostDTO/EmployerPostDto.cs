using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PTJ_Models.DTO.PostDTO
{
    public class EmployerPostCreateDto
        {
        public int UserID { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(5000, MinimumLength = 20)]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal? Salary { get; set; }

        [StringLength(50)]
        public string? SalaryText { get; set; }

        public string? Requirements { get; set; }

        [Required]
        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$")]
        public string WorkHourStart { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$")]
        public string WorkHourEnd { get; set; } = string.Empty;

        public DateTime? ExpiredAt { get; set; }


        [Range(1, int.MaxValue)]
        public int ProvinceId { get; set; }

        [Range(1, int.MaxValue)]
        public int DistrictId { get; set; }

        [Range(1, int.MaxValue)]
        public int WardId { get; set; }

        [Required]
        public string DetailAddress { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int CategoryID { get; set; }

        public int? SubCategoryId { get; set; }

        [Required]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$")]
        public string PhoneContact { get; set; } = string.Empty;

        public List<IFormFile>? Images { get; set; }
        }

    public class EmployerPostUpdateDto
        {
        [StringLength(120, MinimumLength = 5)]
        public string? Title { get; set; }

        [StringLength(5000, MinimumLength = 20)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Salary { get; set; }

        [StringLength(50)]
        public string? SalaryText { get; set; }

        public string? Requirements { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$")]
        public string? WorkHourStart { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$")]
        public string? WorkHourEnd { get; set; }

        [Range(1, int.MaxValue)]
        public int? ProvinceId { get; set; }

        [Range(1, int.MaxValue)]
        public int? DistrictId { get; set; }

        [Range(1, int.MaxValue)]
        public int? WardId { get; set; }

        public string? DetailAddress { get; set; }

        [Range(1, int.MaxValue)]
        public int? CategoryID { get; set; }

        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$")]
        public string? PhoneContact { get; set; }

        public List<IFormFile>? Images { get; set; }
        public List<int>? DeleteImageIds { get; set; }
        }


    public class EmployerPostResultDto
    {
        public EmployerPostDtoOut Post { get; set; } = new();
        public List<AIResultDto> SuggestedCandidates { get; set; } = new();
    }
}
