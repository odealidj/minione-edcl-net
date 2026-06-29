using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Database.Migrations.Auth
{
    /// <inheritdoc />
    public partial class SeedAdminAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "auth",
                table: "app_users",
                columns: new[] { "Id", "created_at", "created_by", "deleted_at", "deleted_by", "Email", "IsActive", "Name", "PasswordHash", "RoleId", "trace_id", "updated_at", "updated_by" },
                values: new object[] { -1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "admin@edcl.com", true, "System Admin", "$2b$12$Bo9S.nsQu4rwtECmOp0lTO5VCRWAvI1mkVe/pmkiLA1C7PJkE1xom", 1L, null, null, null });

            migrationBuilder.InsertData(
                schema: "auth",
                table: "roles",
                columns: new[] { "Id", "Code", "created_at", "created_by", "deleted_at", "deleted_by", "Description", "IsActive", "Name", "trace_id", "updated_at", "updated_by" },
                values: new object[] { 3L, "DRIVER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Driver access for mobile application", true, "Driver", null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "auth",
                table: "app_users",
                keyColumn: "Id",
                keyValue: -1L);

            migrationBuilder.DeleteData(
                schema: "auth",
                table: "roles",
                keyColumn: "Id",
                keyValue: 3L);
        }
    }
}
