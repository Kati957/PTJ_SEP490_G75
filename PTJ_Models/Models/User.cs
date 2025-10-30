using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? LastLogin { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public virtual ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

    public virtual ICollection<EmployerCandidatesList> EmployerCandidatesLists { get; set; } = new List<EmployerCandidatesList>();

    public virtual ICollection<EmployerFollower> EmployerFollowerEmployers { get; set; } = new List<EmployerFollower>();

    public virtual ICollection<EmployerFollower> EmployerFollowerJobSeekers { get; set; } = new List<EmployerFollower>();

    public virtual ICollection<EmployerPost> EmployerPosts { get; set; } = new List<EmployerPost>();

    public virtual EmployerProfile? EmployerProfile { get; set; }

    public virtual ICollection<EmployerShortlistedCandidate> EmployerShortlistedCandidateEmployers { get; set; } = new List<EmployerShortlistedCandidate>();

    public virtual ICollection<EmployerShortlistedCandidate> EmployerShortlistedCandidateJobSeekers { get; set; } = new List<EmployerShortlistedCandidate>();

    public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

    public virtual ICollection<FavoritePost> FavoritePosts { get; set; } = new List<FavoritePost>();

    public virtual ICollection<JobSeekerPost> JobSeekerPosts { get; set; } = new List<JobSeekerPost>();

    public virtual JobSeekerProfile? JobSeekerProfile { get; set; }

    public virtual ICollection<JobSeekerShortlistedJob> JobSeekerShortlistedJobs { get; set; } = new List<JobSeekerShortlistedJob>();

    public virtual ICollection<JobSeekerSubmission> JobSeekerSubmissions { get; set; } = new List<JobSeekerSubmission>();

    public virtual ICollection<LoginAttempt> LoginAttempts { get; set; } = new List<LoginAttempt>();

    public virtual ICollection<News> News { get; set; } = new List<News>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual ICollection<PostReport> PostReportReporters { get; set; } = new List<PostReport>();

    public virtual ICollection<PostReportSolved> PostReportSolvedAdmins { get; set; } = new List<PostReportSolved>();

    public virtual ICollection<PostReportSolved> PostReportSolvedAffectedUsers { get; set; } = new List<PostReportSolved>();

    public virtual ICollection<PostReport> PostReportTargetUsers { get; set; } = new List<PostReport>();

    public virtual ICollection<Rating> RatingRatees { get; set; } = new List<Rating>();

    public virtual ICollection<Rating> RatingRaters { get; set; } = new List<Rating>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<SystemReport> SystemReports { get; set; } = new List<SystemReport>();

    public virtual ICollection<UserActivityLog> UserActivityLogs { get; set; } = new List<UserActivityLog>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
