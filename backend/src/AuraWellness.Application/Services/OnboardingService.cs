using AuraWellness.Application.DTOs;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Enums;
using AuraWellness.Domain.Interfaces;

namespace AuraWellness.Application.Services;

public interface IOnboardingService
{
    Task<OnboardResponse> OnboardAsync(OnboardRequest request, CancellationToken ct = default);
}

public class OnboardingService(
    IUnitOfWork unitOfWork,
    ICompanyRepository companyRepo,
    IBusinessUnitRepository buRepo,
    IPersonRepository personRepo,
    IBuStaffProfileRepository profileRepo,
    IPasswordHasher passwordHasher,
    IChatServiceClient chatClient) : IOnboardingService
{
    private const string DefaultPassword = "Welcome@123";

    public async Task<OnboardResponse> OnboardAsync(OnboardRequest request, CancellationToken ct = default)
    {
        Company company = null!;
        BusinessUnit defaultBu = null!;
        Person person = null!;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            company = Company.Create(request.CompanyName, request.Address, request.ContactNumber);
            await companyRepo.AddAsync(company, ct);

            defaultBu = BusinessUnit.Create(company.Id, $"{request.CompanyName} HQ");
            await buRepo.AddAsync(defaultBu, ct);

            person = Person.Create(company.Id, request.OwnerFirstName, request.OwnerLastName);
            await personRepo.AddAsync(person, ct);

            var hash = passwordHasher.Hash(DefaultPassword);
            var profile = BuStaffProfile.Create(person.Id, defaultBu.Id, request.OwnerEmail, hash, StaffRole.Owner);
            await profileRepo.AddAsync(profile, ct);

            return true;
        }, ct);

        // Provision chat workspace outside the DB transaction — if this fails, HTTP 500 is returned.
        // For production, an outbox pattern would guarantee delivery.
        var workspaceId = await chatClient.CreateWorkspaceAsync(defaultBu.Id, company.Id, defaultBu.Name, ct);
        await chatClient.AddWorkspaceMemberAsync(workspaceId, person.Id, "Admin", ct);

        return new OnboardResponse(company.Id, defaultBu.Id, person.Id, DefaultPassword);
    }
}
