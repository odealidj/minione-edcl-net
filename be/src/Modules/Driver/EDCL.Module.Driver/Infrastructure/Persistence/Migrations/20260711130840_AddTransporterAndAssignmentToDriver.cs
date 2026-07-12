using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLogisticPartnerAndAssignmentToDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LogisticPartnerId",
                schema: "driver",
                table: "trucks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "logisticPartners",
                schema: "driver",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("PK_logisticPartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "truck_driver_assignments",
                schema: "driver",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TruckId = table.Column<long>(type: "bigint", nullable: false),
                    DriverId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UnassignedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
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
                    table.PrimaryKey("PK_truck_driver_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_truck_driver_assignments_trucks_TruckId",
                        column: x => x.TruckId,
                        principalSchema: "driver",
                        principalTable: "trucks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trucks_LogisticPartnerId",
                schema: "driver",
                table: "trucks",
                column: "LogisticPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_assignments_driver_active",
                schema: "driver",
                table: "truck_driver_assignments",
                columns: new[] { "DriverId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_assignments_truck_active",
                schema: "driver",
                table: "truck_driver_assignments",
                columns: new[] { "TruckId", "IsActive" });

            migrationBuilder.Sql("DELETE FROM [driver].[trucks]");

            migrationBuilder.AddForeignKey(
                name: "FK_trucks_logisticPartners_LogisticPartnerId",
                schema: "driver",
                table: "trucks",
                column: "LogisticPartnerId",
                principalSchema: "driver",
                principalTable: "logisticPartners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_trucks_logisticPartners_LogisticPartnerId",
                schema: "driver",
                table: "trucks");

            migrationBuilder.DropTable(
                name: "logisticPartners",
                schema: "driver");

            migrationBuilder.DropTable(
                name: "truck_driver_assignments",
                schema: "driver");

            migrationBuilder.DropIndex(
                name: "IX_trucks_LogisticPartnerId",
                schema: "driver",
                table: "trucks");

            migrationBuilder.DropColumn(
                name: "LogisticPartnerId",
                schema: "driver",
                table: "trucks");
        }
    }
}
