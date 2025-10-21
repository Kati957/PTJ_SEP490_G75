using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class EmployerPost
{
    public int EmployerPostId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? Salary { get; set; }

    public string? Requirements { get; set; }

    public string? WorkHours { get; set; }

    public string? Location { get; set; }

    public int? CategoryId { get; set; }

    public string? PhoneContact { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual Category? Category { get; set; }

    public virtual ICollection<EmployerCandidatesList> EmployerCandidatesLists { get; set; } = new List<EmployerCandidatesList>();

    public virtual ICollection<JobSeekerSubmission> JobSeekerSubmissions { get; set; } = new List<JobSeekerSubmission>();

    public virtual ICollection<PostReport> PostReports { get; set; } = new List<PostReport>();

    public virtual User User { get; set; } = null!;
}
