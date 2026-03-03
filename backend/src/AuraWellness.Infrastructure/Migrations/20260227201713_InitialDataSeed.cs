using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuraWellness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDataSeed : Migration
    {
        // Fixed GUIDs for the demo seed row — referenced by Program.cs for chat-workspace bootstrap.
        public static readonly Guid CompanyId  = new("10000000-0000-0000-0000-000000000001");
        public static readonly Guid BuId       = new("20000000-0000-0000-0000-000000000001");
        public static readonly Guid PersonId   = new("30000000-0000-0000-0000-000000000001");
        public static readonly Guid ProfileId  = new("40000000-0000-0000-0000-000000000001");

        // BCrypt hash of "P@ssw0rd" (cost 11) — for local / demo login only.
        private const string DemoPasswordHash =
            "$2b$11$qxivdApojomdiQv8sthJDOsUS0GcQigEDmhF1ObImcOozUX14LeGq";

        private static readonly DateTime SeedDate =
            new(2026, 2, 27, 20, 17, 13, DateTimeKind.Utc);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "companies",
                columns: new[] { "id", "name", "address", "contact_number", "created_at" },
                values: new object[]
                {
                    CompanyId,
                    "Aura Wellness Demo",
                    "1 Wellness Way",
                    "555-0100",
                    SeedDate
                });

            migrationBuilder.InsertData(
                table: "business_units",
                columns: new[] { "id", "company_id", "name", "created_at" },
                values: new object[]
                {
                    BuId,
                    CompanyId,
                    "Aura Wellness Demo HQ",
                    SeedDate
                });

            migrationBuilder.InsertData(
                table: "persons",
                columns: new[] { "id", "company_id", "first_name", "last_name", "created_at" },
                values: new object[]
                {
                    PersonId,
                    CompanyId,
                    "Demo",
                    "Owner",
                    SeedDate
                });

            migrationBuilder.InsertData(
                table: "bu_staff_profiles",
                columns: new[] { "id", "person_id", "bu_id", "email", "password_hash", "role", "created_at" },
                values: new object[]
                {
                    ProfileId,
                    PersonId,
                    BuId,
                    "Welcome@example.com",
                    DemoPasswordHash,
                    "Owner",
                    SeedDate
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "bu_staff_profiles",
                keyColumn: "id",
                keyValue: ProfileId);

            migrationBuilder.DeleteData(
                table: "persons",
                keyColumn: "id",
                keyValue: PersonId);

            migrationBuilder.DeleteData(
                table: "business_units",
                keyColumn: "id",
                keyValue: BuId);

            migrationBuilder.DeleteData(
                table: "companies",
                keyColumn: "id",
                keyValue: CompanyId);
        }
    }
}
