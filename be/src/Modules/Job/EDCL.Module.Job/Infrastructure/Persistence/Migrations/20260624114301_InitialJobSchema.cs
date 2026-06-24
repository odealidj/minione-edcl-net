using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialJobSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "job");

            migrationBuilder.CreateTable(
                name: "pickup_orders",
                schema: "job",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<long>(type: "bigint", nullable: false),
                    TruckId = table.Column<long>(type: "bigint", nullable: true),
                    PoNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TraceId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pickup_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pickup_order_details",
                schema: "job",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickupOrderId = table.Column<long>(type: "bigint", nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ArrivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PickedUpAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TraceId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pickup_order_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pickup_order_details_pickup_orders_PickupOrderId",
                        column: x => x.PickupOrderId,
                        principalSchema: "job",
                        principalTable: "pickup_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pickup_order_manifests",
                schema: "job",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickupOrderDetailId = table.Column<long>(type: "bigint", nullable: false),
                    ManifestNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalKanban = table.Column<int>(type: "int", nullable: false),
                    ScannedKanban = table.Column<int>(type: "int", nullable: false),
                    PickupOrderId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TraceId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pickup_order_manifests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pickup_order_manifests_pickup_order_details_PickupOrderDetailId",
                        column: x => x.PickupOrderDetailId,
                        principalSchema: "job",
                        principalTable: "pickup_order_details",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pickup_order_manifests_pickup_orders_PickupOrderId",
                        column: x => x.PickupOrderId,
                        principalSchema: "job",
                        principalTable: "pickup_orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "pickup_order_kanbans",
                schema: "job",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickupOrderManifestId = table.Column<long>(type: "bigint", nullable: false),
                    KanbanCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TraceId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pickup_order_kanbans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pickup_order_kanbans_pickup_order_manifests_PickupOrderManifestId",
                        column: x => x.PickupOrderManifestId,
                        principalSchema: "job",
                        principalTable: "pickup_order_manifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pickup_order_details_PickupOrderId",
                schema: "job",
                table: "pickup_order_details",
                column: "PickupOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_order_kanbans_PickupOrderManifestId",
                schema: "job",
                table: "pickup_order_kanbans",
                column: "PickupOrderManifestId");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_order_manifests_ManifestNo",
                schema: "job",
                table: "pickup_order_manifests",
                column: "ManifestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pickup_order_manifests_PickupOrderDetailId",
                schema: "job",
                table: "pickup_order_manifests",
                column: "PickupOrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_order_manifests_PickupOrderId",
                schema: "job",
                table: "pickup_order_manifests",
                column: "PickupOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_orders_PoNo",
                schema: "job",
                table: "pickup_orders",
                column: "PoNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pickup_order_kanbans",
                schema: "job");

            migrationBuilder.DropTable(
                name: "pickup_order_manifests",
                schema: "job");

            migrationBuilder.DropTable(
                name: "pickup_order_details",
                schema: "job");

            migrationBuilder.DropTable(
                name: "pickup_orders",
                schema: "job");
        }
    }
}
