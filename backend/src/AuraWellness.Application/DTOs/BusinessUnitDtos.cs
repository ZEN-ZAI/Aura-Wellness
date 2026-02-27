namespace AuraWellness.Application.DTOs;

public record CreateBuRequest(string Name);

public record BusinessUnitResponse(Guid Id, Guid CompanyId, string Name, DateTime CreatedAt);
