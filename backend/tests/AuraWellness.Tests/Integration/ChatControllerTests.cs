using System.Net;
using System.Net.Http.Json;
using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Enums;
using FluentAssertions;
using Moq;

namespace AuraWellness.Tests.Integration;

public class ChatControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;
    private Guid _companyId;
    private Guid _buId;
    private Guid _personId;
    private Guid _workspaceId;

    public ChatControllerTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> SetupAsync(bool hasChatAccess = true, string role = "Owner")
    {
        _companyId = Guid.NewGuid();
        _buId = Guid.NewGuid();
        _personId = Guid.NewGuid();
        _workspaceId = Guid.NewGuid();

        await _factory.SeedAsync(async db =>
        {
            var company = Company.Create("Chat Corp", "1 St", "555");
            SetId(company, _companyId);
            db.Companies.Add(company);

            var bu = BusinessUnit.Create(_companyId, "Chat BU");
            SetId(bu, _buId);
            db.BusinessUnits.Add(bu);

            var person = Person.Create(_companyId, "Chat", "User");
            SetId(person, _personId);
            db.Persons.Add(person);

            db.BuStaffProfiles.Add(BuStaffProfile.Create(
                _personId, _buId, "chat@corp.com", "h", StaffRole.Owner));
            await Task.CompletedTask;
        });

        // Set up chat client mock shared across tests in this setup
        var workspace = new ChatWorkspaceDto(_workspaceId, _buId, _companyId, "Chat BU", DateTime.UtcNow);
        var member = new ChatMemberDto(Guid.NewGuid(), _workspaceId, _personId, "Member", hasChatAccess, DateTime.UtcNow);

        _factory.ChatClientMock.Reset();
        _factory.ChatClientMock
            .Setup(c => c.GetWorkspaceByBuIdAsync(_buId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _factory.ChatClientMock
            .Setup(c => c.GetWorkspaceMembersAsync(_workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([member]);

        return _factory.CreateAuthenticatedClient(_personId, _buId, _companyId, role);
    }

    // ── GET /api/chat/workspace/{buId} ─────────────────────────────────────────

    [Fact]
    public async Task GetWorkspace_Exists_Returns200()
    {
        var client = await SetupAsync();

        var resp = await client.GetAsync($"/api/chat/workspace/{_buId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ChatWorkspaceResponse>();
        body!.WorkspaceId.Should().Be(_workspaceId);
    }

    [Fact]
    public async Task GetWorkspace_Unauthenticated_Returns401()
    {
        _ = await SetupAsync();
        var resp = await _factory.CreateClient().GetAsync($"/api/chat/workspace/{_buId}");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── PUT /api/chat/workspace/{buId}/members/{personId}/access ──────────────

    [Fact]
    public async Task UpdateAccess_AsOwner_Returns204()
    {
        var client = await SetupAsync(role: "Owner");

        _factory.ChatClientMock
            .Setup(c => c.UpdateMemberAccessAsync(_workspaceId, _personId, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var resp = await client.PutAsJsonAsync(
            $"/api/chat/workspace/{_buId}/members/{_personId}/access",
            new UpdateChatAccessRequest(true));

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateAccess_AsStaff_Returns403()
    {
        var client = await SetupAsync(role: "Staff");
        var resp = await client.PutAsJsonAsync(
            $"/api/chat/workspace/{_buId}/members/{_personId}/access",
            new UpdateChatAccessRequest(true));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/chat/workspace/{buId}/messages ────────────────────────────────

    [Fact]
    public async Task GetMessages_HasAccess_Returns200()
    {
        var client = await SetupAsync(hasChatAccess: true);
        var msg = new ChatMessageServiceDto(
            Guid.NewGuid(), _workspaceId, _personId, "Alice", "Hello", DateTime.UtcNow);

        _factory.ChatClientMock
            .Setup(c => c.GetMessagesAsync(_workspaceId, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([msg]);

        var resp = await client.GetAsync($"/api/chat/workspace/{_buId}/messages");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<GetMessagesResponse>();
        body!.Messages.Should().HaveCount(1);
        body.Messages[0].Content.Should().Be("Hello");
    }

    [Fact]
    public async Task GetMessages_NoAccess_Returns403()
    {
        var client = await SetupAsync(hasChatAccess: false);
        var resp = await client.GetAsync($"/api/chat/workspace/{_buId}/messages");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/chat/workspace/{buId}/messages ───────────────────────────────

    [Fact]
    public async Task SendMessage_HasAccess_Returns200WithMessage()
    {
        var client = await SetupAsync(hasChatAccess: true);
        var sent = new ChatMessageServiceDto(
            Guid.NewGuid(), _workspaceId, _personId, "Chat User", "Hi!", DateTime.UtcNow);

        _factory.ChatClientMock
            .Setup(c => c.SendMessageAsync(_workspaceId, _personId, It.IsAny<string>(), "Hi!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sent);

        var resp = await client.PostAsJsonAsync(
            $"/api/chat/workspace/{_buId}/messages",
            new SendChatMessageRequest("Hi!"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ChatMessageDto>();
        body!.Content.Should().Be("Hi!");
    }

    private static void SetId(object obj, Guid id)
    {
        var prop = obj.GetType().GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        prop?.SetValue(obj, id);
    }
}
