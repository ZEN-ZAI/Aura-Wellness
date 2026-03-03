using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Application.Interfaces.Services;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Interfaces;

namespace AuraWellness.Application.Services;

public class BusinessUnitService(
    IBusinessUnitRepository buRepo,
    IUnitOfWork unitOfWork,
    IChatServiceClient chatClient) : IBusinessUnitService
{
    public async Task<List<BusinessUnitResponse>> GetAllAsync(Guid companyId, CancellationToken ct = default)
    {
        var bus = await buRepo.GetByCompanyAsync(companyId, ct);
        return bus.Select(b => new BusinessUnitResponse(b.Id, b.CompanyId, b.Name, b.CreatedAt)).ToList();
    }

    public async Task<BusinessUnitResponse> CreateAsync(Guid companyId, Guid ownerPersonId, CreateBuRequest request, CancellationToken ct = default)
    {
        BusinessUnit bu = null!;

        // Wrap both the DB insert and gRPC provisioning in a transaction so that a
        // chat-service failure rolls back the BU record rather than leaving an
        // orphaned BU with no workspace.
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            bu = BusinessUnit.Create(companyId, request.Name);
            await buRepo.AddAsync(bu, ct);

            var workspaceId = await chatClient.CreateWorkspaceAsync(bu.Id, companyId, bu.Name, ct);
            await chatClient.AddWorkspaceMemberAsync(workspaceId, ownerPersonId, "Admin", ct);

            return true;
        }, ct);

        return new BusinessUnitResponse(bu.Id, bu.CompanyId, bu.Name, bu.CreatedAt);
    }
}
