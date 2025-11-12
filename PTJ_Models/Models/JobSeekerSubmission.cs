using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Table("JobSeeker_Submissions")]
public partial class JobSeekerSubmission
{
    [Key]
    [Column("SubmissionID")]
    public int SubmissionId { get; set; }

    [Column("JobSeekerID")]
    public int JobSeekerId { get; set; }

    [Column("EmployerPostID")]
    public int EmployerPostId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime AppliedAt { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    [Column("CVID")]
    public int? Cvid { get; set; }

    [ForeignKey("Cvid")]
    [InverseProperty("JobSeekerSubmissions")]
    public virtual JobSeekerCv? Cv { get; set; }

    [ForeignKey("EmployerPostId")]
    [InverseProperty("JobSeekerSubmissions")]
    public virtual EmployerPost EmployerPost { get; set; } = null!;

    [ForeignKey("JobSeekerId")]
    [InverseProperty("JobSeekerSubmissions")]
    public virtual User JobSeeker { get; set; } = null!;

    [InverseProperty("Submission")]
    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}
