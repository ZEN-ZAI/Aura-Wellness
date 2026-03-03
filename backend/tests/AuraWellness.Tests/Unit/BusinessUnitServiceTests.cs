using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Application.Services;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Interfaces;
using AuraWellness.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace AuraWellness.Tests.Unit;

public class BusinessUnitServiceTests
{
    private readonly Mock<IBusinessUnitRepository> _buRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IChatServiceClient> _chatClient = new();
    private readonly BusinessUnitService _sut;

    public BusinessUnitServiceTests()
    {
        _sut = new BusinessUnitService(_buRepo.Object, _uow.Object, _chatClient.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsProjectedList()
    {
        var companyId = Guid.NewGuid();
        var bu1 = Build.BusinessUnit(companyId: companyId, name: "Alpha");
        var bu2 = Build.BusinessUnit(companyId: companyId, name: "Beta");
        _buRepo.Setup(r => r.GetByCompanyAsync(companyId, default)).ReturnsAsync([bu1, bu2]);

        var result = await _sut.GetAllAsync(companyId, default);

        result.Should().HaveCount(2);
        result.Select(r => r.Name).Should().BeEquivalentTo(["Alpha", "Beta"]);
    }

    [Fact]
    public async Task CreateAsync_Valid_ExecutesTransactionAndReturnsMappedDto()
    {
        var companyId = Guid.NewGuid();
        var ownerPersonId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        // UnitOfWork executes the delegate immediately
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>(), default))
            .Returns<Func<Task<bool>>, CancellationToken>(async (fn, _) => await fn());

        _buRepo.Setup(r => r.AddAsync(It.IsAny<BusinessUnit>(), default))
            .ReturnsAsync((BusinessUnit b, CancellationToken _) => b);

        _chatClient.Setup(c => c.CreateWorkspaceAsync(
                It.IsAny<Guid>(), companyId, It.IsAny<string>(), default))
            .ReturnsAsync(workspaceId);

        _chatClient.Setup(c => c.AddWorkspaceMemberAsync(workspaceId, ownerPersonId, "Admin", default))
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(companyId, ownerPersonId,
            new CreateBuRequest("New Branch"), default);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Branch");
        result.CompanyId.Should().Be(companyId);
        _chatClient.Verify(c => c.AddWorkspaceMemberAsync(workspaceId, ownerPersonId, "Admin", default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ChatClientThrows_TransactionRolledBack()
    {
        var companyId = Guid.NewGuid();

        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>(), default))
            .Returns<Func<Task<bool>>, CancellationToken>(async (fn, _) => await fn());

        _buRepo.Setup(r => r.AddAsync(It.IsAny<BusinessUnit>(), default))
            .ReturnsAsync((BusinessUnit b, CancellationToken _) => b);

        _chatClient.Setup(c => c.CreateWorkspaceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), default))
            .ThrowsAsync(new InvalidOperationException("gRPC failed"));

        await _sut.Invoking(s => s.CreateAsync(companyId, Guid.NewGuid(),
                new CreateBuRequest("Bad BU"), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*gRPC failed*");
    }
}
