using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApi.Models;

namespace RestApi.Data;

internal sealed class WeaponConfiguration : IEntityTypeConfiguration<Weapon>
{
    public void Configure(EntityTypeBuilder<Weapon> weapon)
    {
        weapon.ToTable("weapons", table =>
        {
            table.HasCheckConstraint("ck_weapons_price_nonnegative", "price >= 0");
        });

        weapon.HasKey(w => w.Id);

        weapon.Property(w => w.Id)
            .HasColumnName("id")
            .HasIdentityOptions(startValue: 100);

        weapon.Property(w => w.Name)
            .HasColumnName("name")
            .HasMaxLength(64);

        weapon.Property(w => w.Code)
            .HasColumnName("code")
            .HasMaxLength(32);

        weapon.Property(w => w.CategoryId).HasColumnName("category_id");
        weapon.Property(w => w.MinRankId).HasColumnName("min_rank_id");
        weapon.Property(w => w.Icon).HasColumnName("icon").HasColumnType("varchar");

        weapon.Property(w => w.Preview)
            .HasColumnName("preview")
            .HasMaxLength(512);
        
        weapon.Property(w => w.IsStarter).HasColumnName("is_starter").HasDefaultValue(false);
        weapon.HasIndex(w => w.IsStarter)
            .IsUnique()
            .HasFilter("is_starter")
            .HasDatabaseName("ux_weapons_one_starter");

        weapon.Property(w => w.Price).HasColumnName("price");
        weapon.Property(w => w.Sort).HasColumnName("sort");

        weapon.HasIndex(w => w.Code).IsUnique();
        weapon.HasIndex(w => new { w.CategoryId, w.Sort, w.Code });

        weapon.HasOne(w => w.Category)
            .WithMany()
            .HasForeignKey(w => w.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        weapon.HasOne(w => w.MinRank)
            .WithMany()
            .HasForeignKey(w => w.MinRankId)
            .OnDelete(DeleteBehavior.Restrict);

        weapon.HasData(
            new
            {
                Id = 1L,
                Name = "VEX-9",
                Code = "VEX-9",
                IsStarter = true,
                CategoryId = 1L,
                MinRankId = 1L,
                Price = 500,
                Sort = 10
            },
            new
            {
                Id = 2L,
                Name = "RAVEX-50",
                Code = "RAVEX-50",
                CategoryId = 1L,
                MinRankId = 2L,
                Price = 1000,
                Sort = 20
            },
            new
            {
                Id = 3L,
                Name = "NEON-X",
                Code = "NEON-X",
                CategoryId = 1L,
                MinRankId = 3L,
                Price = 2000,
                Sort = 20
            }
        );
    }
}