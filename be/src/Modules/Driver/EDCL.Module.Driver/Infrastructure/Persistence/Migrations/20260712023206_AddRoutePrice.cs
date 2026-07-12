using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "route_prices",
                schema: "driver",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RouteId = table.Column<long>(type: "bigint", nullable: false),
                    LogisticPartnerId = table.Column<long>(type: "bigint", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "date", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "date", nullable: false),
                    PriceType = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
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
                    table.PrimaryKey("PK_route_prices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_route_prices_logistic_partners_LogisticPartnerId",
                        column: x => x.LogisticPartnerId,
                        principalSchema: "driver",
                        principalTable: "logistic_partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_prices_routes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "driver",
                        principalTable: "routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_route_prices_LogisticPartnerId",
                schema: "driver",
                table: "route_prices",
                column: "LogisticPartnerId");

            migrationBuilder.CreateIndex(
                name: "UQ_route_prices_route_lp_dates_type",
                schema: "driver",
                table: "route_prices",
                columns: new[] { "RouteId", "LogisticPartnerId", "ValidFrom", "ValidTo", "PriceType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "route_prices",
                schema: "driver");
        }
    }
}
