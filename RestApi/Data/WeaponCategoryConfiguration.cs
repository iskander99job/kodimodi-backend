using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApi.Models;

namespace RestApi.Data;

internal sealed class WeaponCategoryConfiguration : IEntityTypeConfiguration<WeaponCategory>
{
    public void Configure(EntityTypeBuilder<WeaponCategory> category)
    {
        category.ToTable("weapon_categories");
        category.HasKey(c => c.Id);
        category.Property(c => c.Id).HasColumnName("id").HasIdentityOptions(startValue: 100);
        category.Property(c => c.Code).HasColumnName("code").HasMaxLength(32);
        category.Property(c => c.EnName).HasColumnName("en_name").HasMaxLength(64);
        category.Property(c => c.RuName).HasColumnName("ru_name").HasMaxLength(64);
        category.Property(c => c.Icon).HasColumnName("icon").HasColumnType("varchar");
        category.Property(c => c.Sort).HasColumnName("sort");
        category.HasIndex(c => c.Code).IsUnique();

        category.HasData(
            new WeaponCategory { Id = 1, Code = "pistol", EnName = "Pistol", RuName = "Pistol", Sort = 10 },
            new WeaponCategory { Id = 2, Code = "ar", EnName = "AR", RuName = "AR", Sort = 20 },
            new WeaponCategory { Id = 3, Code = "smg", EnName = "SMG", RuName = "SMG", Sort = 30 },
            new WeaponCategory { Id = 4, Code = "shotgun", EnName = "Shotgun", RuName = "Shotgun", Sort = 40 }
        );
    }
}
