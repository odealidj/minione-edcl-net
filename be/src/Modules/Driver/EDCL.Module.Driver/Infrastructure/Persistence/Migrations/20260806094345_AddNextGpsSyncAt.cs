using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNextGpsSyncAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextGpsSyncAt",
                schema: "driver",
                table: "logistic_partner_gps_vendors",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextGpsSyncAt",
                schema: "driver",
                table: "logistic_partner_gps_vendors");
        }
    }
}
