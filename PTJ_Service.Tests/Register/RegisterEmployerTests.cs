using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;

using PTJ_API.Controllers.AuthController;
using PTJ_Service.AuthService.Interfaces;
using PTJ_Models.DTO.Auth;

public class AuthController_RegisterEmployerTests
{
    private readonly Mock<IAuthService> _svc = new();
    private readonly Mock<IConfiguration> _cfg = new();
    private readonly AuthController _controller;

    public AuthController_RegisterEmployerTests()
    {
        _controller = new AuthController(_svc.Object, _cfg.Object);
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

    // 1️⃣ ContactPhone null
    [Fact]
    public async Task RegisterEmployer_Should_Return_Error_When_ContactPhone_Null()
    {
        var dto = new RegisterEmployerDto
        {
            ContactPhone = null,
            Email = "boss@gmail.com",
            Password = "123456"
        };

        ValidateDto(dto);

        var result = await _controller.RegisterEmployer(dto) as BadRequestObjectResult;
        result.Should().NotBeNull();

        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Dữ liệu không hợp lệ.");
        body["errors"].Should().NotBeNull();

        _controller.ModelState["ContactPhone"].Errors[0].ErrorMessage
            .Should().Be("Số điện thoại là bắt buộc.");
    }

    // 2️⃣ ContactEmail invalid (có giá trị nhưng sai format)
    [Fact]
    public async Task RegisterEmployer_Should_Return_Error_When_ContactEmail_Invalid()
    {
        var dto = new RegisterEmployerDto
        {
            ContactPhone = "0905123456",
            Email = "boss@gmail.com",
            Password = "123456",
            ContactEmail = "abc123"
        };

        ValidateDto(dto);

        var result = await _controller.RegisterEmployer(dto) as BadRequestObjectResult;
        result.Should().NotBeNull();

        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Dữ liệu không hợp lệ.");
        body["errors"].Should().NotBeNull();

        _controller.ModelState["ContactEmail"].Errors[0].ErrorMessage
            .Should().Be("Email liên hệ không hợp lệ.");
    }

    // 3️⃣ Account Email null
    [Fact]
    public async Task RegisterEmployer_Should_Return_Error_When_AccountEmail_Null()
    {
        var dto = new RegisterEmployerDto
        {
            ContactPhone = "0905123456",
            Email = null,
            Password = "123456"
        };

        ValidateDto(dto);

        var result = await _controller.RegisterEmployer(dto) as BadRequestObjectResult;
        result.Should().NotBeNull();

        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Dữ liệu không hợp lệ.");
        body["errors"].Should().NotBeNull();

        _controller.ModelState["Email"].Errors[0].ErrorMessage
            .Should().Be("Email tài khoản là bắt buộc.");
    }

    // 4️⃣ Account Email invalid
    [Fact]
    public async Task RegisterEmployer_Should_Return_Error_When_AccountEmail_Invalid()
    {
        var dto = new RegisterEmployerDto
        {
            ContactPhone = "0905123456",
            Email = "abc123",
            Password = "123456"
        };

        ValidateDto(dto);

        var result = await _controller.RegisterEmployer(dto) as BadRequestObjectResult;
        result.Should().NotBeNull();

        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Dữ liệu không hợp lệ.");
        body["errors"].Should().NotBeNull();

        _controller.ModelState["Email"].Errors[0].ErrorMessage
            .Should().Be("Email tài khoản không hợp lệ.");
    }

    // 5️⃣ Password null
    [Fact]
    public async Task RegisterEmployer_Should_Return_Error_When_Password_Null()
    {
        var dto = new RegisterEmployerDto
        {
            ContactPhone = "0905123456",
            Email = "boss@gmail.com",
            Password = null
        };

        ValidateDto(dto);

        var result = await _controller.RegisterEmployer(dto) as BadRequestObjectResult;
        result.Should().NotBeNull();

        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Dữ liệu không hợp lệ.");
        body["errors"].Should().NotBeNull();

        _controller.ModelState["Password"].Errors[0].ErrorMessage
            .Should().Be("Mật khẩu là bắt buộc.");
    }

    // 6️⃣ Password too short
    [Fact]
    public async Task RegisterEmployer_Should_Return_Error_When_Password_TooShort()
    {
        var dto = new RegisterEmployerDto
        {
            ContactPhone = "0905123456",
            Email = "boss@gmail.com",
            Password = "12"
        };

        ValidateDto(dto);

        var result = await _controller.RegisterEmployer(dto) as BadRequestObjectResult;
        result.Should().NotBeNull();

        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Dữ liệu không hợp lệ.");
        body["errors"].Should().NotBeNull();

        _controller.ModelState["Password"].Errors[0].ErrorMessage
            .Should().Be("Mật khẩu phải ít nhất 6 ký tự.");
    }

    // 7️⃣ Duplicate Email (service throw)
    [Fact]
    public async Task RegisterEmployer_Should_Return_Error_When_Email_AlreadyUsed()
    {
        var dto = new RegisterEmployerDto
        {
            ContactPhone = "0905123456",
            Email = "boss@gmail.com",
            Password = "123456"
        };

        _svc.Setup(s => s.SubmitEmployerRegistrationAsync(dto))
            .ThrowsAsync(new Exception("Email này đã được sử dụng."));

        var result = await _controller.RegisterEmployer(dto) as BadRequestObjectResult;
        result.Should().NotBeNull();

        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Email này đã được sử dụng.");
    }

    // 8️⃣ Pending request exists (service throw)
    [Fact]
    public async Task RegisterEmployer_Should_Return_Error_When_Request_Pending()
    {
        var dto = new RegisterEmployerDto
        {
            ContactPhone = "0905123456",
            Email = "boss@gmail.com",
            Password = "123456"
        };

        _svc.Setup(s => s.SubmitEmployerRegistrationAsync(dto))
            .ThrowsAsync(new Exception("Email này đã gửi yêu cầu và đang chờ duyệt."));

        var result = await _controller.RegisterEmployer(dto) as BadRequestObjectResult;
        result.Should().NotBeNull();

        var body = ToDict(result!.Value);

        body["success"].Should().Be(false);
        body["message"].Should().Be("Email này đã gửi yêu cầu và đang chờ duyệt.");
    }

    // 9️⃣ SUCCESS CASE
    [Fact]
    public async Task RegisterEmployer_Should_Return_Success()
    {
        var dto = new RegisterEmployerDto
        {
            CompanyName = "Highlands Coffee",
            ContactPhone = "0905123456",
            Email = "boss@gmail.com",
            Password = "123456"
        };

        var serviceResult = new
        {
            message = "Gửi yêu cầu đăng ký thành công. Vui lòng chờ quản trị viên phê duyệt.",
            requestId = 123
        };

        _svc.Setup(s => s.SubmitEmployerRegistrationAsync(dto))
            .ReturnsAsync(serviceResult);

        var result = await _controller.RegisterEmployer(dto) as OkObjectResult;
        result.Should().NotBeNull();

        var body = ToDict(result!.Value);

        body["success"].Should().Be(true);
        body["message"].Should().Be("Gửi yêu cầu đăng ký thành công. Vui lòng chờ quản trị viên phê duyệt.");
        body["data"].Should().NotBeNull();
    }
}
