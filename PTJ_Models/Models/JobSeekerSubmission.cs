using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class JobSeekerSubmission
{
    public int SubmissionId { get; set; }

    public int JobSeekerId { get; set; }

    public int EmployerPostId { get; set; }

    public DateTime AppliedAt { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? Cvid { get; set; }

    public virtual JobSeekerCv? Cv { get; set; }

    public virtual EmployerPost EmployerPost { get; set; } = null!;

    public virtual User JobSeeker { get; set; } = null!;

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}
