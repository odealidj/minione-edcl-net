using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGpsAndTruckLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GpsVehicleId",
                schema: "driver",
                table: "trucks",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSimulated",
                schema: "driver",
                table: "trucks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GpsApiPassword",
                schema: "driver",
                table: "logistic_partners",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GpsApiToken",
                schema: "driver",
                table: "logistic_partners",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GpsApiUrl",
                schema: "driver",
                table: "logistic_partners",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GpsApiUsername",
                schema: "driver",
                table: "logistic_partners",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GpsProviderType",
                schema: "driver",
                table: "logistic_partners",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsGpsIntegrationActive",
                schema: "driver",
                table: "logistic_partners",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "truck_locations",
                schema: "driver",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TruckId = table.Column<long>(type: "bigint", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Speed = table.Column<double>(type: "float", nullable: true),
                    Heading = table.Column<double>(type: "float", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    deleted_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    trace_id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_truck_locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_truck_locations_trucks_TruckId",
                        column: x => x.TruckId,
                        principalSchema: "driver",
                        principalTable: "trucks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_truck_locations_timestamp",
                schema: "driver",
                table: "truck_locations",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_truck_locations_truck_id",
                schema: "driver",
                table: "truck_locations",
                column: "TruckId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "truck_locations",
                schema: "driver");

            migrationBuilder.DropColumn(
                name: "GpsVehicleId",
                schema: "driver",
                table: "trucks");

            migrationBuilder.DropColumn(
                name: "IsSimulated",
                schema: "driver",
                table: "trucks");

            migrationBuilder.DropColumn(
                name: "GpsApiPassword",
                schema: "driver",
                table: "logistic_partners");

            migrationBuilder.DropColumn(
                name: "GpsApiToken",
                schema: "driver",
                table: "logistic_partners");

            migrationBuilder.DropColumn(
                name: "GpsApiUrl",
                schema: "driver",
                table: "logistic_partners");

            migrationBuilder.DropColumn(
                name: "GpsApiUsername",
                schema: "driver",
                table: "logistic_partners");

            migrationBuilder.DropColumn(
                name: "GpsProviderType",
                schema: "driver",
                table: "logistic_partners");

            migrationBuilder.DropColumn(
                name: "IsGpsIntegrationActive",
                schema: "driver",
                table: "logistic_partners");
        }
    }
}
