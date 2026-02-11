using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PandaBot.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleCacheVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add cache version column to track schema changes
            migrationBuilder.AddColumn<int>(
                name: "CacheVersion",
                table: "UexVehicleCache",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
            
            // Clear existing cache entries as they have empty Type fields
            // The cache initializer will rebuild with proper vehicle types
            migrationBuilder.Sql("DELETE FROM UexVehicleCache;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CacheVersion",
                table: "UexVehicleCache");
        }
    }
}
