using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class EmployerPost
{
    [Key]
    [Column("EmployerPostID")]
    public int EmployerPostId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Salary { get; set; }

    public string? Requirements { get; set; }

    [StringLength(50)]
    public string? WorkHours { get; set; }

    [StringLength(255)]
    public string? Location { get; set; }

    [Column("CategoryID")]
    public int? CategoryId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? PhoneContact { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [ForeignKey("CategoryId")]
    [InverseProperty("EmployerPosts")]
    public virtual Category? Category { get; set; }

    [InverseProperty("EmployerPost")]
    public virtual ICollection<EmployerCandidatesList> EmployerCandidatesLists { get; set; } = new List<EmployerCandidatesList>();

    [InverseProperty("EmployerPost")]
    public virtual ICollection<EmployerShortlistedCandidate> EmployerShortlistedCandidates { get; set; } = new List<EmployerShortlistedCandidate>();

    [InverseProperty("EmployerPost")]
    public virtual ICollection<JobSeekerShortlistedJob> JobSeekerShortlistedJobs { get; set; } = new List<JobSeekerShortlistedJob>();

    [InverseProperty("EmployerPost")]
    public virtual ICollection<JobSeekerSubmission> JobSeekerSubmissions { get; set; } = new List<JobSeekerSubmission>();

    [InverseProperty("EmployerPost")]
    public virtual ICollection<PostReport> PostReports { get; set; } = new List<PostReport>();

    [ForeignKey("UserId")]
    [InverseProperty("EmployerPosts")]
    public virtual User User { get; set; } = null!;
}
