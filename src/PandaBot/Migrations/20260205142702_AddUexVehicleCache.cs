using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PandaBot.Migrations
{
    /// <inheritdoc />
    public partial class AddUexVehicleCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UexVehicleCache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UexVehicleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UexVehicleCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UexVehicleCache_Name",
                table: "UexVehicleCache",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_UexVehicleCache_Type",
                table: "UexVehicleCache",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_UexVehicleCache_UexVehicleId",
                table: "UexVehicleCache",
                column: "UexVehicleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UexVehicleCache");
        }
    }
}
