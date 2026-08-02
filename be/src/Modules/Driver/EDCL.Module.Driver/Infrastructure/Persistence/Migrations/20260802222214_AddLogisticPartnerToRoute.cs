using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLogisticPartnerToRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LogisticPartnerId",
                schema: "driver",
                table: "routes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_routes_LogisticPartnerId",
                schema: "driver",
                table: "routes",
                column: "LogisticPartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_routes_logistic_partners_LogisticPartnerId",
                schema: "driver",
                table: "routes",
                column: "LogisticPartnerId",
                principalSchema: "driver",
                principalTable: "logistic_partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_routes_logistic_partners_LogisticPartnerId",
                schema: "driver",
                table: "routes");

            migrationBuilder.DropIndex(
                name: "IX_routes_LogisticPartnerId",
                schema: "driver",
                table: "routes");

            migrationBuilder.DropColumn(
                name: "LogisticPartnerId",
                schema: "driver",
                table: "routes");
        }
    }
}
