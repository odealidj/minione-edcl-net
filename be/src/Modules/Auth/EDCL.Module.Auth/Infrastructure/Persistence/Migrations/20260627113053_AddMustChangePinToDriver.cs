using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMustChangePinToDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "must_change_pin",
                schema: "auth",
                table: "drivers",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "must_change_pin",
                schema: "auth",
                table: "drivers");
        }
    }
}
