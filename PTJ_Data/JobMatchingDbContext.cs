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

    public virtual DbSet<AiQueryCache> AiQueryCaches { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<EmployerCandidatesList> EmployerCandidatesLists { get; set; }

    public virtual DbSet<EmployerFollower> EmployerFollowers { get; set; }

    public virtual DbSet<EmployerPost> EmployerPosts { get; set; }

    public virtual DbSet<EmployerProfile> EmployerProfiles { get; set; }

    public virtual DbSet<EmployerShortlistedCandidate> EmployerShortlistedCandidates { get; set; }

    public virtual DbSet<ExternalLogin> ExternalLogins { get; set; }

    public virtual DbSet<FavoritePost> FavoritePosts { get; set; }

    public virtual DbSet<JobSeekerPost> JobSeekerPosts { get; set; }

    public virtual DbSet<JobSeekerProfile> JobSeekerProfiles { get; set; }

    public virtual DbSet<JobSeekerShortlistedJob> JobSeekerShortlistedJobs { get; set; }

    public virtual DbSet<JobSeekerSubmission> JobSeekerSubmissions { get; set; }

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
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("server =(local); database = JobMatching_DB; uid=sa; pwd=123456;Trusted_Connection=True;Encrypt=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiContentForEmbedding>(entity =>
        {
            entity.HasKey(e => e.ContentId).HasName("PK__AI_Conte__2907A87EA2208052");

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
            entity.HasKey(e => e.EmbeddingId).HasName("PK__AI_Embed__07B21270DC8EDA34");

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
            entity.HasKey(e => e.SuggestionId).HasName("PK__AI_Match__940995289896BCA5");

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

        modelBuilder.Entity<AiQueryCache>(entity =>
        {
            entity.HasKey(e => e.CacheId).HasName("PK__AI_Query__4EDCCD13CA8F57F2");

            entity.ToTable("AI_QueryCache");

            entity.Property(e => e.CacheId).HasColumnName("CacheID");
            entity.Property(e => e.CachedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EntityId).HasColumnName("EntityID");
            entity.Property(e => e.Namespace)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A2BEA4DB9C2");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.EvtokenId).HasName("PK__EmailVer__A30302B8EFAA6511");

            entity.HasIndex(e => e.Token, "UQ__EmailVer__1EB4F817BFD45B1D").IsUnique();

            entity.Property(e => e.EvtokenId).HasColumnName("EVTokenID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(200);
            entity.Property(e => e.UsedAt).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.EmailVerificationTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmailVeri__UserI__4B7734FF");
        });

        modelBuilder.Entity<EmployerCandidatesList>(entity =>
        {
            entity.HasKey(e => e.CandidateListId).HasName("PK__Employer__16C4480BC4156A93");

            entity.ToTable("Employer_CandidatesList");

            entity.Property(e => e.CandidateListId).HasColumnName("CandidateListID");
            entity.Property(e => e.ApplicationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.EmployerCandidatesLists)
                .HasForeignKey(d => d.EmployerPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employer___Emplo__4C6B5938");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerCandidatesLists)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employer___JobSe__4D5F7D71");
        });

        modelBuilder.Entity<EmployerFollower>(entity =>
        {
            entity.HasKey(e => e.FollowId).HasName("PK__Employer__2CE8108EE0FDC512");

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
                .HasConstraintName("FK__EmployerF__Emplo__51300E55");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerFollowerJobSeekers)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployerF__JobSe__5224328E");
        });

        modelBuilder.Entity<EmployerPost>(entity =>
        {
            entity.HasKey(e => e.EmployerPostId).HasName("PK__Employer__E0B914A09C4A9169");

            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PhoneContact)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Salary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.WorkHours)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Category).WithMany(p => p.EmployerPosts)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__EmployerP__Categ__531856C7");

            entity.HasOne(d => d.User).WithMany(p => p.EmployerPosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployerP__UserI__540C7B00");
        });

        modelBuilder.Entity<EmployerProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__Employer__290C888435F287DD");

            entity.HasIndex(e => e.UserId, "UQ__Employer__1788CCAD20943BA4").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("ProfileID");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ContactEmail)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ContactName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .IsUnicode(false);
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
                .HasConstraintName("FK__EmployerP__UserI__55009F39");
        });

        modelBuilder.Entity<EmployerShortlistedCandidate>(entity =>
        {
            entity.HasKey(e => e.ShortlistId).HasName("PK__Employer__550C0D70774A3081");

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
                .HasConstraintName("FK__Employer___Emplo__4E53A1AA");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.EmployerShortlistedCandidates)
                .HasForeignKey(d => d.EmployerPostId)
                .HasConstraintName("FK__Employer___Emplo__4F47C5E3");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerShortlistedCandidateJobSeekers)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employer___JobSe__503BEA1C");
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasKey(e => e.ExternalLoginId).HasName("PK__External__A8FDB38E67310C21");

            entity.HasIndex(e => new { e.Provider, e.ProviderKey }, "UQ__External__619A22CEA57F5D7C").IsUnique();

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
                .HasConstraintName("FK__ExternalL__UserI__55F4C372");
        });

        modelBuilder.Entity<FavoritePost>(entity =>
        {
            entity.HasKey(e => e.FavoriteId).HasName("PK__Favorite__CE74FAF5AB7C5CB8");

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
                .HasConstraintName("FK__FavoriteP__UserI__56E8E7AB");
        });

        modelBuilder.Entity<JobSeekerPost>(entity =>
        {
            entity.HasKey(e => e.JobSeekerPostId).HasName("PK__JobSeeke__6DA106EE25A52CD8");

            entity.Property(e => e.JobSeekerPostId).HasColumnName("JobSeekerPostID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.PhoneContact)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PreferredLocation)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PreferredWorkHours)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Category).WithMany(p => p.JobSeekerPosts)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__JobSeeker__Categ__5BAD9CC8");

            entity.HasOne(d => d.User).WithMany(p => p.JobSeekerPosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__UserI__5CA1C101");
        });

        modelBuilder.Entity<JobSeekerProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__JobSeeke__290C88842BED02AF");

            entity.HasIndex(e => e.UserId, "UQ__JobSeeke__1788CCADE0D070B4").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("ProfileID");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.PreferredJobType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PreferredLocation)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ProfilePicture)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithOne(p => p.JobSeekerProfile)
                .HasForeignKey<JobSeekerProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__UserI__5D95E53A");
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
            entity.HasKey(e => e.SubmissionId).HasName("PK__JobSeeke__449EE1057D1D31AC");

            entity.ToTable("JobSeeker_Submissions");

            entity.Property(e => e.SubmissionId).HasColumnName("SubmissionID");
            entity.Property(e => e.AppliedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Applied");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.JobSeekerSubmissions)
                .HasForeignKey(d => d.EmployerPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__Emplo__59C55456");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.JobSeekerSubmissions)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__JobSe__5AB9788F");
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId).HasName("PK__LoginAtt__891A688657D30212");

            entity.Property(e => e.AttemptId).HasColumnName("AttemptID");
            entity.Property(e => e.DeviceInfo).HasMaxLength(255);
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
                .HasConstraintName("FK__LoginAtte__UserI__5E8A0973");
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.HasKey(e => e.NewsId).HasName("PK__News__954EBDD35329610F");

            entity.Property(e => e.NewsId).HasColumnName("NewsID");
            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Admin).WithMany(p => p.News)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__News__AdminID__5F7E2DAC");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E3299634EC4");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RelatedItemId).HasColumnName("RelatedItemID");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__UserI__607251E5");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("PK__Password__658FEE8AF0A754D3");

            entity.Property(e => e.TokenId).HasColumnName("TokenID");
            entity.Property(e => e.Expiration).HasColumnType("datetime");
            entity.Property(e => e.Token).HasMaxLength(200);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PasswordR__UserI__6166761E");
        });

        modelBuilder.Entity<PostReport>(entity =>
        {
            entity.HasKey(e => e.PostReportId).HasName("PK__PostRepo__D6F03B6A87E235AD");

            entity.Property(e => e.PostReportId).HasColumnName("PostReportID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.JobSeekerPostId).HasColumnName("JobSeekerPostID");
            entity.Property(e => e.ReportType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ReportedItemId).HasColumnName("ReportedItemID");
            entity.Property(e => e.ReporterId).HasColumnName("ReporterID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TargetUserId).HasColumnName("TargetUserID");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.PostReports)
                .HasForeignKey(d => d.EmployerPostId)
                .HasConstraintName("FK__PostRepor__Emplo__65370702");

            entity.HasOne(d => d.JobSeekerPost).WithMany(p => p.PostReports)
                .HasForeignKey(d => d.JobSeekerPostId)
                .HasConstraintName("FK__PostRepor__JobSe__662B2B3B");

            entity.HasOne(d => d.Reporter).WithMany(p => p.PostReportReporters)
                .HasForeignKey(d => d.ReporterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostRepor__Repor__671F4F74");

            entity.HasOne(d => d.TargetUser).WithMany(p => p.PostReportTargetUsers)
                .HasForeignKey(d => d.TargetUserId)
                .HasConstraintName("FK__PostRepor__Targe__681373AD");
        });

        modelBuilder.Entity<PostReportSolved>(entity =>
        {
            entity.HasKey(e => e.SolvedPostReportId).HasName("PK__PostRepo__9089CF026096413B");

            entity.ToTable("PostReport_Solved");

            entity.HasIndex(e => e.PostReportId, "UQ__PostRepo__D6F03B6BB23A43CD").IsUnique();

            entity.Property(e => e.SolvedPostReportId).HasColumnName("SolvedPostReportID");
            entity.Property(e => e.ActionTaken)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.PostReportId).HasColumnName("PostReportID");
            entity.Property(e => e.SolvedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Admin).WithMany(p => p.PostReportSolveds)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostRepor__Admin__625A9A57");

            entity.HasOne(d => d.Notification).WithMany(p => p.PostReportSolveds)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("FK__PostRepor__Notif__634EBE90");

            entity.HasOne(d => d.PostReport).WithOne(p => p.PostReportSolved)
                .HasForeignKey<PostReportSolved>(d => d.PostReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostRepor__PostR__6442E2C9");
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.RatingId).HasName("PK__Ratings__FCCDF85C259F923B");

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
                .HasConstraintName("FK__Ratings__RateeID__690797E6");

            entity.HasOne(d => d.Rater).WithMany(p => p.RatingRaters)
                .HasForeignKey(d => d.RaterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ratings__RaterID__69FBBC1F");

            entity.HasOne(d => d.Submission).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.SubmissionId)
                .HasConstraintName("FK__Ratings__Submiss__6AEFE058");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenId).HasName("PK__RefreshT__F5845E59DE78953A");

            entity.HasIndex(e => e.Token, "UQ__RefreshT__1EB4F817E4842841").IsUnique();

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
                .HasConstraintName("FK__RefreshTo__UserI__6BE40491");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3A9A03D5EB");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61602828B173").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SystemReport>(entity =>
        {
            entity.HasKey(e => e.SystemReportId).HasName("PK__SystemRe__327400E52776483F");

            entity.Property(e => e.SystemReportId).HasColumnName("SystemReportID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.SystemReports)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SystemRep__UserI__6CD828CA");
        });

        modelBuilder.Entity<SystemStatistic>(entity =>
        {
            entity.HasKey(e => e.StatId).HasName("PK__SystemSt__3A162D1E97CB956A");

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
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACE72093CD");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4674F7677").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105348C514386").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.LockoutEnd).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
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
                        .HasConstraintName("FK__UserRoles__RoleI__6EC0713C"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRoles__UserI__6FB49575"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK__UserRole__AF27604F07AF6D69");
                        j.ToTable("UserRoles");
                        j.IndexerProperty<int>("UserId").HasColumnName("UserID");
                        j.IndexerProperty<int>("RoleId").HasColumnName("RoleID");
                    });
        });

        modelBuilder.Entity<UserActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__UserActi__5E5499A83B2D85E3");

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
                .HasConstraintName("FK__UserActiv__UserI__6DCC4D03");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
