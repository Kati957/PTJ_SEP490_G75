using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PTJ_Data;
using PTJ_Service.Helpers;
using PTJ_Service.Interfaces;
using PTJ_Service.Implementations;
using PTJ_Models.Models;
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

// Middleware pipeline
app.UseHttpsRedirection();
app.UseCors("AllowLocalhost");   // Phải đặt trước Authentication
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
