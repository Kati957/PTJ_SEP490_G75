using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;

using PTJ_API.Controllers.AuthController;
using PTJ_Service.AuthService.Interfaces;
using PTJ_Models.DTO.Auth;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

public class AuthController_RegisterJobSeekerTests
{
    private readonly Mock<IAuthService> _service = new();
    private readonly Mock<IConfiguration> _cfg = new();
    private readonly AuthController _controller;

    public AuthController_RegisterJobSeekerTests()
    {
        _controller = new AuthController(_service.Object, _cfg.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private void ValidateDto(object dto)
    {
        var ctx = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, ctx, results, true);

        foreach (var err in results)
            _controller.ModelState.AddModelError(err.MemberNames.First(), err.ErrorMessage);
    }

    private IDictionary<string, object?> ToDict(object obj)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var p in obj.GetType().GetProperties())
            dict[p.Name] = p.GetValue(obj);

        return dict;
    }

    // 1) EMAIL NULL
    [Fact]
    public async Task RegisterJobSeeker_Should_Return_Error_When_Email_Null()
    {
        var dto = new RegisterJobSeekerDto
        {
            Email = null,
            Password = "123456",
            FullName = "Nguyen Van A"
        };

        ValidateDto(dto);

        var result = await _controller.RegisterJobSeeker(dto) as BadRequestObjectResult;
        result.Should().NotBeNull();

        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Dữ liệu không hợp lệ.");
        body["errors"].Should().NotBeNull();
    }

    // 2) EMAIL INVALID
    [Fact]
    public async Task RegisterJobSeeker_Should_Return_Error_When_Email_Invalid()
    {
        var dto = new RegisterJobSeekerDto
        {
            Email = "abc123",
            Password = "123456",
            FullName = "Nguyen Van A"
        };

        ValidateDto(dto);

        var result = await _controller.RegisterJobSeeker(dto) as BadRequestObjectResult;
        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Dữ liệu không hợp lệ.");
        body["errors"].Should().NotBeNull();

        _controller.ModelState["Email"].Errors[0].ErrorMessage
            .Should().Be("Invalid email format.");
    }

    // 3) PASSWORD NULL
    [Fact]
    public async Task RegisterJobSeeker_Should_Return_Error_When_Password_Null()
    {
        var dto = new RegisterJobSeekerDto
        {
            Email = "test@gmail.com",
            Password = null,
            FullName = "Nguyen Van A"
        };

        ValidateDto(dto);

        var result = await _controller.RegisterJobSeeker(dto) as BadRequestObjectResult;
        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Dữ liệu không hợp lệ.");
        body["errors"].Should().NotBeNull();
    }

    // 4) PASSWORD SHORT
    [Fact]
    public async Task RegisterJobSeeker_Should_Return_Error_When_Password_Short()
    {
        var dto = new RegisterJobSeekerDto
        {
            Email = "test@gmail.com",
            Password = "123",
            FullName = "Nguyen Van A"
        };

        ValidateDto(dto);

        var result = await _controller.RegisterJobSeeker(dto) as BadRequestObjectResult;
        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Dữ liệu không hợp lệ.");
        body["errors"].Should().NotBeNull();
    }

    // 5) FULLNAME SHORT
    [Fact]
    public async Task RegisterJobSeeker_Should_Return_Error_When_FullName_Short()
    {
        var dto = new RegisterJobSeekerDto
        {
            Email = "test@gmail.com",
            Password = "123456",
            FullName = "A"
        };

        ValidateDto(dto);

        var result = await _controller.RegisterJobSeeker(dto) as BadRequestObjectResult;
        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Dữ liệu không hợp lệ.");
        body["errors"].Should().NotBeNull();
    }

    // 6) EMAIL DUPLICATE
    [Fact]
    public async Task RegisterJobSeeker_Should_Return_Error_When_Email_Exists()
    {
        var dto = new RegisterJobSeekerDto
        {
            Email = "dup@gmail.com",
            Password = "123456",
            FullName = "Nguyen Van A"
        };

        _service.Setup(x => x.RegisterJobSeekerAsync(dto))
                .ThrowsAsync(new Exception("Email này đã được sử dụng."));

        var result = await _controller.RegisterJobSeeker(dto) as BadRequestObjectResult;
        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Email này đã được sử dụng.");
    }

    // 7) SUCCESS CASE
    [Fact]
    public async Task RegisterJobSeeker_Should_Return_Success()
    {
        var dto = new RegisterJobSeekerDto
        {
            Email = "ok@gmail.com",
            Password = "123456",
            FullName = "Nguyen Van A"
        };

        _service.Setup(x => x.RegisterJobSeekerAsync(dto))
                .ReturnsAsync(new AuthResponseDto { Success = true });

        var result = await _controller.RegisterJobSeeker(dto) as OkObjectResult;
        var body = ToDict(result!.Value);

        body["success"].Should().Be(true);
        body["message"].Should().Be("Vui lòng kiểm tra email để xác minh tài khoản của bạn.");
    }
}
