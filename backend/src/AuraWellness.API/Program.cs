using System.Text;
using AuraWellness.API.Middleware;
using AuraWellness.Application;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Infrastructure;
using AuraWellness.Infrastructure.Migrations;
using AuraWellness.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<AuraWellness.API.Middleware.ChatWebSocketHandler>();

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

// Apply EF migrations at startup (skipped when running under integration tests)
if (!app.Environment.IsEnvironment("Testing"))
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // Ensure chat workspace exists for the seeded demo data — idempotent.
    // IDs are fixed in the InitialDataSeed migration.
    var chatClient = scope.ServiceProvider.GetRequiredService<IChatServiceClient>();
    var workspace = await chatClient.GetWorkspaceByBuIdAsync(InitialDataSeed.BuId);
    if (workspace is null)
    {
        var workspaceId = await chatClient.CreateWorkspaceAsync(
            InitialDataSeed.BuId,
            InitialDataSeed.CompanyId,
            "Aura Wellness Demo HQ");
        await chatClient.AddWorkspaceMemberAsync(workspaceId, InitialDataSeed.PersonId, "Admin");
    }
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
