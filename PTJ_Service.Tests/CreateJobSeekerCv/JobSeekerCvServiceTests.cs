using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using PTJ_Models.DTO.CvDTO;
using PTJ_Service.JobSeekerCvService.Interfaces;
using PTJ_API.Controllers;
using Microsoft.AspNetCore.Http;

public class JobSeekerCvController_CreateTests
{
    private readonly Mock<IJobSeekerCvService> _service;
    private readonly JobSeekerCvController _controller;

    public JobSeekerCvController_CreateTests()
    {
        _service = new Mock<IJobSeekerCvService>();
        _controller = new JobSeekerCvController(_service.Object);

        // Fake token userId = 5
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "5")
        }, "mock"));

        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = user
        };
    }

    // Helper validate
    private void ValidateDto(object dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, context, results, true);

        foreach (var validation in results)
        {
            _controller.ModelState.AddModelError(validation.MemberNames.First(), validation.ErrorMessage);
        }
    }

    // ----------------------------------------------------
    // 1️⃣ Success Case — All Valid
    // ----------------------------------------------------
    [Fact]
    public async Task Create_Should_Return_Success_When_Valid()
    {
        var dto = new JobSeekerCvCreateDto
        {
            CvTitle = "My CV",
            ContactPhone = "0901234567",
            ProvinceId = 1,
            DistrictId = 1,
            WardId = 1
        };

        ValidateDto(dto);

        _service.Setup(s => s.CreateAsync(5, dto))
            .ReturnsAsync(new JobSeekerCvResultDto { CvTitle = "My CV" });

        var result = await _controller.Create(dto) as OkObjectResult;

        result.Should().NotBeNull();

        result!.Value.Should().BeEquivalentTo(new
        {
            success = true,
            message = "Tạo CV thành công.",
            data = new { CvTitle = "My CV" }
        });
    }

    // ----------------------------------------------------
    // 2️⃣ PhoneNumber = null
    // ----------------------------------------------------
    [Fact]
    public async Task Create_Should_Return_Error_When_Phone_Is_Null()
    {
        var dto = new JobSeekerCvCreateDto
        {
            CvTitle = "Test CV",
            ContactPhone = null,
            ProvinceId = 1,
            DistrictId = 1,
            WardId = 1
        };

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;

        result.Should().NotBeNull();

        result!.Value.Should().BeEquivalentTo(new
        {
            success = false,
            message = "Dữ liệu không hợp lệ.",
            errors = new[] { "Please enter PhoneNumber" }
        });
    }

    // ----------------------------------------------------
    // 3️⃣ PhoneNumber invalid
    // ----------------------------------------------------
    [Fact]
    public async Task Create_Should_Return_Error_When_Phone_Invalid()
    {
        var dto = new JobSeekerCvCreateDto
        {
            CvTitle = "Test CV",
            ContactPhone = "abc123",
            ProvinceId = 1,
            DistrictId = 1,
            WardId = 1
        };

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;

        result.Should().NotBeNull();

        result!.Value.Should().BeEquivalentTo(new
        {
            success = false,
            message = "Dữ liệu không hợp lệ.",
            errors = new[] { "Invalid PhoneNumber" }
        });
    }

    // ----------------------------------------------------
    // 4️⃣ CvTitle null
    // ----------------------------------------------------
    [Fact]
    public async Task Create_Should_Return_Error_When_Title_Null()
    {
        var dto = new JobSeekerCvCreateDto
        {
            CvTitle = null,
            ContactPhone = "0901234567",
            ProvinceId = 1,
            DistrictId = 1,
            WardId = 1
        };

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;

        result.Should().NotBeNull();

        result!.Value.Should().BeEquivalentTo(new
        {
            success = false,
            message = "Dữ liệu không hợp lệ.",
            errors = new[] { "Please enter a Title" }
        });
    }

    // ----------------------------------------------------
    // 5️⃣ ProvinceId = 0
    // ----------------------------------------------------
    [Fact]
    public async Task Create_Should_Return_Error_When_Province_Zero()
    {
        var dto = new JobSeekerCvCreateDto
        {
            CvTitle = "CV",
            ContactPhone = "0901234567",
            ProvinceId = 0,
            DistrictId = 1,
            WardId = 1
        };

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;

        result.Should().NotBeNull();

        result!.Value.Should().BeEquivalentTo(new
        {
            success = false,
            message = "Dữ liệu không hợp lệ.",
            errors = new[] { "Please select province/city" }
        });
    }

    // ----------------------------------------------------
    // 6️⃣ DistrictId = 0
    // ----------------------------------------------------
    [Fact]
    public async Task Create_Should_Return_Error_When_District_Zero()
    {
        var dto = new JobSeekerCvCreateDto
        {
            CvTitle = "CV",
            ContactPhone = "0901234567",
            ProvinceId = 1,
            DistrictId = 0,
            WardId = 1
        };

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;

        result.Should().NotBeNull();

        result!.Value.Should().BeEquivalentTo(new
        {
            success = false,
            message = "Dữ liệu không hợp lệ.",
            errors = new[] { "Please select district" }
        });
    }

    // ----------------------------------------------------
    // 7️⃣ WardId = 0
    // ----------------------------------------------------
    [Fact]
    public async Task Create_Should_Return_Error_When_Ward_Zero()
    {
        var dto = new JobSeekerCvCreateDto
        {
            CvTitle = "CV",
            ContactPhone = "0901234567",
            ProvinceId = 1,
            DistrictId = 1,
            WardId = 0
        };

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;

        result.Should().NotBeNull();

        result!.Value.Should().BeEquivalentTo(new
        {
            success = false,
            message = "Dữ liệu không hợp lệ.",
            errors = new[] { "Please select ward/commune" }
        });
    }

    // ----------------------------------------------------
    // 8️⃣ SkillSummary null → SUCCESS
    // ----------------------------------------------------
    [Fact]
    public async Task Create_Should_Succeed_When_SkillSummary_Null()
    {
        var dto = new JobSeekerCvCreateDto
        {
            CvTitle = "CV",
            ContactPhone = "0901234567",
            ProvinceId = 1,
            DistrictId = 1,
            WardId = 1,
            SkillSummary = null
        };

        ValidateDto(dto);

        _service.Setup(s => s.CreateAsync(5, dto))
            .ReturnsAsync(new JobSeekerCvResultDto { CvTitle = "CV" });

        var result = await _controller.Create(dto) as OkObjectResult;

        result.Should().NotBeNull();
        result!.Value.Should().BeEquivalentTo(new
        {
            success = true,
            message = "Tạo CV thành công.",
            data = new { CvTitle = "CV" }
        });
    }

    // ----------------------------------------------------
    // 9️⃣ Skills null → SUCCESS
    // ----------------------------------------------------
    [Fact]
    public async Task Create_Should_Succeed_When_Skills_Null()
    {
        var dto = new JobSeekerCvCreateDto
        {
            CvTitle = "CV",
            ContactPhone = "0901234567",
            ProvinceId = 1,
            DistrictId = 1,
            WardId = 1,
            Skills = null
        };

        ValidateDto(dto);

        _service.Setup(s => s.CreateAsync(5, dto))
            .ReturnsAsync(new JobSeekerCvResultDto { CvTitle = "CV" });

        var result = await _controller.Create(dto) as OkObjectResult;

        result.Should().NotBeNull();
        result!.Value.Should().BeEquivalentTo(new
        {
            success = true,
            message = "Tạo CV thành công.",
            data = new { CvTitle = "CV" }
        });
    }

    // ----------------------------------------------------
    // 🔟 JobType null → SUCCESS
    // ----------------------------------------------------
    [Fact]
    public async Task Create_Should_Succeed_When_JobType_Null()
    {
        var dto = new JobSeekerCvCreateDto
        {
            CvTitle = "CV",
            ContactPhone = "0901234567",
            ProvinceId = 1,
            DistrictId = 1,
            WardId = 1,
            PreferredJobType = null
        };

        ValidateDto(dto);

        _service.Setup(s => s.CreateAsync(5, dto))
            .ReturnsAsync(new JobSeekerCvResultDto { CvTitle = "CV" });

        var result = await _controller.Create(dto) as OkObjectResult;

        result.Should().NotBeNull();
        result!.Value.Should().BeEquivalentTo(new
        {
            success = true,
            message = "Tạo CV thành công.",
            data = new { CvTitle = "CV" }
        });
    }
}
