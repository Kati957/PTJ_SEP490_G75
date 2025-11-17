using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class EmployerProfile
{
    public int ProfileId { get; set; }

    public int UserId { get; set; }

    public string DisplayName { get; set; } = null!;

    public string? Description { get; set; }

    public string? AvatarUrl { get; set; }

    public string? ContactName { get; set; }

    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }

    public string? Website { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? AvatarPublicId { get; set; }

    public bool IsAvatarHidden { get; set; }

    public int ProvinceId { get; set; }

    public int DistrictId { get; set; }

    public int WardId { get; set; }

    public string? FullLocation { get; set; }

    public virtual User User { get; set; } = null!;
}
