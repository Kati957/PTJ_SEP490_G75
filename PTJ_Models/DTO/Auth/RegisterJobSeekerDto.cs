using System.ComponentModel.DataAnnotations;

public class RegisterJobSeekerDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = default!;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = default!;

    [Required(ErrorMessage = "FullName is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "FullName must be between 2 and 100 characters.")]
    public string FullName { get; set; } = default!;
}
