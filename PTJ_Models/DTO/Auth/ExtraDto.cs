namespace PTJ_Models.DTO.Auth;

public class RefreshDto
{
    public string RefreshToken { get; set; } = default!;
    public string? DeviceInfo { get; set; }
}

public class ResendVerifyDto
{
    public string Email { get; set; } = default!;
}
