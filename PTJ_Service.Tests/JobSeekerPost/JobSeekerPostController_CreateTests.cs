using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using PTJ_Models.DTO.PostDTO;
using PTJ_API.Controllers.Post;
using PTJ_Service.JobSeekerPostService.cs.Interfaces;

public class JobSeekerPostController_CreateTests
{
    private readonly Mock<IJobSeekerPostService> _service;
    private readonly JobSeekerPostController _controller;

    public JobSeekerPostController_CreateTests()
    {
        _service = new Mock<IJobSeekerPostService>();

        _controller = new JobSeekerPostController(_service.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "5"),
            new Claim(ClaimTypes.Role, "JobSeeker")
        }, "mock"));

        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
    }

    private void ValidateDto(object dto)
    {
        var ctx = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, ctx, results, true);

        foreach (var e in results)
            _controller.ModelState.AddModelError(e.MemberNames.First(), e.ErrorMessage);
    }

    private JobSeekerPostCreateDto BaseDto() => new()
    {
        UserID = 5,
        Title = "Customer support staff",
        Description = new string('x', 100),
        Age = 20,
        Gender = "nam",
        PreferredWorkHourStart = "07:00",
        PreferredWorkHourEnd = "10:00",
        ProvinceId = 1,
        DistrictId = 1,
        WardId = 1,
        CategoryID = 1,
        PhoneContact = "0866719337",
        SelectedCvId = 1
    };

    // TC01 SUCCESS
    [Fact]
    public async Task UTCID01_Success()
    {
        var dto = BaseDto();
        ValidateDto(dto);

        _service.Setup(x => x.CreateJobSeekerPostAsync(dto))
            .ReturnsAsync(new JobSeekerPostResultDto());

        var result = await _controller.Create(dto) as OkObjectResult;

        result.Should().NotBeNull();
        result!.Value.ToString().Should().Contain("Đăng bài tìm việc thành công");
    }

    // TC02 Title OK
    [Fact]
    public async Task UTCID02_Title_Valid()
    {
        var dto = BaseDto();
        dto.Title = "abcdE";

        ValidateDto(dto);

        _service.Setup(x => x.CreateJobSeekerPostAsync(dto))
            .ReturnsAsync(new JobSeekerPostResultDto());

        var result = await _controller.Create(dto) as OkObjectResult;
        result.Should().NotBeNull();
    }

    // TC03 Title 120 chars
    [Fact]
    public async Task UTCID03_Title_120chars()
    {
        var dto = BaseDto();
        dto.Title = new string('a', 120);

        ValidateDto(dto);

        _service.Setup(x => x.CreateJobSeekerPostAsync(dto))
            .ReturnsAsync(new JobSeekerPostResultDto());

        var result = await _controller.Create(dto) as OkObjectResult;
        result.Should().NotBeNull();
    }

    // TC04 Title too short
    [Fact]
    public async Task UTCID04_Title_TooShort()
    {
        var dto = BaseDto();
        dto.Title = "abc";

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;

        result!.Value.ToString().Should().Contain("Tiêu đề phải có ít nhất 5 ký tự!");
    }

    // TC05 Title too long
    [Fact]
    public async Task UTCID05_Title_TooLong()
    {
        var dto = BaseDto();
        dto.Title = new string('a', 121);
        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Tiêu đề không vượt quá 120 ký tự!");
    }

    // TC06 Title null
    [Fact]
    public async Task UTCID06_Title_Null()
    {
        var dto = BaseDto();
        dto.Title = null;

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Vui lòng nhập tiêu đề!");
    }

    // TC07 Category null
    [Fact]
    public async Task UTCID07_Category_Null()
    {
        var dto = BaseDto();
        dto.CategoryID = 0;

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Vui lòng chọn ngành nghề!");
    }

    // TC08 Province null
    [Fact]
    public async Task UTCID08_Province_Null()
    {
        var dto = BaseDto();
        dto.ProvinceId = 0;

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Vui lòng chọn tỉnh/thành!");
    }

    // TC09 District null
    [Fact]
    public async Task UTCID09_District_Null()
    {
        var dto = BaseDto();
        dto.DistrictId = 0;

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Vui lòng chọn Quận Huyện!");
    }

    // TC10 Ward null
    [Fact]
    public async Task UTCID10_Ward_Null()
    {
        var dto = BaseDto();
        dto.WardId = 0;

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Vui lòng chọn Phường Xã!");
    }

    // TC11 Description null
    [Fact]
    public async Task UTCID11_Description_Null()
    {
        var dto = BaseDto();
        dto.Description = null;

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Mô tả bản thân không được bỏ trống!");
    }

    // TC12 Description too short
    [Fact]
    public async Task UTCID12_Description_TooShort()
    {
        var dto = BaseDto();
        dto.Description = new string('x', 19);

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Mô tả bản thân phải từ 20 ký tự!");
    }

    // TC13 Work hours null
    [Fact]
    public async Task UTCID13_WorkHours_Null()
    {
        var dto = BaseDto();
        dto.PreferredWorkHourStart = null;
        dto.PreferredWorkHourEnd = null;

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Vui lòng chọn giờ Làm!");
    }

    // TC14 Invalid hours
    [Fact]
    public async Task UTCID14_WorkHours_InvalidRange()
    {
        var dto = BaseDto();
        dto.PreferredWorkHourStart = "11:00";
        dto.PreferredWorkHourEnd = "10:00";

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Giờ kết thúc phải sau giờ bắt đầu!");
    }

    // TC15 Phone invalid prefix
    [Fact]
    public async Task UTCID15_Phone_InvalidPrefix()
    {
        var dto = BaseDto();
        dto.PhoneContact = "8667193378";

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Số điện thoại không hợp lệ");
    }

    // TC16 Phone too short
    [Fact]
    public async Task UTCID16_Phone_TooShort()
    {
        var dto = BaseDto();
        dto.PhoneContact = "086671933";

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Số điện thoại không hợp lệ");
    }

    // TC17 Phone null
    [Fact]
    public async Task UTCID17_Phone_Null()
    {
        var dto = BaseDto();
        dto.PhoneContact = null;

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Vui lòng nhập số điện thoại!");
    }

    // TC18 Age null
    [Fact]
    public async Task UTCID18_Age_Null()
    {
        var dto = BaseDto();
        dto.Age = null;

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Vui lòng nhập tuổi của bạn");
    }

    // TC19 Age < 16
    [Fact]
    public async Task UTCID19_Age_Under16()
    {
        var dto = BaseDto();
        dto.Age = 15;

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Tuổi phải từ 16 đến 100.");
    }

    // TC20 Age > 100
    [Fact]
    public async Task UTCID20_Age_Over100()
    {
        var dto = BaseDto();
        dto.Age = 101;

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Tuổi phải từ 16 đến 100.");
    }

    // TC21 Gender null
    [Fact]
    public async Task UTCID21_Gender_Null()
    {
        var dto = BaseDto();
        dto.Gender = null;

        ValidateDto(dto);

        var result = await _controller.Create(dto) as BadRequestObjectResult;
        result!.Value.ToString().Should().Contain("Vui lòng chọn giới tính!");
    }

    // TC22 SelectedCV null → still success
    [Fact]
    public async Task UTCID22_SelectedCv_Null()
    {
        var dto = BaseDto();
        dto.SelectedCvId = null;

        ValidateDto(dto);

        _service.Setup(s => s.CreateJobSeekerPostAsync(dto))
            .ReturnsAsync(new JobSeekerPostResultDto());

        var result = await _controller.Create(dto) as OkObjectResult;
        result.Should().NotBeNull();
    }
}
