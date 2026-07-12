using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameTransporterToLogisticPartner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_trucks_logisticPartners_LogisticPartnerId",
                schema: "driver",
                table: "trucks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_logisticPartners",
                schema: "driver",
                table: "logisticPartners");

            migrationBuilder.RenameTable(
                name: "logisticPartners",
                schema: "driver",
                newName: "logistic_partners",
                newSchema: "driver");

            migrationBuilder.AddColumn<long>(
                name: "LogisticPartnerId1",
                schema: "driver",
                table: "trucks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_logistic_partners",
                schema: "driver",
                table: "logistic_partners",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_trucks_LogisticPartnerId1",
                schema: "driver",
                table: "trucks",
                column: "LogisticPartnerId1");

            migrationBuilder.AddForeignKey(
                name: "FK_trucks_logistic_partners_LogisticPartnerId",
                schema: "driver",
                table: "trucks",
                column: "LogisticPartnerId",
                principalSchema: "driver",
                principalTable: "logistic_partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_trucks_logistic_partners_LogisticPartnerId1",
                schema: "driver",
                table: "trucks",
                column: "LogisticPartnerId1",
                principalSchema: "driver",
                principalTable: "logistic_partners",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_trucks_logistic_partners_LogisticPartnerId",
                schema: "driver",
                table: "trucks");

            migrationBuilder.DropForeignKey(
                name: "FK_trucks_logistic_partners_LogisticPartnerId1",
                schema: "driver",
                table: "trucks");

            migrationBuilder.DropIndex(
                name: "IX_trucks_LogisticPartnerId1",
                schema: "driver",
                table: "trucks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_logistic_partners",
                schema: "driver",
                table: "logistic_partners");

            migrationBuilder.DropColumn(
                name: "LogisticPartnerId1",
                schema: "driver",
                table: "trucks");

            migrationBuilder.RenameTable(
                name: "logistic_partners",
                schema: "driver",
                newName: "logisticPartners",
                newSchema: "driver");

            migrationBuilder.AddPrimaryKey(
                name: "PK_logisticPartners",
                schema: "driver",
                table: "logisticPartners",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_trucks_logisticPartners_LogisticPartnerId",
                schema: "driver",
                table: "trucks",
                column: "LogisticPartnerId",
                principalSchema: "driver",
                principalTable: "logisticPartners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
