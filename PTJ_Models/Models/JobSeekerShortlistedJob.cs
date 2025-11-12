using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Table("JobSeeker_ShortlistedJobs")]
[Index("JobSeekerId", Name = "IX_JobSeeker_ShortlistedJobs_JobSeeker")]
public partial class JobSeekerShortlistedJob
{
    [Key]
    [Column("ShortlistID")]
    public int ShortlistId { get; set; }

    [Column("JobSeekerID")]
    public int JobSeekerId { get; set; }

    [Column("EmployerPostID")]
    public int EmployerPostId { get; set; }

    public string? Note { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime AddedAt { get; set; }

    [ForeignKey("EmployerPostId")]
    [InverseProperty("JobSeekerShortlistedJobs")]
    public virtual EmployerPost EmployerPost { get; set; } = null!;

    [ForeignKey("JobSeekerId")]
    [InverseProperty("JobSeekerShortlistedJobs")]
    public virtual User JobSeeker { get; set; } = null!;
}
