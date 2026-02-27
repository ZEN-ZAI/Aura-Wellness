using AuraWellness.Domain.Entities;

namespace AuraWellness.Domain.Interfaces;

public interface ICompanyRepository
{
    Task<Company> AddAsync(Company company, CancellationToken ct = default);
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
