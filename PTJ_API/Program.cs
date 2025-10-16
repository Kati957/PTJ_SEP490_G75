using Microsoft.EntityFrameworkCore;
using PTJ_Models.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<JobMatchingAiDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn"));
});
// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
