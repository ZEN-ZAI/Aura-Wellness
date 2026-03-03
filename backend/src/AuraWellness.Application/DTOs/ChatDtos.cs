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

public record ChatMessageDto(
    Guid MessageId,
    Guid PersonId,
    string SenderName,
    string Content,
    DateTime CreatedAt
);

public record GetMessagesResponse(List<ChatMessageDto> Messages);

public record SendChatMessageRequest(string Content);
