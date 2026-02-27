using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuraWellness.Infrastructure.Persistence.Repositories;

public class CompanyRepository(AppDbContext db) : ICompanyRepository
{
    public async Task<Company> AddAsync(Company company, CancellationToken ct = default)
    {
        db.Companies.Add(company);
        await db.SaveChangesAsync(ct);
        return company;
    }

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);
}
