using AuraWellness.Application.DTOs;

namespace AuraWellness.Application.Interfaces.Services;

public interface IBusinessUnitService
{
    Task<List<BusinessUnitResponse>> GetAllAsync(Guid companyId, CancellationToken ct = default);
    Task<BusinessUnitResponse> CreateAsync(Guid companyId, Guid ownerPersonId, CreateBuRequest request, CancellationToken ct = default);
}
