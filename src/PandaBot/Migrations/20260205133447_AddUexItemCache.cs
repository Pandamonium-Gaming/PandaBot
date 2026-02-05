using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PandaBot.Migrations
{
    /// <inheritdoc />
    public partial class AddUexItemCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UexItemCache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UexItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Company = table.Column<string>(type: "TEXT", nullable: true),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UexItemCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UexItemCache_Category",
                table: "UexItemCache",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_UexItemCache_Name",
                table: "UexItemCache",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_UexItemCache_UexItemId",
                table: "UexItemCache",
                column: "UexItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UexItemCache");
        }
    }
}
