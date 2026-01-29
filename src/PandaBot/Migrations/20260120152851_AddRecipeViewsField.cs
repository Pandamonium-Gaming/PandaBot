using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PandaBot.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeViewsField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Views",
                table: "CachedCraftingRecipes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Views",
                table: "CachedCraftingRecipes");
        }
    }
}
