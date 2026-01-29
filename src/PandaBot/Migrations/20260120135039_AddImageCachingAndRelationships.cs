using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PandaBot.Migrations
{
    /// <inheritdoc />
    public partial class AddImageCachingAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedNewsArticles");

            migrationBuilder.DropTable(
                name: "CachedWikiPages");

            migrationBuilder.CreateTable(
                name: "CachedItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Rarity = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: true),
                    IconUrl = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: false),
                    LocalIconPath = table.Column<string>(type: "TEXT", nullable: false),
                    LocalImagePath = table.Column<string>(type: "TEXT", nullable: false),
                    IsStackable = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxStackSize = table.Column<int>(type: "INTEGER", nullable: true),
                    SlotType = table.Column<string>(type: "TEXT", nullable: false),
                    RawJson = table.Column<string>(type: "TEXT", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CachedMobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MobId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: false),
                    LocalImagePath = table.Column<string>(type: "TEXT", nullable: false),
                    IsBoss = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsElite = table.Column<bool>(type: "INTEGER", nullable: false),
                    RawJson = table.Column<string>(type: "TEXT", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedMobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CachedVendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VendorId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    Region = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: false),
                    LocalImagePath = table.Column<string>(type: "TEXT", nullable: false),
                    RawJson = table.Column<string>(type: "TEXT", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedVendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CachedCraftingRecipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipeId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Profession = table.Column<string>(type: "TEXT", nullable: false),
                    ProfessionLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputItemCachedId = table.Column<int>(type: "INTEGER", nullable: true),
                    OutputItemId = table.Column<string>(type: "TEXT", nullable: false),
                    OutputItemName = table.Column<string>(type: "TEXT", nullable: false),
                    OutputQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Station = table.Column<string>(type: "TEXT", nullable: false),
                    CraftTime = table.Column<int>(type: "INTEGER", nullable: false),
                    RawJson = table.Column<string>(type: "TEXT", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedCraftingRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CachedCraftingRecipes_CachedItems_OutputItemCachedId",
                        column: x => x.OutputItemCachedId,
                        principalTable: "CachedItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MobItemDrops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CachedMobId = table.Column<int>(type: "INTEGER", nullable: false),
                    CachedItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    DropRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    MinQuantity = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxQuantity = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobItemDrops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MobItemDrops_CachedItems_CachedItemId",
                        column: x => x.CachedItemId,
                        principalTable: "CachedItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MobItemDrops_CachedMobs_CachedMobId",
                        column: x => x.CachedMobId,
                        principalTable: "CachedMobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CachedRecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CachedCraftingRecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedRecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CachedRecipeIngredients_CachedCraftingRecipes_CachedCraftingRecipeId",
                        column: x => x.CachedCraftingRecipeId,
                        principalTable: "CachedCraftingRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MobRecipeDrops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CachedMobId = table.Column<int>(type: "INTEGER", nullable: false),
                    CachedCraftingRecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DropRate = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobRecipeDrops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MobRecipeDrops_CachedCraftingRecipes_CachedCraftingRecipeId",
                        column: x => x.CachedCraftingRecipeId,
                        principalTable: "CachedCraftingRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MobRecipeDrops_CachedMobs_CachedMobId",
                        column: x => x.CachedMobId,
                        principalTable: "CachedMobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedCraftingRecipes_Name",
                table: "CachedCraftingRecipes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CachedCraftingRecipes_OutputItemCachedId",
                table: "CachedCraftingRecipes",
                column: "OutputItemCachedId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedCraftingRecipes_Profession",
                table: "CachedCraftingRecipes",
                column: "Profession");

            migrationBuilder.CreateIndex(
                name: "IX_CachedCraftingRecipes_RecipeId",
                table: "CachedCraftingRecipes",
                column: "RecipeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedItems_ItemId",
                table: "CachedItems",
                column: "ItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedItems_Name",
                table: "CachedItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CachedMobs_MobId",
                table: "CachedMobs",
                column: "MobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedMobs_Name",
                table: "CachedMobs",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CachedRecipeIngredients_CachedCraftingRecipeId",
                table: "CachedRecipeIngredients",
                column: "CachedCraftingRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedRecipeIngredients_ItemId",
                table: "CachedRecipeIngredients",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedVendors_Name",
                table: "CachedVendors",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CachedVendors_VendorId",
                table: "CachedVendors",
                column: "VendorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MobItemDrops_CachedItemId",
                table: "MobItemDrops",
                column: "CachedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MobItemDrops_CachedMobId_CachedItemId",
                table: "MobItemDrops",
                columns: new[] { "CachedMobId", "CachedItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MobRecipeDrops_CachedCraftingRecipeId",
                table: "MobRecipeDrops",
                column: "CachedCraftingRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_MobRecipeDrops_CachedMobId_CachedCraftingRecipeId",
                table: "MobRecipeDrops",
                columns: new[] { "CachedMobId", "CachedCraftingRecipeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedRecipeIngredients");

            migrationBuilder.DropTable(
                name: "CachedVendors");

            migrationBuilder.DropTable(
                name: "MobItemDrops");

            migrationBuilder.DropTable(
                name: "MobRecipeDrops");

            migrationBuilder.DropTable(
                name: "CachedCraftingRecipes");

            migrationBuilder.DropTable(
                name: "CachedMobs");

            migrationBuilder.DropTable(
                name: "CachedItems");

            migrationBuilder.CreateTable(
                name: "CachedNewsArticles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedNewsArticles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CachedWikiPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    PageId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedWikiPages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedNewsArticles_Url",
                table: "CachedNewsArticles",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedWikiPages_PageId",
                table: "CachedWikiPages",
                column: "PageId",
                unique: true);
        }
    }
}
