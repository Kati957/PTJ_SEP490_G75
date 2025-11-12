using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Index("Email", Name = "UQ_Users_Email", IsUnique = true)]
[Index("Username", Name = "UQ_Users_Username", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(50)]
    public string Username { get; set; } = null!;

    [StringLength(255)]
    public string? PasswordHash { get; set; }

    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string? PhoneNumber { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public bool IsVerified { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastLogin { get; set; }

    public int FailedLoginCount { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LockoutEnd { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

    [InverseProperty("JobSeeker")]
    public virtual ICollection<EmployerCandidatesList> EmployerCandidatesLists { get; set; } = new List<EmployerCandidatesList>();

    [InverseProperty("Employer")]
    public virtual ICollection<EmployerFollower> EmployerFollowerEmployers { get; set; } = new List<EmployerFollower>();

    [InverseProperty("JobSeeker")]
    public virtual ICollection<EmployerFollower> EmployerFollowerJobSeekers { get; set; } = new List<EmployerFollower>();

    [InverseProperty("User")]
    public virtual ICollection<EmployerPost> EmployerPosts { get; set; } = new List<EmployerPost>();

    [InverseProperty("User")]
    public virtual EmployerProfile? EmployerProfile { get; set; }

    [InverseProperty("Employer")]
    public virtual ICollection<EmployerShortlistedCandidate> EmployerShortlistedCandidateEmployers { get; set; } = new List<EmployerShortlistedCandidate>();

    [InverseProperty("JobSeeker")]
    public virtual ICollection<EmployerShortlistedCandidate> EmployerShortlistedCandidateJobSeekers { get; set; } = new List<EmployerShortlistedCandidate>();

    [InverseProperty("User")]
    public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

    [InverseProperty("User")]
    public virtual ICollection<FavoritePost> FavoritePosts { get; set; } = new List<FavoritePost>();

    [InverseProperty("JobSeeker")]
    public virtual ICollection<JobSeekerCv> JobSeekerCvs { get; set; } = new List<JobSeekerCv>();

    [InverseProperty("User")]
    public virtual ICollection<JobSeekerPost> JobSeekerPosts { get; set; } = new List<JobSeekerPost>();

    [InverseProperty("User")]
    public virtual JobSeekerProfile? JobSeekerProfile { get; set; }

    [InverseProperty("JobSeeker")]
    public virtual ICollection<JobSeekerShortlistedJob> JobSeekerShortlistedJobs { get; set; } = new List<JobSeekerShortlistedJob>();

    [InverseProperty("JobSeeker")]
    public virtual ICollection<JobSeekerSubmission> JobSeekerSubmissions { get; set; } = new List<JobSeekerSubmission>();

    [InverseProperty("User")]
    public virtual ICollection<LoginAttempt> LoginAttempts { get; set; } = new List<LoginAttempt>();

    [InverseProperty("Admin")]
    public virtual ICollection<News> News { get; set; } = new List<News>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    [InverseProperty("Reporter")]
    public virtual ICollection<PostReport> PostReportReporters { get; set; } = new List<PostReport>();

    [InverseProperty("Admin")]
    public virtual ICollection<PostReportSolved> PostReportSolvedAdmins { get; set; } = new List<PostReportSolved>();

    [InverseProperty("AffectedUser")]
    public virtual ICollection<PostReportSolved> PostReportSolvedAffectedUsers { get; set; } = new List<PostReportSolved>();

    [InverseProperty("TargetUser")]
    public virtual ICollection<PostReport> PostReportTargetUsers { get; set; } = new List<PostReport>();

    [InverseProperty("Ratee")]
    public virtual ICollection<Rating> RatingRatees { get; set; } = new List<Rating>();

    [InverseProperty("Rater")]
    public virtual ICollection<Rating> RatingRaters { get; set; } = new List<Rating>();

    [InverseProperty("User")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [InverseProperty("User")]
    public virtual ICollection<SystemReport> SystemReports { get; set; } = new List<SystemReport>();

    [InverseProperty("User")]
    public virtual ICollection<UserActivityLog> UserActivityLogs { get; set; } = new List<UserActivityLog>();

    [ForeignKey("UserId")]
    [InverseProperty("Users")]
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
