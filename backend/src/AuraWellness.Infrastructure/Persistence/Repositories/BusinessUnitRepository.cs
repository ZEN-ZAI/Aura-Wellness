using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuraWellness.Infrastructure.Persistence.Repositories;

public class BusinessUnitRepository(AppDbContext db) : IBusinessUnitRepository
{
    public async Task<BusinessUnit> AddAsync(BusinessUnit bu, CancellationToken ct = default)
    {
        db.BusinessUnits.Add(bu);
        await db.SaveChangesAsync(ct);
        return bu;
    }

    public Task<List<BusinessUnit>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        db.BusinessUnits
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(ct);

    public Task<BusinessUnit?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default) =>
        db.BusinessUnits.FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId, ct);
}
