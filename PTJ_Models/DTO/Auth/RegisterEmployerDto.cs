using System.ComponentModel.DataAnnotations;

public class RegisterEmployerDto
{
    //[Required(ErrorMessage = "Tên công ty là bắt buộc.")]
    //[StringLength(200)]
    public string? CompanyName { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? CompanyDescription { get; set; }

    [StringLength(200)]
    public string? ContactPerson { get; set; }

    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    public string ContactPhone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email liên hệ không hợp lệ.")]
    public string? ContactEmail { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    // Tài khoản đăng nhập
    [Required(ErrorMessage = "Email tài khoản là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email tài khoản không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải ít nhất 6 ký tự.")]
    public string Password { get; set; } = string.Empty;
}
