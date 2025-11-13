using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class JobSeekerPost
{
    public int JobSeekerPostId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? Age { get; set; }

    public string? Gender { get; set; }

    public string? PreferredWorkHours { get; set; }

    public string? PreferredLocation { get; set; }

    public int? CategoryId { get; set; }

    public string? PhoneContact { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string Status { get; set; } = null!;

    public int? SelectedCvId { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<PostReport> PostReports { get; set; } = new List<PostReport>();

    public virtual User User { get; set; } = null!;
}
