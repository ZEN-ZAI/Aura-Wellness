using AuraWellness.Application.Interfaces.External;
using AuraWellness.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AuraWellness.Infrastructure.Persistence;

public class DatabaseResetter(AppDbContext db, IConfiguration configuration) : IDatabaseResetter
{
    private static readonly DateTime SeedDate =
        new(2026, 2, 27, 20, 17, 13, DateTimeKind.Utc);

    // BCrypt hash of "P@ssw0rd" (cost 11) — for local / demo login only.
    private const string DemoPasswordHash =
        "$2b$11$qxivdApojomdiQv8sthJDOsUS0GcQigEDmhF1ObImcOozUX14LeGq";

    public async Task ResetToSeedAsync(CancellationToken ct = default)
    {
        // Cascade-delete everything: companies → business_units / persons → bu_staff_profiles
        await db.Database.ExecuteSqlRawAsync("DELETE FROM companies", ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO companies (id, name, address, contact_number, created_at)
            VALUES ({0}, {1}, {2}, {3}, {4})
            """,
            InitialDataSeed.CompanyId, "Aura Wellness Demo", "1 Wellness Way", "555-0100", SeedDate);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO business_units (id, company_id, name, created_at)
            VALUES ({0}, {1}, {2}, {3})
            """,
            InitialDataSeed.BuId, InitialDataSeed.CompanyId, "Aura Wellness Demo HQ", SeedDate);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO persons (id, company_id, first_name, last_name, created_at)
            VALUES ({0}, {1}, {2}, {3}, {4})
            """,
            InitialDataSeed.PersonId, InitialDataSeed.CompanyId, "Demo", "Owner", SeedDate);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO bu_staff_profiles (id, person_id, bu_id, email, password_hash, role, created_at)
            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})
            """,
            InitialDataSeed.ProfileId, InitialDataSeed.PersonId, InitialDataSeed.BuId,
            "Welcome@example.com", DemoPasswordHash, "Owner", SeedDate);

        // Also clear chat message history from the chat service DB
        var chatConnStr = configuration.GetConnectionString("ChatConnection");
        if (!string.IsNullOrEmpty(chatConnStr))
        {
            await using var conn = new NpgsqlConnection(chatConnStr);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM chat_messages", conn);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
