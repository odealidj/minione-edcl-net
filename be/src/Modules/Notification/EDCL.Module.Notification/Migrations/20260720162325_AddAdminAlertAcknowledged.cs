using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAlertAcknowledged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AdminAlertAcknowledged",
                schema: "notification",
                table: "driver_notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminAlertAcknowledged",
                schema: "notification",
                table: "driver_notifications");
        }
    }
}
