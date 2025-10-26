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

// =============================================
// 3️⃣ BUILD APP
// =============================================
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
