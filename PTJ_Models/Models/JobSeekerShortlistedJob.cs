using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class JobSeekerShortlistedJob
{
    public int ShortlistId { get; set; }

    public int JobSeekerId { get; set; }

    public int EmployerPostId { get; set; }

    public string? Note { get; set; }

    public DateTime AddedAt { get; set; }

    public virtual EmployerPost EmployerPost { get; set; } = null!;

    public virtual User JobSeeker { get; set; } = null!;
}
