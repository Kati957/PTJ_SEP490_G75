using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Table("Employer_ShortlistedCandidates")]
public partial class EmployerShortlistedCandidate
{
    [Key]
    [Column("ShortlistID")]
    public int ShortlistId { get; set; }

    [Column("EmployerID")]
    public int EmployerId { get; set; }

    [Column("JobSeekerID")]
    public int JobSeekerId { get; set; }

    [Column("EmployerPostID")]
    public int? EmployerPostId { get; set; }

    public string? Note { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime AddedAt { get; set; }

    [ForeignKey("EmployerId")]
    [InverseProperty("EmployerShortlistedCandidateEmployers")]
    public virtual User Employer { get; set; } = null!;

    [ForeignKey("EmployerPostId")]
    [InverseProperty("EmployerShortlistedCandidates")]
    public virtual EmployerPost? EmployerPost { get; set; }

    [ForeignKey("JobSeekerId")]
    [InverseProperty("EmployerShortlistedCandidateJobSeekers")]
    public virtual User JobSeeker { get; set; } = null!;
}
