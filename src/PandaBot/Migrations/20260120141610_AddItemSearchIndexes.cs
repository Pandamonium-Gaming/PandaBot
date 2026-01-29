using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PandaBot.Migrations
{
    /// <inheritdoc />
    public partial class AddItemSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CachedItems_Rarity",
                table: "CachedItems",
                column: "Rarity");

            migrationBuilder.CreateIndex(
                name: "IX_CachedItems_Type",
                table: "CachedItems",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CachedItems_Rarity",
                table: "CachedItems");

            migrationBuilder.DropIndex(
                name: "IX_CachedItems_Type",
                table: "CachedItems");
        }
    }
}
