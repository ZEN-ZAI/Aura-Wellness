using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Application.Interfaces.Services;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Enums;
using AuraWellness.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AuraWellness.Application.Services;

public class StaffService(
    IPersonRepository personRepo,
    IBuStaffProfileRepository profileRepo,
    IBusinessUnitRepository buRepo,
    IPasswordHasher passwordHasher,
    IChatServiceClient chatClient,
    IConfiguration configuration) : IStaffService
{
    private string DefaultPassword =>
        configuration["DefaultStaffPassword"] ?? "P@ssw0rd";

    public async Task<List<StaffResponse>> GetAllAsync(Guid companyId, CancellationToken ct = default)
    {
        var profiles = await profileRepo.GetByCompanyAsync(companyId, ct);
        return profiles.Select(p => new StaffResponse(
            p.PersonId, p.Id, p.BuId, p.BusinessUnit.Name,
            p.Person.FirstName, p.Person.LastName, p.Email,
            p.Role.ToString(), p.CreatedAt)).ToList();
    }

    public async Task<List<PersonOption>> GetPersonsAsync(Guid companyId, CancellationToken ct = default)
    {
        var persons = await personRepo.GetByCompanyAsync(companyId, ct);
        return persons.Select(p => new PersonOption(p.Id, p.FirstName, p.LastName)).ToList();
    }

    public async Task<StaffResponse> CreateAsync(Guid companyId, CreateStaffRequest request, CancellationToken ct = default)
    {
        var bu = await buRepo.GetByIdAsync(request.BuId, companyId, ct)
            ?? throw new InvalidOperationException("Business unit not found.");

        if (!Enum.TryParse<StaffRole>(request.Role, true, out var parsedRole) || parsedRole == StaffRole.Owner)
            throw new InvalidOperationException("Invalid role. Allowed: Admin, Staff.");

        var person = Person.Create(companyId, request.FirstName, request.LastName);
        await personRepo.AddAsync(person, ct);

        var hash = passwordHasher.Hash(DefaultPassword);
        var profile = BuStaffProfile.Create(person.Id, bu.Id, request.Email, hash, parsedRole);
        await profileRepo.AddAsync(profile, ct);

        // Add to chat workspace as a Member with no access (Owner can grant later)
        var workspace = await chatClient.GetWorkspaceByBuIdAsync(bu.Id, ct);
        if (workspace is not null)
            await chatClient.AddWorkspaceMemberAsync(workspace.Id, person.Id, "Member", ct);

        return new StaffResponse(person.Id, profile.Id, bu.Id, bu.Name,
            person.FirstName, person.LastName, profile.Email,
            profile.Role.ToString(), profile.CreatedAt);
    }

    public async Task<StaffResponse> EnrollExistingAsync(Guid companyId, EnrollExistingStaffRequest request, CancellationToken ct = default)
    {
        var bu = await buRepo.GetByIdAsync(request.BuId, companyId, ct)
            ?? throw new InvalidOperationException("Business unit not found.");

        var person = await personRepo.GetByIdAsync(request.PersonId, companyId, ct)
            ?? throw new InvalidOperationException("Person not found or does not belong to this company.");

        var existing = await profileRepo.GetByPersonAndBuAsync(person.Id, bu.Id, ct);
        if (existing is not null)
            throw new InvalidOperationException("This person already has a profile in the selected Business Unit.");

        if (!Enum.TryParse<StaffRole>(request.Role, true, out var parsedRole) || parsedRole == StaffRole.Owner)
            throw new InvalidOperationException("Invalid role. Allowed: Admin, Staff.");

        var hash = passwordHasher.Hash(DefaultPassword);
        var profile = BuStaffProfile.Create(person.Id, bu.Id, request.Email, hash, parsedRole);
        await profileRepo.AddAsync(profile, ct);

        var workspace = await chatClient.GetWorkspaceByBuIdAsync(bu.Id, ct);
        if (workspace is not null)
            await chatClient.AddWorkspaceMemberAsync(workspace.Id, person.Id, "Member", ct);

        return new StaffResponse(person.Id, profile.Id, bu.Id, bu.Name,
            person.FirstName, person.LastName, profile.Email,
            profile.Role.ToString(), profile.CreatedAt);
    }

    public async Task UpdateRoleAsync(Guid personId, Guid buId, Guid companyId, string role, CancellationToken ct = default)
    {
        var profile = await profileRepo.GetByPersonAndBuAsync(personId, buId, ct)
            ?? throw new InvalidOperationException("Staff profile not found.");

        if (profile.Person.CompanyId != companyId)
            throw new UnauthorizedAccessException("Access denied.");

        if (!Enum.TryParse<StaffRole>(role, true, out var parsedRole) || parsedRole == StaffRole.Owner)
            throw new InvalidOperationException("Invalid role. Allowed: Admin, Staff.");

        profile.UpdateRole(parsedRole);
        await profileRepo.SaveChangesAsync(ct);
    }
}
