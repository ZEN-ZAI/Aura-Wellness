using AuraWellness.Domain.Entities;

namespace AuraWellness.Domain.Interfaces;

public interface IBusinessUnitRepository
{
    Task<BusinessUnit> AddAsync(BusinessUnit bu, CancellationToken ct = default);
    Task<List<BusinessUnit>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<BusinessUnit?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default);
}
