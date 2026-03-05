using AuraWellness.Application.DTOs;
using AuraWellness.Application.Interfaces.External;
using AuraWellness.Application.Interfaces.Services;
using AuraWellness.Domain.Interfaces;

namespace AuraWellness.Application.Services;

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

    // ── Conversation-scoped methods ──────────────────────────────────────────

    public async Task<ListConversationsResponse?> GetConversationsAsync(Guid buId, Guid personId, Guid companyId, CancellationToken ct = default)
    {
        var workspace = await chatClient.GetWorkspaceByBuIdAsync(buId, ct);
        if (workspace is null) return null;

        // Ensure the group conversation exists before listing (auto-creates on first access)
        await chatClient.GetGroupConversationAsync(workspace.Id, ct);

        var conversations = await chatClient.ListConversationsAsync(workspace.Id, personId, ct);
        var persons = await personRepo.GetByCompanyAsync(companyId, ct);
        var personMap = persons.ToDictionary(p => p.Id);

        var convResponses = new List<ChatConversationResponse>();
        foreach (var conv in conversations)
        {
            var participants = await chatClient.GetConversationParticipantsAsync(conv.Id, ct);
            var participantResponses = participants
                .Where(p => personMap.ContainsKey(p.PersonId))
                .Select(p => new ChatConversationParticipantResponse(
                    p.PersonId,
                    personMap[p.PersonId].FirstName,
                    personMap[p.PersonId].LastName))
                .ToList();

            convResponses.Add(new ChatConversationResponse(
                conv.Id, conv.Type, conv.WorkspaceId, participantResponses));
        }

        return new ListConversationsResponse(convResponses);
    }

    public async Task<ChatConversationResponse?> GetOrCreateDMAsync(Guid buId, Guid personId, Guid targetPersonId, Guid companyId, CancellationToken ct = default)
    {
        var workspace = await chatClient.GetWorkspaceByBuIdAsync(buId, ct);
        if (workspace is null) return null;

        var conv = await chatClient.GetOrCreateDMAsync(workspace.Id, personId, targetPersonId, ct);
        var participants = await chatClient.GetConversationParticipantsAsync(conv.Id, ct);
        var persons = await personRepo.GetByCompanyAsync(companyId, ct);
        var personMap = persons.ToDictionary(p => p.Id);

        var participantResponses = participants
            .Where(p => personMap.ContainsKey(p.PersonId))
            .Select(p => new ChatConversationParticipantResponse(
                p.PersonId,
                personMap[p.PersonId].FirstName,
                personMap[p.PersonId].LastName))
            .ToList();

        return new ChatConversationResponse(conv.Id, conv.Type, conv.WorkspaceId, participantResponses);
    }

    public async Task<GetMessagesResponse?> GetConversationMessagesAsync(Guid buId, Guid conversationId, Guid personId, Guid companyId, int limit = 50, DateTime? before = null, CancellationToken ct = default)
    {
        var workspace = await chatClient.GetWorkspaceByBuIdAsync(buId, ct);
        if (workspace is null) return null;

        var members = await chatClient.GetWorkspaceMembersAsync(workspace.Id, ct);
        var member = members.FirstOrDefault(m => m.PersonId == personId);
        if (member is null || !member.HasAccess)
            throw new UnauthorizedAccessException("You do not have chat access for this workspace.");

        var serviceMessages = await chatClient.GetMessagesAsync(conversationId, limit, before, ct);
        var messages = serviceMessages
            .Select(m => new ChatMessageDto(m.Id, m.ConversationId, m.PersonId, m.SenderName, m.Content, m.CreatedAt))
            .ToList();

        return new GetMessagesResponse(messages);
    }

    public async Task<ChatMessageDto?> SendConversationMessageAsync(Guid buId, Guid conversationId, Guid personId, string senderName, string content, Guid companyId, CancellationToken ct = default)
    {
        var workspace = await chatClient.GetWorkspaceByBuIdAsync(buId, ct);
        if (workspace is null) return null;

        var members = await chatClient.GetWorkspaceMembersAsync(workspace.Id, ct);
        var member = members.FirstOrDefault(m => m.PersonId == personId);
        if (member is null || !member.HasAccess)
            throw new UnauthorizedAccessException("You do not have chat access for this workspace.");

        var result = await chatClient.SendMessageAsync(workspace.Id, conversationId, personId, senderName, content, ct);
        return new ChatMessageDto(result.Id, result.ConversationId, result.PersonId, result.SenderName, result.Content, result.CreatedAt);
    }

    // ── Legacy workspace-level methods (delegate to group conversation) ──────

    public async Task<GetMessagesResponse?> GetMessagesAsync(Guid buId, Guid personId, Guid companyId, int limit = 50, DateTime? before = null, CancellationToken ct = default)
    {
        var workspace = await chatClient.GetWorkspaceByBuIdAsync(buId, ct);
        if (workspace is null) return null;

        var groupConv = await chatClient.GetGroupConversationAsync(workspace.Id, ct);
        return await GetConversationMessagesAsync(buId, groupConv.Id, personId, companyId, limit, before, ct);
    }

    public async Task<ChatMessageDto?> SendMessageAsync(Guid buId, Guid personId, string senderName, string content, Guid companyId, CancellationToken ct = default)
    {
        var workspace = await chatClient.GetWorkspaceByBuIdAsync(buId, ct);
        if (workspace is null) return null;

        var groupConv = await chatClient.GetGroupConversationAsync(workspace.Id, ct);
        return await SendConversationMessageAsync(buId, groupConv.Id, personId, senderName, content, companyId, ct);
    }
}
