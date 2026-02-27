using AuraWellness.Domain.Entities;

namespace AuraWellness.Domain.Interfaces;

public interface IPersonRepository
{
    Task<Person> AddAsync(Person person, CancellationToken ct = default);
    Task<List<Person>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<Person?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default);
}
