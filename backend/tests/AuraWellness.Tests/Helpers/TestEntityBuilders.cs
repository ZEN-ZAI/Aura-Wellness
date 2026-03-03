using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Enums;
using System.Reflection;

namespace AuraWellness.Tests.Helpers;

/// <summary>
/// Builds domain entities for unit tests, using reflection to set navigation
/// properties that have private setters (EF Core pattern).
/// </summary>
public static class Build
{
    public static Person Person(
        Guid? id = null,
        Guid? companyId = null,
        string firstName = "John",
        string lastName = "Doe")
    {
        var person = Domain.Entities.Person.Create(
            companyId ?? Guid.NewGuid(), firstName, lastName);
        if (id.HasValue)
            SetPrivate(person, "Id", id.Value);
        return person;
    }

    public static BusinessUnit BusinessUnit(
        Guid? id = null,
        Guid? companyId = null,
        string name = "Test BU")
    {
        var bu = Domain.Entities.BusinessUnit.Create(
            companyId ?? Guid.NewGuid(), name);
        if (id.HasValue)
            SetPrivate(bu, "Id", id.Value);
        return bu;
    }

    public static BuStaffProfile Profile(
        Guid? personId = null,
        Guid? buId = null,
        string email = "test@example.com",
        string passwordHash = "hashed",
        StaffRole role = StaffRole.Staff,
        Domain.Entities.Person? person = null,
        Domain.Entities.BusinessUnit? bu = null)
    {
        var pid = personId ?? Guid.NewGuid();
        var bid = buId ?? Guid.NewGuid();
        var profile = BuStaffProfile.Create(pid, bid, email, passwordHash, role);
        if (person is not null) SetPrivate(profile, "Person", person);
        if (bu is not null) SetPrivate(profile, "BusinessUnit", bu);
        return profile;
    }

    private static void SetPrivate(object obj, string propertyName, object? value)
    {
        var prop = obj.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        prop?.SetValue(obj, value);
    }
}
