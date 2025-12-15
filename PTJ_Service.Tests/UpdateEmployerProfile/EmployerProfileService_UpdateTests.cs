using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using PTJ_Data;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Repositories.Interfaces;
using PTJ_Service.LocationService;
using PTJ_Services.Implementations;
using Xunit;

namespace PTJ_Service.Tests.EmployerProfiles
    {
    public class EmployerProfileService_UpdateTests
        {
        // ============================
        // 1. Helper: InMemory DbContext
        // ============================
        private JobMatchingDbContext CreateDb()
            {
            var opt = new DbContextOptionsBuilder<JobMatchingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new JobMatchingDbContext(opt);
            }

        // ============================
        // 2. Helper: Fake IConfiguration
        // ============================
        private IConfiguration FakeConfig()
            {
            var mock = new Mock<IConfiguration>();
            mock.Setup(x => x["Cloudinary:CloudName"]).Returns("demo");
            mock.Setup(x => x["Cloudinary:ApiKey"]).Returns("123");
            mock.Setup(x => x["Cloudinary:ApiSecret"]).Returns("456");
            return mock.Object;
            }

        // ============================
        // 3. Helper: DTO hợp lệ
        // ============================
        private EmployerProfileUpdateDto ValidDto() =>
            new EmployerProfileUpdateDto
                {
                DisplayName = "Company ABC",
                Description = "Description OK",
                ContactName = "John Doe",
                ContactPhone = "0987654321",
                ContactEmail = "company@mail.com",
                ProvinceId = 1,
                DistrictId = 1,
                WardId = 1,
                FullLocation = "123 Street",
                Website = "https://company.com"
                };

        // =====================================================
        // 4. TEST SERVICE UpdateProfileAsync
        // =====================================================

        // TC01 – Happy case: Profile tồn tại, dữ liệu hợp lệ
        [Fact]
        public async Task UpdateProfileAsync_ShouldReturnTrue_WhenValid()
            {
            // Arrange
            var db = CreateDb();
            var repo = new Mock<IEmployerProfileRepository>();

            // Dùng real VnPostLocationService với HttpClient thật
            var locationService = new VnProstLocationService(new HttpClient());

            var existing = new EmployerProfile
                {
                UserId = 10,
                DisplayName = "Old Name"
                };

            repo.Setup(r => r.GetByUserIdAsync(10))
                .ReturnsAsync(existing);

            repo.Setup(r => r.UpdateAsync(existing))
                .Returns(Task.CompletedTask);

            var service = new EmployerProfileService(
                repo.Object,
                FakeConfig(),
                db,
                locationService);

            var dto = ValidDto();

            // Act
            var result = await service.UpdateProfileAsync(10, dto);

            // Assert
            Assert.True(result);
            Assert.Equal("Company ABC", existing.DisplayName);
            Assert.Equal("0987654321", existing.ContactPhone);
            repo.Verify(r => r.UpdateAsync(existing), Times.Once);
            }

        // TC02 – Profile không tồn tại → false
        [Fact]
        public async Task UpdateProfileAsync_ShouldReturnFalse_WhenProfileNotFound()
            {
            // Arrange
            var db = CreateDb();
            var repo = new Mock<IEmployerProfileRepository>();
            var locationService = new VnProstLocationService(new HttpClient());

            repo.Setup(r => r.GetByUserIdAsync(10))
                .ReturnsAsync((EmployerProfile)null);

            var service = new EmployerProfileService(
                repo.Object,
                FakeConfig(),
                db,
                locationService);

            // Act
            var result = await service.UpdateProfileAsync(10, ValidDto());

            // Assert
            Assert.False(result);
            repo.Verify(r => r.UpdateAsync(It.IsAny<EmployerProfile>()), Times.Never);
            }

        // =====================================================
        // 5. TEST VALIDATION CHO EmployerProfileUpdateDto
        // =====================================================

        // TC03 – DisplayName missing
        [Fact]
        public void Validate_ShouldFail_WhenDisplayNameMissing()
            {
            var dto = ValidDto();
            dto.DisplayName = null;

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), validateAllProperties: true));
            }

        // TC04 – DisplayName quá 500 ký tự
        [Fact]
        public void Validate_ShouldFail_WhenDisplayNameTooLong()
            {
            var dto = ValidDto();
            dto.DisplayName = new string('a', 501); // > 500

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), true));
            }

        // TC05 – Description quá 1000 ký tự
        [Fact]
        public void Validate_ShouldFail_WhenDescriptionTooLong()
            {
            var dto = ValidDto();
            dto.Description = new string('x', 1001); // > 1000

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), true));
            }

        // TC06 – ContactName quá 500 ký tự
        [Fact]
        public void Validate_ShouldFail_WhenContactNameTooLong()
            {
            var dto = ValidDto();
            dto.ContactName = new string('c', 501); // > 500

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), true));
            }

        // TC07 – ContactPhone missing
        [Fact]
        public void Validate_ShouldFail_WhenPhoneMissing()
            {
            var dto = ValidDto();
            dto.ContactPhone = null;

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), true));
            }

        // TC08 – ContactPhone sai định dạng (quá ngắn / quá dài / không phải số)
        [Theory]
        [InlineData("123")]            // quá ngắn
        [InlineData("abcdefghijk")]    // không phải số
        [InlineData("123456789012")]   // quá dài
        public void Validate_ShouldFail_WhenPhoneInvalid(string phone)
            {
            var dto = ValidDto();
            dto.ContactPhone = phone;

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), true));
            }

        // TC09 – ContactEmail missing
        [Fact]
        public void Validate_ShouldFail_WhenEmailMissing()
            {
            var dto = ValidDto();
            dto.ContactEmail = null;

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), true));
            }

        // TC10 – ContactEmail sai định dạng
        [Theory]
        [InlineData("abc")]
        [InlineData("@mail.com")]
        [InlineData("test@")]
        public void Validate_ShouldFail_WhenEmailInvalid(string email)
            {
            var dto = ValidDto();
            dto.ContactEmail = email;

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), true));
            }

        // TC11 – ProvinceId = 0
        [Fact]
        public void Validate_ShouldFail_WhenProvinceInvalid()
            {
            var dto = ValidDto();
            dto.ProvinceId = 0;

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), true));
            }

        // TC12 – DistrictId = 0
        [Fact]
        public void Validate_ShouldFail_WhenDistrictInvalid()
            {
            var dto = ValidDto();
            dto.DistrictId = 0;

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), true));
            }

        // TC13 – WardId = 0
        [Fact]
        public void Validate_ShouldFail_WhenWardInvalid()
            {
            var dto = ValidDto();
            dto.WardId = 0;

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), true));
            }

        // TC14 – FullLocation quá 500 ký tự
        [Fact]
        public void Validate_ShouldFail_WhenFullLocationTooLong()
            {
            var dto = ValidDto();
            dto.FullLocation = new string('L', 501); // > 500

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), true));
            }

        // TC15 – Website sai định dạng URL
        [Fact]
        public void Validate_ShouldFail_WhenWebsiteInvalid()
            {
            var dto = ValidDto();
            dto.Website = "abc123"; // không phải URL

            Assert.Throws<ValidationException>(() =>
                Validator.ValidateObject(dto, new ValidationContext(dto), true));
            }
        }
    }
