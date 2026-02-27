using AuraWellness.Domain.Entities;

namespace AuraWellness.Domain.Interfaces;

public interface IBuStaffProfileRepository
{
    Task<BuStaffProfile> AddAsync(BuStaffProfile profile, CancellationToken ct = default);
    Task<BuStaffProfile?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<List<BuStaffProfile>> GetByEmailMultipleAsync(string email, CancellationToken ct = default);
    Task<BuStaffProfile?> GetByEmailAndBuAsync(string email, Guid buId, CancellationToken ct = default);
    Task<BuStaffProfile?> GetByPersonAndBuAsync(Guid personId, Guid buId, CancellationToken ct = default);
    Task<List<BuStaffProfile>> GetByBuAsync(Guid buId, CancellationToken ct = default);
    Task<List<BuStaffProfile>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
