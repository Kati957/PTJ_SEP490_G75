using System.Text.Json.Serialization;

public class JobSeekerProfileDto
    {
    public int ProfileId { get; set; }
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public int? BirthYear { get; set; }
    public string? ProfilePicture { get; set; }
    public string? ContactPhone { get; set; }

    public int ProvinceId { get; set; }
    public int DistrictId { get; set; }
    public int WardId { get; set; }

    public string Location { get; set; } = string.Empty;
    }
