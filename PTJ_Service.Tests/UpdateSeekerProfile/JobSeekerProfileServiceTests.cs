using Xunit;
using Moq;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Repositories.Interfaces;
using PTJ_Services.Implementations;
using PTJ_Service.LocationService;
using Microsoft.Extensions.Configuration;

public class JobSeekerProfileServiceTests
    {
    // ============================================================
    // Fake cấu hình Cloudinary (KHÔNG BAO GIỜ DÙNG, chỉ để service chạy)
    // ============================================================
    private IConfiguration FakeConfig()
        {
        var mock = new Mock<IConfiguration>();
        mock.Setup(x => x["Cloudinary:CloudName"]).Returns("demo");
        mock.Setup(x => x["Cloudinary:ApiKey"]).Returns("123");
        mock.Setup(x => x["Cloudinary:ApiSecret"]).Returns("456");
        return mock.Object;
        }

    // ============================================================
    // DTO hợp lệ
    // ============================================================
    private JobSeekerProfileUpdateDto ValidDto() =>
        new JobSeekerProfileUpdateDto
            {
            FullName = "Nguyen Van A",
            Gender = "Male",
            BirthYear = 1995,
            ProvinceId = 1,
            DistrictId = 1,
            WardId = 1,
            ContactPhone = "0987654321",
            FullLocation = "123 Street",
            ImageFile = null // ĐỂ SKIP UPLOAD
            };

    // ============================================================
    // TC01 – HAPPY CASE
    // ============================================================
    [Fact]
    public async Task UpdateProfile_ShouldReturnTrue_WhenValid()
        {
        var repo = new Mock<IJobSeekerProfileRepository>();
        var loc = new Mock<VnProstLocationService>(new HttpClient()).Object;

        var profile = new JobSeekerProfile { UserId = 10 };

        repo.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(profile);
        repo.Setup(r => r.UpdateAsync(profile)).Returns(Task.CompletedTask);

        var service = new JobSeekerProfileService(repo.Object, FakeConfig(), loc);

        var result = await service.UpdateProfileAsync(10, ValidDto());

        Assert.True(result);
        Assert.Equal("Nguyen Van A", profile.FullName);
        }

    // ============================================================
    // TC02 – PROFILE NOT FOUND
    // ============================================================
    [Fact]
    public async Task UpdateProfile_ShouldReturnFalse_WhenNotFound()
        {
        var repo = new Mock<IJobSeekerProfileRepository>();
        var loc = new Mock<VnProstLocationService>(new HttpClient()).Object;

        repo.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync((JobSeekerProfile?)null);

        var service = new JobSeekerProfileService(repo.Object, FakeConfig(), loc);

        var result = await service.UpdateProfileAsync(10, ValidDto());

        Assert.False(result);
        }

    // ============================================================
    // VALIDATION TESTS (THEO DTO)
    // ============================================================

    [Fact]
    public void Validate_FullNameMissing()
        {
        var dto = ValidDto();
        dto.FullName = null;
        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(dto, new ValidationContext(dto), true)
        );
        }

    [Fact]
    public void Validate_FullNameTooLong()
        {
        var dto = ValidDto();
        dto.FullName = new string('A', 201);
        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(dto, new ValidationContext(dto), true)
        );
        }

    [Fact]
    public void Validate_GenderInvalid()
        {
        var dto = ValidDto();
        dto.Gender = "Unknown";
        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(dto, new ValidationContext(dto), true)
        );
        }

    [Theory]
    [InlineData(1500)]
    [InlineData(3000)]
    public void Validate_BirthYearInvalid(int year)
        {
        var dto = ValidDto();
        dto.BirthYear = year;
        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(dto, new ValidationContext(dto), true)
        );
        }

    [Fact]
    public void Validate_ProvinceInvalid()
        {
        var dto = ValidDto();
        dto.ProvinceId = 0;
        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(dto, new ValidationContext(dto), true)
        );
        }

    [Fact]
    public void Validate_DistrictInvalid()
        {
        var dto = ValidDto();
        dto.DistrictId = 0;
        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(dto, new ValidationContext(dto), true)
        );
        }

    [Fact]
    public void Validate_WardInvalid()
        {
        var dto = ValidDto();
        dto.WardId = 0;
        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(dto, new ValidationContext(dto), true)
        );
        }

    [Fact]
    public void Validate_ContactPhoneMissing()
        {
        var dto = ValidDto();
        dto.ContactPhone = null;
        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(dto, new ValidationContext(dto), true)
        );
        }

    [Theory]
    [InlineData("12")]
    [InlineData("abcdefghijk")]
    [InlineData("123456789012")]
    public void Validate_ContactPhoneInvalid(string phone)
        {
        var dto = ValidDto();
        dto.ContactPhone = phone;
        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(dto, new ValidationContext(dto), true)
        );
        }

    [Fact]
    public void Validate_FullLocationTooLong()
        {
        var dto = ValidDto();
        dto.FullLocation = new string('L', 501);
        Assert.Throws<ValidationException>(() =>
            Validator.ValidateObject(dto, new ValidationContext(dto), true)
        );
        }

    // ============================================================
    // TC UPLOAD ẢNH — SKIP (Length = 0)
    // ============================================================
    [Fact]
    public async Task UpdateProfile_ShouldSkipUpload_WhenImageEmpty()
        {
        var repo = new Mock<IJobSeekerProfileRepository>();
        var loc = new Mock<VnProstLocationService>(new HttpClient()).Object;

        var profile = new JobSeekerProfile { UserId = 10 };

        repo.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(profile);

        var service = new JobSeekerProfileService(repo.Object, FakeConfig(), loc);

        var dto = ValidDto();
        dto.ImageFile = new FormFile(new MemoryStream(), 0, 0, "file", "img.png");

        var result = await service.UpdateProfileAsync(10, dto);

        Assert.True(result); // PASS vì skip upload
        }
    }
