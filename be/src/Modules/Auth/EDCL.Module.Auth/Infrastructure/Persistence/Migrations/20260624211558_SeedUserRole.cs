using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "auth",
                table: "roles",
                columns: new[] { "Id", "Code", "created_at", "created_by", "deleted_at", "deleted_by", "Description", "IsActive", "Name", "trace_id", "updated_at", "updated_by" },
                values: new object[] { 2L, "USER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, "Standard access for web client", true, "Standard User", null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "auth",
                table: "roles",
                keyColumn: "Id",
                keyValue: 2L);
        }
    }
}
