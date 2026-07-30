using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGpsSyncFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastGpsSyncAt",
                schema: "driver",
                table: "logistic_partners",
                type: "datetime2(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastGpsSyncMessage",
                schema: "driver",
                table: "logistic_partners",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastGpsSyncStatus",
                schema: "driver",
                table: "logistic_partners",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastGpsSyncAt",
                schema: "driver",
                table: "logistic_partners");

            migrationBuilder.DropColumn(
                name: "LastGpsSyncMessage",
                schema: "driver",
                table: "logistic_partners");

            migrationBuilder.DropColumn(
                name: "LastGpsSyncStatus",
                schema: "driver",
                table: "logistic_partners");
        }
    }
}
