using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManifestDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "ingestion",
                table: "manifests",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Cycle",
                schema: "ingestion",
                table: "manifests",
                newName: "cycle");

            migrationBuilder.RenameColumn(
                name: "OrderType",
                schema: "ingestion",
                table: "manifests",
                newName: "order_type");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "ingestion",
                table: "manifest_parts",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "KanbanNo",
                schema: "ingestion",
                table: "manifest_parts",
                newName: "kanban_no");

            migrationBuilder.AddColumn<string>(
                name: "dock_cd",
                schema: "ingestion",
                table: "manifests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "order_no",
                schema: "ingestion",
                table: "manifests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "p_lane_no",
                schema: "ingestion",
                table: "manifests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "box_type",
                schema: "ingestion",
                table: "manifest_parts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "uniq_no",
                schema: "ingestion",
                table: "manifest_parts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dock_cd",
                schema: "ingestion",
                table: "manifests");

            migrationBuilder.DropColumn(
                name: "order_no",
                schema: "ingestion",
                table: "manifests");

            migrationBuilder.DropColumn(
                name: "p_lane_no",
                schema: "ingestion",
                table: "manifests");

            migrationBuilder.DropColumn(
                name: "box_type",
                schema: "ingestion",
                table: "manifest_parts");

            migrationBuilder.DropColumn(
                name: "uniq_no",
                schema: "ingestion",
                table: "manifest_parts");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "ingestion",
                table: "manifests",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "cycle",
                schema: "ingestion",
                table: "manifests",
                newName: "Cycle");

            migrationBuilder.RenameColumn(
                name: "order_type",
                schema: "ingestion",
                table: "manifests",
                newName: "OrderType");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "ingestion",
                table: "manifest_parts",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "kanban_no",
                schema: "ingestion",
                table: "manifest_parts",
                newName: "KanbanNo");
        }
    }
}
