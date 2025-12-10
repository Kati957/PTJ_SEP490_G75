using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.ComponentModel.DataAnnotations;

using PTJ_API.Controllers;
using PTJ_Models.DTO.Auth;
using PTJ_Service.AuthService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PTJ_API.Controllers.AuthController;

public class LoginControllerTests
{
    private readonly Mock<IAuthService> _service;
    private readonly Mock<IConfiguration> _config;
    private readonly AuthController _controller;

    public LoginControllerTests()
    {
        _service = new Mock<IAuthService>();
        _config = new Mock<IConfiguration>();

        _controller = new AuthController(_service.Object, _config.Object);

        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
    }

    private void ValidateDto(object dto)
    {
        var ctx = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(dto, ctx, results, true);

        foreach (var e in results)
            _controller.ModelState.AddModelError(e.MemberNames.First(), e.ErrorMessage);
    }

    // ------------------------------------------------------
    // 01 — Success
    // ------------------------------------------------------
    [Fact]
    public async Task Login_Should_Return_Success_When_Valid()
    {
        var dto = new LoginDto
        {
            UsernameOrEmail = "nguyenminhtuan@gmail.com",
            Password = "123456"
        };

        ValidateDto(dto);

        _service.Setup(s => s.LoginAsync(dto, It.IsAny<string>()))
            .ReturnsAsync(new AuthResponseDto { AccessToken = "abc" });

        var actionResult = await _controller.Login(dto);

        var ok = actionResult.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeOfType<AuthResponseDto>();
    }

    // ------------------------------------------------------
    // 02 — Email null
    // ------------------------------------------------------
    [Fact]
    public async Task Login_Should_Return_Error_When_Email_Null()
    {
        var dto = new LoginDto
        {
            UsernameOrEmail = null,
            Password = "123456"
        };

        ValidateDto(dto);

        var result = await _controller.Login(dto);
        var bad = result.Result as BadRequestObjectResult;

        bad.Should().NotBeNull();
    }

    // ------------------------------------------------------
    // 03 — Password null
    // ------------------------------------------------------
    [Fact]
    public async Task Login_Should_Return_Error_When_Password_Null()
    {
        var dto = new LoginDto
        {
            UsernameOrEmail = "nguyen@gmail.com",
            Password = null
        };

        ValidateDto(dto);

        var result = await _controller.Login(dto);
        var bad = result.Result as BadRequestObjectResult;

        bad.Should().NotBeNull();
    }

    // ------------------------------------------------------
    // 04 — Wrong password
    // ------------------------------------------------------
    [Fact]
    public async Task Login_Should_Return_Error_When_Wrong_Password()
    {
        var dto = new LoginDto
        {
            UsernameOrEmail = "nguyen@gmail.com",
            Password = "wrongpass"
        };

        ValidateDto(dto);

        _service.Setup(s => s.LoginAsync(dto, It.IsAny<string>()))
            .ThrowsAsync(new Exception("Incorrect email or password"));

        var result = await _controller.Login(dto);

        var bad = result.Result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        bad!.Value.ToString().Should().Contain("Incorrect email or password");
    }

    // ------------------------------------------------------
    // 05 — Email invalid
    // ------------------------------------------------------
    [Fact]
    public async Task Login_Should_Return_Error_When_Email_Invalid()
    {
        var dto = new LoginDto
        {
            UsernameOrEmail = "111111",
            Password = "123456"
        };

        ValidateDto(dto);

        var result = await _controller.Login(dto);

        var bad = result.Result as BadRequestObjectResult;

        bad.Should().NotBeNull();
    }

    // ------------------------------------------------------
    // 06 — Email & Password null
    // ------------------------------------------------------
    [Fact]
    public async Task Login_Should_Return_Error_When_Email_And_Password_Null()
    {
        var dto = new LoginDto
        {
            UsernameOrEmail = null,
            Password = null
        };

        ValidateDto(dto);

        var result = await _controller.Login(dto);

        var bad = result.Result as BadRequestObjectResult;
        bad.Should().NotBeNull();
    }
}
