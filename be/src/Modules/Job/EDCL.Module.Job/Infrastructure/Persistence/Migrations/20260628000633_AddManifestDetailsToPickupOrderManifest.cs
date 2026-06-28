using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManifestDetailsToPickupOrderManifest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DockCode",
                schema: "job",
                table: "pickup_order_manifests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrderType",
                schema: "job",
                table: "pickup_order_manifests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalSkid",
                schema: "job",
                table: "pickup_order_manifests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DockCode",
                schema: "job",
                table: "pickup_order_manifests");

            migrationBuilder.DropColumn(
                name: "OrderType",
                schema: "job",
                table: "pickup_order_manifests");

            migrationBuilder.DropColumn(
                name: "TotalSkid",
                schema: "job",
                table: "pickup_order_manifests");
        }
    }
}
