namespace AuraWellness.Domain.Entities;

public class Company
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string ContactNumber { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public ICollection<BusinessUnit> BusinessUnits { get; private set; } = [];
    public ICollection<Person> Persons { get; private set; } = [];

    private Company() { }

    public static Company Create(string name, string address, string contactNumber) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Address = address,
        ContactNumber = contactNumber,
        CreatedAt = DateTime.UtcNow
    };
}
