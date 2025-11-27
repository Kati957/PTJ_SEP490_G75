using System.Text.Json.Serialization;

public class EmployerProfileDto
    {
    public int ProfileId { get; set; }
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Website { get; set; }

    //  Không trả lại ID
    [JsonIgnore] public int ProvinceId { get; set; }
    [JsonIgnore] public int DistrictId { get; set; }
    [JsonIgnore] public int WardId { get; set; }

    //  Trả chuỗi location gộp
    public string Location { get; set; } = string.Empty;
    }
