using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using PTJ_Service.JobApplicationService.Implementations;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Service.Interfaces;
using PTJ_Service.Helpers.Interfaces;
using System.Threading.Tasks;
using System;

// Alias ngắn gọn
using ApplySvc = PTJ_Service.JobApplicationService.Implementations.JobApplicationService;

namespace PTJ_Service.Tests.ApplyJob
    {
    public class ApplyTests
        {
        // Tạo DB InMemory
        private JobMatchingDbContext CreateDb()
            {
            var options = new DbContextOptionsBuilder<JobMatchingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new JobMatchingDbContext(options);
            }

        // Helper tạo User đầy đủ required fields
        private User MakeUser(int id, bool isActive = true)
            {
            return new User
                {
                UserId = id,
                IsActive = isActive,
                Email = $"user{id}@mail.com",
                Username = $"user{id}"
                };
            }

        // Helper Post đầy đủ required fields
        private EmployerPost MakePost(int id, int employerId, string status = "Active")
            {
            return new EmployerPost
                {
                EmployerPostId = id,
                Status = status,
                UserId = employerId,
                Title = $"Post {id}"
                };
            }

        // Helper tạo service
        private ApplySvc CreateService(JobMatchingDbContext db, Mock<IJobApplicationRepository>? repo = null)
            {
            return new ApplySvc(
                repo?.Object ?? new Mock<IJobApplicationRepository>().Object,
                db,
                new Mock<INotificationService>().Object,
                new Mock<IEmailSender>().Object,
                new Mock<IEmailTemplateService>().Object
            );
            }

        // === 1️⃣ Job seeker inactive ===
        [Fact]
        public async Task Apply_Fail_JobSeeker_Inactive()
            {
            var db = CreateDb();
            db.Users.Add(MakeUser(1, false));
            db.SaveChanges();

            var service = CreateService(db);

            var result = await service.ApplyAsync(1, 10, "note");

            Assert.False(result.success);
            Assert.Equal("Tài khoản ứng viên không tồn tại hoặc đã bị khóa.", result.error);
            }

        // === 2️⃣ Post not found ===
        [Fact]
        public async Task Apply_Fail_Post_NotFound()
            {
            var db = CreateDb();
            db.Users.Add(MakeUser(1));
            db.SaveChanges();

            var service = CreateService(db);

            var result = await service.ApplyAsync(1, 999, null);

            Assert.False(result.success);
            Assert.Equal("Bài đăng không tồn tại.", result.error);
            }

        // === 3️⃣ Employer inactive ===
        [Fact]
        public async Task Apply_Fail_Employer_Inactive()
            {
            var db = CreateDb();

            db.Users.Add(MakeUser(1));
            db.Users.Add(MakeUser(2, false));

            db.EmployerPosts.Add(MakePost(10, 2)); // Title + Required fields OK
            db.SaveChanges();

            var service = CreateService(db);

            var result = await service.ApplyAsync(1, 10, null);

            Assert.False(result.success);
            Assert.Equal("Nhà tuyển dụng đã bị khóa.", result.error);
            }

        // === 4️⃣ CV không thuộc user ===
        [Fact]
        public async Task Apply_Fail_Cv_NotOwned()
            {
            var db = CreateDb();

            db.Users.Add(MakeUser(1));
            db.Users.Add(MakeUser(2));

            db.EmployerPosts.Add(MakePost(10, 2));
            db.SaveChanges();

            var service = CreateService(db);

            var result = await service.ApplyAsync(1, 10, "note", 999);

            Assert.False(result.success);
            Assert.Equal("CV không hợp lệ hoặc không thuộc về bạn.", result.error);
            }

        // === 5️⃣ Already applied ===
        [Fact]
        public async Task Apply_Fail_AlreadyApplied()
            {
            var db = CreateDb();
            var repo = new Mock<IJobApplicationRepository>();

            repo.Setup(r => r.GetAsync(1, 10))
                .ReturnsAsync(new JobSeekerSubmission { Status = "Pending" });

            db.Users.Add(MakeUser(1));
            db.EmployerPosts.Add(MakePost(10, 1));
            db.SaveChanges();

            var service = CreateService(db, repo);

            var result = await service.ApplyAsync(1, 10, null);

            Assert.False(result.success);
            Assert.Equal("Bạn đã ứng tuyển bài này trước đó.", result.error);
            }

        // === 6️⃣ Reapply withdrawn ===
        [Fact]
        public async Task Apply_Success_Reapply_Withdrawn()
            {
            var db = CreateDb();
            var repo = new Mock<IJobApplicationRepository>();

            var oldApp = new JobSeekerSubmission { Status = "Withdrawn" };

            repo.Setup(r => r.GetAsync(1, 10)).ReturnsAsync(oldApp);

            db.Users.Add(MakeUser(1));
            db.EmployerPosts.Add(MakePost(10, 1));
            db.SaveChanges();

            var service = CreateService(db, repo);

            var result = await service.ApplyAsync(1, 10, "note");

            Assert.True(result.success);
            Assert.Null(result.error);
            Assert.Equal("Pending", oldApp.Status);
            }

        // === 7️⃣ Fresh apply success ===
        [Fact]
        public async Task Apply_Success_NewSubmission()
            {
            var db = CreateDb();
            var repo = new Mock<IJobApplicationRepository>();

            repo.Setup(r => r.GetAsync(1, 10))
                .ReturnsAsync((JobSeekerSubmission)null);

            db.Users.Add(MakeUser(1));
            db.Users.Add(MakeUser(2));

            db.JobSeekerProfiles.Add(new JobSeekerProfile
                {
                UserId = 1,
                FullName = "Tester"
                });

            db.EmployerPosts.Add(MakePost(10, 2));
            db.SaveChanges();

            var service = CreateService(db, repo);

            var result = await service.ApplyAsync(1, 10, "hello");

            Assert.True(result.success);
            Assert.Null(result.error);
            }
        }
    }
