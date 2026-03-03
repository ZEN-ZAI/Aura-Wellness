namespace AuraWellness.Application.Interfaces.External;

public interface IChatServiceClient
{
    Task<Guid> CreateWorkspaceAsync(Guid buId, Guid companyId, string name, CancellationToken ct = default);
    Task AddWorkspaceMemberAsync(Guid workspaceId, Guid personId, string role, CancellationToken ct = default);
    Task UpdateMemberAccessAsync(Guid workspaceId, Guid personId, bool hasAccess, CancellationToken ct = default);
    Task<ChatWorkspaceDto?> GetWorkspaceByBuIdAsync(Guid buId, CancellationToken ct = default);
    Task<List<ChatMemberDto>> GetWorkspaceMembersAsync(Guid workspaceId, CancellationToken ct = default);
    Task<List<ChatMessageServiceDto>> GetMessagesAsync(Guid workspaceId, int limit = 50, DateTime? before = null, CancellationToken ct = default);
    Task<ChatMessageServiceDto> SendMessageAsync(Guid workspaceId, Guid personId, string senderName, string content, CancellationToken ct = default);
    IAsyncEnumerable<ChatMessageServiceDto> StreamMessagesAsync(Guid workspaceId, CancellationToken ct = default);
}

public record ChatWorkspaceDto(Guid Id, Guid BuId, Guid CompanyId, string Name, DateTime CreatedAt);
public record ChatMemberDto(Guid Id, Guid WorkspaceId, Guid PersonId, string Role, bool HasAccess, DateTime CreatedAt);
public record ChatMessageServiceDto(Guid Id, Guid WorkspaceId, Guid PersonId, string SenderName, string Content, DateTime CreatedAt);
