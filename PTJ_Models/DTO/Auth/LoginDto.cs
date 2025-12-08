using System.ComponentModel.DataAnnotations;

namespace PTJ_Models.DTO.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Please enter a valid email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email")]
        public string UsernameOrEmail { get; set; } = default!;

        [Required(ErrorMessage = "Please enter password")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = default!;
        public string? DeviceInfo { get; set; }
    }
}
