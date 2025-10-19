namespace PTJ_Models.DTO.Auth;

public class RegisterJobSeekerDto
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? FullName { get; set; }
}
