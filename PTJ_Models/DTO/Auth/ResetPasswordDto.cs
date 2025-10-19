namespace PTJ_Models.DTO.Auth;

public class ResetPasswordDto
{
    public string Token { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}
