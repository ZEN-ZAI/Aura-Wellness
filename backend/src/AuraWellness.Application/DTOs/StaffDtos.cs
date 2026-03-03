namespace AuraWellness.Application.DTOs;

public record CreateStaffRequest(
    string FirstName,
    string LastName,
    Guid BuId,
    string Email,
    string Role
);

public record EnrollExistingStaffRequest(
    Guid PersonId,
    Guid BuId,
    string Email,
    string Role
);

public record UpdateRoleRequest(string Role, Guid BuId);

public record StaffResponse(
    Guid PersonId,
    Guid ProfileId,
    Guid BuId,
    string BuName,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    DateTime CreatedAt
);

public record PersonOption(Guid PersonId, string FirstName, string LastName);
