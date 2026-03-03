using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Application.Services;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Interfaces;
using AuraWellness.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace AuraWellness.Tests.Unit;

public class ChatAccessServiceTests
{
    private readonly Mock<IChatServiceClient> _chatClient = new();
    private readonly Mock<IBusinessUnitRepository> _buRepo = new();
    private readonly Mock<IPersonRepository> _personRepo = new();
    private readonly ChatAccessService _sut;

    public ChatAccessServiceTests()
    {
        _sut = new ChatAccessService(_chatClient.Object, _buRepo.Object, _personRepo.Object);
    }

    // Shared fixtures
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid PersonId = Guid.NewGuid();

    private ChatWorkspaceDto MakeWorkspace() =>
        new(WorkspaceId, BuId, CompanyId, "Test BU", DateTime.UtcNow);

    private ChatMemberDto MakeMember(bool hasAccess = true) =>
        new(Guid.NewGuid(), WorkspaceId, PersonId, "Member", hasAccess, DateTime.UtcNow);

    // ── GetWorkspaceAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetWorkspaceAsync_BuNotFound_ReturnsNull()
    {
        _buRepo.Setup(r => r.GetByIdAsync(BuId, CompanyId, default)).ReturnsAsync((BusinessUnit?)null);

        var result = await _sut.GetWorkspaceAsync(BuId, CompanyId, default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWorkspaceAsync_WorkspaceNotFound_ReturnsNull()
    {
        var bu = Build.BusinessUnit(companyId: CompanyId);
        _buRepo.Setup(r => r.GetByIdAsync(BuId, CompanyId, default)).ReturnsAsync(bu);
        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(BuId, default))
            .ReturnsAsync((ChatWorkspaceDto?)null);

        var result = await _sut.GetWorkspaceAsync(BuId, CompanyId, default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWorkspaceAsync_Valid_ReturnsMappedResponseWithMembers()
    {
        var bu = Build.BusinessUnit(companyId: CompanyId, name: "HQ");
        var person = Build.Person(id: PersonId, companyId: CompanyId, firstName: "Eve", lastName: "Adams");
        var member = MakeMember(hasAccess: true);
        var workspace = MakeWorkspace();

        _buRepo.Setup(r => r.GetByIdAsync(BuId, CompanyId, default)).ReturnsAsync(bu);
        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(BuId, default)).ReturnsAsync(workspace);
        _chatClient.Setup(c => c.GetWorkspaceMembersAsync(WorkspaceId, default)).ReturnsAsync([member]);
        _personRepo.Setup(r => r.GetByCompanyAsync(CompanyId, default)).ReturnsAsync([person]);

        var result = await _sut.GetWorkspaceAsync(BuId, CompanyId, default);

        result.Should().NotBeNull();
        result!.WorkspaceId.Should().Be(WorkspaceId);
        result.Members.Should().HaveCount(1);
        result.Members[0].FirstName.Should().Be("Eve");
        result.Members[0].HasAccess.Should().BeTrue();
    }

    // ── UpdateAccessAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAccessAsync_BuNotFound_ThrowsInvalidOperationException()
    {
        _buRepo.Setup(r => r.GetByIdAsync(BuId, CompanyId, default)).ReturnsAsync((BusinessUnit?)null);

        await _sut.Invoking(s => s.UpdateAccessAsync(BuId, PersonId, true, CompanyId, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Business unit not found*");
    }

    [Fact]
    public async Task UpdateAccessAsync_PersonNotFound_ThrowsInvalidOperationException()
    {
        var bu = Build.BusinessUnit(companyId: CompanyId);
        _buRepo.Setup(r => r.GetByIdAsync(BuId, CompanyId, default)).ReturnsAsync(bu);
        _personRepo.Setup(r => r.GetByIdAsync(PersonId, CompanyId, default)).ReturnsAsync((Person?)null);

        await _sut.Invoking(s => s.UpdateAccessAsync(BuId, PersonId, true, CompanyId, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Person not found*");
    }

    [Fact]
    public async Task UpdateAccessAsync_WorkspaceNotFound_ThrowsInvalidOperationException()
    {
        var bu = Build.BusinessUnit(companyId: CompanyId);
        var person = Build.Person(companyId: CompanyId);
        _buRepo.Setup(r => r.GetByIdAsync(BuId, CompanyId, default)).ReturnsAsync(bu);
        _personRepo.Setup(r => r.GetByIdAsync(PersonId, CompanyId, default)).ReturnsAsync(person);
        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(BuId, default))
            .ReturnsAsync((ChatWorkspaceDto?)null);

        await _sut.Invoking(s => s.UpdateAccessAsync(BuId, PersonId, true, CompanyId, default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Chat workspace not found*");
    }

    [Fact]
    public async Task UpdateAccessAsync_Valid_CallsChatClientUpdateMemberAccess()
    {
        var bu = Build.BusinessUnit(companyId: CompanyId);
        var person = Build.Person(id: PersonId, companyId: CompanyId);
        var workspace = MakeWorkspace();

        _buRepo.Setup(r => r.GetByIdAsync(BuId, CompanyId, default)).ReturnsAsync(bu);
        _personRepo.Setup(r => r.GetByIdAsync(PersonId, CompanyId, default)).ReturnsAsync(person);
        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(BuId, default)).ReturnsAsync(workspace);
        _chatClient.Setup(c => c.UpdateMemberAccessAsync(WorkspaceId, PersonId, true, default))
            .Returns(Task.CompletedTask);

        await _sut.UpdateAccessAsync(BuId, PersonId, true, CompanyId, default);

        _chatClient.Verify(c => c.UpdateMemberAccessAsync(WorkspaceId, PersonId, true, default), Times.Once);
    }

    // ── GetMessagesAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetMessagesAsync_WorkspaceNotFound_ReturnsNull()
    {
        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(BuId, default))
            .ReturnsAsync((ChatWorkspaceDto?)null);

        var result = await _sut.GetMessagesAsync(BuId, PersonId, CompanyId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMessagesAsync_MemberNotFound_ThrowsUnauthorizedAccessException()
    {
        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(BuId, default)).ReturnsAsync(MakeWorkspace());
        _chatClient.Setup(c => c.GetWorkspaceMembersAsync(WorkspaceId, default))
            .ReturnsAsync([]); // no members

        await _sut.Invoking(s => s.GetMessagesAsync(BuId, PersonId, CompanyId))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*chat access*");
    }

    [Fact]
    public async Task GetMessagesAsync_MemberNoAccess_ThrowsUnauthorizedAccessException()
    {
        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(BuId, default)).ReturnsAsync(MakeWorkspace());
        _chatClient.Setup(c => c.GetWorkspaceMembersAsync(WorkspaceId, default))
            .ReturnsAsync([MakeMember(hasAccess: false)]);

        await _sut.Invoking(s => s.GetMessagesAsync(BuId, PersonId, CompanyId))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetMessagesAsync_Valid_ReturnsMappedMessages()
    {
        var msgId = Guid.NewGuid();
        var serviceMsg = new ChatMessageServiceDto(
            msgId, WorkspaceId, PersonId, "Alice", "Hello!", DateTime.UtcNow);

        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(BuId, default)).ReturnsAsync(MakeWorkspace());
        _chatClient.Setup(c => c.GetWorkspaceMembersAsync(WorkspaceId, default))
            .ReturnsAsync([MakeMember(hasAccess: true)]);
        _chatClient.Setup(c => c.GetMessagesAsync(WorkspaceId, 50, null, default))
            .ReturnsAsync([serviceMsg]);

        var result = await _sut.GetMessagesAsync(BuId, PersonId, CompanyId);

        result.Should().NotBeNull();
        result!.Messages.Should().HaveCount(1);
        result.Messages[0].Content.Should().Be("Hello!");
        result.Messages[0].SenderName.Should().Be("Alice");
    }

    // ── SendMessageAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessageAsync_WorkspaceNotFound_ReturnsNull()
    {
        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(BuId, default))
            .ReturnsAsync((ChatWorkspaceDto?)null);

        var result = await _sut.SendMessageAsync(BuId, PersonId, "Alice", "Hello", CompanyId, default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SendMessageAsync_NoAccess_ThrowsUnauthorizedAccessException()
    {
        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(BuId, default)).ReturnsAsync(MakeWorkspace());
        _chatClient.Setup(c => c.GetWorkspaceMembersAsync(WorkspaceId, default))
            .ReturnsAsync([MakeMember(hasAccess: false)]);

        await _sut.Invoking(s => s.SendMessageAsync(BuId, PersonId, "Alice", "Hi", CompanyId, default))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SendMessageAsync_Valid_ReturnsChatMessageDto()
    {
        var sentMsg = new ChatMessageServiceDto(
            Guid.NewGuid(), WorkspaceId, PersonId, "Alice", "Hello!", DateTime.UtcNow);

        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(BuId, default)).ReturnsAsync(MakeWorkspace());
        _chatClient.Setup(c => c.GetWorkspaceMembersAsync(WorkspaceId, default))
            .ReturnsAsync([MakeMember(hasAccess: true)]);
        _chatClient.Setup(c => c.SendMessageAsync(WorkspaceId, PersonId, "Alice", "Hello!", default))
            .ReturnsAsync(sentMsg);

        var result = await _sut.SendMessageAsync(BuId, PersonId, "Alice", "Hello!", CompanyId, default);

        result.Should().NotBeNull();
        result!.Content.Should().Be("Hello!");
        result.SenderName.Should().Be("Alice");
    }
}
