using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Linq;

using PTJ_Data;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Service.Admin.Implementations;
using PTJ_Service.ImageService;
using PTJ_Service.Interfaces;
using PTJ_Models.DTO.Admin;
using PTJ_Models.DTO.Notification;
using PTJ_Models.Models;

public class AdminNewsService_CreateAsync_Tests
    {
    // ===========================
    // Helper validate DTO
    // ===========================
    private IList<ValidationResult> Validate(object model)
        {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
        }

    private JobMatchingOpenAiDbContext CreateDb()
        {
        var opt = new DbContextOptionsBuilder<JobMatchingOpenAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JobMatchingOpenAiDbContext(opt);
        }

    // ============================================================
    // TC01 – Validation: Title quá ngắn
    // ============================================================
    [Fact]
    public void TC01_ShouldFail_WhenTitleTooShort()
        {
        var dto = new AdminCreateNewsDto
            {
            Title = "abc",
            Content = new string('A', 30),
            Priority = 1
            };

        var results = Validate(dto);

        Assert.Contains(results, r => r.ErrorMessage!.Contains("Tiêu đề"));
        }

    // ============================================================
    // TC02 – Validation: Content quá ngắn
    // ============================================================
    [Fact]
    public void TC02_ShouldFail_WhenContentTooShort()
        {
        var dto = new AdminCreateNewsDto
            {
            Title = "Valid Title",
            Content = "short",
            Priority = 1
            };

        var results = Validate(dto);

        Assert.Contains(results, r => r.ErrorMessage!.Contains("Nội dung"));
        }

    // ============================================================
    // TC03 – Validation: Priority âm
    // ============================================================
    [Fact]
    public void TC03_ShouldFail_WhenPriorityNegative()
        {
        var dto = new AdminCreateNewsDto
            {
            Title = "Valid Title",
            Content = new string('A', 30),
            Priority = -1
            };

        var results = Validate(dto);

        Assert.Contains(results, r => r.ErrorMessage!.Contains("Priority phải >= 0"));
        }

    // ============================================================
    // TC04 – Validation: Title vượt 200 ký tự
    // ============================================================
    [Fact]
    public void TC04_ShouldFail_WhenTitleTooLong()
        {
        var dto = new AdminCreateNewsDto
            {
            Title = new string('A', 201),
            Content = new string('A', 30),
            Priority = 1
            };

        var results = Validate(dto);

        Assert.Contains(results, r => r.ErrorMessage!.Contains("Tiêu đề"));
        }

    // ============================================================
    // TC05 – Validation: Content vượt 10.000 ký tự
    // ============================================================
    [Fact]
    public void TC05_ShouldFail_WhenContentTooLong()
        {
        var dto = new AdminCreateNewsDto
            {
            Title = "Valid Title",
            Content = new string('A', 10001),
            Priority = 1
            };

        var results = Validate(dto);

        Assert.Contains(results, r => r.ErrorMessage!.Contains("Nội dung"));
        }

    // ============================================================
    // TC06 – Validation: Priority invalid (negative boundary)
    // ============================================================
    [Fact]
    public void TC06_ShouldFail_WhenPriorityBelowZero()
        {
        var dto = new AdminCreateNewsDto
            {
            Title = "Valid Title",
            Content = new string('A', 30),
            Priority = -1
            };

        var results = Validate(dto);

        Assert.Contains(results, r => r.ErrorMessage!.Contains("Priority phải >= 0"));
        }

    // ============================================================
    // TC07 – CreateAsync: Thành công không ảnh
    // ============================================================
    [Fact]
    public async Task TC07_ShouldCreateSuccess_WhenValid()
        {
        var db = CreateDb();

        var repo = new Mock<IAdminNewsRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<News>())).ReturnsAsync(1);

        var svc = new AdminNewsService(repo.Object, Mock.Of<IImageService>(), db, Mock.Of<INotificationService>());

        var dto = new AdminCreateNewsDto
            {
            Title = "Valid Title",
            Content = new string('A', 30),
            Priority = 1
            };

        var id = await svc.CreateAsync(1, dto);

        Assert.Equal(1, id);
        }

    // ============================================================
    // TC08 – CreateAsync: Upload ảnh khi có CoverImage
    // ============================================================
    [Fact]
    public async Task TC08_ShouldUploadImage_WhenCoverImageProvided()
        {
        var db = CreateDb();
        var file = new Mock<IFormFile>().Object;

        var repo = new Mock<IAdminNewsRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<News>())).ReturnsAsync(10);

        var img = new Mock<IImageService>();
        img.Setup(i => i.UploadImageAsync(file, "News"))
           .ReturnsAsync(("http://img.com/a.jpg", "id"));

        var svc = new AdminNewsService(repo.Object, img.Object, db, Mock.Of<INotificationService>());

        var dto = new AdminCreateNewsDto
            {
            Title = "Valid Title",
            Content = new string('A', 30),
            Priority = 1,
            CoverImage = file
            };

        await svc.CreateAsync(1, dto);

        img.Verify(i => i.UploadImageAsync(file, "News"), Times.Once);
        }

    // ============================================================
    // TC09 – CreateAsync: Publish = true → gửi notification
    // ============================================================
    [Fact]
    public async Task TC09_ShouldSendNotifications_WhenPublished()
        {
        var db = CreateDb();

        db.Users.Add(new User
            {
            UserId = 1,
            Email = "u1@test.com",
            Username = "u1",
            IsActive = true,
            Roles = new List<Role> { new Role { RoleName = "JobSeeker" } }
            });

        db.Users.Add(new User
            {
            UserId = 2,
            Email = "u2@test.com",
            Username = "u2",
            IsActive = true,
            Roles = new List<Role> { new Role { RoleName = "Employer" } }
            });

        db.SaveChanges();

        var repo = new Mock<IAdminNewsRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<News>())).ReturnsAsync(2);

        var noti = new Mock<INotificationService>();

        var svc = new AdminNewsService(repo.Object, Mock.Of<IImageService>(), db, noti.Object);

        var dto = new AdminCreateNewsDto
            {
            Title = "Publish Title",
            Content = new string('A', 30),
            Priority = 1,
            IsPublished = true
            };

        await svc.CreateAsync(1, dto);

        noti.Verify(n => n.SendAsync(It.IsAny<CreateNotificationDto>()), Times.Exactly(2));
        }

    // ============================================================
    // TC10 – CreateAsync: Publish = false → không gửi notification
    // ============================================================
    [Fact]
    public async Task TC10_ShouldNotSendNotification_WhenNotPublished()
        {
        var db = CreateDb();

        var repo = new Mock<IAdminNewsRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<News>())).ReturnsAsync(5);

        var noti = new Mock<INotificationService>();

        var svc = new AdminNewsService(repo.Object, Mock.Of<IImageService>(), db, noti.Object);

        var dto = new AdminCreateNewsDto
            {
            Title = "Draft",
            Content = new string('A', 30),
            Priority = 1,
            IsPublished = false
            };

        await svc.CreateAsync(1, dto);

        noti.Verify(n => n.SendAsync(It.IsAny<CreateNotificationDto>()), Times.Never);
        }

    // ============================================================
    // TC12 – CreateAsync: Publish = true nhưng không có user
    // ============================================================
    [Fact]
    public async Task TC12_ShouldNotFail_WhenPublishedButNoUsers()
        {
        var db = CreateDb(); // no users

        var repo = new Mock<IAdminNewsRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<News>())).ReturnsAsync(123);

        var noti = new Mock<INotificationService>();

        var svc = new AdminNewsService(repo.Object, Mock.Of<IImageService>(), db, noti.Object);

        var dto = new AdminCreateNewsDto
            {
            Title = "Publish",
            Content = new string('A', 30),
            Priority = 1,
            IsPublished = true
            };

        var id = await svc.CreateAsync(1, dto);

        Assert.Equal(123, id);
        noti.Verify(n => n.SendAsync(It.IsAny<CreateNotificationDto>()), Times.Never);
        }
    }
