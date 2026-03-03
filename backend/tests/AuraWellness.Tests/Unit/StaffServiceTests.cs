using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Application.Services;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Enums;
using AuraWellness.Domain.Interfaces;
using AuraWellness.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace AuraWellness.Tests.Unit;

public class StaffServiceTests
{
    private readonly Mock<IPersonRepository> _personRepo = new();
    private readonly Mock<IBuStaffProfileRepository> _profileRepo = new();
    private readonly Mock<IBusinessUnitRepository> _buRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IChatServiceClient> _chatClient = new();
    private readonly StaffService _sut;

    public StaffServiceTests()
    {
        _sut = new StaffService(
            _personRepo.Object, _profileRepo.Object,
            _buRepo.Object, _hasher.Object, _chatClient.Object);
    }

    // ── GetAllAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsProjectedStaffResponses()
    {
        var companyId = Guid.NewGuid();
        var person = Build.Person(companyId: companyId, firstName: "Jane", lastName: "Kim");
        var bu = Build.BusinessUnit(companyId: companyId, name: "HQ");
        var profile = Build.Profile(personId: person.Id, buId: bu.Id,
            email: "jane@hq.com", role: StaffRole.Admin, person: person, bu: bu);

        _profileRepo.Setup(r => r.GetByCompanyAsync(companyId, default))
            .ReturnsAsync([profile]);

        var result = await _sut.GetAllAsync(companyId, default);

        result.Should().HaveCount(1);
        result[0].FirstName.Should().Be("Jane");
        result[0].BuName.Should().Be("HQ");
        result[0].Role.Should().Be("Admin");
    }

    // ── GetPersonsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPersonsAsync_ReturnsProjectedPersonOptions()
    {
        var companyId = Guid.NewGuid();
        var person = Build.Person(companyId: companyId, firstName: "Tom", lastName: "Hardy");
        _personRepo.Setup(r => r.GetByCompanyAsync(companyId, default)).ReturnsAsync([person]);

        var result = await _sut.GetPersonsAsync(companyId, default);

        result.Should().HaveCount(1);
        result[0].FirstName.Should().Be("Tom");
        result[0].LastName.Should().Be("Hardy");
    }

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_BuNotFound_ThrowsInvalidOperationException()
    {
        _buRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default))
            .ReturnsAsync((BusinessUnit?)null);

        await _sut.Invoking(s => s.CreateAsync(Guid.NewGuid(),
                new CreateStaffRequest("A", "B", Guid.NewGuid(), "a@b.com", "Staff"), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Business unit not found*");
    }

    [Fact]
    public async Task CreateAsync_InvalidRole_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        var bu = Build.BusinessUnit(companyId: companyId);
        _buRepo.Setup(r => r.GetByIdAsync(bu.Id, companyId, default)).ReturnsAsync(bu);

        await _sut.Invoking(s => s.CreateAsync(companyId,
                new CreateStaffRequest("A", "B", bu.Id, "a@b.com", "Owner"), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid role*");
    }

    [Fact]
    public async Task CreateAsync_Valid_CreatePersonProfileAndAddsToChat()
    {
        var companyId = Guid.NewGuid();
        var bu = Build.BusinessUnit(companyId: companyId, name: "HQ");
        var workspaceId = Guid.NewGuid();

        _buRepo.Setup(r => r.GetByIdAsync(bu.Id, companyId, default)).ReturnsAsync(bu);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _personRepo.Setup(r => r.AddAsync(It.IsAny<Person>(), default))
            .ReturnsAsync((Person p, CancellationToken _) => p);
        _profileRepo.Setup(r => r.AddAsync(It.IsAny<BuStaffProfile>(), default))
            .ReturnsAsync((BuStaffProfile p, CancellationToken _) => p);
        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(bu.Id, default))
            .ReturnsAsync(new ChatWorkspaceDto(workspaceId, bu.Id, companyId, "HQ", DateTime.UtcNow));
        _chatClient.Setup(c => c.AddWorkspaceMemberAsync(workspaceId, It.IsAny<Guid>(), "Member", default))
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(companyId,
            new CreateStaffRequest("Alice", "Wu", bu.Id, "alice@hq.com", "Staff"), default);

        result.Should().NotBeNull();
        result.FirstName.Should().Be("Alice");
        result.Role.Should().Be("Staff");
        _chatClient.Verify(c => c.AddWorkspaceMemberAsync(workspaceId, It.IsAny<Guid>(), "Member", default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ChatWorkspaceNotFound_SkipsAddMember()
    {
        var companyId = Guid.NewGuid();
        var bu = Build.BusinessUnit(companyId: companyId);

        _buRepo.Setup(r => r.GetByIdAsync(bu.Id, companyId, default)).ReturnsAsync(bu);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("h");
        _personRepo.Setup(r => r.AddAsync(It.IsAny<Person>(), default))
            .ReturnsAsync((Person p, CancellationToken _) => p);
        _profileRepo.Setup(r => r.AddAsync(It.IsAny<BuStaffProfile>(), default))
            .ReturnsAsync((BuStaffProfile p, CancellationToken _) => p);
        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(bu.Id, default))
            .ReturnsAsync((ChatWorkspaceDto?)null);

        await _sut.CreateAsync(companyId,
            new CreateStaffRequest("X", "Y", bu.Id, "x@y.com", "Staff"), default);

        _chatClient.Verify(c => c.AddWorkspaceMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
    }

    // ── EnrollExistingAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task EnrollExistingAsync_BuNotFound_ThrowsInvalidOperationException()
    {
        _buRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default))
            .ReturnsAsync((BusinessUnit?)null);

        await _sut.Invoking(s => s.EnrollExistingAsync(Guid.NewGuid(),
                new EnrollExistingStaffRequest(Guid.NewGuid(), Guid.NewGuid(), "e@e.com", "Staff"), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Business unit not found*");
    }

    [Fact]
    public async Task EnrollExistingAsync_PersonNotFound_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        var bu = Build.BusinessUnit(companyId: companyId);
        _buRepo.Setup(r => r.GetByIdAsync(bu.Id, companyId, default)).ReturnsAsync(bu);
        _personRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), companyId, default))
            .ReturnsAsync((Person?)null);

        await _sut.Invoking(s => s.EnrollExistingAsync(companyId,
                new EnrollExistingStaffRequest(Guid.NewGuid(), bu.Id, "e@e.com", "Staff"), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Person not found*");
    }

    [Fact]
    public async Task EnrollExistingAsync_AlreadyEnrolled_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        var person = Build.Person(companyId: companyId);
        var bu = Build.BusinessUnit(companyId: companyId);
        var existing = Build.Profile(personId: person.Id, buId: bu.Id);

        _buRepo.Setup(r => r.GetByIdAsync(bu.Id, companyId, default)).ReturnsAsync(bu);
        _personRepo.Setup(r => r.GetByIdAsync(person.Id, companyId, default)).ReturnsAsync(person);
        _profileRepo.Setup(r => r.GetByPersonAndBuAsync(person.Id, bu.Id, default)).ReturnsAsync(existing);

        await _sut.Invoking(s => s.EnrollExistingAsync(companyId,
                new EnrollExistingStaffRequest(person.Id, bu.Id, "e@e.com", "Staff"), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has a profile*");
    }

    [Fact]
    public async Task EnrollExistingAsync_InvalidRole_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        var person = Build.Person(companyId: companyId);
        var bu = Build.BusinessUnit(companyId: companyId);

        _buRepo.Setup(r => r.GetByIdAsync(bu.Id, companyId, default)).ReturnsAsync(bu);
        _personRepo.Setup(r => r.GetByIdAsync(person.Id, companyId, default)).ReturnsAsync(person);
        _profileRepo.Setup(r => r.GetByPersonAndBuAsync(person.Id, bu.Id, default))
            .ReturnsAsync((BuStaffProfile?)null);

        await _sut.Invoking(s => s.EnrollExistingAsync(companyId,
                new EnrollExistingStaffRequest(person.Id, bu.Id, "e@e.com", "Owner"), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid role*");
    }

    [Fact]
    public async Task EnrollExistingAsync_Valid_CreatesProfileAndAddsToChat()
    {
        var companyId = Guid.NewGuid();
        var person = Build.Person(companyId: companyId, firstName: "Sam", lastName: "Lee");
        var bu = Build.BusinessUnit(companyId: companyId, name: "Sales");
        var wsId = Guid.NewGuid();

        _buRepo.Setup(r => r.GetByIdAsync(bu.Id, companyId, default)).ReturnsAsync(bu);
        _personRepo.Setup(r => r.GetByIdAsync(person.Id, companyId, default)).ReturnsAsync(person);
        _profileRepo.Setup(r => r.GetByPersonAndBuAsync(person.Id, bu.Id, default))
            .ReturnsAsync((BuStaffProfile?)null);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("h");
        _profileRepo.Setup(r => r.AddAsync(It.IsAny<BuStaffProfile>(), default))
            .ReturnsAsync((BuStaffProfile p, CancellationToken _) => p);
        _chatClient.Setup(c => c.GetWorkspaceByBuIdAsync(bu.Id, default))
            .ReturnsAsync(new ChatWorkspaceDto(wsId, bu.Id, companyId, "Sales", DateTime.UtcNow));
        _chatClient.Setup(c => c.AddWorkspaceMemberAsync(wsId, person.Id, "Member", default))
            .Returns(Task.CompletedTask);

        var result = await _sut.EnrollExistingAsync(companyId,
            new EnrollExistingStaffRequest(person.Id, bu.Id, "sam@sales.com", "Staff"), default);

        result.PersonId.Should().Be(person.Id);
        result.BuName.Should().Be("Sales");
        _chatClient.Verify(c => c.AddWorkspaceMemberAsync(wsId, person.Id, "Member", default), Times.Once);
    }

    // ── UpdateRoleAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRoleAsync_ProfileNotFound_ThrowsInvalidOperationException()
    {
        _profileRepo.Setup(r => r.GetByPersonAndBuAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default))
            .ReturnsAsync((BuStaffProfile?)null);

        await _sut.Invoking(s => s.UpdateRoleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Staff", default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Staff profile not found*");
    }

    [Fact]
    public async Task UpdateRoleAsync_WrongCompany_ThrowsUnauthorizedAccessException()
    {
        var personId = Guid.NewGuid();
        var buId = Guid.NewGuid();
        var actualCompanyId = Guid.NewGuid();
        var differentCompanyId = Guid.NewGuid();

        var person = Build.Person(companyId: actualCompanyId);
        var profile = Build.Profile(personId: personId, buId: buId, person: person);

        _profileRepo.Setup(r => r.GetByPersonAndBuAsync(personId, buId, default)).ReturnsAsync(profile);

        await _sut.Invoking(s => s.UpdateRoleAsync(personId, buId, differentCompanyId, "Staff", default))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Access denied*");
    }

    [Fact]
    public async Task UpdateRoleAsync_InvalidRole_ThrowsInvalidOperationException()
    {
        var companyId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var buId = Guid.NewGuid();
        var person = Build.Person(companyId: companyId);
        var profile = Build.Profile(personId: personId, buId: buId, person: person);

        _profileRepo.Setup(r => r.GetByPersonAndBuAsync(personId, buId, default)).ReturnsAsync(profile);

        await _sut.Invoking(s => s.UpdateRoleAsync(personId, buId, companyId, "Owner", default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid role*");
    }

    [Fact]
    public async Task UpdateRoleAsync_Valid_UpdatesRoleAndSavesChanges()
    {
        var companyId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var buId = Guid.NewGuid();
        var person = Build.Person(companyId: companyId);
        var profile = Build.Profile(personId: personId, buId: buId, role: StaffRole.Staff, person: person);

        _profileRepo.Setup(r => r.GetByPersonAndBuAsync(personId, buId, default)).ReturnsAsync(profile);

        await _sut.UpdateRoleAsync(personId, buId, companyId, "Admin", default);

        profile.Role.Should().Be(StaffRole.Admin);
        _profileRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
