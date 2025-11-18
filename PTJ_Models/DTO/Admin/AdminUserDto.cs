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
    // ⭐ JOB SEEKER
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public int? BirthYear { get; set; }

    // ⭐ Cả 2 role đều dùng ContactPhone
    public string? ContactPhone { get; set; }

    // ⭐ Địa chỉ lấy từ Profile.FullLocation
    public string? FullLocation { get; set; }

    // Internal IDs (Admin dùng để map tỉnh/huyện/xã)
    [JsonIgnore] public int ProvinceId { get; set; }
    [JsonIgnore] public int DistrictId { get; set; }
    [JsonIgnore] public int WardId { get; set; }

    // ⭐ Tên tỉnh/huyện/xã (admin UI)
    public string? ProvinceName { get; set; }
    public string? DistrictName { get; set; }
    public string? WardName { get; set; }

    // ⭐ EMPLOYER
    public string? CompanyName { get; set; }
    public string? Website { get; set; }
}
