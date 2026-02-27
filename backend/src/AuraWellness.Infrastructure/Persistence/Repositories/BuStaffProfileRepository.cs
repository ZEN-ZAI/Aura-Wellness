using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuraWellness.Infrastructure.Persistence.Repositories;

public class BuStaffProfileRepository(AppDbContext db) : IBuStaffProfileRepository
{
    public async Task<BuStaffProfile> AddAsync(BuStaffProfile profile, CancellationToken ct = default)
    {
        db.BuStaffProfiles.Add(profile);
        await db.SaveChangesAsync(ct);
        return profile;
    }

    public Task<BuStaffProfile?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.BuStaffProfiles
            .Include(p => p.Person)
            .Include(p => p.BusinessUnit)
            .FirstOrDefaultAsync(p => p.Email == email, ct);

    public Task<List<BuStaffProfile>> GetByEmailMultipleAsync(string email, CancellationToken ct = default) =>
        db.BuStaffProfiles
            .Include(p => p.Person)
            .Include(p => p.BusinessUnit)
            .Where(p => p.Email == email)
            .ToListAsync(ct);

    public Task<BuStaffProfile?> GetByEmailAndBuAsync(string email, Guid buId, CancellationToken ct = default) =>
        db.BuStaffProfiles
            .Include(p => p.Person)
            .Include(p => p.BusinessUnit)
            .FirstOrDefaultAsync(p => p.Email == email && p.BuId == buId, ct);

    public Task<BuStaffProfile?> GetByPersonAndBuAsync(Guid personId, Guid buId, CancellationToken ct = default) =>
        db.BuStaffProfiles
            .Include(p => p.Person)
            .FirstOrDefaultAsync(p => p.PersonId == personId && p.BuId == buId, ct);

    public Task<List<BuStaffProfile>> GetByBuAsync(Guid buId, CancellationToken ct = default) =>
        db.BuStaffProfiles
            .Include(p => p.Person)
            .Where(p => p.BuId == buId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);

    public Task<List<BuStaffProfile>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        db.BuStaffProfiles
            .Include(p => p.Person)
            .Include(p => p.BusinessUnit)
            .Where(p => p.Person.CompanyId == companyId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
