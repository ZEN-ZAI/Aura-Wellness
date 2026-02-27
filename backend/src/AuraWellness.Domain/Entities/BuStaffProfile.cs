using AuraWellness.Domain.Enums;

namespace AuraWellness.Domain.Entities;

public class BuStaffProfile
{
    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid BuId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public StaffRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Person Person { get; private set; } = null!;
    public BusinessUnit BusinessUnit { get; private set; } = null!;

    private BuStaffProfile() { }

    public static BuStaffProfile Create(Guid personId, Guid buId, string email, string passwordHash, StaffRole role) => new()
    {
        Id = Guid.NewGuid(),
        PersonId = personId,
        BuId = buId,
        Email = email,
        PasswordHash = passwordHash,
        Role = role,
        CreatedAt = DateTime.UtcNow
    };

    public void UpdateRole(StaffRole newRole)
    {
        Role = newRole;
    }
}
