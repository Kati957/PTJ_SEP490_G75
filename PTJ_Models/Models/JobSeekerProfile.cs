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

    public string? Skills { get; set; }

    public string? Experience { get; set; }

    public string? Education { get; set; }

    public string? PreferredJobType { get; set; }

    public string? PreferredLocation { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
