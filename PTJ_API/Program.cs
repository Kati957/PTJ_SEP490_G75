using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using PTJ_Models.Models;
using PTJ_Service.AIService;
using PTJ_Service.EmployerPostService;
using PTJ_Service.ProfileService; // ✅ thêm dòng này
using PTJ_Service.RatingService;
using PTJ_Service.SystemReportService;
<<<<<<< Updated upstream
=======
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
using PTJ_Service.HomeService;
using PTJ_Service.FollowService;

>>>>>>> Stashed changes
var builder = WebApplication.CreateBuilder(args);

// =============================================
// 1️⃣ CONFIG DATABASE (EF CORE)
// =============================================
builder.Services.AddDbContext<JobMatchingDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn"));
});

// =============================================
// 2️⃣ ĐĂNG KÝ (REGISTER) CÁC SERVICE
// =============================================

// ⚙️ Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ⚙️ AI Services
builder.Services.AddHttpClient<OpenAIService>();
builder.Services.AddHttpClient<PineconeService>();
builder.Services.AddScoped<AiMatchService>();

// ⚙️ Business Services
builder.Services.AddScoped<IEmployerPostService, EmployerPostService>();
builder.Services.AddScoped<IProfileService, ProfileService>(); // ✅ thêm dòng này
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<ISystemReportService, SystemReportService>();

// ⚙️ Controller
builder.Services.AddControllers();

<<<<<<< Updated upstream
// =============================================
// 3️⃣ BUILD APP
// =============================================
=======
// Repository
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IAdminReportRepository, AdminReportRepository>();
builder.Services.AddScoped<IEmployerPostRepository, EmployerPostRepository>();
builder.Services.AddScoped<IJobSeekerPostRepository, JobSeekerPostRepository>();
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<IEmployerSearchRepository, EmployerSearchRepository>();
builder.Services.AddScoped<IJobSeekerSearchRepository, JobSeekerSearchRepository>();
builder.Services.AddScoped<IFollowService, FollowService>();



// Other Services
builder.Services.AddScoped<OpenMapService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IHomeService, HomeService>();

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


// 6️⃣ BUILD APP

>>>>>>> Stashed changes
var app = builder.Build();

// =============================================
// 4️⃣ MIDDLEWARE PIPELINE
// =============================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
