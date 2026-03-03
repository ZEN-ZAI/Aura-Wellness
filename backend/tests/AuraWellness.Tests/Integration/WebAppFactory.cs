using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace AuraWellness.Tests.Integration;

/// <summary>
/// Shared WebApplicationFactory for all integration tests.
/// Replaces Postgres with an in-memory database and mocks the gRPC chat client.
/// </summary>
public class WebAppFactory : WebApplicationFactory<Program>
{
    // Must match appsettings.json — ConfigureAppConfiguration cannot override values
    // read by Program.cs before builder.Build() in the minimal-API hosting model.
    internal const string JwtSecret = "dev_secret_key_min_32_characters_long_here";
    internal const string JwtIssuer = "aura-wellness";
    internal const string JwtAudience = "aura-wellness-client";

    public Mock<IChatServiceClient> ChatClientMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=unused",
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:ExpiryMinutes"] = "60",
                ["ChatService:GrpcUrl"] = "http://localhost:50051",
                ["ChatService:InternalApiKey"] = "test-key",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core DbContext registrations for AppDbContext so that
            // only the InMemory provider (added below) is registered. We must remove
            // IDbContextOptionsConfiguration<T> too — leaving it causes EF Core to
            // apply both UseNpgsql and UseInMemoryDatabase, triggering a multi-provider error.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // Replace with in-memory database (unique per factory instance).
            // Capture name OUTSIDE the lambda — evaluating Guid.NewGuid() inside would
            // give each DI scope its own isolated store, breaking data seeded via SeedAsync.
            var dbName = $"TestDb_{Guid.NewGuid()}";
            services.AddDbContext<AppDbContext>(opts => opts.UseInMemoryDatabase(dbName));

            // Replace real IChatServiceClient with a shared mock
            services.RemoveAll<IChatServiceClient>();
            services.AddSingleton<IChatServiceClient>(ChatClientMock.Object);
        });
    }

    /// <summary>
    /// Creates a valid JWT for tests, matching the same claim structure
    /// used by JwtTokenService in production.
    /// </summary>
    public static string CreateToken(
        Guid personId,
        Guid buId,
        Guid companyId,
        string role,
        string firstName = "Test",
        string lastName = "User")
    {
        var claims = new[]
        {
            new Claim("personId", personId.ToString()),
            new Claim("buId", buId.ToString()),
            new Claim("companyId", companyId.ToString()),
            new Claim("role", role),
            new Claim("firstName", firstName),
            new Claim("lastName", lastName),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates an HttpClient with a pre-attached Bearer token for the given user claims.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(
        Guid personId, Guid buId, Guid companyId, string role,
        string firstName = "Test", string lastName = "User")
    {
        var client = CreateClient();
        var token = CreateToken(personId, buId, companyId, role, firstName, lastName);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Seeds data directly into the in-memory database inside a scope.
    /// </summary>
    public async Task SeedAsync(Func<AppDbContext, Task> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await seed(db);
        await db.SaveChangesAsync();
    }
}
