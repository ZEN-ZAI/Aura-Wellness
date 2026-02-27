namespace AuraWellness.Application.DTOs;

public record CreateStaffRequest(
    string FirstName,
    string LastName,
    Guid BuId,
    string Email,
    string Role
);

public record UpdateRoleRequest(string Role);

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
