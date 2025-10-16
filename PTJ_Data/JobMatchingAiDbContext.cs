using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class JobMatchingAiDbContext : DbContext
{
    public JobMatchingAiDbContext()
    {
    }

    public JobMatchingAiDbContext(DbContextOptions<JobMatchingAiDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AiContentForEmbedding> AiContentForEmbeddings { get; set; }

    public virtual DbSet<AiEmbeddingStatus> AiEmbeddingStatuses { get; set; }

    public virtual DbSet<AiMatchSuggestion> AiMatchSuggestions { get; set; }

    public virtual DbSet<Application> Applications { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<EmployerFollower> EmployerFollowers { get; set; }

    public virtual DbSet<EmployerPost> EmployerPosts { get; set; }

    public virtual DbSet<EmployerProfile> EmployerProfiles { get; set; }

    public virtual DbSet<FavoritePost> FavoritePosts { get; set; }

    public virtual DbSet<JobSeekerApplicationList> JobSeekerApplicationLists { get; set; }

    public virtual DbSet<JobSeekerPost> JobSeekerPosts { get; set; }

    public virtual DbSet<JobSeekerProfile> JobSeekerProfiles { get; set; }

    public virtual DbSet<News> News { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PostReport> PostReports { get; set; }

    public virtual DbSet<PostReportSolved> PostReportSolveds { get; set; }

    public virtual DbSet<Rating> Ratings { get; set; }

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
            entity.HasKey(e => e.ContentId).HasName("PK__AI_Conte__2907A87EBB75E52B");

            entity.ToTable("AI_ContentForEmbedding");

            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "UQ_AI_Content").IsUnique();

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
            entity.HasKey(e => e.EmbeddingId).HasName("PK__AI_Embed__07B21270628F1615");

            entity.ToTable("AI_EmbeddingStatus");

            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "UQ_AI_Embedding").IsUnique();

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
            entity.HasKey(e => e.SuggestionId).HasName("PK__AI_Match__940995283D53667B");

            entity.ToTable("AI_MatchSuggestions");

            entity.HasIndex(e => new { e.SourceType, e.SourceId, e.TargetType, e.TargetId }, "UQ_AI_Match").IsUnique();

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
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.ApplicationId).HasName("PK__Applicat__C93A4F790461B0D6");

            entity.Property(e => e.ApplicationId).HasColumnName("ApplicationID");
            entity.Property(e => e.ApplicationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");
            entity.Property(e => e.Notes).HasColumnType("text");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.Applications)
                .HasForeignKey(d => d.EmployerPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Applicati__Emplo__6477ECF3");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.Applications)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Applicati__JobSe__6383C8BA");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A2B8F5DC7A1");

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

        modelBuilder.Entity<EmployerFollower>(entity =>
        {
            entity.HasKey(e => e.FollowId).HasName("PK__Employer__2CE8108E417C42A5");

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
                .HasConstraintName("FK__EmployerF__Emplo__2A164134");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.EmployerFollowerJobSeekers)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployerF__JobSe__29221CFB");
        });

        modelBuilder.Entity<EmployerPost>(entity =>
        {
            entity.HasKey(e => e.EmployerPostId).HasName("PK__Employer__E0B914A0C7ADD02D");

            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PhoneContact)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Requirements).HasColumnType("text");
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
                .HasConstraintName("FK__EmployerP__Categ__5535A963");

            entity.HasOne(d => d.User).WithMany(p => p.EmployerPosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EmployerP__UserI__5441852A");
        });

        modelBuilder.Entity<EmployerProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__Employer__290C8884136EEBF4");

            entity.HasIndex(e => e.UserId, "UQ__Employer__1788CCAD8C1B15FB").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("ProfileID");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.AverageRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.CompanyDescription).HasColumnType("text");
            entity.Property(e => e.CompanyLogo)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CompanyName)
                .HasMaxLength(100)
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
                .HasConstraintName("FK__EmployerP__UserI__44FF419A");
        });

        modelBuilder.Entity<FavoritePost>(entity =>
        {
            entity.HasKey(e => e.FavoriteId).HasName("PK__Favorite__CE74FAF59630F475");

            entity.Property(e => e.FavoriteId).HasColumnName("FavoriteID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Notes).HasColumnType("text");
            entity.Property(e => e.PostId).HasColumnName("PostID");
            entity.Property(e => e.PostType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.FavoritePosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FavoriteP__UserI__6B24EA82");
        });

        modelBuilder.Entity<JobSeekerApplicationList>(entity =>
        {
            entity.HasKey(e => e.ApplicationListId).HasName("PK__JobSeeke__AA57C7F9A6A983F4");

            entity.ToTable("JobSeekerApplicationList");

            entity.HasIndex(e => e.EmployerPostId, "IDX_JobSeekerApplicationList_EmployerPost");

            entity.HasIndex(e => e.JobSeekerId, "IDX_JobSeekerApplicationList_JobSeeker");

            entity.Property(e => e.ApplicationListId).HasColumnName("ApplicationListID");
            entity.Property(e => e.ApplicationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EmployerPostId).HasColumnName("EmployerPostID");
            entity.Property(e => e.JobSeekerId).HasColumnName("JobSeekerID");
            entity.Property(e => e.Notes).HasColumnType("text");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Applied");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.EmployerPost).WithMany(p => p.JobSeekerApplicationLists)
                .HasForeignKey(d => d.EmployerPostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__Emplo__5CD6CB2B");

            entity.HasOne(d => d.JobSeeker).WithMany(p => p.JobSeekerApplicationLists)
                .HasForeignKey(d => d.JobSeekerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__JobSe__5BE2A6F2");
        });

        modelBuilder.Entity<JobSeekerPost>(entity =>
        {
            entity.HasKey(e => e.JobSeekerPostId).HasName("PK__JobSeeke__6DA106EE9D3CBCF8");

            entity.Property(e => e.JobSeekerPostId).HasColumnName("JobSeekerPostID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
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
                .HasConstraintName("FK__JobSeeker__Categ__4D94879B");

            entity.HasOne(d => d.User).WithMany(p => p.JobSeekerPosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__UserI__4CA06362");
        });

        modelBuilder.Entity<JobSeekerProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__JobSeeke__290C8884E44AD89D");

            entity.HasIndex(e => e.UserId, "UQ__JobSeeke__1788CCAD73F98AE1").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("ProfileID");
            entity.Property(e => e.AverageRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.Education).HasColumnType("text");
            entity.Property(e => e.Experience).HasColumnType("text");
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
            entity.Property(e => e.Skills).HasColumnType("text");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithOne(p => p.JobSeekerProfile)
                .HasForeignKey<JobSeekerProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__JobSeeker__UserI__403A8C7D");
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.HasKey(e => e.NewsId).HasName("PK__News__954EBDD3AAC5127F");

            entity.Property(e => e.NewsId).HasColumnName("NewsID");
            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.IsFeatured).HasDefaultValue(false);
            entity.Property(e => e.Priority).HasDefaultValue(0);
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Admin).WithMany(p => p.News)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__News__AdminID__7D439ABD");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E3248480D9A");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Message).HasColumnType("text");
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
                .HasConstraintName("FK__Notificat__UserI__03F0984C");
        });

        modelBuilder.Entity<PostReport>(entity =>
        {
            entity.HasKey(e => e.PostReportId).HasName("PK__PostRepo__D6F03B6A74AA1BF6");

            entity.Property(e => e.PostReportId).HasColumnName("PostReportID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Reason).HasColumnType("text");
            entity.Property(e => e.ReportType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ReportedItemId).HasColumnName("ReportedItemID");
            entity.Property(e => e.ReporterId).HasColumnName("ReporterID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Reporter).WithMany(p => p.PostReports)
                .HasForeignKey(d => d.ReporterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostRepor__Repor__6FE99F9F");
        });

        modelBuilder.Entity<PostReportSolved>(entity =>
        {
            entity.HasKey(e => e.SolvedPostReportId).HasName("PK__PostRepo__9089CF02DEE094F1");

            entity.ToTable("PostReport_Solved");

            entity.HasIndex(e => e.PostReportId, "UQ__PostRepo__D6F03B6B0296AE8E").IsUnique();

            entity.Property(e => e.SolvedPostReportId).HasColumnName("SolvedPostReportID");
            entity.Property(e => e.ActionTaken)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.AffectedUserId).HasColumnName("AffectedUserID");
            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.PostReportId).HasColumnName("PostReportID");
            entity.Property(e => e.Reason).HasColumnType("text");
            entity.Property(e => e.SolvedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Admin).WithMany(p => p.PostReportSolvedAdmins)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostRepor__Admin__0B91BA14");

            entity.HasOne(d => d.AffectedUser).WithMany(p => p.PostReportSolvedAffectedUsers)
                .HasForeignKey(d => d.AffectedUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostRepor__Affec__0C85DE4D");

            entity.HasOne(d => d.Notification).WithMany(p => p.PostReportSolveds)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("FK__PostRepor__Notif__0E6E26BF");

            entity.HasOne(d => d.PostReport).WithOne(p => p.PostReportSolved)
                .HasForeignKey<PostReportSolved>(d => d.PostReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostRepor__PostR__0A9D95DB");
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.RatingId).HasName("PK__Ratings__FCCDF85CC858EDE2");

            entity.Property(e => e.RatingId).HasColumnName("RatingID");
            entity.Property(e => e.ApplicationId).HasColumnName("ApplicationID");
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RateeId).HasColumnName("RateeID");
            entity.Property(e => e.RaterId).HasColumnName("RaterID");
            entity.Property(e => e.RatingValue).HasColumnType("decimal(3, 2)");

            entity.HasOne(d => d.Application).WithMany(p => p.Ratings)
                .HasForeignKey(d => d.ApplicationId)
                .HasConstraintName("FK__Ratings__Applica__787EE5A0");

            entity.HasOne(d => d.Ratee).WithMany(p => p.RatingRatees)
                .HasForeignKey(d => d.RateeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ratings__RateeID__778AC167");

            entity.HasOne(d => d.Rater).WithMany(p => p.RatingRaters)
                .HasForeignKey(d => d.RaterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ratings__RaterID__76969D2E");
        });

        modelBuilder.Entity<SystemReport>(entity =>
        {
            entity.HasKey(e => e.SystemReportId).HasName("PK__SystemRe__327400E59289BC8F");

            entity.Property(e => e.SystemReportId).HasColumnName("SystemReportID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
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
                .HasConstraintName("FK__SystemRep__UserI__123EB7A3");
        });

        modelBuilder.Entity<SystemStatistic>(entity =>
        {
            entity.HasKey(e => e.StatId).HasName("PK__SystemSt__3A162D1E022305AC");

            entity.Property(e => e.StatId).HasColumnName("StatID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.StatDate).HasColumnType("datetime");
            entity.Property(e => e.TotalActiveUsers).HasDefaultValue(0);
            entity.Property(e => e.TotalApplications).HasDefaultValue(0);
            entity.Property(e => e.TotalEmployers).HasDefaultValue(0);
            entity.Property(e => e.TotalJobSeekers).HasDefaultValue(0);
            entity.Property(e => e.TotalLogins).HasDefaultValue(0);
            entity.Property(e => e.TotalNews).HasDefaultValue(0);
            entity.Property(e => e.TotalPosts).HasDefaultValue(0);
            entity.Property(e => e.TotalReports).HasDefaultValue(0);
            entity.Property(e => e.TotalUsers).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACE5FA8A8C");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E48DD62620").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534440F8454").IsUnique();

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
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UserActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__UserActi__5E5499A85F6CF36E");

            entity.ToTable("UserActivityLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.ActivityType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Details).HasColumnType("text");
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
                .HasConstraintName("FK__UserActiv__UserI__25518C17");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
