using System.Net;
using System.Net.Http.Json;
using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AuraWellness.Tests.Integration;

public class BusinessUnitsControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public BusinessUnitsControllerTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient client, Guid companyId, Guid buId, Guid personId)> SetupAsync(string role = "Owner")
    {
        var companyId = Guid.NewGuid();
        var buId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        await _factory.SeedAsync(async db =>
        {
            var company = Company.Create("BU Corp", "1 St", "555");
            SetId(company, companyId);
            db.Companies.Add(company);

            var bu = BusinessUnit.Create(companyId, "HQ");
            SetId(bu, buId);
            db.BusinessUnits.Add(bu);

            var person = Person.Create(companyId, "Owner", "User");
            SetId(person, personId);
            db.Persons.Add(person);

            db.BuStaffProfiles.Add(BuStaffProfile.Create(personId, buId, "o@o.com", "h", StaffRole.Owner));

            await Task.CompletedTask;
        });

        return (_factory.CreateAuthenticatedClient(personId, buId, companyId, role), companyId, buId, personId);
    }

    // ── GET /api/business-units ────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Authenticated_Returns200WithList()
    {
        var (client, _, _, _) = await SetupAsync("Staff");
        var resp = await client.GetAsync("/api/business-units");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await resp.Content.ReadFromJsonAsync<List<BusinessUnitResponse>>();
        list.Should().NotBeNull();
        list!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var resp = await _factory.CreateClient().GetAsync("/api/business-units");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/business-units ───────────────────────────────────────────────

    [Fact]
    public async Task Create_AsOwner_Returns201WithNewBu()
    {
        var (client, companyId, _, personId) = await SetupAsync("Owner");
        var newWsId = Guid.NewGuid();

        _factory.ChatClientMock
            .Setup(c => c.CreateWorkspaceAsync(It.IsAny<Guid>(), companyId, It.IsAny<string>(), default))
            .ReturnsAsync(newWsId);
        _factory.ChatClientMock
            .Setup(c => c.AddWorkspaceMemberAsync(newWsId, personId, "Admin", default))
            .Returns(Task.CompletedTask);

        var resp = await client.PostAsJsonAsync("/api/business-units",
            new CreateBuRequest("New Branch"));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<BusinessUnitResponse>();
        body!.Name.Should().Be("New Branch");
    }

    [Fact]
    public async Task Create_AsStaff_Returns403()
    {
        var (client, _, _, _) = await SetupAsync("Staff");
        var resp = await client.PostAsJsonAsync("/api/business-units",
            new CreateBuRequest("Forbidden Branch"));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static void SetId(object obj, Guid id)
    {
        var prop = obj.GetType().GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        prop?.SetValue(obj, id);
    }
}
