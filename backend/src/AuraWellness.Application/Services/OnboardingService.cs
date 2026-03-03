using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Application.Interfaces.Services;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Enums;
using AuraWellness.Domain.Interfaces;

namespace AuraWellness.Application.Services;

public class OnboardingService(
    IUnitOfWork unitOfWork,
    ICompanyRepository companyRepo,
    IBusinessUnitRepository buRepo,
    IPersonRepository personRepo,
    IBuStaffProfileRepository profileRepo,
    IPasswordHasher passwordHasher,
    IChatServiceClient chatClient) : IOnboardingService
{
    public async Task<OnboardResponse> OnboardAsync(OnboardRequest request, CancellationToken ct = default)
    {
        Company company = null!;
        BusinessUnit defaultBu = null!;
        Person person = null!;

        // All DB writes and the gRPC provisioning run inside a single execution strategy.
        // If the gRPC call fails the DB transaction is rolled back, so the system never
        // ends up with a company/owner record but no chat workspace.
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            company = Company.Create(request.CompanyName, request.Address ?? string.Empty, request.ContactNumber ?? string.Empty);
            await companyRepo.AddAsync(company, ct);

            defaultBu = BusinessUnit.Create(company.Id, $"{request.CompanyName} HQ");
            await buRepo.AddAsync(defaultBu, ct);

            person = Person.Create(company.Id, request.OwnerFirstName, request.OwnerLastName ?? string.Empty);
            await personRepo.AddAsync(person, ct);

            var hash = passwordHasher.Hash(request.OwnerPassword);
            var profile = BuStaffProfile.Create(person.Id, defaultBu.Id, request.OwnerEmail, hash, StaffRole.Owner);
            await profileRepo.AddAsync(profile, ct);

            // Provision chat workspace before committing — a gRPC failure here rolls back
            // the DB transaction, keeping both systems consistent.
            var workspaceId = await chatClient.CreateWorkspaceAsync(defaultBu.Id, company.Id, defaultBu.Name, ct);
            await chatClient.AddWorkspaceMemberAsync(workspaceId, person.Id, "Admin", ct);

            return true;
        }, ct);

        return new OnboardResponse(company.Id, defaultBu.Id, person.Id);
    }
}
