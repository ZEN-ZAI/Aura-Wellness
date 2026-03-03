using AuraWellness.Application.DTOs;

namespace AuraWellness.Application.Interfaces.Services;

public interface IChatAccessService
{
    Task<ChatWorkspaceResponse?> GetWorkspaceAsync(Guid buId, Guid companyId, CancellationToken ct = default);
    Task UpdateAccessAsync(Guid buId, Guid personId, bool hasAccess, Guid companyId, CancellationToken ct = default);
    Task<GetMessagesResponse?> GetMessagesAsync(Guid buId, Guid personId, Guid companyId, int limit = 50, DateTime? before = null, CancellationToken ct = default);
    Task<ChatMessageDto?> SendMessageAsync(Guid buId, Guid personId, string senderName, string content, Guid companyId, CancellationToken ct = default);
}
