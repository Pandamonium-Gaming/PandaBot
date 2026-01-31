using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PandaBot.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificationLevelToRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificationLevel",
                table: "CachedCraftingRecipes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificationLevel",
                table: "CachedCraftingRecipes");
        }
    }
}
