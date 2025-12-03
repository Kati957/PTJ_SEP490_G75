namespace PTJ_Models.DTO.Auth
{
    public class AuthResponseDto
    {
        public bool Success { get; set; } = true;

        public string? Message { get; set; }

        
        public bool RequiresApproval { get; set; } = false;

       
        public string? AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? RefreshToken { get; set; }

     
        public object? User { get; set; }
        public string? Role { get; set; }

       
        public string? Warning { get; set; }
    }
}
