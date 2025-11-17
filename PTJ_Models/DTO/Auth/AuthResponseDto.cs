namespace PTJ_Models.DTO.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = default!;
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; } = default!;
        public object User { get; set; } = default!;
        public string? Warning { get; set; }
        public string? Role { get; set; }
    }
}
