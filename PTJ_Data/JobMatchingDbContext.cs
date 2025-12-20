using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class JobMatchingDbContext : DbContext
{
    public JobMatchingDbContext()
    {
    }

    public JobMatchingDbContext(DbContextOptions<JobMatchingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AiEmbeddingStatus> AiEmbeddingStatuses { get; set; }

    public virtual DbSet<AiMatchSuggestion> AiMatchSuggestions { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<EmployerCandidatesList> EmployerCandidatesLists { get; set; }

    public virtual DbSet<EmployerFollower> EmployerFollowers { get; set; }

    public virtual DbSet<EmployerPlan> EmployerPlans { get; set; }

    public virtual DbSet<EmployerPost> EmployerPosts { get; set; }

    public virtual DbSet<EmployerProfile> EmployerProfiles { get; set; }

    public virtual DbSet<EmployerRegistrationRequest> EmployerRegistrationRequests { get; set; }

    public virtual DbSet<EmployerShortlistedCandidate> EmployerShortlistedCandidates { get; set; }

    public virtual DbSet<EmployerSubscription> EmployerSubscriptions { get; set; }

    public virtual DbSet<EmployerTransaction> EmployerTransactions { get; set; }

    public virtual DbSet<ExternalLogin> ExternalLogins { get; set; }

    public virtual DbSet<FavoritePost> FavoritePosts { get; set; }

    public virtual DbSet<GoogleEmployerRequest> GoogleEmployerRequests { get; set; }

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

    public virtual DbSet<NotificationTemplate> NotificationTemplates { get; set; }

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
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("server =ADMIN-PC\\SQLEXPRESS; database = JobMatching_DB;uid=sa;pwd=123; TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiEmbeddingStatus>(entity =>
        {
            entity.HasKey(e => e.EmbeddingId);

            entity.ToTable("AI_EmbeddingStatus");

            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "IX_AI_EmbeddingStatus_Entity");

            entity.Property(e => e.EmbeddingId).HasColumnName("EmbeddingID");
            entity.Property(e => e.ContentHash)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.EntityId).HasColumnName("EntityID");
            entity.Property(e => e.EntityType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ErrorMsg).HasMaxLength(1000);
            entity.Property(e => e.Model)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PineconeId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<AiMatchSuggestion>(entity =>
        {
            entity.HasKey(e => e.SuggestionId);

            entity.ToTable("AI_MatchSuggestions");

            entity.HasIndex(e => new { e.SourceType, e.SourceId }, "IX_AI_MatchSuggestions_Source");

            entity.HasIndex(e => new { e.TargetType, e.TargetId }, "IX_AI_MatchSuggestions_Target");

            entity.Property(e => e.SuggestionId).HasColumnName("SuggestionID");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.SourceId).HasColumnName("SourceID");
            entity.Property(e => e.SourceType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TargetId).HasColumnName("TargetID");
            entity.Property(e => e.TargetType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Name, "IX_Categories_Name").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryGroup).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.EvtokenId);

            entity.Property(e => e.EvtokenId).HasColumnName("EVTokenID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.Token)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UsedAt).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.EmailVerificationTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmailVerificationTokens_Users");
        });

        modelBuilder.Entity<EmployerCandidatesList>(entity =>
        {
            entity.HasKey(e => e.CandidateListId);

            entity.ToTable("Employer_CandidatesList");

            entity.Property(e => e.CandidateListId).HasColumnName("CandidateListID");
            entity.Property(e => e.ApplicationDate).HasColumnType("datetime");
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.EmployerCandidatesLists)
                .HasForeignKey(d => d.EmployerPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployerCandidatesList_Post");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerCandidatesLists)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployerCandidatesList_JobSeeker");
        });

        modelBuilder.Entity<EmployerFollower>(entity =>
        {
            entity.HasKey(e => e.FollowId);

            entity.HasIndex(e => new { e.JobSeekerId, e.EmployerId }, "IX_EmployerFollowers").IsUnique();

            entity.Property(e => e.FollowId).HasColumnName("FollowID");
            entity.Property(e => e.EmployerId).HasColumnName("EmployerID");
            entity.Property(e => e.FollowDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");

            entity.HasOne(d => d.Employer).WithMany(p => p.EmployerFollowerEmployers)
                .HasForeignKey(d => d.EmployerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployerFollowers_Employer");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerFollowerJobSeekers)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployerFollowers_JobSeeker");
        });

        modelBuilder.Entity<EmployerPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PlanName).HasMaxLength(50);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<EmployerPost>(entity =>
        {
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiredAt).HasColumnType("datetime");
            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.PhoneContact)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SalaryMax).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SalaryMin).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.WorkHours).HasMaxLength(50);

            entity.HasOne(d => d.Category).WithMany(p => p.EmployerPosts)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_EmployerPosts_Categories");

            entity.HasOne(d => d.User).WithMany(p => p.EmployerPosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployerPosts_Users");
        });

        modelBuilder.Entity<EmployerProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId);

            entity.HasIndex(e => e.UserId, "UQ_EmployerProfiles_UserId").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("ProfileID");
            entity.Property(e => e.AvatarPublicId)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ContactEmail)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ContactName).HasMaxLength(100);
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.FullLocation).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Website)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithOne(p => p.EmployerProfile)
                .HasForeignKey<EmployerProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployerProfiles_Users");
        });

        modelBuilder.Entity<EmployerRegistrationRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CompanyName).HasMaxLength(255);
            entity.Property(e => e.ContactEmail)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ContactPerson).HasMaxLength(255);
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ReviewedAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Website)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EmployerShortlistedCandidate>(entity =>
        {
            entity.HasKey(e => e.ShortlistId);

            entity.ToTable("Employer_ShortlistedCandidates");

            entity.Property(e => e.ShortlistId).HasColumnName("ShortlistID");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EmployerId).HasColumnName("EmployerID");
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");

            entity.HasOne(d => d.Employer).WithMany(p => p.EmployerShortlistedCandidateEmployers)
                .HasForeignKey(d => d.EmployerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShortlistedCandidates_Employer");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.EmployerShortlistedCandidates)
                .HasForeignKey(d => d.EmployerPostId)
                .HasConstraintName("FK_ShortlistedCandidates_Post");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerShortlistedCandidateJobSeekers)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShortlistedCandidates_JobSeeker");
        });

        modelBuilder.Entity<EmployerSubscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId);

            entity.HasIndex(e => e.UserId, "IX_EmployerSubscriptions_User");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Plan).WithMany(p => p.EmployerSubscriptions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployerSubscriptions_Plans");

            entity.HasOne(d => d.User).WithMany(p => p.EmployerSubscriptions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployerSubscriptions_Users");
        });

        modelBuilder.Entity<EmployerTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaidAt).HasColumnType("datetime");
            entity.Property(e => e.PayOsorderCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PayOSOrderCode");
            entity.Property(e => e.QrCodeUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.QrExpiredAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Plan).WithMany(p => p.EmployerTransactions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployerTransactions_Plans");

            entity.HasOne(d => d.User).WithMany(p => p.EmployerTransactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployerTransactions_Users");
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasIndex(e => new { e.Provider, e.ProviderKey }, "IX_ExternalLogins_Provider_User").IsUnique();

            entity.Property(e => e.ExternalLoginId).HasColumnName("ExternalLoginID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ProviderKey)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.ExternalLogins)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExternalLogins_Users");
        });

        modelBuilder.Entity<FavoritePost>(entity =>
        {
            entity.HasKey(e => e.FavoriteId);

            entity.HasIndex(e => new { e.UserId, e.PostId, e.PostType }, "IX_FavoritePosts_Unique").IsUnique();

            entity.Property(e => e.FavoriteId).HasColumnName("FavoriteID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PostId).HasColumnName("PostID");
            entity.Property(e => e.PostType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.FavoritePosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FavoritePosts_Users");
        });

        modelBuilder.Entity<GoogleEmployerRequest>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_GoogleEmployerRequests_User").IsUnique();

            entity.HasIndex(e => e.UserId, "UQ_GoogleEmployerRequests_UserId").IsUnique();

            entity.Property(e => e.AdminNote).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.PictureUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ReviewedAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.User).WithOne(p => p.GoogleEmployerRequest)
                .HasForeignKey<GoogleEmployerRequest>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GoogleEmployerRequests_Users");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "IX_Images_Entity");

            entity.Property(e => e.ImageId).HasColumnName("ImageID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EntityId).HasColumnName("EntityID");
            entity.Property(e => e.EntityType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Format)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PublicId)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Url)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<JobSeekerCv>(entity =>
        {
            entity.HasKey(e => e.Cvid);

            entity.ToTable("JobSeekerCVs");

            entity.Property(e => e.Cvid).HasColumnName("CVID");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Cvtitle)
                .HasMaxLength(150)
                .HasColumnName("CVTitle");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");
            entity.Property(e => e.PreferredJobType).HasMaxLength(100);
            entity.Property(e => e.PreferredLocation).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.JobSeekerCvs)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeekerCVs_Users");
        });

        modelBuilder.Entity<JobSeekerPost>(entity =>
        {
            entity.Property(e => e.JobSeekerPostId).HasColumnName("JobSeekerPostID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.PhoneContact)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PreferredLocation).HasMaxLength(255);
            entity.Property(e => e.PreferredWorkHours).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Category).WithMany(p => p.JobSeekerPosts)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_JobSeekerPosts_Categories");

            entity.HasOne(d => d.User).WithMany(p => p.JobSeekerPosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeekerPosts_Users");
        });

        modelBuilder.Entity<JobSeekerProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId);

            entity.HasIndex(e => e.UserId, "UQ_JobSeekerProfiles_UserId").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("ProfileID");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FullLocation).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.ProfilePicture)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ProfilePicturePublicId)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithOne(p => p.JobSeekerProfile)
                .HasForeignKey<JobSeekerProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeekerProfiles_Users");
        });

        modelBuilder.Entity<JobSeekerShortlistedJob>(entity =>
        {
            entity.HasKey(e => e.ShortlistId);

            entity.ToTable("JobSeeker_ShortlistedJobs");

            entity.HasIndex(e => new { e.JobSeekerId, e.EmployerPostId }, "IX_JobSeekerShortlist").IsUnique();

            entity.Property(e => e.ShortlistId).HasColumnName("ShortlistID");
            entity.Property(e => e.AddedAt).HasColumnType("datetime");
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.JobSeekerShortlistedJobs)
                .HasForeignKey(d => d.EmployerPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeekerShortlistedJobs_Post");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.JobSeekerShortlistedJobs)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeekerShortlistedJobs_JobSeeker");
        });

        modelBuilder.Entity<JobSeekerSubmission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId);

            entity.ToTable("JobSeeker_Submissions");

            entity.HasIndex(e => new { e.JobSeekerId, e.EmployerPostId }, "IX_JobSeeker_Submissions_Unique").IsUnique();

            entity.Property(e => e.SubmissionId).HasColumnName("SubmissionID");
            entity.Property(e => e.AppliedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Cvid).HasColumnName("CVID");
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Cv).WithMany(p => p.JobSeekerSubmissions)
                .HasForeignKey(d => d.Cvid)
                .HasConstraintName("FK_JobSeekerSubmissions_CV");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.JobSeekerSubmissions)
                .HasForeignKey(d => d.EmployerPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeekerSubmissions_Post");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.JobSeekerSubmissions)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeekerSubmissions_JobSeeker");
        });

        modelBuilder.Entity<LocationCache>(entity =>
        {
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId);

            entity.Property(e => e.AttemptId).HasColumnName("AttemptID");
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("IPAddress");
            entity.Property(e => e.Message).HasMaxLength(255);
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.UsernameOrEmail)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.LoginAttempts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_LoginAttempts_Users");
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.Property(e => e.NewsId).HasColumnName("NewsID");
            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Admin).WithMany(p => p.News)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_News_Users");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RelatedItemId).HasColumnName("RelatedItemID");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_Users");
        });

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId);

            entity.Property(e => e.TemplateId).HasColumnName("TemplateID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TitleTemplate).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.TokenId);

            entity.Property(e => e.TokenId).HasColumnName("TokenID");
            entity.Property(e => e.Expiration).HasColumnType("datetime");
            entity.Property(e => e.Token)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PasswordResetTokens_Users");
        });

        modelBuilder.Entity<PostReport>(entity =>
        {
            entity.Property(e => e.PostReportId).HasColumnName("PostReportID");
            entity.Property(e => e.AffectedPostType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReportType).HasMaxLength(255);
            entity.Property(e => e.ReporterId).HasColumnName("ReporterID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TargetUserId).HasColumnName("TargetUserID");

            entity.HasOne(d => d.Reporter).WithMany(p => p.PostReportReporters)
                .HasForeignKey(d => d.ReporterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PostReports_Reporter");

            entity.HasOne(d => d.TargetUser).WithMany(p => p.PostReportTargetUsers)
                .HasForeignKey(d => d.TargetUserId)
                .HasConstraintName("FK_PostReports_TargetUser");
        });

        modelBuilder.Entity<PostReportSolved>(entity =>
        {
            entity.HasKey(e => e.SolvedPostReportId);

            entity.ToTable("PostReport_Solved");

            entity.Property(e => e.SolvedPostReportId).HasColumnName("SolvedPostReportID");
            entity.Property(e => e.ActionTaken)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.AffectedPostId).HasColumnName("AffectedPostID");
            entity.Property(e => e.AffectedPostType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.AffectedUserId).HasColumnName("AffectedUserID");
            entity.Property(e => e.AppliedAction).HasMaxLength(255);
            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.PostReportId).HasColumnName("PostReportID");
            entity.Property(e => e.SolvedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Admin).WithMany(p => p.PostReportSolvedAdmins)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PostReportSolved_Admin");

            entity.HasOne(d => d.AffectedUser).WithMany(p => p.PostReportSolvedAffectedUsers)
                .HasForeignKey(d => d.AffectedUserId)
                .HasConstraintName("FK_PostReportSolved_TargetUser");

            entity.HasOne(d => d.Notification).WithMany(p => p.PostReportSolveds)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("FK_PostReportSolved_Notification");

            entity.HasOne(d => d.PostReport).WithMany(p => p.PostReportSolveds)
                .HasForeignKey(d => d.PostReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PostReportSolved_PostReport");
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.Property(e => e.RatingId).HasColumnName("RatingID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RateeId).HasColumnName("RateeID");
            entity.Property(e => e.RaterId).HasColumnName("RaterID");
            entity.Property(e => e.RatingValue).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.SubmissionId).HasColumnName("SubmissionID");

            entity.HasOne(d => d.Ratee).WithMany(p => p.RatingRatees)
                .HasForeignKey(d => d.RateeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ratings_Ratee");

            entity.HasOne(d => d.Rater).WithMany(p => p.RatingRaters)
                .HasForeignKey(d => d.RaterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ratings_Rater");

            entity.HasOne(d => d.Submission).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.SubmissionId)
                .HasConstraintName("FK_Ratings_Submission");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(e => e.RefreshTokenId).HasColumnName("RefreshTokenID");
            entity.Property(e => e.DeviceInfo).HasMaxLength(255);
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("IPAddress");
            entity.Property(e => e.IssuedAt).HasColumnType("datetime");
            entity.Property(e => e.JwtId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RevokedAt).HasColumnType("datetime");
            entity.Property(e => e.Token)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleName, "IX_Roles_RoleName").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SystemReport>(entity =>
        {
            entity.Property(e => e.SystemReportId).HasColumnName("SystemReportID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.SystemReports)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SystemReports_Users");
        });

        modelBuilder.Entity<SystemStatistic>(entity =>
        {
            entity.HasKey(e => e.StatId);

            entity.Property(e => e.StatId).HasColumnName("StatID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.StatDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "IX_Users_Email").IsUnique();

            entity.HasIndex(e => e.Username, "IX_Users_Username").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.LockoutEnd).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserRoles_Roles"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserRoles_Users"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("UserRoles");
                        j.HasIndex(new[] { "UserId", "RoleId" }, "IX_UserRoles_UserID_RoleID").IsUnique();
                        j.IndexerProperty<int>("UserId").HasColumnName("UserID");
                        j.IndexerProperty<int>("RoleId").HasColumnName("RoleID");
                    });
        });

        modelBuilder.Entity<UserActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId);

            entity.ToTable("UserActivityLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.ActivityType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("IPAddress");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.UserActivityLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserActivityLog_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
