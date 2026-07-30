using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGpsVendorTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "gps_vendors",
                schema: "driver",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    ApiUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApiUsername = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApiPassword = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApiToken = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_gps_vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "logistic_partner_gps_vendors",
                schema: "driver",
                columns: table => new
                {
                    LogisticPartnerId = table.Column<long>(type: "bigint", nullable: false),
                    GpsVendorId = table.Column<long>(type: "bigint", nullable: false),
                    LastGpsSyncAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    LastGpsSyncStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastGpsSyncMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_logistic_partner_gps_vendors", x => new { x.LogisticPartnerId, x.GpsVendorId });
                    table.ForeignKey(
                        name: "FK_logistic_partner_gps_vendors_gps_vendors_GpsVendorId",
                        column: x => x.GpsVendorId,
                        principalSchema: "driver",
                        principalTable: "gps_vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_logistic_partner_gps_vendors_logistic_partners_LogisticPartnerId",
                        column: x => x.LogisticPartnerId,
                        principalSchema: "driver",
                        principalTable: "logistic_partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_gps_vendors_code",
                schema: "driver",
                table: "gps_vendors",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_logistic_partner_gps_vendors_GpsVendorId",
                schema: "driver",
                table: "logistic_partner_gps_vendors",
                column: "GpsVendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "logistic_partner_gps_vendors",
                schema: "driver");

            migrationBuilder.DropTable(
                name: "gps_vendors",
                schema: "driver");

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
    }
}
