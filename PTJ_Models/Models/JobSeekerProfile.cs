using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class JobSeekerProfile
{
    public int ProfileId { get; set; }

    public int UserId { get; set; }

    public string? FullName { get; set; }

    public string? Gender { get; set; }

    public int? BirthYear { get; set; }

    public string? ProfilePicture { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? ProfilePicturePublicId { get; set; }

    public bool IsPictureHidden { get; set; }

    public string? ContactPhone { get; set; }

    public int ProvinceId { get; set; }

    public int DistrictId { get; set; }

    public int WardId { get; set; }

    public string? FullLocation { get; set; }

    public virtual User User { get; set; } = null!;
}
