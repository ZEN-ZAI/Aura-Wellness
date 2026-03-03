namespace AuraWellness.Application.DTOs;

public record LoginRequest(string Email, string Password, Guid? BuId = null);

public record LoginResponse(string Token, Guid PersonId, Guid BuId, Guid CompanyId, string Role, string FirstName, string LastName);

public record BuChoiceResponse(Guid BuId, string BuName);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
