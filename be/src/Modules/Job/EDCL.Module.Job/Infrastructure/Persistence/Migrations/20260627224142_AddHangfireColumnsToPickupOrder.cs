using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHangfireColumnsToPickupOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HangfireJobIdH1",
                schema: "job",
                table: "pickup_orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HangfireJobIdH30",
                schema: "job",
                table: "pickup_orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HangfireJobIdH1",
                schema: "job",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "HangfireJobIdH30",
                schema: "job",
                table: "pickup_orders");
        }
    }
}
