namespace AuraWellness.Domain.Entities;

public class Person
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public Company Company { get; private set; } = null!;
    public ICollection<BuStaffProfile> BuProfiles { get; private set; } = [];

    private Person() { }

    public static Person Create(Guid companyId, string firstName, string lastName) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        FirstName = firstName,
        LastName = lastName,
        CreatedAt = DateTime.UtcNow
    };
}
