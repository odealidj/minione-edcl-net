using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupOrderRouteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PoNo",
                schema: "job",
                table: "pickup_orders",
                newName: "delivery_no");

            migrationBuilder.RenameIndex(
                name: "IX_pickup_orders_PoNo",
                schema: "job",
                table: "pickup_orders",
                newName: "IX_pickup_orders_delivery_no");

            migrationBuilder.AddColumn<string>(
                name: "cycle_code",
                schema: "job",
                table: "pickup_orders",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "estimated_departure_time",
                schema: "job",
                table: "pickup_orders",
                type: "time(0)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<DateTime>(
                name: "pickup_date",
                schema: "job",
                table: "pickup_orders",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "route_code",
                schema: "job",
                table: "pickup_orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cycle_code",
                schema: "job",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "estimated_departure_time",
                schema: "job",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "pickup_date",
                schema: "job",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "route_code",
                schema: "job",
                table: "pickup_orders");

            migrationBuilder.RenameColumn(
                name: "delivery_no",
                schema: "job",
                table: "pickup_orders",
                newName: "PoNo");

            migrationBuilder.RenameIndex(
                name: "IX_pickup_orders_delivery_no",
                schema: "job",
                table: "pickup_orders",
                newName: "IX_pickup_orders_PoNo");
        }
    }
}
