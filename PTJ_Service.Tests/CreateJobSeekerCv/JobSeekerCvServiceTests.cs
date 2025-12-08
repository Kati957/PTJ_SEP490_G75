using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.Models;
using PTJ_Models.DTO.CvDTO;
using CvService = PTJ_Service.JobSeekerCvService.Implementations.JobSeekerCvService;
using PTJ_Service.LocationService;

namespace PTJ_Service.Tests
{
    // Fake để override BuildAddressAsync vì LocationDisplayService là class thường không mock được
    public class FakeLocationDisplayService : LocationDisplayService
    {
        public FakeLocationDisplayService() : base(null) { }

        public override Task<string> BuildAddressAsync(int p, int d, int w)
        {
            return Task.FromResult("Fake Address");
        }
    }

    public class JobSeekerCvServiceTests
    {
        private readonly Mock<IJobSeekerCvRepository> _repo;
        private readonly FakeLocationDisplayService _location;
        private readonly JobMatchingDbContext _db;
        private readonly CvService _service;

        public JobSeekerCvServiceTests()
        {
            _repo = new Mock<IJobSeekerCvRepository>();
            _location = new FakeLocationDisplayService();

            var options = new DbContextOptionsBuilder<JobMatchingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Mỗi test dùng DB mới
                .Options;

            _db = new JobMatchingDbContext(options);

            _service = new CvService(_repo.Object, _location, _db);
        }

        // =====================================================================
        // GET: JobSeeker xem CV của chính mình
        // =====================================================================

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_If_Not_Owner()
        {
            _repo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new JobSeekerCv { Cvid = 10, JobSeekerId = 99 });

            var result = await _service.GetByIdAsync(10, 5);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Dto_When_Owner_Valid()
        {
            _repo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new JobSeekerCv
                {
                    Cvid = 10,
                    JobSeekerId = 5,
                    Cvtitle = "Test CV",
                    ProvinceId = 1,
                    DistrictId = 2,
                    WardId = 3,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });

            var result = await _service.GetByIdAsync(10, 5);

            result.Should().NotBeNull();
            result!.CvTitle.Should().Be("Test CV");
            result.PreferredLocationName.Should().Be("Fake Address");
        }

        // =====================================================================
        // GET: Employer xem CV (không cần check owner)
        // =====================================================================

        [Fact]
        public async Task GetByIdForEmployerAsync_Should_Return_Null_If_Not_Found()
        {
            _repo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync((JobSeekerCv?)null);

            var result = await _service.GetByIdForEmployerAsync(10);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForEmployerAsync_Should_Return_Dto()
        {
            _repo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new JobSeekerCv
                {
                    Cvid = 10,
                    JobSeekerId = 5,
                    Cvtitle = "Employer CV",
                    ProvinceId = 1,
                    DistrictId = 2,
                    WardId = 3,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });

            var result = await _service.GetByIdForEmployerAsync(10);

            result.Should().NotBeNull();
            result!.CvTitle.Should().Be("Employer CV");
        }

        // =====================================================================
        // GET: Lấy toàn bộ CV của JobSeeker
        // =====================================================================

        [Fact]
        public async Task GetByJobSeekerAsync_Should_Return_Empty_List()
        {
            _repo.Setup(r => r.GetByJobSeekerAsync(5))
                .ReturnsAsync(new List<JobSeekerCv>());

            var result = await _service.GetByJobSeekerAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByJobSeekerAsync_Should_Return_List_Of_Dtos()
        {
            _repo.Setup(r => r.GetByJobSeekerAsync(5))
                .ReturnsAsync(new List<JobSeekerCv>
                {
                    new JobSeekerCv {
                        Cvid = 1, JobSeekerId = 5,
                        Cvtitle = "CV1",
                        ProvinceId = 1, DistrictId = 2, WardId = 3,
                        CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now
                    },
                    new JobSeekerCv {
                        Cvid = 2, JobSeekerId = 5,
                        Cvtitle = "CV2",
                        ProvinceId = 1, DistrictId = 2, WardId = 3,
                        CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now
                    }
                });

            var result = await _service.GetByJobSeekerAsync(5);

            result.Should().HaveCount(2);
            result.First().PreferredLocationName.Should().Be("Fake Address");
        }

        // =====================================================================
        // CREATE CV
        // =====================================================================

        [Fact]
        public async Task CreateAsync_Should_Create_Successfully()
        {
            _repo.Setup(r => r.AddAsync(It.IsAny<JobSeekerCv>()))
                .Returns(Task.CompletedTask);

            var dto = new JobSeekerCvCreateDto
            {
                CvTitle = "New CV"
            };

            var result = await _service.CreateAsync(5, dto);

            result.CvTitle.Should().Be("New CV");
        }

        // =====================================================================
        // UPDATE CV
        // =====================================================================

        [Fact]
        public async Task UpdateAsync_Should_Return_False_If_Not_Found()
        {
            _repo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync((JobSeekerCv?)null);

            var result = await _service.UpdateAsync(5, 10, new JobSeekerCvUpdateDto());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_Should_Return_False_If_Not_Owner()
        {
            _repo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new JobSeekerCv { Cvid = 10, JobSeekerId = 99 });

            var result = await _service.UpdateAsync(5, 10, new JobSeekerCvUpdateDto());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_Should_Update_And_Return_True()
        {
            _repo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new JobSeekerCv
                {
                    Cvid = 10,
                    JobSeekerId = 5,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });

            _repo.Setup(r => r.UpdateAsync(It.IsAny<JobSeekerCv>()))
                .Returns(Task.CompletedTask);

            var result = await _service.UpdateAsync(5, 10, new JobSeekerCvUpdateDto());

            result.Should().BeTrue();
        }

        // =====================================================================
        // DELETE CV
        // =====================================================================

        [Fact]
        public async Task DeleteAsync_Should_Throw_If_Cv_In_Use()
        {
            _db.JobSeekerPosts.Add(new JobSeekerPost
            {
                SelectedCvId = 10,
                Status = "Active"
            });
            await _db.SaveChangesAsync();

            var act = async () => await _service.DeleteAsync(5, 10);

            await act.Should().ThrowAsync<Exception>()
                     .WithMessage("Không thể xoá CV vì bạn đang sử dụng CV này*");
        }

        [Fact]
        public async Task DeleteAsync_Should_Return_False_If_Cv_Not_Found()
        {
            var result = await _service.DeleteAsync(5, 10);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_Should_Delete_Successfully()
        {
            _repo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new JobSeekerCv
                {
                    Cvid = 10,
                    JobSeekerId = 5
                });

            _repo.Setup(r => r.SoftDeleteAsync(10))
                .Returns(Task.CompletedTask);

            var result = await _service.DeleteAsync(5, 10);

            result.Should().BeTrue();
        }
    }
}
