namespace AuraWellness.Application.DTOs;

public record ChatWorkspaceResponse(
    Guid WorkspaceId,
    Guid BuId,
    string BuName,
    List<ChatMemberResponse> Members
);

public record ChatMemberResponse(
    Guid PersonId,
    string FirstName,
    string LastName,
    string Role,
    bool HasAccess
);

public record UpdateChatAccessRequest(bool HasAccess);
