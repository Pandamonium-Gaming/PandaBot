using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PandaBot.Core.Models;
using PandaBot.Models.AshesOfCreation;
using PandaBot.Models.StarCitizen;

namespace PandaBot.Core.Data;

public class PandaBotContext : DbContext
{
    public PandaBotContext(DbContextOptions<PandaBotContext> options) : base(options)
    {
    }

    public DbSet<GuildSettings> GuildSettings { get; set; }
    public DbSet<CachedItem> CachedItems { get; set; }
    public DbSet<CachedMob> CachedMobs { get; set; }
    public DbSet<CachedVendor> CachedVendors { get; set; }
    public DbSet<CachedCraftingRecipe> CachedCraftingRecipes { get; set; }
    public DbSet<CachedRecipeIngredient> CachedRecipeIngredients { get; set; }
    public DbSet<MobItemDrop> MobItemDrops { get; set; }
    public DbSet<MobRecipeDrop> MobRecipeDrops { get; set; }
    public DbSet<ItemCache> UexItemCache { get; set; }
    public DbSet<VehicleCache> UexVehicleCache { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GuildSettings>(entity =>
        {
            entity.HasKey(e => e.GuildId);
            entity.Property(e => e.NewsChannelId).IsRequired(false);
            entity.Property(e => e.LastNewsCheck).IsRequired(false);
        });

        modelBuilder.Entity<CachedItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ItemId).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Rarity);
            entity.Property(e => e.CachedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<CachedMob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MobId).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.CachedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<CachedVendor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VendorId).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.CachedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<CachedCraftingRecipe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RecipeId).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Profession);
            entity.Property(e => e.CachedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.OutputItem)
                .WithMany()
                .HasForeignKey(e => e.OutputItemCachedId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CachedRecipeIngredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.CachedCraftingRecipe)
                .WithMany(r => r.Ingredients)
                .HasForeignKey(e => e.CachedCraftingRecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ItemId);
        });

        modelBuilder.Entity<MobItemDrop>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.CachedMob)
                .WithMany(m => m.ItemDrops)
                .HasForeignKey(e => e.CachedMobId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CachedItem)
                .WithMany(i => i.MobDrops)
                .HasForeignKey(e => e.CachedItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.CachedMobId, e.CachedItemId }).IsUnique();
        });

        modelBuilder.Entity<MobRecipeDrop>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.CachedMob)
                .WithMany(m => m.RecipeDrops)
                .HasForeignKey(e => e.CachedMobId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CachedCraftingRecipe)
                .WithMany(r => r.MobDrops)
                .HasForeignKey(e => e.CachedCraftingRecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.CachedMobId, e.CachedCraftingRecipeId }).IsUnique();
        });

        modelBuilder.Entity<ItemCache>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UexItemId).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Category);
            entity.Property(e => e.CachedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<VehicleCache>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UexVehicleId).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
            entity.Property(e => e.CachedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
