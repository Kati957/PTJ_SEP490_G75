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

    public virtual DbSet<AiContentForEmbedding> AiContentForEmbeddings { get; set; }

    public virtual DbSet<AiEmbeddingStatus> AiEmbeddingStatuses { get; set; }

    public virtual DbSet<AiMatchSuggestion> AiMatchSuggestions { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<EmployerCandidatesList> EmployerCandidatesLists { get; set; }

    public virtual DbSet<EmployerFollower> EmployerFollowers { get; set; }

    public virtual DbSet<EmployerPost> EmployerPosts { get; set; }

    public virtual DbSet<EmployerProfile> EmployerProfiles { get; set; }

    public virtual DbSet<EmployerRegistrationRequest> EmployerRegistrationRequests { get; set; }

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
        modelBuilder.Entity<AiContentForEmbedding>(entity =>
        {
            entity.HasKey(e => e.ContentId);

            entity.ToTable("AI_ContentForEmbedding");

            entity.Property(e => e.ContentId).HasColumnName("ContentID");
            entity.Property(e => e.EntityId).HasColumnName("EntityID");
            entity.Property(e => e.EntityType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Hash)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.Lang)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LastPreparedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<AiEmbeddingStatus>(entity =>
        {
            entity.HasKey(e => e.EmbeddingId);

            entity.ToTable("AI_EmbeddingStatus");

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
                .IsUnicode(false)
                .HasDefaultValue("text-embedding-3-large");
            entity.Property(e => e.PineconeId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("OK");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.VectorDim).HasDefaultValue(3072);
        });

        modelBuilder.Entity<AiMatchSuggestion>(entity =>
        {
            entity.HasKey(e => e.SuggestionId);

            entity.ToTable("AI_MatchSuggestions");

            entity.Property(e => e.SuggestionId).HasColumnName("SuggestionID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
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
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Type).HasMaxLength(20);
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.EvtokenId);

            entity.HasIndex(e => e.Token, "UQ_EmailVerificationTokens_Token").IsUnique();

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
                .HasConstraintName("FK__EmailVeri__UserI__5AB9788F");
        });

        modelBuilder.Entity<EmployerCandidatesList>(entity =>
        {
            entity.HasKey(e => e.CandidateListId);

            entity.ToTable("Employer_CandidatesList");

            entity.Property(e => e.CandidateListId).HasColumnName("CandidateListID");
            entity.Property(e => e.ApplicationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.EmployerCandidatesLists)
                .HasForeignKey(d => d.EmployerPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employer___Emplo__5BAD9CC8");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerCandidatesLists)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employer___JobSe__5CA1C101");
        });

        modelBuilder.Entity<EmployerFollower>(entity =>
        {
            entity.HasKey(e => e.FollowId);

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
                .HasConstraintName("FK__EmployerF__Emplo__607251E5");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerFollowerJobSeekers)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployerF__JobSe__6166761E");
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
            entity.Property(e => e.Salary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.WorkHours).HasMaxLength(50);

            entity.HasOne(d => d.Category).WithMany(p => p.EmployerPosts)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__EmployerP__Categ__625A9A57");

            entity.HasOne(d => d.User).WithMany(p => p.EmployerPosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployerP__UserI__634EBE90");
        });

        modelBuilder.Entity<EmployerProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId);

            entity.HasIndex(e => e.UserId, "UQ_EmployerProfiles_UserID").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("ProfileID");
            entity.Property(e => e.AvatarPublicId).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(255);
            entity.Property(e => e.ContactEmail).HasMaxLength(100);
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
            entity.Property(e => e.Website).HasMaxLength(255);

            entity.HasOne(d => d.User).WithOne(p => p.EmployerProfile)
                .HasForeignKey<EmployerProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployerP__UserI__65370702");
        });

        modelBuilder.Entity<EmployerRegistrationRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__Employer__33A8517AEFEF64F2");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CompanyName).HasMaxLength(255);
            entity.Property(e => e.ContactEmail).HasMaxLength(255);
            entity.Property(e => e.ContactPerson).HasMaxLength(255);
            entity.Property(e => e.ContactPhone).HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.ReviewedAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.Website).HasMaxLength(255);
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
                .HasConstraintName("FK__Employer___Emplo__5D95E53A");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.EmployerShortlistedCandidates)
                .HasForeignKey(d => d.EmployerPostId)
                .HasConstraintName("FK__Employer___Emplo__5E8A0973");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerShortlistedCandidateJobSeekers)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employer___JobSe__5F7E2DAC");
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasIndex(e => new { e.Provider, e.ProviderKey }, "UQ_ExternalLogins_ProviderKey").IsUnique();

            entity.Property(e => e.ExternalLoginId).HasColumnName("ExternalLoginID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Provider).HasMaxLength(50);
            entity.Property(e => e.ProviderKey).HasMaxLength(200);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.ExternalLogins)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ExternalL__UserI__662B2B3B");
        });

        modelBuilder.Entity<FavoritePost>(entity =>
        {
            entity.HasKey(e => e.FavoriteId);

            entity.Property(e => e.FavoriteId).HasColumnName("FavoriteID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PostId).HasColumnName("PostID");
            entity.Property(e => e.PostType).HasMaxLength(20);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.FavoritePosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FavoriteP__UserI__671F4F74");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__Images__7516F4ECD231F846");

            entity.Property(e => e.ImageId).HasColumnName("ImageID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EntityId).HasColumnName("EntityID");
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.Format).HasMaxLength(20);
            entity.Property(e => e.PublicId).HasMaxLength(255);
            entity.Property(e => e.Url).HasMaxLength(500);
        });

        modelBuilder.Entity<JobSeekerCv>(entity =>
        {
            entity.HasKey(e => e.Cvid).HasName("PK__JobSeeke__A04CFC4307C25B6E");

            entity.ToTable("JobSeekerCVs");

            entity.Property(e => e.Cvid).HasColumnName("CVID");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Cvtitle)
                .HasMaxLength(150)
                .HasColumnName("CVTitle");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");
            entity.Property(e => e.PreferredJobType).HasMaxLength(100);
            entity.Property(e => e.PreferredLocation).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.JobSeekerCvs)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeekerCVs_User");
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
                .HasDefaultValue("Active");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Category).WithMany(p => p.JobSeekerPosts)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__JobSeeker__Categ__6DCC4D03");

            entity.HasOne(d => d.User).WithMany(p => p.JobSeekerPosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__UserI__6EC0713C");
        });

        modelBuilder.Entity<JobSeekerProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId);

            entity.HasIndex(e => e.UserId, "UQ_JobSeekerProfiles_UserID").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("ProfileID");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FullLocation).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.ProfilePicture).HasMaxLength(255);
            entity.Property(e => e.ProfilePicturePublicId).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithOne(p => p.JobSeekerProfile)
                .HasForeignKey<JobSeekerProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__UserI__70A8B9AE");
        });

        modelBuilder.Entity<JobSeekerShortlistedJob>(entity =>
        {
            entity.HasKey(e => e.ShortlistId);

            entity.ToTable("JobSeeker_ShortlistedJobs");

            entity.HasIndex(e => e.JobSeekerId, "IX_JobSeeker_ShortlistedJobs_JobSeeker");

            entity.Property(e => e.ShortlistId).HasColumnName("ShortlistID");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.JobSeekerShortlistedJobs)
                .HasForeignKey(d => d.EmployerPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeeker_ShortlistedJobs_EmployerPost");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.JobSeekerShortlistedJobs)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSeeker_ShortlistedJobs_JobSeeker");
        });

        modelBuilder.Entity<JobSeekerSubmission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId);

            entity.ToTable("JobSeeker_Submissions");

            entity.Property(e => e.SubmissionId).HasColumnName("SubmissionID");
            entity.Property(e => e.AppliedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Cvid).HasColumnName("CVID");
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Applied");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Cv).WithMany(p => p.JobSeekerSubmissions)
                .HasForeignKey(d => d.Cvid)
                .HasConstraintName("FK_JobSeeker_Submissions_CV");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.JobSeekerSubmissions)
                .HasForeignKey(d => d.EmployerPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__Emplo__69FBBC1F");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.JobSeekerSubmissions)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__JobSe__6AEFE058");
        });

        modelBuilder.Entity<LocationCache>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Location__3214EC0777EA9FFF");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId);

            entity.Property(e => e.AttemptId).HasColumnName("AttemptID");
            entity.Property(e => e.DeviceInfo).HasMaxLength(255);
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .HasColumnName("IPAddress");
            entity.Property(e => e.Message).HasMaxLength(255);
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.UsernameOrEmail).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.LoginAttempts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__LoginAtte__UserI__719CDDE7");
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.Property(e => e.NewsId).HasColumnName("NewsID");
            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Admin).WithMany(p => p.News)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__News__AdminID__72910220");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NotificationType).HasMaxLength(50);
            entity.Property(e => e.RelatedItemId).HasColumnName("RelatedItemID");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__UserI__73852659");
        });

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId);

            entity.Property(e => e.TemplateId).HasColumnName("TemplateID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NotificationType).HasMaxLength(50);
            entity.Property(e => e.TitleTemplate).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.TokenId);

            entity.Property(e => e.TokenId).HasColumnName("TokenID");
            entity.Property(e => e.Expiration).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(200);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PasswordR__UserI__74794A92");
        });

        modelBuilder.Entity<PostReport>(entity =>
        {
            entity.Property(e => e.PostReportId).HasColumnName("PostReportID");
            entity.Property(e => e.AffectedPostType).HasMaxLength(50);
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
                .HasConstraintName("FK__PostRepor__Repor__793DFFAF");

            entity.HasOne(d => d.TargetUser).WithMany(p => p.PostReportTargetUsers)
                .HasForeignKey(d => d.TargetUserId)
                .HasConstraintName("FK__PostRepor__Targe__7A3223E8");
        });

        modelBuilder.Entity<PostReportSolved>(entity =>
        {
            entity.HasKey(e => e.SolvedPostReportId);

            entity.ToTable("PostReport_Solved");

            entity.HasIndex(e => e.PostReportId, "UQ_PostReport_Solved_PostReportID").IsUnique();

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
            entity.Property(e => e.AppliedAction)
                .HasMaxLength(255)
                .HasDefaultValue("");
            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.PostReportId).HasColumnName("PostReportID");
            entity.Property(e => e.SolvedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Admin).WithMany(p => p.PostReportSolvedAdmins)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostRepor__Admin__756D6ECB");

            entity.HasOne(d => d.AffectedUser).WithMany(p => p.PostReportSolvedAffectedUsers)
                .HasForeignKey(d => d.AffectedUserId)
                .HasConstraintName("FK__PostRepor__Affec__76619304");

            entity.HasOne(d => d.Notification).WithMany(p => p.PostReportSolveds)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("FK__PostRepor__Notif__7755B73D");

            entity.HasOne(d => d.PostReport).WithOne(p => p.PostReportSolved)
                .HasForeignKey<PostReportSolved>(d => d.PostReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostRepor__PostR__7849DB76");
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
                .HasConstraintName("FK__Ratings__RateeID__7B264821");

            entity.HasOne(d => d.Rater).WithMany(p => p.RatingRaters)
                .HasForeignKey(d => d.RaterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ratings__RaterID__7C1A6C5A");

            entity.HasOne(d => d.Submission).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.SubmissionId)
                .HasConstraintName("FK__Ratings__Submiss__7D0E9093");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token, "UQ_RefreshTokens_Token").IsUnique();

            entity.Property(e => e.RefreshTokenId).HasColumnName("RefreshTokenID");
            entity.Property(e => e.DeviceInfo).HasMaxLength(255);
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("IPAddress");
            entity.Property(e => e.IssuedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.JwtId).HasMaxLength(100);
            entity.Property(e => e.RevokedAt).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(200);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RefreshTo__UserI__7E02B4CC");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleName, "UQ_Roles_Name").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(20);
        });

        modelBuilder.Entity<SystemReport>(entity =>
        {
            entity.Property(e => e.SystemReportId).HasColumnName("SystemReportID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.SystemReports)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SystemRep__UserI__7EF6D905");
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
            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_Users_Username").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.LockoutEnd).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__RoleI__00DF2177"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__UserI__01D345B0"),
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
            entity.HasKey(e => e.LogId);

            entity.ToTable("UserActivityLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.ActivityType).HasMaxLength(50);
            entity.Property(e => e.DeviceInfo).HasMaxLength(255);
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .HasColumnName("IPAddress");
            entity.Property(e => e.Timestamp).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.UserActivityLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserActiv__UserI__7FEAFD3E");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
