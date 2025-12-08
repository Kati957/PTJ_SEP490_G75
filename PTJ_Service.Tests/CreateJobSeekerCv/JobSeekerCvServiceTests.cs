using Xunit;
using Moq;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;

using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.Models;
using PTJ_Models.DTO.CvDTO;
using PTJ_Service.LocationService;

// Alias để tránh lỗi namespace trùng tên
using CvService = PTJ_Service.JobSeekerCvService.Implementations.JobSeekerCvService;

namespace PTJ_Service.Tests
{
    public class JobSeekerCvServiceTests
    {
        private readonly Mock<IJobSeekerCvRepository> _repo;
        private readonly Mock<LocationDisplayService> _location;
        private readonly CvService _service;

        public JobSeekerCvServiceTests()
        {
            _repo = new Mock<IJobSeekerCvRepository>();
            _location = new Mock<LocationDisplayService>();

            _location.Setup(l => l.BuildAddressAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                     .ReturnsAsync("Địa chỉ test");

            _service = new CvService(_repo.Object, _location.Object);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_If_Not_Owner()
        {
            _repo.Setup(r => r.GetByIdAsync(10))
                 .ReturnsAsync(new JobSeekerCv { Cvid = 10, JobSeekerId = 99 });

            var result = await _service.GetByIdAsync(10, 5);
            result.Should().BeNull();
        }
    }
}
