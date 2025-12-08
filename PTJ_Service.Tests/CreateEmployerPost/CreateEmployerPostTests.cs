using System;
using System.Collections.Generic;
using System.Linq;
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
using PTJ_Service.Tests.CreateEmployerPost;   // dùng FakeOpenMapService

using EmployerPostSvc = PTJ_Service.EmployerPostService.Implementations.EmployerPostService;

namespace PTJ_Service.Tests.EmployerPosts
    {
    public class CreateEmployerPostTests
        {
        private readonly JobMatchingDbContext _db;
        private readonly Mock<IEmployerPostRepository> _repo = new();
        private readonly Mock<IAIService> _ai = new();       // không verify AI, chỉ setup cho chạy không lỗi
        private readonly Mock<IImageService> _image = new();
        private readonly Mock<INotificationService> _noti = new();
        private readonly OpenMapService _map;                // FakeOpenMapService
        private readonly Mock<LocationDisplayService> _loc;  // mock đúng cách

        private readonly EmployerPostSvc _service;

        public CreateEmployerPostTests()
            {
            // InMemory DbContext
            var options = new DbContextOptionsBuilder<JobMatchingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new JobMatchingDbContext(options);

            // Fake OpenMapService (không gọi API thật)
            _map = new FakeOpenMapService(_db);

            // Mock LocationDisplayService: PHẢI truyền VnPostLocationService vào constructor
            _loc = new Mock<LocationDisplayService>(
                new VnPostLocationService(new HttpClient())
            );

            // Fake BuildAddressAsync để trả về chuỗi cố định
            _loc.Setup(x => x.BuildAddressAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((int p, int d, int w) => $"Ward {w}, District {d}, Province {p}");

            // Khi AddAsync được gọi -> thực sự add vào DbContext để service load lại được freshPost
            _repo.Setup(r => r.AddAsync(It.IsAny<EmployerPost>()))
                .Callback<EmployerPost>(p =>
                {
                    _db.EmployerPosts.Add(p);
                })
                .Returns(Task.CompletedTask);

            // Setup AI để không bị null (không test logic AI)
            _ai.Setup(x => x.CreateEmbeddingAsync(It.IsAny<string>()))
                .ReturnsAsync(new float[] { 0.1f, 0.2f });

            _ai.Setup(x => x.QueryWithIDsAsync(
                    It.IsAny<string>(),
                    It.IsAny<float[]>(),
                    It.IsAny<List<int>>(),
                    It.IsAny<int>()))
                .ReturnsAsync(new List<(string Id, double Score)>());

            // Inject vào service thật
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

        /// <summary>
        /// Seed gói Free trong EmployerPlans cho các test về subscription.
        /// </summary>
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

        /// <summary>
        /// Tạo user active trong DB.
        /// </summary>
        private void AddUser(int id = 1, bool active = true)
            {
            _db.Users.Add(new User
                {
                UserId = id,
                Username = $"user{id}",
                Email = $"user{id}@mail.com",
                IsActive = active,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
                });
            _db.SaveChanges();
            }

        /// <summary>
        /// Tạo DTO hợp lệ mặc định.
        /// </summary>
        private EmployerPostCreateDto ValidDto(int userId = 1)
            {
            return new EmployerPostCreateDto
                {
                UserID = userId,
                Title = "Test Post",
                Description = "Test description",
                WorkHourStart = "08:00",
                WorkHourEnd = "17:00",
                ProvinceId = 1,
                DistrictId = 2,
                WardId = 3,
                CategoryID = 1,
                PhoneContact = "0901234567",
                DetailAddress = "123 ABC"
                };
            }

        // =====================================================================
        // 1) dto = null → phải throw ArgumentNullException
        // =====================================================================
        [Fact]
        public async Task ShouldThrow_WhenDtoIsNull()
            {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.CreateEmployerPostAsync(null!));
            }

        // =====================================================================
        // 2) UserID <= 0 → phải throw "Thiếu thông tin UserID..."
        // =====================================================================
        [Fact]
        public async Task ShouldThrow_WhenUserIdInvalid()
            {
            var dto = ValidDto();
            dto.UserID = 0;

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(dto));

            Assert.Equal("Thiếu thông tin UserID khi tạo bài đăng tuyển dụng.", ex.Message);
            }

        // =====================================================================
        // 3) User không tồn tại → throw "Không tìm thấy tài khoản."
        // =====================================================================
        [Fact]
        public async Task ShouldThrow_WhenUserNotFound()
            {
            var dto = ValidDto(99);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(dto));

            Assert.Equal("Không tìm thấy tài khoản.", ex.Message);
            }

        // =====================================================================
        // 4) User bị khóa → throw "Tài khoản đã bị khóa..."
        // =====================================================================
        [Fact]
        public async Task ShouldThrow_WhenUserInactive()
            {
            AddUser(1, active: false);

            var dto = ValidDto(1);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(dto));

            Assert.Equal("Tài khoản đã bị khóa. Không thể đăng bài tuyển dụng.", ex.Message);
            }

        // =====================================================================
        // 5) Chưa có subscription → phải tự tạo Free subscription
        // =====================================================================
        [Fact]
        public async Task ShouldAutoCreateFreeSubscription_WhenUserHasNone()
            {
            AddUser(1);

            await _service.CreateEmployerPostAsync(ValidDto());

            var sub = _db.EmployerSubscriptions.FirstOrDefault(s => s.UserId == 1 && s.PlanId == 1);
            Assert.NotNull(sub);
            }

        // =====================================================================
        // 6) Free subscription đã hết hạn → phải reset lại (RemainingPosts = MaxPosts)
        // =====================================================================
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

            var sub = _db.EmployerSubscriptions.First(s => s.UserId == 1 && s.PlanId == 1);

            // đúng theo logic create: reset về 1 → trừ 1 → còn 0
            Assert.Equal(0, sub.RemainingPosts);
            }


        // =====================================================================
        // 7) Có cả Free và Paid → phải ưu tiên trừ vào gói Paid
        // =====================================================================
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
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 2,
                RemainingPosts = 5,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });
            _db.SaveChanges();

            await _service.CreateEmployerPostAsync(ValidDto());

            var paid = _db.EmployerSubscriptions.First(s => s.PlanId == 2);
            Assert.Equal(4, paid.RemainingPosts); // bị trừ 1
            }

        // =====================================================================
        // 8) RemainingPosts = 0 → throw "Bạn đã hết lượt đăng bài."
        // =====================================================================
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
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });
            _db.SaveChanges();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(ValidDto()));

            Assert.Equal("Bạn đã hết lượt đăng bài.", ex.Message);
            }

        // =====================================================================
        // 9) Đăng bài thành công → RemainingPosts phải giảm 1
        // =====================================================================
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
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });
            _db.SaveChanges();

            await _service.CreateEmployerPostAsync(ValidDto());

            var sub = _db.EmployerSubscriptions.First(s => s.UserId == 1 && s.PlanId == 1);
            Assert.Equal(0, sub.RemainingPosts);
            }

        // =====================================================================
        // 10) Có DetailAddress → Location phải chứa "123 ABC"
        // =====================================================================
        [Fact]
        public async Task ShouldBuildLocationWithDetailAddress()
            {
            AddUser(1);

            // cần có subscription active
            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });
            _db.SaveChanges();

            var result = await _service.CreateEmployerPostAsync(ValidDto());

            Assert.Contains("123 ABC", result.Post.Location);
            }

        // =====================================================================
        // 11) DetailAddress rỗng → Location chỉ là Ward/District/Province
        // =====================================================================
        [Fact]
        public async Task ShouldBuildLocationWithoutDetail()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });
            _db.SaveChanges();

            var dto = ValidDto();
            dto.DetailAddress = "";

            var result = await _service.CreateEmployerPostAsync(dto);

            Assert.Equal("Ward 3, District 2, Province 1", result.Post.Location);
            }

        // =====================================================================
        // 12) SalaryMin < 0 → throw "SalaryMin không hợp lệ."
        // =====================================================================
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
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });
            _db.SaveChanges();

            var dto = ValidDto();
            dto.SalaryMin = -1;

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(dto));

            Assert.Equal("SalaryMin không hợp lệ.", ex.Message);
            }

        // =====================================================================
        // 13) SalaryMax < SalaryMin → throw "SalaryMax phải ≥ SalaryMin."
        // =====================================================================
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
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });
            _db.SaveChanges();

            var dto = ValidDto();
            dto.SalaryMin = 10;
            dto.SalaryMax = 5;

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(dto));

            Assert.Equal("SalaryMax phải ≥ SalaryMin.", ex.Message);
            }

        // =====================================================================
        // 14) Có Min/Max nhưng SalaryType = null → throw
        // =====================================================================
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
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });
            _db.SaveChanges();

            var dto = ValidDto();
            dto.SalaryMin = 5;
            dto.SalaryMax = 10;
            dto.SalaryType = null;

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateEmployerPostAsync(dto));

            Assert.Equal("SalaryType là bắt buộc.", ex.Message);
            }

        // =====================================================================
        // 15) Không nhập SalaryMin/Max/Type → cho phép (lương thỏa thuận)
        // =====================================================================
        [Fact]
        public async Task ShouldAllowNegotiableSalary_WhenAllNull()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });
            _db.SaveChanges();

            var dto = ValidDto();
            dto.SalaryMin = null;
            dto.SalaryMax = null;
            dto.SalaryType = null;

            var result = await _service.CreateEmployerPostAsync(dto);

            Assert.NotNull(result.Post);
            Assert.Null(result.Post.SalaryMin);
            Assert.Null(result.Post.SalaryMax);
            Assert.Null(result.Post.SalaryType);
            }

        // =====================================================================
        // 16) Có Images → phải gọi UploadImageAsync
        // =====================================================================
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
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });
            _db.SaveChanges();

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.ContentType).Returns("image/png");

            _image.Setup(x => x.UploadImageAsync(fileMock.Object, "EmployerPosts"))
                  .ReturnsAsync(("url123", "pid123"));

            var dto = ValidDto();
            dto.Images = new List<IFormFile> { fileMock.Object };

            await _service.CreateEmployerPostAsync(dto);

            _image.Verify(x => x.UploadImageAsync(fileMock.Object, "EmployerPosts"), Times.Once);
            }

        // =====================================================================
        // 17) Không có Images → không được gọi UploadImageAsync
        // =====================================================================
        [Fact]
        public async Task ShouldNotUploadImages_WhenNoneProvided()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });
            _db.SaveChanges();

            var dto = ValidDto();
            dto.Images = null;

            await _service.CreateEmployerPostAsync(dto);

            _image.Verify(
                x => x.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()),
                Times.Never);
            }

        // =====================================================================
        // 18) Đăng bài thành công → repository.AddAsync phải được gọi
        // =====================================================================
        [Fact]
        public async Task ShouldCallRepositoryAddAsync()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });
            _db.SaveChanges();

            await _service.CreateEmployerPostAsync(ValidDto());

            _repo.Verify(r => r.AddAsync(It.IsAny<EmployerPost>()), Times.Once);
            }

        // =====================================================================
        // 19) Có follower active → phải gửi notification
        // =====================================================================
        [Fact]
        public async Task ShouldSendNotificationToFollowers()
            {
            AddUser(1);

            _db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                RemainingPosts = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(10),
                Status = "Active"
                });

            _db.EmployerProfiles.Add(new EmployerProfile
                {
                UserId = 1,
                DisplayName = "Công ty A"
                });

            _db.EmployerFollowers.Add(new EmployerFollower
                {
                EmployerId = 1,
                JobSeekerId = 100,
                IsActive = true
                });

            _db.SaveChanges();

            await _service.CreateEmployerPostAsync(ValidDto());

            _noti.Verify(x => x.SendAsync(It.IsAny<CreateNotificationDto>()), Times.AtLeastOnce);
            }
        }
    }
