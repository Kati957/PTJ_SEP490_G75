using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string Role { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<EmployerFollower> EmployerFollowerEmployers { get; set; } = new List<EmployerFollower>();

    public virtual ICollection<EmployerFollower> EmployerFollowerJobSeekers { get; set; } = new List<EmployerFollower>();

    public virtual ICollection<EmployerPost> EmployerPosts { get; set; } = new List<EmployerPost>();

    public virtual EmployerProfile? EmployerProfile { get; set; }

    public virtual ICollection<FavoritePost> FavoritePosts { get; set; } = new List<FavoritePost>();

    public virtual ICollection<JobSeekerApplicationList> JobSeekerApplicationLists { get; set; } = new List<JobSeekerApplicationList>();

    public virtual ICollection<JobSeekerPost> JobSeekerPosts { get; set; } = new List<JobSeekerPost>();

    public virtual JobSeekerProfile? JobSeekerProfile { get; set; }

    public virtual ICollection<News> News { get; set; } = new List<News>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PostReportSolved> PostReportSolvedAdmins { get; set; } = new List<PostReportSolved>();

    public virtual ICollection<PostReportSolved> PostReportSolvedAffectedUsers { get; set; } = new List<PostReportSolved>();

    public virtual ICollection<PostReport> PostReports { get; set; } = new List<PostReport>();

    public virtual ICollection<Rating> RatingRatees { get; set; } = new List<Rating>();

    public virtual ICollection<Rating> RatingRaters { get; set; } = new List<Rating>();

    public virtual ICollection<SystemReport> SystemReports { get; set; } = new List<SystemReport>();

    public virtual ICollection<UserActivityLog> UserActivityLogs { get; set; } = new List<UserActivityLog>();
}
