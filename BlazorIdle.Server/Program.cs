using Microsoft.EntityFrameworkCore;
using BlazorIdle.Server.Data;
using BlazorIdle.Server.Services;
using BlazorIdle.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.StaticFiles; // 新增
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure SQLite database
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=gamedata.db"));

// Register custom services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IGameConfigProvider, GameConfigProvider>();
builder.Services.AddControllers();

// CORS: 从配置读取允许的前端域名（同域托管时不会触发跨域，可保持不变）
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins("https://localhost:5001", "http://localhost:5000").AllowAnyMethod().AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Create database and seed default user if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    dbContext.Database.EnsureCreated();

    var defaultMaxSlots = configuration.GetValue<int>("CharacterConfig:defaultMaxCharacterSlots", 3);

    if (!dbContext.Users.Any())
    {
        var defaultUser = new User
        {
            Username = "test123",
            PasswordHash = passwordHasher.HashPassword("test123123"),
            CreatedAt = DateTime.UtcNow,
            MaxCharacterSlots = defaultMaxSlots,
            UsedCharacterSlots = 0
        };
        dbContext.Users.Add(defaultUser);
        dbContext.SaveChanges();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Default test user created: test123 / test123123 with {MaxSlots} character slots", defaultMaxSlots);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazorClient");
app.UseAuthentication();
app.UseAuthorization();

// 关键：托管 Blazor 框架文件 + 静态文件 + 前端回退
app.UseBlazorFrameworkFiles();

// 可选：明确映射 .dat，避免未知类型被拒
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".dat"] = "application/octet-stream";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });

// API
app.MapControllers();

// SPA 路由回退到 index.html（前端已复制到 wwwroot）
app.MapFallbackToFile("index.html");

app.Run();