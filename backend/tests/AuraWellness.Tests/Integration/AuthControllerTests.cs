using System.Net;
using System.Net.Http.Json;
using AuraWellness.Application.DTOs;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Enums;
using AuraWellness.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AuraWellness.Tests.Integration;

public class AuthControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── POST /api/auth/login ───────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var (_, _, email, plainPassword) = await SeedOwnerAsync();

        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, plainPassword));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidEmailFormat_Returns400()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("not-an-email", "pass"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var (_, _, email, _) = await SeedOwnerAsync();

        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "wrong-password"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("ghost@nowhere.com", "any"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/auth/me ───────────────────────────────────────────────────────

    [Fact]
    public async Task Me_WithValidToken_Returns200WithClaims()
    {
        var personId = Guid.NewGuid();
        var buId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var authClient = _factory.CreateAuthenticatedClient(personId, buId, companyId, "Staff");
        var resp = await authClient.GetAsync("/api/auth/me");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var resp = await _client.GetAsync("/api/auth/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── PUT /api/auth/password ─────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ValidCurrentPassword_Returns204()
    {
        var (personId, buId, _, plainPassword) = await SeedOwnerAsync();
        var authClient = _factory.CreateAuthenticatedClient(personId, buId, Guid.NewGuid(), "Owner");

        var resp = await authClient.PutAsJsonAsync("/api/auth/password",
            new ChangePasswordRequest(plainPassword, "NewSecure@456"));

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns401()
    {
        var (personId, buId, _, _) = await SeedOwnerAsync();
        var authClient = _factory.CreateAuthenticatedClient(personId, buId, Guid.NewGuid(), "Owner");

        var resp = await authClient.PutAsJsonAsync("/api/auth/password",
            new ChangePasswordRequest("definitely-wrong", "NewSecure@456"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_Returns401()
    {
        var resp = await _client.PutAsJsonAsync("/api/auth/password",
            new ChangePasswordRequest("old", "new"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<(Guid personId, Guid buId, string email, string plainPassword)> SeedOwnerAsync()
    {
        var plainPassword = "Test@Password1";
        Guid personId = Guid.Empty, buId = Guid.Empty;
        var email = $"owner_{Guid.NewGuid():N}@test.com";

        await _factory.SeedAsync(async db =>
        {
            using var scope = _factory.Services.CreateScope();
            var hasher = scope.ServiceProvider.GetRequiredService<AuraWellness.Domain.Interfaces.IPasswordHasher>();

            var company = Company.Create("Test Corp", "1 Test St", "555-0000");
            db.Companies.Add(company);

            var bu = BusinessUnit.Create(company.Id, "Test BU");
            db.BusinessUnits.Add(bu);

            var person = Person.Create(company.Id, "Owner", "User");
            db.Persons.Add(person);

            var profile = BuStaffProfile.Create(
                person.Id, bu.Id, email, hasher.Hash(plainPassword), StaffRole.Owner);
            db.BuStaffProfiles.Add(profile);

            personId = person.Id;
            buId = bu.Id;

            await Task.CompletedTask;
        });

        return (personId, buId, email, plainPassword);
    }
}
