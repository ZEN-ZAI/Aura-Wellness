namespace AuraWellness.Application.DTOs;

public record OnboardRequest(
    string CompanyName,
    string OwnerFirstName,
    string OwnerEmail,
    string OwnerPassword,
    string? Address,
    string? ContactNumber,
    string? OwnerLastName
);

public record OnboardResponse(
    Guid CompanyId,
    Guid DefaultBuId,
    Guid OwnerPersonId
);
