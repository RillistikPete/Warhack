using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using WarHack.Logging;

#nullable disable

namespace WarHack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLogQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogQueue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProjectName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Logger = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogQueue", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogQueue_DateAdded",
                table: "LogQueue",
                column: "DateAdded");

            migrationBuilder.CreateIndex(
                name: "IX_LogQueue_Level",
                table: "LogQueue",
                column: "Level");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogQueue");
        }
    }
}
