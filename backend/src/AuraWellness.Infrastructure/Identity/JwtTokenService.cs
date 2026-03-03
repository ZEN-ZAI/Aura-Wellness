using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuraWellness.Infrastructure.Identity;

public class JwtTokenService(IConfiguration config) : IJwtTokenService
{
    public string GenerateToken(BuStaffProfile profile)
    {
        var secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, profile.Id.ToString()),
            new Claim("companyId", profile.Person.CompanyId.ToString()),
            new Claim("personId", profile.PersonId.ToString()),
            new Claim("buId", profile.BuId.ToString()),
            new Claim("role", profile.Role.ToString()),
            new Claim("firstName", profile.Person.FirstName),
            new Claim("lastName", profile.Person.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
