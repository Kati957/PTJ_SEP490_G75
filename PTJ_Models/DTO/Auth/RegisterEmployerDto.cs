using System.ComponentModel.DataAnnotations;

public class RegisterEmployerDto
{
    [Required(ErrorMessage = "DisplayName is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "DisplayName must be between 2 and 100 characters.")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number.")]
    [RegularExpression(@"^(0[0-9]{9})$", ErrorMessage = "ContactPhone must be a 10-digit VN phone number.")]
    public string ContactPhone { get; set; } = string.Empty;

    [Url(ErrorMessage = "Website must be a valid URL.")]
    public string? Website { get; set; }
}
