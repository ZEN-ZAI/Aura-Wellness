using AuraWellness.Application.DTOs;
using AuraWellness.Application.Services;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Enums;
using AuraWellness.Domain.Interfaces;
using AuraWellness.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace AuraWellness.Tests.Unit;

public class AuthServiceTests
{
    private readonly Mock<IBuStaffProfileRepository> _profileRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_profileRepo.Object, _hasher.Object, _jwt.Object);
    }

    // ── LoginAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_SingleBu_ValidCredentials_ReturnsLoginResponse()
    {
        var companyId = Guid.NewGuid();
        var person = Build.Person(companyId: companyId, firstName: "Alice", lastName: "Smith");
        var bu = Build.BusinessUnit(companyId: companyId, name: "HQ");
        var profile = Build.Profile(personId: person.Id, buId: bu.Id,
            email: "alice@acme.com", passwordHash: "hash",
            role: StaffRole.Staff, person: person, bu: bu);

        _profileRepo.Setup(r => r.GetByEmailMultipleAsync("alice@acme.com", default))
            .ReturnsAsync([profile]);
        _hasher.Setup(h => h.Verify("pass", "hash")).Returns(true);
        _jwt.Setup(j => j.GenerateToken(profile)).Returns("jwt-token");

        var result = await _sut.LoginAsync(new LoginRequest("alice@acme.com", "pass"), default);

        var resp = result.Should().BeOfType<LoginResponse>().Subject;
        resp.Token.Should().Be("jwt-token");
        resp.PersonId.Should().Be(person.Id);
        resp.BuId.Should().Be(bu.Id);
        resp.Role.Should().Be("Staff");
    }

    [Fact]
    public async Task LoginAsync_MultipleBus_NoBuId_ReturnsBuChoiceList()
    {
        var companyId = Guid.NewGuid();
        var bu1 = Build.BusinessUnit(name: "BU-A");
        var bu2 = Build.BusinessUnit(name: "BU-B");
        var person = Build.Person(companyId: companyId);
        var p1 = Build.Profile(buId: bu1.Id, person: person, bu: bu1);
        var p2 = Build.Profile(buId: bu2.Id, person: person, bu: bu2);

        _profileRepo.Setup(r => r.GetByEmailMultipleAsync("x@x.com", default))
            .ReturnsAsync([p1, p2]);

        var result = await _sut.LoginAsync(new LoginRequest("x@x.com", "any"), default);

        var choices = result.Should().BeAssignableTo<List<BuChoiceResponse>>().Subject;
        choices.Should().HaveCount(2);
        choices.Should().Contain(c => c.BuName == "BU-A");
        choices.Should().Contain(c => c.BuName == "BU-B");
    }

    [Fact]
    public async Task LoginAsync_MultipleBus_WithBuId_ReturnsMatchingProfile()
    {
        var companyId = Guid.NewGuid();
        var buA = Build.BusinessUnit(name: "BU-A");
        var buB = Build.BusinessUnit(name: "BU-B");
        var person = Build.Person(companyId: companyId, firstName: "Bob", lastName: "Lee");
        var pA = Build.Profile(buId: buA.Id, email: "bob@x.com", passwordHash: "h",
            person: person, bu: buA);
        var pB = Build.Profile(buId: buB.Id, email: "bob@x.com", passwordHash: "h",
            person: person, bu: buB);

        _profileRepo.Setup(r => r.GetByEmailMultipleAsync("bob@x.com", default))
            .ReturnsAsync([pA, pB]);
        _hasher.Setup(h => h.Verify("pw", "h")).Returns(true);
        _jwt.Setup(j => j.GenerateToken(pB)).Returns("token-b");

        var result = await _sut.LoginAsync(
            new LoginRequest("bob@x.com", "pw", buB.Id), default);

        var resp = result.Should().BeOfType<LoginResponse>().Subject;
        resp.BuId.Should().Be(buB.Id);
        resp.Token.Should().Be("token-b");
    }

    [Fact]
    public async Task LoginAsync_NoProfileFound_ThrowsUnauthorizedAccessException()
    {
        _profileRepo.Setup(r => r.GetByEmailMultipleAsync("nobody@x.com", default))
            .ReturnsAsync([]);

        await _sut.Invoking(s => s.LoginAsync(new LoginRequest("nobody@x.com", "pw"), default))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid credentials*");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var profile = Build.Profile(passwordHash: "hash",
            person: Build.Person(), bu: Build.BusinessUnit());

        _profileRepo.Setup(r => r.GetByEmailMultipleAsync("x@x.com", default))
            .ReturnsAsync([profile]);
        _hasher.Setup(h => h.Verify("wrong", "hash")).Returns(false);

        await _sut.Invoking(s => s.LoginAsync(new LoginRequest("x@x.com", "wrong"), default))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_MultipleBus_InvalidBuId_ThrowsUnauthorizedAccessException()
    {
        var person = Build.Person();
        var bu = Build.BusinessUnit();
        var profile = Build.Profile(buId: bu.Id, person: person, bu: bu);

        _profileRepo.Setup(r => r.GetByEmailMultipleAsync("x@x.com", default))
            .ReturnsAsync([profile, profile]); // triggers multi-BU path

        var unknownBuId = Guid.NewGuid();
        await _sut.Invoking(s =>
                s.LoginAsync(new LoginRequest("x@x.com", "pw", unknownBuId), default))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── ChangePasswordAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_ValidCredentials_UpdatesPasswordAndSaves()
    {
        var personId = Guid.NewGuid();
        var buId = Guid.NewGuid();
        var profile = Build.Profile(personId: personId, buId: buId, passwordHash: "old-hash");

        _profileRepo.Setup(r => r.GetByPersonAndBuAsync(personId, buId, default))
            .ReturnsAsync(profile);
        _hasher.Setup(h => h.Verify("old-pass", "old-hash")).Returns(true);
        _hasher.Setup(h => h.Hash("new-pass")).Returns("new-hash");

        await _sut.ChangePasswordAsync(personId, buId, "old-pass", "new-pass", default);

        _profileRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        profile.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public async Task ChangePasswordAsync_ProfileNotFound_ThrowsUnauthorizedAccessException()
    {
        _profileRepo.Setup(r => r.GetByPersonAndBuAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default))
            .ReturnsAsync((BuStaffProfile?)null);

        await _sut.Invoking(s =>
                s.ChangePasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "old", "new", default))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Profile not found*");
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsUnauthorizedAccessException()
    {
        var personId = Guid.NewGuid();
        var buId = Guid.NewGuid();
        var profile = Build.Profile(personId: personId, buId: buId, passwordHash: "hash");

        _profileRepo.Setup(r => r.GetByPersonAndBuAsync(personId, buId, default))
            .ReturnsAsync(profile);
        _hasher.Setup(h => h.Verify("wrong", "hash")).Returns(false);

        await _sut.Invoking(s =>
                s.ChangePasswordAsync(personId, buId, "wrong", "new", default))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Current password is incorrect*");
    }
}
