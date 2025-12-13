using System.ComponentModel.DataAnnotations;

public class JobSeekerCvCreateDto
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề CV")]
    public string CvTitle { get; set; } = null!;

    [MaxLength(1000, ErrorMessage = "Tóm tắt kỹ năng quá dài (tối đa 1000 ký tự)")]
    public string? SkillSummary { get; set; }

    public string? Skills { get; set; }

    public string? PreferredJobType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố")]
    public int ProvinceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Quận/Huyện")]
    public int DistrictId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Phường/Xã")]
    public int WardId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^[0-9]{9,11}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? ContactPhone { get; set; }
}
