using System.ComponentModel.DataAnnotations;
using CommandLine.Text;
using Microsoft.AspNetCore.Http;
using NHibernate.Criterion;

namespace PTJ_Models.DTO.PostDTO
{
    public class EmployerPostCreateDto
    {
        [Required(ErrorMessage = "UserID là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "UserID không hợp lệ")]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(120, MinimumLength = 5, ErrorMessage = "Tiêu đề phải từ 5 đến 120 ký tự")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả không được để trống")]
        [StringLength(5000, MinimumLength = 20, ErrorMessage = "Mô tả phải từ 20 đến 5000 ký tự")]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Lương tối thiểu không hợp lệ")]
        public decimal? SalaryMin { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Lương tối đa không hợp lệ")]
        public decimal? SalaryMax { get; set; }

        [Range(1, 5, ErrorMessage = "Loại lương không hợp lệ")]
        public int? SalaryType { get; set; }

        [StringLength(3000, MinimumLength = 10, ErrorMessage = "Yêu cầu phải từ 10 đến 3000 ký tự")]
        public string? Requirements { get; set; }

        [Required(ErrorMessage = "Giờ bắt đầu làm việc là bắt buộc")]
        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$", ErrorMessage = "Giờ bắt đầu không đúng định dạng HH:mm")]
        public string WorkHourStart { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giờ kết thúc làm việc là bắt buộc")]
        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$", ErrorMessage = "Giờ kết thúc không đúng định dạng HH:mm")]
        public string WorkHourEnd { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày hết hạn là bắt buộc")]
        [RegularExpression(@"^(0[1-9]|[12][0-9]|3[01])/(0[1-9]|1[0-2])/\d{4}$",
            ErrorMessage = "Ngày hết hạn phải theo định dạng dd/MM/yyyy")]
        public string? ExpiredAt { get; set; }

        [Required(ErrorMessage = "Tỉnh/Thành phố là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Tỉnh/Thành phố không hợp lệ")]
        public int ProvinceId { get; set; }

        [Required(ErrorMessage = "Quận/Huyện là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Quận/Huyện không hợp lệ")]
        public int DistrictId { get; set; }

        [Required(ErrorMessage = "Phường/Xã là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Phường/Xã không hợp lệ")]
        public int WardId { get; set; }

        [Required(ErrorMessage = "Địa chỉ chi tiết không được để trống")]
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
        public string DetailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Danh mục công việc là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Danh mục công việc không hợp lệ")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Số điện thoại liên hệ là bắt buộc")]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$",
            ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam")]
        public string PhoneContact { get; set; } = string.Empty;

        public List<IFormFile>? Images { get; set; }
    }

    public class EmployerPostUpdateDto
    {
        [StringLength(120, MinimumLength = 5,
            ErrorMessage = "Tiêu đề phải từ 5 đến 120 ký tự")]
        public string? Title { get; set; }

        [StringLength(5000, MinimumLength = 20,
            ErrorMessage = "Mô tả phải từ 20 đến 5000 ký tự")]
        public string? Description { get; set; }

        [Range(0, double.MaxValue,
            ErrorMessage = "Lương tối thiểu không được nhỏ hơn 0")]
        public decimal? SalaryMin { get; set; }

        [Range(0, double.MaxValue,
            ErrorMessage = "Lương tối đa không được nhỏ hơn 0")]
        public decimal? SalaryMax { get; set; }

        [Range(1, 5,
            ErrorMessage = "Loại lương không hợp lệ")]
        public int? SalaryType { get; set; }

        [StringLength(3000, MinimumLength = 10,
            ErrorMessage = "Yêu cầu công việc phải từ 10 đến 3000 ký tự")]
        public string? Requirements { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$",
            ErrorMessage = "Giờ bắt đầu phải đúng định dạng HH:mm")]
        public string? WorkHourStart { get; set; }

        [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$",
            ErrorMessage = "Giờ kết thúc phải đúng định dạng HH:mm")]
        public string? WorkHourEnd { get; set; }

        [RegularExpression(
            @"^(0[1-9]|[12][0-9]|3[01])/(0[1-9]|1[0-2])/\d{4}$",
            ErrorMessage = "Ngày hết hạn phải theo định dạng dd/MM/yyyy")]
        public string? ExpiredAt { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Tỉnh/Thành phố không hợp lệ")]
        public int? ProvinceId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Quận/Huyện không hợp lệ")]
        public int? DistrictId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Phường/Xã không hợp lệ")]
        public int? WardId { get; set; }

        [StringLength(255,
            ErrorMessage = "Địa chỉ chi tiết tối đa 255 ký tự")]
        public string? DetailAddress { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Danh mục công việc không hợp lệ")]
        public int? CategoryID { get; set; }

        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$",
            ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam")]
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
