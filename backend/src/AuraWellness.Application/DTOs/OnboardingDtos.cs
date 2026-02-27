namespace AuraWellness.Application.DTOs;

public record OnboardRequest(
    string CompanyName,
    string Address,
    string ContactNumber,
    string OwnerFirstName,
    string OwnerLastName,
    string OwnerEmail
);

public record OnboardResponse(
    Guid CompanyId,
    Guid DefaultBuId,
    Guid OwnerPersonId,
    string DefaultPassword
);
