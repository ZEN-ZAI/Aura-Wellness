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
    Guid ConversationId,
    Guid PersonId,
    string SenderName,
    string Content,
    DateTime CreatedAt
);

public record GetMessagesResponse(List<ChatMessageDto> Messages);

public record SendChatMessageRequest(string Content);

public record ChatConversationResponse(
    Guid ConversationId,
    string Type,
    Guid WorkspaceId,
    List<ChatConversationParticipantResponse> Participants
);

public record ChatConversationParticipantResponse(
    Guid PersonId,
    string FirstName,
    string LastName
);

public record ListConversationsResponse(List<ChatConversationResponse> Conversations);

public record GetOrCreateDMRequest(Guid TargetPersonId);
