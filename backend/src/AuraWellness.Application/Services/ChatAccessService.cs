using AuraWellness.Application.DTOs;
using AuraWellness.Domain.Interfaces;

namespace AuraWellness.Application.Services;

public interface IChatAccessService
{
    Task<ChatWorkspaceResponse?> GetWorkspaceAsync(Guid buId, Guid companyId, CancellationToken ct = default);
    Task UpdateAccessAsync(Guid buId, Guid personId, bool hasAccess, Guid companyId, CancellationToken ct = default);
}

public class ChatAccessService(
    IChatServiceClient chatClient,
    IBusinessUnitRepository buRepo,
    IPersonRepository personRepo) : IChatAccessService
{
    public async Task<ChatWorkspaceResponse?> GetWorkspaceAsync(Guid buId, Guid companyId, CancellationToken ct = default)
    {
        var bu = await buRepo.GetByIdAsync(buId, companyId, ct);
        if (bu is null) return null;

        var workspace = await chatClient.GetWorkspaceByBuIdAsync(buId, ct);
        if (workspace is null) return null;

        var members = await chatClient.GetWorkspaceMembersAsync(workspace.Id, ct);
        var persons = await personRepo.GetByCompanyAsync(companyId, ct);
        var personMap = persons.ToDictionary(p => p.Id);

        var memberResponses = members
            .Where(m => personMap.ContainsKey(m.PersonId))
            .Select(m => new ChatMemberResponse(
                m.PersonId,
                personMap[m.PersonId].FirstName,
                personMap[m.PersonId].LastName,
                m.Role,
                m.HasAccess))
            .ToList();

        return new ChatWorkspaceResponse(workspace.Id, buId, bu.Name, memberResponses);
    }

    public async Task UpdateAccessAsync(Guid buId, Guid personId, bool hasAccess, Guid companyId, CancellationToken ct = default)
    {
        var bu = await buRepo.GetByIdAsync(buId, companyId, ct)
            ?? throw new InvalidOperationException("Business unit not found.");

        var person = await personRepo.GetByIdAsync(personId, companyId, ct)
            ?? throw new InvalidOperationException("Person not found.");

        var workspace = await chatClient.GetWorkspaceByBuIdAsync(buId, ct)
            ?? throw new InvalidOperationException("Chat workspace not found.");

        await chatClient.UpdateMemberAccessAsync(workspace.Id, personId, hasAccess, ct);
    }
}
