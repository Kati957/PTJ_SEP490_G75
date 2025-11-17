using System.Text.Json.Serialization;

public class AdminUserDto
    {
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public string? AvatarUrl { get; set; }
    }

public class AdminUserDetailDto : AdminUserDto
    {
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public int? BirthYear { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }

    [JsonIgnore]
    public int ProvinceId { get; set; }

    [JsonIgnore]
    public int DistrictId { get; set; }

    [JsonIgnore]
    public int WardId { get; set; }
    public string? ProvinceName { get; set; }
    public string? DistrictName { get; set; }
    public string? WardName { get; set; }

    public string? CompanyName { get; set; }
    public string? Website { get; set; }
    }
