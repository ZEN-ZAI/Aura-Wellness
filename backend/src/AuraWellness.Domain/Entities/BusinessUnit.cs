namespace AuraWellness.Domain.Entities;

public class BusinessUnit
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public Company Company { get; private set; } = null!;
    public ICollection<BuStaffProfile> StaffProfiles { get; private set; } = [];

    private BusinessUnit() { }

    public static BusinessUnit Create(Guid companyId, string name) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        Name = name,
        CreatedAt = DateTime.UtcNow
    };
}
