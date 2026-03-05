using System.Runtime.CompilerServices;
using AuraWellness.Application.Interfaces.External;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Proto = AuraWellness.ChatService.V1;

namespace AuraWellness.Infrastructure.Grpc;

public class ChatServiceClient(Proto.ChatService.ChatServiceClient grpcClient) : IChatServiceClient
{
    private readonly Proto.ChatService.ChatServiceClient _client = grpcClient;

    public async Task<Guid> CreateWorkspaceAsync(Guid buId, Guid companyId, string name, CancellationToken ct = default)
    {
        var reply = await _client.CreateWorkspaceAsync(new Proto.CreateWorkspaceRequest
        {
            BuId      = buId.ToString(),
            CompanyId = companyId.ToString(),
            Name      = name,
        }, cancellationToken: ct);
        return Guid.Parse(reply.Id);
    }

    public async Task AddWorkspaceMemberAsync(Guid workspaceId, Guid personId, string role, CancellationToken ct = default)
    {
        await _client.AddMemberAsync(new Proto.AddMemberRequest
        {
            WorkspaceId = workspaceId.ToString(),
            PersonId    = personId.ToString(),
            Role        = role,
        }, cancellationToken: ct);
    }

    public async Task UpdateMemberAccessAsync(Guid workspaceId, Guid personId, bool hasAccess, CancellationToken ct = default)
    {
        await _client.UpdateMemberAccessAsync(new Proto.UpdateMemberAccessRequest
        {
            WorkspaceId = workspaceId.ToString(),
            PersonId    = personId.ToString(),
            HasAccess   = hasAccess,
        }, cancellationToken: ct);
    }

    public async Task<ChatWorkspaceDto?> GetWorkspaceByBuIdAsync(Guid buId, CancellationToken ct = default)
    {
        try
        {
            var reply = await _client.GetWorkspaceByBuIdAsync(
                new Proto.GetWorkspaceByBuIdRequest { BuId = buId.ToString() },
                cancellationToken: ct);
            return new ChatWorkspaceDto(
                Guid.Parse(reply.Id),
                Guid.Parse(reply.BuId),
                Guid.Parse(reply.CompanyId),
                reply.Name,
                reply.CreatedAt.ToDateTime());
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<ChatMemberDto>> GetWorkspaceMembersAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var reply = await _client.ListMembersAsync(
            new Proto.ListMembersRequest { WorkspaceId = workspaceId.ToString() },
            cancellationToken: ct);
        return reply.Members.Select(m => new ChatMemberDto(
            Guid.Parse(m.Id),
            Guid.Parse(m.WorkspaceId),
            Guid.Parse(m.PersonId),
            m.Role,
            m.HasAccess,
            m.CreatedAt.ToDateTime())).ToList();
    }

    public async Task<List<ChatMessageServiceDto>> GetMessagesAsync(
        Guid conversationId, int limit = 50, DateTime? before = null, CancellationToken ct = default)
    {
        var req = new Proto.ListMessagesRequest
        {
            ConversationId = conversationId.ToString(),
            Limit          = limit,
        };
        if (before.HasValue)
            req.Before = Timestamp.FromDateTime(before.Value.ToUniversalTime());

        var reply = await _client.ListMessagesAsync(req, cancellationToken: ct);
        return reply.Messages.Select(m => new ChatMessageServiceDto(
            Guid.Parse(m.Id),
            Guid.Parse(m.ConversationId),
            Guid.Parse(m.WorkspaceId),
            Guid.Parse(m.PersonId),
            m.SenderName,
            m.Content,
            m.CreatedAt.ToDateTime())).ToList();
    }

    public async Task<ChatMessageServiceDto> SendMessageAsync(
        Guid workspaceId, Guid conversationId, Guid personId, string senderName, string content, CancellationToken ct = default)
    {
        var reply = await _client.SendMessageAsync(new Proto.SendMessageRequest
        {
            WorkspaceId    = workspaceId.ToString(),
            ConversationId = conversationId.ToString(),
            PersonId       = personId.ToString(),
            SenderName     = senderName,
            Content        = content,
        }, cancellationToken: ct);
        return new ChatMessageServiceDto(
            Guid.Parse(reply.Id),
            Guid.Parse(reply.ConversationId),
            Guid.Parse(reply.WorkspaceId),
            Guid.Parse(reply.PersonId),
            reply.SenderName,
            reply.Content,
            reply.CreatedAt.ToDateTime());
    }

    public async IAsyncEnumerable<ChatMessageServiceDto> StreamMessagesAsync(
        Guid conversationId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var call = _client.StreamMessages(
            new Proto.StreamMessagesRequest { ConversationId = conversationId.ToString() },
            cancellationToken: ct);

        await foreach (var msg in call.ResponseStream.ReadAllAsync(ct))
        {
            yield return new ChatMessageServiceDto(
                Guid.Parse(msg.Id),
                Guid.Parse(msg.ConversationId),
                Guid.Parse(msg.WorkspaceId),
                Guid.Parse(msg.PersonId),
                msg.SenderName,
                msg.Content,
                msg.CreatedAt.ToDateTime());
        }
    }

    // ── Conversation RPCs ─────────────────────────────────────────────────────

    public async Task<ChatConversationDto> GetOrCreateDMAsync(Guid workspaceId, Guid personAId, Guid personBId, CancellationToken ct = default)
    {
        var reply = await _client.GetOrCreateDMAsync(new Proto.GetOrCreateDMRequest
        {
            WorkspaceId = workspaceId.ToString(),
            PersonAId   = personAId.ToString(),
            PersonBId   = personBId.ToString(),
        }, cancellationToken: ct);
        return new ChatConversationDto(
            Guid.Parse(reply.Id),
            Guid.Parse(reply.WorkspaceId),
            reply.Type,
            reply.CreatedAt.ToDateTime());
    }

    public async Task<ChatConversationDto> GetGroupConversationAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var reply = await _client.GetGroupConversationAsync(new Proto.GetGroupConversationRequest
        {
            WorkspaceId = workspaceId.ToString(),
        }, cancellationToken: ct);
        return new ChatConversationDto(
            Guid.Parse(reply.Id),
            Guid.Parse(reply.WorkspaceId),
            reply.Type,
            reply.CreatedAt.ToDateTime());
    }

    public async Task<List<ChatConversationDto>> ListConversationsAsync(Guid workspaceId, Guid personId, CancellationToken ct = default)
    {
        var reply = await _client.ListConversationsAsync(new Proto.ListConversationsRequest
        {
            WorkspaceId = workspaceId.ToString(),
            PersonId    = personId.ToString(),
        }, cancellationToken: ct);
        return reply.Conversations.Select(c => new ChatConversationDto(
            Guid.Parse(c.Id),
            Guid.Parse(c.WorkspaceId),
            c.Type,
            c.CreatedAt.ToDateTime())).ToList();
    }

    public async Task<List<ChatConversationParticipantDto>> GetConversationParticipantsAsync(Guid conversationId, CancellationToken ct = default)
    {
        var reply = await _client.GetConversationParticipantsAsync(new Proto.GetConversationParticipantsRequest
        {
            ConversationId = conversationId.ToString(),
        }, cancellationToken: ct);
        return reply.Participants.Select(p => new ChatConversationParticipantDto(
            Guid.Parse(p.Id),
            Guid.Parse(p.ConversationId),
            Guid.Parse(p.PersonId))).ToList();
    }
}
