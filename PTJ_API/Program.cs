using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

// PTJ Namespaces
using PTJ_Data.Repositories.Interfaces;
using PTJ_Data.Repositories.Implementations;
using PTJ_Service.Helpers;
using PTJ_Service.LocationService;
using PTJ_Service.SystemReportService;
using PTJ_Service.AuthService.Implementations;
using PTJ_Service.AuthService.Interfaces;
using PTJ_Service.SearchService.Interfaces;
using PTJ_Service.SearchService.Implementations;
using PTJ_Service.JobSeekerPostService.cs.Interfaces;
using PTJ_Service.JobSeekerPostService;
using PTJ_Service.JobApplicationService.Interfaces;
using PTJ_Service.JobApplicationService.Implementations;
using PTJ_Service.EmployerPostService.Implementations;
using PTJ_Service.AiService.Implementations;
using PTJ_Service.AiService;
using PTJ_Data;
using PTJ_Data.Repositories.Implementations.Admin;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Service.Admin.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using PTJ_Models;
using PTJ_Services.Implementations;
using PTJ_Services.Interfaces;
using PTJ_Repositories.Implementations;
using PTJ_Repositories.Interfaces;
using PTJ_Service.ImageService;
using PTJ_Service.NewsService;
using CloudinaryDotNet;
//using dotenv.net;
using PTJ_Service.Helpers.Implementations;
using PTJ_Service.Helpers.Interfaces;
using PTJ_Models.Models;
using PTJ_Service.Interfaces.Admin;
using PTJ_Service.Admin.Implementations;
using PTJ_Data.Repositories.Interfaces.EPost;
using PTJ_Data.Repositories.Implementations.EPost;
using PTJ_Data.Repositories.Interfaces.JPost;
using PTJ_Data.Repositories.Implementations.JPost;
using PTJ_Data.Repositories.Implementations.ActivityUsers;
using PTJ_Data.Repositories.Interfaces.ActivityUsers;
using PTJ_Data.Repositories.Interfaces.NewsPost;
using PTJ_Service.SystemReportService.Interfaces;
using PTJ_Service.SystemReportService.Implementations;
using PTJ_Service.FollowService;
using PTJ_Service.Interfaces;
using PTJ_Service.Implementations;
using PTJ_Service.JobSeekerCvService.Implementations;
using PTJ_Service.JobSeekerCvService.Interfaces;
using PTJ_Data.Repositories.Implementations.Ratings;
using PTJ_Data.Repositories.Interfaces.Ratings;
using PTJ_Service.RatingService.Implementations;
using PTJ_Service.RatingService.Interfaces;
using PTJ_Data.Repositories.Implementations.NewsPost;
using PTJ_Service.JobSeekerPostService.Implementations;
using PTJ_Service.Hubs;
using PTJ_Data.Repositories.Implementations.Home;
using PTJ_Data.Repositories.Interfaces.Home;
using PTJ_Service.HomeService;
using PTJ_Service.CategoryService.Implementations;
using PTJ_Service.CategoryService.Interfaces;
using PTJ_Service.SearchService.Implementations;
using System.Security.Claims;
using PTJ_API.Middlewares;

using PTJ_Service.UserActivityService;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddAuthorization();


// 1️⃣ CONFIG DATABASE (EF CORE)

builder.Services.AddDbContext<JobMatchingDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn"));
});
// 2️⃣ ĐĂNG KÝ (REGISTER) CÁC SERVICE


// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PTJ API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
        Description = "Nhập JWT token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
        });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// AI Services
builder.Services.AddScoped<IAIService, AIService>();

// Application Services
builder.Services.AddScoped<IEmployerRankingService, EmployerRankingService>();
builder.Services.AddScoped<IChangePasswordService, ChangePasswordService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();    
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<ISystemReportService, SystemReportService>();
builder.Services.AddScoped<IAdminNewsService, AdminNewsService>();
builder.Services.AddScoped<IAdminJobPostService, AdminJobPostService>();
builder.Services.AddScoped<IAdminCategoryService, AdminCategoryService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminReportService, AdminReportService>();
builder.Services.AddScoped<IEmployerPostService, EmployerPostService>();
builder.Services.AddScoped<IJobSeekerPostService, JobSeekerPostService>();
builder.Services.AddScoped<IJobApplicationService, JobApplicationService>();
builder.Services.AddScoped<IEmployerSearchService, EmployerSearchService>();
builder.Services.AddScoped<IJobSeekerSearchService, JobSeekerSearchService>();
builder.Services.AddScoped<ISearchSuggestionService, SearchSuggestionService>();
builder.Services.AddScoped<IEmployerProfileService, EmployerProfileService>();
builder.Services.AddScoped<IJobSeekerProfileService, JobSeekerProfileService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IJobSeekerCvService, JobSeekerCvService>();
builder.Services.AddHttpClient<VnPostLocationService>();
builder.Services.AddScoped<LocationDisplayService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IAdminSystemReportService, AdminSystemReportService>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IAdminStatisticsService, AdminStatisticsService>();
builder.Services.AddScoped<IAdminEmployerRegistrationService, AdminEmployerRegistrationService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IUserActivityService, UserActivityService>();


