using System.Text;
using AuraWellness.API.Middleware;
using AuraWellness.Application.Services;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Enums;
using AuraWellness.Domain.Interfaces;
using AuraWellness.Infrastructure.Http;
using AuraWellness.Infrastructure.Identity;
using AuraWellness.Infrastructure.Persistence;
using AuraWellness.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IBusinessUnitRepository, BusinessUnitRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IBuStaffProfileRepository, BuStaffProfileRepository>();

// Infrastructure services (interfaces in Domain, implementations in Infrastructure)
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// HTTP client for chat service
builder.Services.AddHttpClient<IChatServiceClient, ChatServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ChatService:BaseUrl"]
        ?? throw new InvalidOperationException("ChatService:BaseUrl not configured"));
    client.DefaultRequestHeaders.Add("X-Internal-Key",
        builder.Configuration["ChatService:InternalApiKey"]);
});

// Application services
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBusinessUnitService, BusinessUnitService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IChatAccessService, ChatAccessService>();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            // Use the full .NET claim URIs — the JWT handler maps "sub"→NameIdentifier and "role"→Role by default
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

// Authorization policies
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("OwnerOnly", p => p.RequireClaim("role", "Owner"));
    opts.AddPolicy("OwnerOrAdmin", p => p.RequireClaim("role", "Owner", "Admin"));
});

// CORS — allow frontend
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.AddControllers();

var app = builder.Build();

// Apply EF migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // Seed demo user
    const string demoEmail = "Welcome@example123";
    bool exists = await db.BuStaffProfiles.AnyAsync(p => p.Email == demoEmail);
    if (!exists)
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var company = Company.Create("Aura Wellness Demo", "1 Wellness Way", "555-0100");
        db.Companies.Add(company);

        var bu = BusinessUnit.Create(company.Id, "Aura Wellness Demo HQ");
        db.BusinessUnits.Add(bu);

        var person = Person.Create(company.Id, "Demo", "Owner");
        db.Persons.Add(person);

        var profile = BuStaffProfile.Create(person.Id, bu.Id, demoEmail, hasher.Hash("Password@123"), StaffRole.Owner);
        db.BuStaffProfiles.Add(profile);

        await db.SaveChangesAsync();
    }
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
