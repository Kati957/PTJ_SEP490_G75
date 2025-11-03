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
using PTJ_Service.RatingService;
using PTJ_Service.SystemReportService;
using PTJ_Service.AuthService.Implementations;
using PTJ_Service.AuthService.Interfaces;
using PTJ_Service.SearchService.Interfaces;
using PTJ_Service.SearchService.Implementations;
using PTJ_Service.JobSeekerPostService.cs.Interfaces;
using PTJ_Service.JobSeekerPostService.cs.Implementations;
using PTJ_Service.JobApplicationService.Interfaces;
using PTJ_Service.JobApplicationService.Implementations;
using PTJ_Service.EmployerPostService.Implementations;
using PTJ_Service.AiService.Implementations;
using PTJ_Service.AiService.Interfaces;
using PTJ_Data;
using PTJ_Data.Repo.Implement;
using PTJ_Data.Repo.Interface;
using PTJ_Service.Implement;
using PTJ_Service.Interface;
using PTJ_Data.Repositories.Implementations.Admin;
using PTJ_Data.Repositories.Interfaces.Admin;
using PTJ_Service.Admin.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using PTJ_Models;
using PTJ_Services.Implementations;
using PTJ_Services.Interfaces;
using PTJ_Repositories.Implementations;
using PTJ_Repositories.Interfaces;
using PTJ_Service.SearchService.Services;
using PTJ_Service.ImageService;
using PTJ_Service.NewsService;
using CloudinaryDotNet;
using dotenv.net;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
// =============================================

// 1️⃣ CONFIG DATABASE (EF CORE)

builder.Services.AddDbContext<JobMatchingDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn"));
});
DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));
var cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
if (string.IsNullOrEmpty(cloudinaryUrl))
{
    Console.WriteLine(" CLOUDINARY_URL not found in .env");
}
else
{
    Console.WriteLine(" Cloudinary loaded from .env");
}

// Đăng ký Cloudinary vào DI container
var cloudinary = new Cloudinary(cloudinaryUrl);
cloudinary.Api.Secure = true;
builder.Services.AddSingleton(cloudinary);


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
builder.Services.AddHttpClient<OpenAIService>();
builder.Services.AddHttpClient<PineconeService>();
builder.Services.AddScoped<IAIService, AIService>();

// Application Services
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
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IUserActivityRepository, UserActivityRepository>();

// Repository
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
builder.Services.AddScoped<INewsRepository,NewsRepository>();

// Other Services
builder.Services.AddScoped<OpenMapService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();


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
    });

// 4️⃣ CẤU HÌNH CORS

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7100", // Swagger & API cùng port
            "http://localhost:5169",  // HTTP fallback
            "https://localhost:7025"  // trường hợp FE dev khác port
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

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Middleware
if (app.Environment.IsDevelopment())
    {
    app.UseSwagger();
    app.UseSwaggerUI();
    }

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost");   // Phải đặt trước Authentication
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
