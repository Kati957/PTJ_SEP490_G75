using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

using PTJ_Data;
using PTJ_Data.Repositories.Interfaces.EPost;
using PTJ_Models.DTO.Notification;
using PTJ_Models.DTO.PostDTO;
using PTJ_Models.Models;
using PTJ_Service.AiService;
using PTJ_Service.EmployerPostService.Implementations;
using PTJ_Service.ImageService;
using PTJ_Service.Interfaces;
using PTJ_Service.LocationService;
using PTJ_Service.Tests.CreateEmployerPost;

using EmployerPostSvc = PTJ_Service.EmployerPostService.Implementations.EmployerPostService;

namespace PTJ_Service.Tests.EmployerPosts
    {
    public class CreateEmployerPostTests
        {
        private readonly JobMatchingDbContext _db;
        private readonly Mock<IEmployerPostRepository> _repo = new();
        private readonly Mock<IAIService> _ai = new();
        private readonly Mock<IImageService> _image = new();
        private readonly Mock<INotificationService> _noti = new();
        private readonly OpenMapService _map;
        private readonly Mock<LocationDisplayService> _loc;
        private readonly EmployerPostSvc _service;

        public CreateEmployerPostTests()
            {
            var opt = new DbContextOptionsBuilder<JobMatchingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new JobMatchingDbContext(opt);

            _map = new FakeOpenMapService(_db);

            _loc = new Mock<LocationDisplayService>(
                new VnProstLocationService(new HttpClient())
            );

            _loc.Setup(x => x.BuildAddressAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((int p, int d, int w) => $"Ward {w}, District {d}, Province {p}");

            _repo.Setup(r => r.AddAsync(It.IsAny<EmployerPost>()))
                .Callback<EmployerPost>(p => _db.EmployerPosts.Add(p))
                .Returns(Task.CompletedTask);

            _ai.Setup(x => x.CreateEmbeddingAsync(It.IsAny<string>()))
                .ReturnsAsync(new float[] { 0.1f, 0.2f });

            _ai.Setup(x => x.QueryWithIDsAsync(
                It.IsAny<string>(),
                It.IsAny<float[]>(),
                It.IsAny<List<int>>(),
                It.IsAny<int>()))
                .ReturnsAsync(new List<(string Id, double Score)>());

            _service = new EmployerPostSvc(
                _repo.Object,
                _db,
                _ai.Object,
                _map,
                _loc.Object,
                _image.Object,
                _noti.Object
            );

            SeedFreePlan();
            }

        private void SeedFreePlan()
            {
            _db.EmployerPlans.Add(new EmployerPlan
                {
                PlanId = 1,
                PlanName = "Free",
                MaxPosts = 1,
                DurationDays = 30
                });
            _db.SaveChanges();
            }

        private void AddUser(int id = 1, bool active = true)
            {
            _db.Users.Add(new User
                {
                UserId = id,
                Username = "user" + id,
                Email = "user" + id + "@mail.com",
                IsActive = active,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
                });
            _db.SaveChanges();
            }

        private EmployerPostCreateDto ValidDto(int userId = 1)
            {
            return new EmployerPostCreateDto
                {
                UserID = userId,
                Title = "Test Post",
                Description = "This is a test description with enough length.",
                SalaryMin = null,
                SalaryMax = null,
                SalaryType = null,
                Requirements = "Requirement test",
                WorkHourStart = "08:00",
                WorkHourEnd = "17:00",
                ExpiredAt = null,
                ProvinceId = 1,
                DistrictId = 2,
                WardId = 3,
                DetailAddress = "123 ABC Street",
                CategoryID = 1,
                PhoneContact = "0901234567",
                Images = null
                };
            }

        // ============================
        // SUPPORT: Validate DTO
        // ============================
        private IList<ValidationResult> ValidateModel(object model)
            {
            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, results, true);
            return results;
            }

        // =====================================================
        // 1) DTO = null
        // =====================================================
        [Fact]
        public async Task ShouldThrow_WhenDtoIsNull()
            {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.CreateEmployerPostAsync(null!));
            }

        // =====================================================
        // 2) UserID invalid
        // =====================================================
        [Fact]
        public async Task ShouldThrow_WhenUserIdInvalid()
            {
            var dto = ValidDto();
            dto.UserID = 0;

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(dto));

            Assert.Equal("Thiếu thông tin UserID khi tạo bài đăng tuyển dụng.", ex.Message);
            }

        // =====================================================
        // 3) User not found
        // =====================================================
        [Fact]
        public async Task ShouldThrow_WhenUserNotFound()
            {
            var dto = ValidDto(99);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(dto));

            Assert.Equal("Không tìm thấy tài khoản.", ex.Message);
            }

        // =====================================================
        // 4) User inactive
        // =====================================================
        [Fact]
        public async Task ShouldThrow_WhenUserInactive()
            {
            AddUser(1, active: false);

            var dto = ValidDto();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(dto));

            Assert.Equal("Tài khoản đã bị khóa. Không thể đăng bài tuyển dụng.", ex.Message);
            }

        // =====================================================
        // 5) Auto-create free subscription
        // =====================================================
        [Fact]
        public async Task ShouldAutoCreateFreeSubscription_WhenUserHasNone()
            {
            AddUser(1);

            await _service.CreateEmployerPostAsync(ValidDto());

            Assert.NotNull(_db.EmployerSubscriptions
                .FirstOrDefault(s => s.UserId == 1 && s.PlanId == 1));
            }

        // =====================================================
        // 6) Reset expired subscription
        // =====================================================
        [Fact]
        public async Task ShouldResetFreeSubscription_WhenExpired()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 0,
                StartDate = DateTime.Now.AddMonths(-2),
                EndDate = DateTime.Now.AddMonths(-1),
                Status = "Active"
                });

            _db.SaveChanges();

            await _service.CreateEmployerPostAsync(ValidDto());

            var sub = _db.EmployerSubscriptions.First(s => s.UserId == 1);
            Assert.Equal(0, sub.RemainingPosts);
            }

        // =====================================================
        // 7) Use paid subscription first
        // =====================================================
        [Fact]
        public async Task ShouldUsePaidSubscription_WhenExists()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                Status = "Active"
                });

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 2,
                RemainingPosts = 5,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                Status = "Active"
                });

            _db.SaveChanges();

            await _service.CreateEmployerPostAsync(ValidDto());

            Assert.Equal(4, _db.EmployerSubscriptions.First(s => s.PlanId == 2).RemainingPosts);
            }

        // =====================================================
        // 8) No remaining posts
        // =====================================================
        [Fact]
        public async Task ShouldThrow_WhenRemainingPostsZero()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 0,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                Status = "Active"
                });

            _db.SaveChanges();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(ValidDto()));

            Assert.Equal("Bạn đã hết lượt đăng bài.", ex.Message);
            }

        // =====================================================
        // 9) Decrease remaining posts
        // =====================================================
        [Fact]
        public async Task ShouldDecreaseRemainingPostsAfterCreate()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                Status = "Active"
                });

            _db.SaveChanges();

            await _service.CreateEmployerPostAsync(ValidDto());

            Assert.Equal(0, _db.EmployerSubscriptions.First(s => s.UserId == 1).RemainingPosts);
            }

        // =====================================================
        // 10) Build location with detail address
        // =====================================================
        [Fact]
        public async Task ShouldBuildLocationWithDetailAddress()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                Status = "Active"
                });

            _db.SaveChanges();

            var result = await _service.CreateEmployerPostAsync(ValidDto());

            Assert.Contains("123 ABC Street", result.Post.Location);
            }

        // =====================================================
        // 11) ❌ REPLACED → Title too short
        // =====================================================
        [Fact]
        public void ShouldFail_WhenTitleTooShort()
            {
            var dto = ValidDto();
            dto.Title = "Abc"; // < 5 ký tự

            var validate = ValidateModel(dto);
            Assert.Contains(validate, v => v.ErrorMessage!.Contains("minimum"));
            }

        // =====================================================
        // 12) SalaryMin < 0
        // =====================================================
        [Fact]
        public async Task ShouldThrow_WhenSalaryMinNegative()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                Status = "Active"
                });

            var dto = ValidDto();
            dto.SalaryMin = -1;

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(dto));

            Assert.Equal("SalaryMin không hợp lệ.", ex.Message);
            }

        // =====================================================
        // 13) SalaryMax < SalaryMin
        // =====================================================
        [Fact]
        public async Task ShouldThrow_WhenSalaryMaxLessThanMin()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                Status = "Active"
                });

            var dto = ValidDto();
            dto.SalaryMin = 10;
            dto.SalaryMax = 5;

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(dto));

            Assert.Equal("SalaryMax phải ≥ SalaryMin.", ex.Message);
            }

        // =====================================================
        // 14) SalaryType missing while having SalaryMin/Max
        // =====================================================
        [Fact]
        public async Task ShouldThrow_WhenSalaryTypeMissing()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                Status = "Active"
                });

            var dto = ValidDto();
            dto.SalaryMin = 5;
            dto.SalaryMax = 10;
            dto.SalaryType = null;

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(dto));

            Assert.Equal("SalaryType là bắt buộc.", ex.Message);
            }

        // =====================================================
        // 15)  REPLACED → Title too long
        // =====================================================
        [Fact]
        public void ShouldFail_WhenTitleTooLong()
            {
            var dto = ValidDto();
            dto.Title = new string('A', 121);

            var validate = ValidateModel(dto);
            Assert.Contains(validate, v => v.ErrorMessage!.Contains("maximum"));
            }

        // =====================================================
        // 16) Upload image
        // =====================================================
        [Fact]
        public async Task ShouldUploadImages()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(5),
                Status = "Active"
                });

            var file = new Mock<IFormFile>();
            file.Setup(f => f.ContentType).Returns("image/png");

            _image.Setup(x => x.UploadImageAsync(file.Object, "EmployerPosts"))
                  .ReturnsAsync(("url123", "img123"));

            var dto = ValidDto();
            dto.Images = new List<IFormFile> { file.Object };

            await _service.CreateEmployerPostAsync(dto);

            _image.Verify(x => x.UploadImageAsync(file.Object, "EmployerPosts"), Times.Once);
            }

        // =====================================================
        // 17)  REPLACED → Description too short
        // =====================================================
        [Fact]
        public void ShouldFail_WhenDescriptionTooShort()
            {
            var dto = ValidDto();
            dto.Description = "1234567890123456789"; // 19 chars

            var validate = ValidateModel(dto);
            Assert.Contains(validate, v => v.ErrorMessage!.Contains("minimum"));
            }

        // =====================================================
        // 18) REPLACED → Description too long
        // =====================================================
        [Fact]
        public void ShouldFail_WhenDescriptionTooLong()
            {
            var dto = ValidDto();
            dto.Description = new string('D', 5001);

            var validate = ValidateModel(dto);
            Assert.Contains(validate, v => v.ErrorMessage!.Contains("maximum"));
            }

        // =====================================================
        // 19)  REPLACED → Requirements too short
        // =====================================================
        [Fact]
        public void ShouldFail_WhenRequirementsTooShort()
            {
            var dto = ValidDto();
            dto.Requirements = "short"; // < 10 chars

            var validate = ValidateModel(dto);
            Assert.Contains(validate, v => v.ErrorMessage!.Contains("minimum"));
            }
        }
    }
