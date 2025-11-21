using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class JobSeekerCv
{
    public int Cvid { get; set; }

    public int JobSeekerId { get; set; }

    public string Cvtitle { get; set; } = null!;

    public string? SkillSummary { get; set; }

    public string? Skills { get; set; }

    public string? PreferredJobType { get; set; }

    public string? PreferredLocation { get; set; }

    public string? ContactPhone { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int ProvinceId { get; set; }

    public int DistrictId { get; set; }

    public int WardId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual User JobSeeker { get; set; } = null!;

    public virtual ICollection<JobSeekerSubmission> JobSeekerSubmissions { get; set; } = new List<JobSeekerSubmission>();
}
