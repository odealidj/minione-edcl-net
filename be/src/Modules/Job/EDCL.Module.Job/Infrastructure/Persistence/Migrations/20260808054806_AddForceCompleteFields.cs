using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddForceCompleteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CompletedLatitude",
                schema: "job",
                table: "pickup_orders",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CompletedLongitude",
                schema: "job",
                table: "pickup_orders",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionReason",
                schema: "job",
                table: "pickup_orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManualCompletion",
                schema: "job",
                table: "pickup_orders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedLatitude",
                schema: "job",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "CompletedLongitude",
                schema: "job",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "CompletionReason",
                schema: "job",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "IsManualCompletion",
                schema: "job",
                table: "pickup_orders");
        }
    }
}
