using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using PTJ_Models.Models;
using PTJ_Service.AIService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<JobMatchingAiDbContext>(opt =>
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
builder.Services.AddScoped<AiMatchService>();

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
