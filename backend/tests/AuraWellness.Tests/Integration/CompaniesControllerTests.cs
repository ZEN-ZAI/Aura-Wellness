using System.Net;
using System.Net.Http.Json;
using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.External;
using FluentAssertions;
using Moq;

namespace AuraWellness.Tests.Integration;

public class CompaniesControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public CompaniesControllerTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── POST /api/companies/onboard ────────────────────────────────────────────

    [Fact]
    public async Task Onboard_ValidRequest_Returns201WithIds()
    {
        var workspaceId = Guid.NewGuid();

        _factory.ChatClientMock
            .Setup(c => c.CreateWorkspaceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), default))
            .ReturnsAsync(workspaceId);
        _factory.ChatClientMock
            .Setup(c => c.AddWorkspaceMemberAsync(workspaceId, It.IsAny<Guid>(), "Admin", default))
            .Returns(Task.CompletedTask);

        var resp = await _client.PostAsJsonAsync("/api/companies/onboard",
            new OnboardRequest(
                "New Company",
                "42 Test Ave",
                "555-9876",
                "Jane",
                "Founder",
                $"jane_{Guid.NewGuid():N}@newco.com",
                "Secure@123!"));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<OnboardResponse>();
        body!.CompanyId.Should().NotBe(Guid.Empty);
        body.DefaultBuId.Should().NotBe(Guid.Empty);
        body.OwnerPersonId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Onboard_IsPublicEndpoint_NoAuthRequired()
    {
        var workspaceId = Guid.NewGuid();

        _factory.ChatClientMock
            .Setup(c => c.CreateWorkspaceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), default))
            .ReturnsAsync(workspaceId);
        _factory.ChatClientMock
            .Setup(c => c.AddWorkspaceMemberAsync(workspaceId, It.IsAny<Guid>(), "Admin", default))
            .Returns(Task.CompletedTask);

        // Using unauthenticated client — no Bearer token
        var resp = await _client.PostAsJsonAsync("/api/companies/onboard",
            new OnboardRequest(
                "Public Co",
                "1 Public Rd",
                "555-0001",
                "Pub",
                "Owner",
                $"pub_{Guid.NewGuid():N}@pub.com",
                "Password@1"));

        // Must not be 401 — public endpoint
        resp.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
