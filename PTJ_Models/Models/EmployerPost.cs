using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class EmployerPost
{
    public int EmployerPostId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Requirements { get; set; }

    public string? WorkHours { get; set; }

    public string? Location { get; set; }

    public int? CategoryId { get; set; }

    public string? PhoneContact { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string Status { get; set; } = null!;

    public int ProvinceId { get; set; }

    public int DistrictId { get; set; }

    public int WardId { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public decimal? SalaryMin { get; set; }

    public decimal? SalaryMax { get; set; }

    public int? SalaryType { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<EmployerCandidatesList> EmployerCandidatesLists { get; set; } = new List<EmployerCandidatesList>();

    public virtual ICollection<EmployerShortlistedCandidate> EmployerShortlistedCandidates { get; set; } = new List<EmployerShortlistedCandidate>();

    public virtual ICollection<JobSeekerShortlistedJob> JobSeekerShortlistedJobs { get; set; } = new List<JobSeekerShortlistedJob>();

    public virtual ICollection<JobSeekerSubmission> JobSeekerSubmissions { get; set; } = new List<JobSeekerSubmission>();

    public virtual User User { get; set; } = null!;
}
