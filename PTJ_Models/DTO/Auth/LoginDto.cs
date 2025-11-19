using System.ComponentModel.DataAnnotations;

namespace PTJ_Models.DTO.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Username or email is required.")]
        [StringLength(100, ErrorMessage = "Username/Email is too long.")]
        public string UsernameOrEmail { get; set; } = default!;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = default!;
        public string? DeviceInfo { get; set; }
    }
}
