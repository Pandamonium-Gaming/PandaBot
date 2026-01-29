using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PandaBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CachedNewsArticles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
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
                    PageId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedWikiPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildSettings",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NewsChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LastNewsCheck = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildSettings", x => x.GuildId);
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedNewsArticles");

            migrationBuilder.DropTable(
                name: "CachedWikiPages");

            migrationBuilder.DropTable(
                name: "GuildSettings");
        }
    }
}
