using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateManifestProblem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryNo",
                schema: "ingestion",
                table: "manifest_problems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PickupOrderId",
                schema: "ingestion",
                table: "manifest_problems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionReason",
                schema: "ingestion",
                table: "manifest_problems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                schema: "ingestion",
                table: "manifest_problems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedBy",
                schema: "ingestion",
                table: "manifest_problems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryNo",
                schema: "ingestion",
                table: "manifest_problems");

            migrationBuilder.DropColumn(
                name: "PickupOrderId",
                schema: "ingestion",
                table: "manifest_problems");

            migrationBuilder.DropColumn(
                name: "ResolutionReason",
                schema: "ingestion",
                table: "manifest_problems");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                schema: "ingestion",
                table: "manifest_problems");

            migrationBuilder.DropColumn(
                name: "ResolvedBy",
                schema: "ingestion",
                table: "manifest_problems");
        }
    }
}
