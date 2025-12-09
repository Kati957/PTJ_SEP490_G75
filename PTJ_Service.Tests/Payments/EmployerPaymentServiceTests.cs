using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Service.PaymentsService.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using PTJ_Service.Hubs;
using PTJ_Service.Helpers.Interfaces;
using PTJ_Service.Helpers.Implementations;
using System.Net.Http;
using System.Reflection;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Moq.Protected;
using System;

namespace PTJ_Service.Tests.Payments
    {
    public class EmployerPaymentServiceTests
        {
        // ============================
        // 1. InMemory DB
        // ============================
        private JobMatchingDbContext CreateDb()
            {
            var opt = new DbContextOptionsBuilder<JobMatchingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new JobMatchingDbContext(opt);
            }

        // ============================
        // 2. Fake Config
        // ============================
        private IConfiguration FakeConfig()
            {
            var cfg = new Mock<IConfiguration>();
            cfg.Setup(x => x["PayOS:ReturnUrl"]).Returns("https://return.com");
            cfg.Setup(x => x["PayOS:CancelUrl"]).Returns("https://cancel.com");
            cfg.Setup(x => x["PayOS:ChecksumKey"]).Returns("secret");
            cfg.Setup(x => x["PayOS:BaseUrl"]).Returns("https://api.payos.dev");
            cfg.Setup(x => x["PayOS:CreatePaymentUrl"]).Returns("/v1/payment");
            cfg.Setup(x => x["PayOS:ClientId"]).Returns("client");
            cfg.Setup(x => x["PayOS:ApiKey"]).Returns("api-key");
            return cfg.Object;
            }

        // ============================
        // 3. Fake Environment
        // ============================
        private IWebHostEnvironment FakeEnv()
            {
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(x => x.EnvironmentName).Returns("Development");
            return env.Object;
            }

        // ============================
        // 4. Fake SignalR Hub
        // ============================
        private IHubContext<PaymentHub> FakeHub()
            {
            var hub = new Mock<IHubContext<PaymentHub>>();
            hub.Setup(x => x.Clients).Returns(Mock.Of<IHubClients>());
            return hub.Object;
            }

        // ============================
        // 5. Fake EmailSender (không mock được)
        // ============================
        private SmtpEmailSender FakeEmailSender()
            {
            var cfg = new Mock<IConfiguration>();
            cfg.Setup(x => x["Email:Host"]).Returns("smtp.test");
            cfg.Setup(x => x["Email:Port"]).Returns("587");
            cfg.Setup(x => x["Email:Username"]).Returns("mail@test.com");
            cfg.Setup(x => x["Email:Password"]).Returns("123");
            cfg.Setup(x => x["Email:EnableSSL"]).Returns("false");

            return new SmtpEmailSender(cfg.Object);
            }

        // ============================
        // 6. Inject HttpClient bằng Reflection
        // ============================
        private void InjectHttpClient(EmployerPaymentService service, HttpClient client)
            {
            typeof(EmployerPaymentService)
                .GetField("_http", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(service, client);
            }

        // ==========================================================
        // 1️⃣ SUCCESS: PayOS trả link OK
        // ==========================================================
        [Fact]
        public async Task CreatePaymentLink_Success()
            {
            var db = CreateDb();

            db.Users.Add(new User { UserId = 1, Username = "A", IsActive = true, Email = "a@mail.com" });
            db.EmployerPlans.Add(new EmployerPlan { PlanId = 10, PlanName = "Basic", Price = 100000, MaxPosts = 5 });
            db.SaveChanges();

            string fakeJson = @"{
                ""data"": {
                    ""checkoutUrl"": ""https://payos.vn/checkout/123"",
                    ""orderCode"": ""123"",
                    ""qrCode"": ""qr_raw_123""
                }
            }";

            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                    {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fakeJson)
                    });

            var fakeHttp = new HttpClient(handler.Object);

            var service = new EmployerPaymentService(
                db, FakeConfig(), FakeEnv(), FakeHub(),
                Mock.Of<IEmailTemplateService>(),
                FakeEmailSender()
            );

            InjectHttpClient(service, fakeHttp);

            var result = await service.CreatePaymentLinkAsync(1, 10);

            Assert.Equal("https://payos.vn/checkout/123", result.CheckoutUrl);
            Assert.Equal("qr_raw_123", result.QrCodeRaw);
            Assert.Equal("123", result.OrderCode);

            Assert.Equal(1, await db.EmployerTransactions.CountAsync());
            }

        // ==========================================================
        // 2️⃣ User Inactive
        // ==========================================================
        [Fact]
        public async Task CreatePaymentLink_Fail_UserInactive()
            {
            var db = CreateDb();

            db.Users.Add(new User { UserId = 1, Username = "A", IsActive = false, Email = "a@mail.com" });
            db.SaveChanges();

            var service = new EmployerPaymentService(
                db, FakeConfig(), FakeEnv(), FakeHub(),
                Mock.Of<IEmailTemplateService>(),
                FakeEmailSender()
            );

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                service.CreatePaymentLinkAsync(1, 10));

            Assert.Equal("Tài khoản của bạn đang bị khóa. Không thể thanh toán.", ex.Message);
            }

        // ==========================================================
        // 3️⃣ Plan không tồn tại
        // ==========================================================
        [Fact]
        public async Task CreatePaymentLink_Fail_PlanNotFound()
            {
            var db = CreateDb();

            db.Users.Add(new User { UserId = 1, Username = "A", IsActive = true, Email = "a@mail.com" });
            db.SaveChanges();

            var service = new EmployerPaymentService(
                db, FakeConfig(), FakeEnv(), FakeHub(),
                Mock.Of<IEmailTemplateService>(),
                FakeEmailSender()
            );

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                service.CreatePaymentLinkAsync(1, 999));

            Assert.Equal("Gói không tồn tại", ex.Message);
            }

        // ==========================================================
        // 4️⃣ Price <= 0
        // ==========================================================
        [Fact]
        public async Task CreatePaymentLink_Fail_InvalidPrice()
            {
            var db = CreateDb();

            db.Users.Add(new User { UserId = 1, Username = "A", IsActive = true, Email = "a@mail.com" });
            db.EmployerPlans.Add(new EmployerPlan { PlanId = 10, PlanName = "Basic", Price = 0 });
            db.SaveChanges();

            var service = new EmployerPaymentService(
                db, FakeConfig(), FakeEnv(), FakeHub(),
                Mock.Of<IEmailTemplateService>(),
                FakeEmailSender()
            );

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                service.CreatePaymentLinkAsync(1, 10));

            Assert.Equal("Giá gói không hợp lệ.", ex.Message);
            }

        // ==========================================================
        // 5️⃣ User đang dùng cùng gói → không mua lại
        // ==========================================================
        [Fact]
        public async Task CreatePaymentLink_Fail_UsingSamePlan()
            {
            var db = CreateDb();

            db.Users.Add(new User { UserId = 1, Username = "A", Email = "a@mail.com", IsActive = true });

            db.EmployerPlans.Add(new EmployerPlan { PlanId = 10, PlanName = "Basic", Price = 100, MaxPosts = 5 });

            db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 10,
                Status = "Active",
                RemainingPosts = 3
                });

            db.SaveChanges();

            var service = new EmployerPaymentService(
                db, FakeConfig(), FakeEnv(), FakeHub(),
                Mock.Of<IEmailTemplateService>(),
                FakeEmailSender()
            );

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                service.CreatePaymentLinkAsync(1, 10));

            Assert.Equal("Bạn đang sử dụng gói này. Không thể mua lại.", ex.Message);
            }

        // ==========================================================
        // 6️⃣ Không được hạ cấp gói
        // ==========================================================
        [Fact]
        public async Task CreatePaymentLink_Fail_DowngradePlan()
            {
            var db = CreateDb();

            db.Users.Add(new User { UserId = 1, Username = "A", Email = "a@mail.com", IsActive = true });

            db.EmployerPlans.Add(new EmployerPlan { PlanId = 1, PlanName = "Pro", Price = 200, MaxPosts = 10 });
            db.EmployerPlans.Add(new EmployerPlan { PlanId = 2, PlanName = "Basic", Price = 100, MaxPosts = 5 });

            db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                Status = "Active",
                RemainingPosts = 2
                });

            db.SaveChanges();

            var service = new EmployerPaymentService(
                db, FakeConfig(), FakeEnv(), FakeHub(),
                Mock.Of<IEmailTemplateService>(),
                FakeEmailSender()
            );

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                service.CreatePaymentLinkAsync(1, 2));

            Assert.Equal("Không thể hạ cấp gói.", ex.Message);
            }

        // ==========================================================
        // 7️⃣ Nâng cấp gói → tính giảm tiền
        // ==========================================================
        [Fact]
        public async Task CreatePaymentLink_UpgradePlan_CalculatesCorrectAmount()
            {
            var db = CreateDb();

            db.Users.Add(new User { UserId = 1, Username = "A", Email = "a@mail.com", IsActive = true });

            db.EmployerPlans.Add(new EmployerPlan { PlanId = 1, PlanName = "Basic", Price = 100, MaxPosts = 5 });
            db.EmployerPlans.Add(new EmployerPlan { PlanId = 2, PlanName = "Pro", Price = 200, MaxPosts = 10 });

            db.EmployerSubscriptions.Add(new EmployerSubscription
                {
                UserId = 1,
                PlanId = 1,
                Status = "Active",
                RemainingPosts = 3
                });

            db.SaveChanges();

            string fakeJson = @"{
                ""data"": {
                    ""checkoutUrl"": ""https://payos.vn/checkout/456"",
                    ""orderCode"": ""456"",
                    ""qrCode"": ""qr456""
                }
            }";

            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                    {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fakeJson)
                    });

            var fakeHttp = new HttpClient(handler.Object);

            var service = new EmployerPaymentService(
                db, FakeConfig(), FakeEnv(), FakeHub(),
                Mock.Of<IEmailTemplateService>(),
                FakeEmailSender()
            );

            InjectHttpClient(service, fakeHttp);

            var result = await service.CreatePaymentLinkAsync(1, 2);

            Assert.Equal("https://payos.vn/checkout/456", result.CheckoutUrl);

            var trans = await db.EmployerTransactions.FirstAsync();

            Assert.True(trans.Amount < 200);
            }

        // ==========================================================
        // 8️⃣ Pending + QR còn hạn → trả QR cũ
        // ==========================================================
        [Fact]
        public async Task CreatePaymentLink_ReturnsOldPending_WhenQrValid()
            {
            var db = CreateDb();

            db.Users.Add(new User { UserId = 1, Username = "A", Email = "a@mail.com", IsActive = true });

            db.EmployerPlans.Add(new EmployerPlan { PlanId = 10, PlanName = "Basic", Price = 100 });

            db.EmployerTransactions.Add(new EmployerTransaction
                {
                TransactionId = 99,
                UserId = 1,
                PlanId = 10,
                Status = "Pending",
                QrCodeUrl = "old_qr",
                PayOsorderCode = "999",
                RawWebhookData = @"{ ""data"": { ""checkoutUrl"": ""https://old.com"" }}",
                QrExpiredAt = DateTime.Now.AddMinutes(10)
                });

            db.SaveChanges();

            var service = new EmployerPaymentService(
                db, FakeConfig(), FakeEnv(), FakeHub(),
                Mock.Of<IEmailTemplateService>(),
                FakeEmailSender()
            );

            var result = await service.CreatePaymentLinkAsync(1, 10);

            Assert.Equal("https://old.com", result.CheckoutUrl);
            Assert.Equal("old_qr", result.QrCodeRaw);
            Assert.Equal("999", result.OrderCode);
            }

        // ==========================================================
        // 9️⃣ Pending + QR hết hạn → phải gọi Refresh
        // ==========================================================
        [Fact]
        public async Task CreatePaymentLink_RefreshesExpiredQr()
            {
            var db = CreateDb();

            db.Users.Add(new User { UserId = 1, Username = "A", Email = "a@mail.com", IsActive = true });
            db.EmployerPlans.Add(new EmployerPlan { PlanId = 10, PlanName = "Basic", Price = 100 });

            db.EmployerTransactions.Add(new EmployerTransaction
                {
                TransactionId = 50,
                UserId = 1,
                PlanId = 10,
                Status = "Pending",
                QrExpiredAt = DateTime.Now.AddMinutes(-1)
                });

            db.SaveChanges();

            string fakeJson = @"{
                ""data"": {
                    ""checkoutUrl"": ""https://refresh.com"",
                    ""orderCode"": ""R123"",
                    ""qrCode"": ""qr_refresh""
                }
            }";

            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                    {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fakeJson)
                    });

            var fakeHttp = new HttpClient(handler.Object);

            var service = new EmployerPaymentService(
                db, FakeConfig(), FakeEnv(), FakeHub(),
                Mock.Of<IEmailTemplateService>(),
                FakeEmailSender()
            );

            InjectHttpClient(service, fakeHttp);

            var result = await service.CreatePaymentLinkAsync(1, 10);

            Assert.Equal("https://refresh.com", result.CheckoutUrl);
            }
        }
    }