// Repository
builder.Services.AddScoped<IEmployerRankingRepository, EmployerRankingRepository>();
builder.Services.AddScoped<IAdminStatisticsRepository, AdminStatisticsRepository>();
builder.Services.AddScoped<IHomeRepository, HomeRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IAdminSystemReportRepository, AdminSystemReportRepository>();
builder.Services.AddScoped<IAdminNewsRepository, AdminNewsRepository>();
builder.Services.AddScoped<IAdminJobPostRepository, AdminJobPostRepository>();
builder.Services.AddScoped<IAdminCategoryRepository, AdminCategoryRepository>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IAdminReportRepository, AdminReportRepository>();
builder.Services.AddScoped<IEmployerPostRepository, EmployerPostRepository>();
builder.Services.AddScoped<IJobSeekerPostRepository, JobSeekerPostRepository>();
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<IEmployerSearchRepository, EmployerSearchRepository>();
builder.Services.AddScoped<IJobSeekerSearchRepository, JobSeekerSearchRepository>();
builder.Services.AddScoped<IJobSeekerProfileRepository, JobSeekerProfileRepository>();
builder.Services.AddScoped<IEmployerProfileRepository, EmployerProfileRepository>();
builder.Services.AddScoped<IUserActivityRepository, UserActivityRepository>();
builder.Services.AddScoped<INewsRepository, NewsRepository>();
builder.Services.AddScoped<IJobSeekerCvRepository, JobSeekerCvRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddHostedService<PostExpirationService>();


// Other Services
builder.Services.AddScoped<OpenMapService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();



// 3️⃣ CẤU HÌNH JWT AUTHENTICATION

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
            {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["SigningKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
            };
        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var db = context.HttpContext.RequestServices
                    .GetRequiredService<JobMatchingDbContext>();

                var claims = context.Principal.Claims;

                var userIdClaim =
                       claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
                    ?? claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

                if (userIdClaim == null)
                {
                    context.Fail("Invalid token: no user ID.");
                    return;
                }

                int userId = int.Parse(userIdClaim.Value);

                var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    context.Fail("User does not exist.");
                    return;
                }

                if (!user.IsActive)
                {
                    context.Fail("Your account has been deactivated.");
                    return;
                }

                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                {
                    context.Fail("Your account has been locked.");
                    return;
                }
            },
            OnChallenge = context =>
            {
                context.HandleResponse(); 

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                string msg = context.ErrorDescription;

                if (msg == "Your account has been locked.")
                    msg = "Tài khoản của bạn đã bị khóa bởi quản trị viên.";

                if (msg == "Your account has been deactivated.")
                    msg = "Tài khoản của bạn đã bị vô hiệu hóa.";

                return context.Response.WriteAsync($$"""
        {
            "success": false,
            "message": "{{msg}}"
        }
        """);
            }
        };


    });

// 4️⃣ SIGNALR
builder.Services.AddSignalR();
 
// 4️⃣ CẤU HÌNH CORS

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7100", // Swagger & API cùng port
            "http://localhost:5169",  // HTTP fallback
            "https://localhost:5174"  // trường hợp FE dev khác port
        )
        .AllowAnyHeader()
        .AllowCredentials()
        .AllowAnyMethod();
    });
});




// 5️⃣ CONTROLLERS + JSON OPTIONS

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddHttpContextAccessor();

// 6️⃣ BUILD APP

var app = builder.Build();

//  Force ASP.NET to always use port 5000 on VPS (Production)
if (!app.Environment.IsDevelopment())
    {
    app.Urls.Clear();
    app.Urls.Add("http://0.0.0.0:5000");
    }

// Swagger chạy cả dev + production
app.UseSwagger();
app.UseSwaggerUI();

// Dev mode (local)
if (app.Environment.IsDevelopment())
    {
    app.UseSwagger();
    app.UseSwaggerUI();
    }

// ❗ HTTPS chỉ dùng local — KHÔNG dùng trên VPS
if (app.Environment.IsDevelopment())
    {
    app.UseHttpsRedirection();
    }

app.UseCors("AllowLocalhost");
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<PendingEmployerMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
// SignalR Hub Registration

app.MapHub<NotificationHub>("/hubs/notification");
app.MapControllers();

app.MapGet("/", () => "PTJ API is running");

app.Run();

