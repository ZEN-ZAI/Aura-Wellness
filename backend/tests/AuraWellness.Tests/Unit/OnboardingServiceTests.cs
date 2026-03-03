using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Application.Services;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace AuraWellness.Tests.Unit;

public class OnboardingServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly Mock<IBusinessUnitRepository> _buRepo = new();
    private readonly Mock<IPersonRepository> _personRepo = new();
    private readonly Mock<IBuStaffProfileRepository> _profileRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IChatServiceClient> _chatClient = new();
    private readonly OnboardingService _sut;

    public OnboardingServiceTests()
    {
        _sut = new OnboardingService(
            _uow.Object, _companyRepo.Object, _buRepo.Object,
            _personRepo.Object, _profileRepo.Object,
            _hasher.Object, _chatClient.Object);
    }

    [Fact]
    public async Task OnboardAsync_Valid_CreatesAllEntitiesAndProvisionsChatWorkspace()
    {
        var workspaceId = Guid.NewGuid();

        // Execute the transaction delegate immediately
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>(), default))
            .Returns<Func<Task<bool>>, CancellationToken>(async (fn, _) => await fn());

        _companyRepo.Setup(r => r.AddAsync(It.IsAny<Company>(), default))
            .ReturnsAsync((Company c, CancellationToken _) => c);
        _buRepo.Setup(r => r.AddAsync(It.IsAny<BusinessUnit>(), default))
            .ReturnsAsync((BusinessUnit b, CancellationToken _) => b);
        _personRepo.Setup(r => r.AddAsync(It.IsAny<Person>(), default))
            .ReturnsAsync((Person p, CancellationToken _) => p);
        _profileRepo.Setup(r => r.AddAsync(It.IsAny<BuStaffProfile>(), default))
            .ReturnsAsync((BuStaffProfile p, CancellationToken _) => p);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _chatClient.Setup(c => c.CreateWorkspaceAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), default))
            .ReturnsAsync(workspaceId);
        _chatClient.Setup(c => c.AddWorkspaceMemberAsync(
                workspaceId, It.IsAny<Guid>(), "Admin", default))
            .Returns(Task.CompletedTask);

        var result = await _sut.OnboardAsync(new OnboardRequest(
            "Acme Corp", "123 Main St", "555-1234",
            "John", "Doe", "john@acme.com", "Secret123"), default);

        result.CompanyId.Should().NotBe(Guid.Empty);
        result.DefaultBuId.Should().NotBe(Guid.Empty);
        result.OwnerPersonId.Should().NotBe(Guid.Empty);

        _companyRepo.Verify(r => r.AddAsync(It.IsAny<Company>(), default), Times.Once);
        _buRepo.Verify(r => r.AddAsync(It.IsAny<BusinessUnit>(), default), Times.Once);
        _personRepo.Verify(r => r.AddAsync(It.IsAny<Person>(), default), Times.Once);
        _profileRepo.Verify(r => r.AddAsync(It.IsAny<BuStaffProfile>(), default), Times.Once);
        _chatClient.Verify(c => c.CreateWorkspaceAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Once);
        _chatClient.Verify(c => c.AddWorkspaceMemberAsync(
            workspaceId, It.IsAny<Guid>(), "Admin", default), Times.Once);
    }

    [Fact]
    public async Task OnboardAsync_ChatClientThrows_ExceptionPropagates()
    {
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>(), default))
            .Returns<Func<Task<bool>>, CancellationToken>(async (fn, _) => await fn());

        _companyRepo.Setup(r => r.AddAsync(It.IsAny<Company>(), default))
            .ReturnsAsync((Company c, CancellationToken _) => c);
        _buRepo.Setup(r => r.AddAsync(It.IsAny<BusinessUnit>(), default))
            .ReturnsAsync((BusinessUnit b, CancellationToken _) => b);
        _personRepo.Setup(r => r.AddAsync(It.IsAny<Person>(), default))
            .ReturnsAsync((Person p, CancellationToken _) => p);
        _profileRepo.Setup(r => r.AddAsync(It.IsAny<BuStaffProfile>(), default))
            .ReturnsAsync((BuStaffProfile p, CancellationToken _) => p);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("h");
        _chatClient.Setup(c => c.CreateWorkspaceAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), default))
            .ThrowsAsync(new InvalidOperationException("gRPC down"));

        await _sut.Invoking(s => s.OnboardAsync(new OnboardRequest(
                "X", "Y", "Z", "A", "B", "a@b.com", "pw"), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*gRPC down*");
    }
}
