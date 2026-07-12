using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTransporterFromAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_drivers_transporters_LogisticPartnerId",
                schema: "auth",
                table: "drivers");

            migrationBuilder.DropTable(
                name: "transporters",
                schema: "auth");

            migrationBuilder.DropIndex(
                name: "IX_drivers_LogisticPartnerId",
                schema: "auth",
                table: "drivers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transporters",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    trace_id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transporters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_drivers_LogisticPartnerId",
                schema: "auth",
                table: "drivers",
                column: "LogisticPartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_drivers_transporters_LogisticPartnerId",
                schema: "auth",
                table: "drivers",
                column: "LogisticPartnerId",
                principalSchema: "auth",
                principalTable: "transporters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
