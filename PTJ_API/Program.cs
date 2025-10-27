using Microsoft.AspNetCore.Builder;
﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PTJ_Data;
using PTJ_Service.Helpers;
using PTJ_Service.Interfaces;
using PTJ_Service.Implementations;
using PTJ_Models.Models;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Data.Repositories.Implementations;
using Microsoft.OpenApi.Models;
using PTJ_Service.AIService;
using PTJ_Service.EmployerPostService;
using PTJ_Service.JobSeekerPostService;
using System.Text.Json.Serialization;
using PTJ_Service.LocationService;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Service.JobApplicationService;
using PTJ_Service.SearchService;

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
builder.Services.AddScoped<OpenMapService>();
builder.Services.AddScoped<IEmployerPostRepository, EmployerPostRepository>();
builder.Services.AddScoped<IJobSeekerPostRepository, JobSeekerPostRepository>();
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<IJobApplicationService, JobApplicationService>();

// Repository
builder.Services.AddScoped<IEmployerSearchRepository, EmployerSearchRepository>();
builder.Services.AddScoped<IJobSeekerSearchRepository, JobSeekerSearchRepository>();

// Service
builder.Services.AddScoped<IEmployerSearchService, EmployerSearchService>();
builder.Services.AddScoped<IJobSeekerSearchService, JobSeekerSearchService>();
builder.Services.AddScoped<ISearchSuggestionService, SearchSuggestionService>();

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Ép API chạy đúng port 7100 (HTTPS) + 5169 (HTTP)
builder.WebHost.UseUrls("https://localhost:7100;http://localhost:5169");

// Add Controllers
builder.Services.AddControllers();

// DbContext (DB-First)
builder.Services.AddDbContext<JobMatchingDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

// Dependency Injection
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// JWT Authentication
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

// CORS – Cho phép FE & Swagger cùng port gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7100", // Swagger và API cùng port
            "http://localhost:5169",  // HTTP fallback
            "https://localhost:7025"  // trường hợp FE dev khác port
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.Services.AddAuthorization();
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

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
    {
    app.UseSwagger();
    app.UseSwaggerUI();
    }

// Middleware pipeline
app.UseHttpsRedirection();
app.UseCors("AllowLocalhost");   // Phải đặt trước Authentication
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
