using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveTrackingFleets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "live_tracking_fleets",
                schema: "job",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickupOrderId = table.Column<long>(type: "bigint", nullable: true),
                    TruckId = table.Column<long>(type: "bigint", nullable: false),
                    DriverId = table.Column<long>(type: "bigint", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Speed = table.Column<double>(type: "float", nullable: false),
                    Heading = table.Column<double>(type: "float", nullable: true),
                    Odometer = table.Column<double>(type: "float", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EngineStatus = table.Column<bool>(type: "bit", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RawData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TraceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_live_tracking_fleets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_live_tracking_fleets_RecordedAt",
                schema: "job",
                table: "live_tracking_fleets",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_live_tracking_fleets_TruckId",
                schema: "job",
                table: "live_tracking_fleets",
                column: "TruckId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "live_tracking_fleets",
                schema: "job");
        }
    }
}
