using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDCL.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IngestionErrorLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ingestion_errors",
                schema: "ingestion",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingestion_errors", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ingestion_errors",
                schema: "ingestion");
        }
    }
}
