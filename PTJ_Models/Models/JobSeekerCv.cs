using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Table("JobSeekerCVs")]
public partial class JobSeekerCv
{
    [Key]
    [Column("CVID")]
    public int Cvid { get; set; }

    [Column("JobSeekerID")]
    public int JobSeekerId { get; set; }

    [Column("CVTitle")]
    [StringLength(150)]
    public string Cvtitle { get; set; } = null!;

    public string? SkillSummary { get; set; }

    public string? Skills { get; set; }

    [StringLength(100)]
    public string? PreferredJobType { get; set; }

    [StringLength(255)]
    public string? PreferredLocation { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? ContactPhone { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("JobSeekerId")]
    [InverseProperty("JobSeekerCvs")]
    public virtual User JobSeeker { get; set; } = null!;

    [InverseProperty("Cv")]
    public virtual ICollection<JobSeekerSubmission> JobSeekerSubmissions { get; set; } = new List<JobSeekerSubmission>();
}
