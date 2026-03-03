using AuraWellness.Application.DTOs;

namespace AuraWellness.Application.Interfaces.Services;

public interface IStaffService
{
    Task<List<StaffResponse>> GetAllAsync(Guid companyId, CancellationToken ct = default);
    Task<List<PersonOption>> GetPersonsAsync(Guid companyId, CancellationToken ct = default);
    Task<StaffResponse> CreateAsync(Guid companyId, CreateStaffRequest request, CancellationToken ct = default);
    Task<StaffResponse> EnrollExistingAsync(Guid companyId, EnrollExistingStaffRequest request, CancellationToken ct = default);
    Task UpdateRoleAsync(Guid personId, Guid buId, Guid companyId, string role, CancellationToken ct = default);
}
