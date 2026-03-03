using System.Net;
using System.Net.Http.Json;
using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AuraWellness.Tests.Integration;

public class StaffControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;
    private Guid _companyId;
    private Guid _buId;
    private Guid _ownerPersonId;

    public StaffControllerTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> SetupAsync(string role = "Owner")
    {
        _companyId = Guid.NewGuid();
        _buId = Guid.NewGuid();
        _ownerPersonId = Guid.NewGuid();

        await _factory.SeedAsync(async db =>
        {
            var company = Company.Create("Staff Corp", "1 St", "555-0001");
            SetId(company, _companyId);
            db.Companies.Add(company);

            var bu = BusinessUnit.Create(_companyId, "HQ");
            SetId(bu, _buId);
            db.BusinessUnits.Add(bu);

            var person = Person.Create(_companyId, "Owner", "User");
            SetId(person, _ownerPersonId);
            db.Persons.Add(person);

            var profile = BuStaffProfile.Create(
                _ownerPersonId, _buId, "owner@corp.com", "hash", StaffRole.Owner);
            db.BuStaffProfiles.Add(profile);

            await Task.CompletedTask;
        });

        return _factory.CreateAuthenticatedClient(_ownerPersonId, _buId, _companyId, role);
    }

    // ── GET /api/staff ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_AsOwner_Returns200WithStaffList()
    {
        var client = await SetupAsync("Owner");
        var resp = await client.GetAsync("/api/staff");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await resp.Content.ReadFromJsonAsync<List<StaffResponse>>();
        list.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_AsStaff_Returns403()
    {
        var client = await SetupAsync("Staff");
        var resp = await client.GetAsync("/api/staff");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/staff");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/staff ────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201WithStaffResponse()
    {
        var client = await SetupAsync("Owner");
        var wsId = Guid.NewGuid();

        _factory.ChatClientMock
            .Setup(c => c.GetWorkspaceByBuIdAsync(_buId, default))
            .ReturnsAsync(new ChatWorkspaceDto(wsId, _buId, _companyId, "HQ", DateTime.UtcNow));
        _factory.ChatClientMock
            .Setup(c => c.AddWorkspaceMemberAsync(wsId, It.IsAny<Guid>(), "Member", default))
            .Returns(Task.CompletedTask);

        var resp = await client.PostAsJsonAsync("/api/staff",
            new CreateStaffRequest("Alice", "Wu", _buId, "alice@corp.com", "Staff"));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<StaffResponse>();
        body!.FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task Create_InvalidRole_Returns400()
    {
        var client = await SetupAsync("Owner");

        var resp = await client.PostAsJsonAsync("/api/staff",
            new CreateStaffRequest("X", "Y", _buId, "x@y.com", "Owner"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/staff/{personId}/role ─────────────────────────────────────────

    [Fact]
    public async Task UpdateRole_ValidRequest_Returns204()
    {
        var client = await SetupAsync("Owner");
        Guid targetPersonId = Guid.Empty;

        await _factory.SeedAsync(async db =>
        {
            var person = Person.Create(_companyId, "Staff", "Member");
            targetPersonId = person.Id;
            db.Persons.Add(person);
            var profile = BuStaffProfile.Create(person.Id, _buId, "staff@corp.com", "h", StaffRole.Staff);
            db.BuStaffProfiles.Add(profile);
            await Task.CompletedTask;
        });

        var resp = await client.PutAsJsonAsync(
            $"/api/staff/{targetPersonId}/role",
            new UpdateRoleRequest("Admin", _buId));

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static void SetId(object obj, Guid id)
    {
        var prop = obj.GetType().GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        prop?.SetValue(obj, id);
    }
}
