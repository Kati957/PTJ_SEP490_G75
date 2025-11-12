using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PTJ_Models.Models;

namespace PTJ_Data;

public partial class JobMatchingDbContext : DbContext
{
    public JobMatchingDbContext()
    {
    }

    public JobMatchingDbContext(DbContextOptions<JobMatchingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AiContentForEmbedding> AiContentForEmbeddings { get; set; }

    public virtual DbSet<AiEmbeddingStatus> AiEmbeddingStatuses { get; set; }

    public virtual DbSet<AiMatchSuggestion> AiMatchSuggestions { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<EmployerCandidatesList> EmployerCandidatesLists { get; set; }

    public virtual DbSet<EmployerFollower> EmployerFollowers { get; set; }

    public virtual DbSet<EmployerPost> EmployerPosts { get; set; }

    public virtual DbSet<EmployerProfile> EmployerProfiles { get; set; }

    public virtual DbSet<EmployerShortlistedCandidate> EmployerShortlistedCandidates { get; set; }

    public virtual DbSet<ExternalLogin> ExternalLogins { get; set; }

    public virtual DbSet<FavoritePost> FavoritePosts { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<JobSeekerCv> JobSeekerCvs { get; set; }

    public virtual DbSet<JobSeekerPost> JobSeekerPosts { get; set; }

    public virtual DbSet<JobSeekerProfile> JobSeekerProfiles { get; set; }

    public virtual DbSet<JobSeekerShortlistedJob> JobSeekerShortlistedJobs { get; set; }

    public virtual DbSet<JobSeekerSubmission> JobSeekerSubmissions { get; set; }

    public virtual DbSet<LocationCache> LocationCaches { get; set; }

    public virtual DbSet<LoginAttempt> LoginAttempts { get; set; }

    public virtual DbSet<News> News { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<PostReport> PostReports { get; set; }

    public virtual DbSet<PostReportSolved> PostReportSolveds { get; set; }

    public virtual DbSet<Rating> Ratings { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SystemReport> SystemReports { get; set; }

    public virtual DbSet<SystemStatistic> SystemStatistics { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserActivityLog> UserActivityLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiContentForEmbedding>(entity =>
        {
            entity.Property(e => e.LastPreparedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<AiEmbeddingStatus>(entity =>
        {
            entity.Property(e => e.Model).HasDefaultValue("text-embedding-3-large");
            entity.Property(e => e.Status).HasDefaultValue("OK");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.VectorDim).HasDefaultValue(3072);
        });

        modelBuilder.Entity<AiMatchSuggestion>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.EmailVerificationTokens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmailVeri__UserI__40F9A68C");
        });

        modelBuilder.Entity<EmployerCandidatesList>(entity =>
        {
            entity.Property(e => e.ApplicationDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.EmployerCandidatesLists)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employer___Emplo__41EDCAC5");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerCandidatesLists)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employer___JobSe__42E1EEFE");
        });

        modelBuilder.Entity<EmployerFollower>(entity =>
        {
            entity.Property(e => e.FollowDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Employer).WithMany(p => p.EmployerFollowerEmployers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployerF__Emplo__46B27FE2");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerFollowerJobSeekers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployerF__JobSe__47A6A41B");
        });

        modelBuilder.Entity<EmployerPost>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Category).WithMany(p => p.EmployerPosts).HasConstraintName("FK__EmployerP__Categ__489AC854");

            entity.HasOne(d => d.User).WithMany(p => p.EmployerPosts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployerP__UserI__498EEC8D");
        });

        modelBuilder.Entity<EmployerProfile>(entity =>
        {
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithOne(p => p.EmployerProfile)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployerP__UserI__4A8310C6");
        });

        modelBuilder.Entity<EmployerShortlistedCandidate>(entity =>
        {
            entity.Property(e => e.AddedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Employer).WithMany(p => p.EmployerShortlistedCandidateEmployers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employer___Emplo__43D61337");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.EmployerShortlistedCandidates).HasConstraintName("FK__Employer___Emplo__44CA3770");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerShortlistedCandidateJobSeekers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employer___JobSe__45BE5BA9");
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.ExternalLogins)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ExternalL__UserI__4B7734FF");
        });

        modelBuilder.Entity<FavoritePost>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.FavoritePosts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FavoriteP__UserI__4C6B5938");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__Images__7516F4ECFFFACB93");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Entity).WithMany(p => p.Images)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Images_News");
        });

        modelBuilder.Entity<JobSeekerCv>(entity =>
        {
            entity.HasKey(e => e.Cvid).HasName("PK__JobSeeke__A04CFC43604CFD91");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.JobSeekerCvs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeekerCVs_User");
        });

        modelBuilder.Entity<JobSeekerPost>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Category).WithMany(p => p.JobSeekerPosts).HasConstraintName("FK__JobSeeker__Categ__540C7B00");

            entity.HasOne(d => d.User).WithMany(p => p.JobSeekerPosts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__UserI__55009F39");
        });

        modelBuilder.Entity<JobSeekerProfile>(entity =>
        {
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithOne(p => p.JobSeekerProfile)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__UserI__55F4C372");
        });

        modelBuilder.Entity<JobSeekerShortlistedJob>(entity =>
        {
            entity.Property(e => e.AddedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.JobSeekerShortlistedJobs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeeker_ShortlistedJobs_EmployerPost");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.JobSeekerShortlistedJobs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeeker_ShortlistedJobs_JobSeeker");
        });

        modelBuilder.Entity<JobSeekerSubmission>(entity =>
        {
            entity.Property(e => e.AppliedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Applied");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Cv).WithMany(p => p.JobSeekerSubmissions).HasConstraintName("FK_JobSeeker_Submissions_CV");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.JobSeekerSubmissions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__Emplo__503BEA1C");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.JobSeekerSubmissions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__JobSe__51300E55");
        });

        modelBuilder.Entity<LocationCache>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Location__3214EC0782E24E1B");
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.LoginAttempts).HasConstraintName("FK__LoginAtte__UserI__56E8E7AB");
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Admin).WithMany(p => p.News)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__News__AdminID__57DD0BE4");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__UserI__58D1301D");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PasswordR__UserI__59C55456");
        });

        modelBuilder.Entity<PostReport>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.PostReports).HasConstraintName("FK__PostRepor__Emplo__5E8A0973");

            entity.HasOne(d => d.JobSeekerPost).WithMany(p => p.PostReports).HasConstraintName("FK__PostRepor__JobSe__5F7E2DAC");

            entity.HasOne(d => d.Reporter).WithMany(p => p.PostReportReporters)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostRepor__Repor__607251E5");

            entity.HasOne(d => d.TargetUser).WithMany(p => p.PostReportTargetUsers).HasConstraintName("FK__PostRepor__Targe__6166761E");
        });

        modelBuilder.Entity<PostReportSolved>(entity =>
        {
            entity.Property(e => e.AppliedAction).HasDefaultValue(true);
            entity.Property(e => e.SolvedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Admin).WithMany(p => p.PostReportSolvedAdmins)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostRepor__Admin__5AB9788F");

            entity.HasOne(d => d.AffectedUser).WithMany(p => p.PostReportSolvedAffectedUsers).HasConstraintName("FK__PostRepor__Affec__5BAD9CC8");

            entity.HasOne(d => d.Notification).WithMany(p => p.PostReportSolveds).HasConstraintName("FK__PostRepor__Notif__5CA1C101");

            entity.HasOne(d => d.PostReport).WithOne(p => p.PostReportSolved)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostRepor__PostR__5D95E53A");
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Ratee).WithMany(p => p.RatingRatees)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ratings__RateeID__625A9A57");

            entity.HasOne(d => d.Rater).WithMany(p => p.RatingRaters)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ratings__RaterID__634EBE90");

            entity.HasOne(d => d.Submission).WithMany(p => p.Ratings).HasConstraintName("FK__Ratings__Submiss__6442E2C9");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(e => e.IssuedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RefreshTo__UserI__65370702");
        });

        modelBuilder.Entity<SystemReport>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.SystemReports)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SystemRep__UserI__662B2B3B");
        });

        modelBuilder.Entity<SystemStatistic>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__RoleI__681373AD"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__UserI__690797E6"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("UserRoles");
                        j.IndexerProperty<int>("UserId").HasColumnName("UserID");
                        j.IndexerProperty<int>("RoleId").HasColumnName("RoleID");
                    });
        });

        modelBuilder.Entity<UserActivityLog>(entity =>
        {
            entity.HasOne(d => d.User).WithMany(p => p.UserActivityLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserActiv__UserI__671F4F74");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
