using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuraWellness.Infrastructure.Persistence.Repositories;

public class PersonRepository(AppDbContext db) : IPersonRepository
{
    public async Task<Person> AddAsync(Person person, CancellationToken ct = default)
    {
        db.Persons.Add(person);
        await db.SaveChangesAsync(ct);
        return person;
    }

    public Task<List<Person>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        db.Persons
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);

    public Task<Person?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default) =>
        db.Persons.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, ct);
}
