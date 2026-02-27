using AuraWellness.Application.DTOs;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Interfaces;

namespace AuraWellness.Application.Services;

public interface IBusinessUnitService
{
    Task<List<BusinessUnitResponse>> GetAllAsync(Guid companyId, CancellationToken ct = default);
    Task<BusinessUnitResponse> CreateAsync(Guid companyId, Guid ownerPersonId, CreateBuRequest request, CancellationToken ct = default);
}

public class BusinessUnitService(
    IBusinessUnitRepository buRepo,
    IChatServiceClient chatClient) : IBusinessUnitService
{
    public async Task<List<BusinessUnitResponse>> GetAllAsync(Guid companyId, CancellationToken ct = default)
    {
        var bus = await buRepo.GetByCompanyAsync(companyId, ct);
        return bus.Select(b => new BusinessUnitResponse(b.Id, b.CompanyId, b.Name, b.CreatedAt)).ToList();
    }

    public async Task<BusinessUnitResponse> CreateAsync(Guid companyId, Guid ownerPersonId, CreateBuRequest request, CancellationToken ct = default)
    {
        var bu = BusinessUnit.Create(companyId, request.Name);
        await buRepo.AddAsync(bu, ct);

        // Provision chat workspace and assign owner as admin
        var workspaceId = await chatClient.CreateWorkspaceAsync(bu.Id, companyId, bu.Name, ct);
        await chatClient.AddWorkspaceMemberAsync(workspaceId, ownerPersonId, "Admin", ct);

        return new BusinessUnitResponse(bu.Id, bu.CompanyId, bu.Name, bu.CreatedAt);
    }
}
