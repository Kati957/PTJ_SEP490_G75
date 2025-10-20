using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using PTJ_Models.Models;

using Microsoft.OpenApi.Models;
using PTJ_Service.AIService;
using PTJ_Service.EmployerPostService;
using PTJ_Service.JobSeekerPostService;

var builder = WebApplication.CreateBuilder(args);

// Ép API chạy đúng port 7100 (HTTPS) + 5169 (HTTP)
//builder.WebHost.UseUrls("https://localhost:7100;http://localhost:5169");

builder.Services.AddDbContext<JobMatchingDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn"));
});
// Add services to the container.
// ⚙️ Thêm Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ⚙️ Thêm các service AI
builder.Services.AddHttpClient<OpenAIService>();
builder.Services.AddHttpClient<PineconeService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<IEmployerPostService, EmployerPostService>();
builder.Services.AddScoped<IJobSeekerPostService, JobSeekerPostService>();


builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
