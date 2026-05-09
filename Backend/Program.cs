using Backend.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend.Helpers;
using Backend.Backend.Service.UtilityServices;
using Backend.Backend.Service.IUtilityService;
using Backend.Backend.Repository.IRepository;
using Backend.Backend.Repository.Repository;
using AutoMapper;
using Backend.Mapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(Option =>
{
    Option.AddPolicy("MyPolicy", builder =>
    {
        builder.WithOrigins(GetAllowedOrigins())
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


builder.Services.AddDbContext<AppDbContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("sqlServerConnStr"));
});
builder.Services.AddHealthChecks()
    .AddCheck<AppDbHealthCheck>("database");


builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddTransient(typeof(IRepository<>), typeof(Repositiory<>));
builder.Services.AddHostedService<ExpireCouponCleanup>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton(new MapperConfiguration(x => x.AddProfile(new MapperProfile())).CreateMapper());

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    var message = "JWT signing key is not configured. Set Jwt:SigningKey using environment variables, user-secrets, or Kubernetes secrets.";
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(message);
    }

    jwtSigningKey = "development-only-signing-key-change-before-production-12345";
}
builder.Configuration["Jwt:SigningKey"] = jwtSigningKey;


builder.Services.AddAuthentication(x =>

{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
        ValidateAudience = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Audience"]),
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuer = !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Issuer"]),
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ClockSkew = TimeSpan.Zero
    };
});


var app = builder.Build();

if (builder.Configuration.GetValue("Database:AutoMigrate", false))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (builder.Configuration.GetValue("App:HttpsRedirectionEnabled", true))
{
    app.UseHttpsRedirection();
}

app.UseCors("MyPolicy");
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

string[] GetAllowedOrigins()
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (origins is { Length: > 0 })
    {
        return origins;
    }

    return builder.Environment.IsDevelopment()
        ? new[] { "http://localhost:4200", "https://localhost:4200" }
        : Array.Empty<string>();
}

public sealed class AppDbHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;

    public AppDbHealthCheck(AppDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await _db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("Database connection is available.")
            : HealthCheckResult.Unhealthy("Database connection is unavailable.");
    }
}
