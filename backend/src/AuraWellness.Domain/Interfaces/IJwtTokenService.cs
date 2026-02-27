using AuraWellness.Domain.Entities;

namespace AuraWellness.Domain.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(BuStaffProfile profile);
}
