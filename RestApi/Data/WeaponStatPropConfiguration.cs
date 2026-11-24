using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApi.Models;

namespace RestApi.Data;

internal sealed class WeaponStatPropConfiguration : IEntityTypeConfiguration<WeaponStatProp>
{
    public void Configure(EntityTypeBuilder<WeaponStatProp> prop)
    {
        prop.ToTable("weapon_stats_prop");
        prop.HasKey(p => p.Id);
        prop.Property(p => p.Id).HasColumnName("id").HasIdentityOptions(startValue: 100);
        prop.Property(p => p.Code).HasColumnName("code").HasMaxLength(32);
        prop.Property(p => p.EnName).HasColumnName("en_name").HasMaxLength(64);
        prop.Property(p => p.RuName).HasColumnName("ru_name").HasMaxLength(64);
        prop.HasIndex(p => p.Code).IsUnique();

        prop.HasData(
            new WeaponStatProp { Id = 1, Code = "damage", EnName = "Damage", RuName = "Урон" },
            new WeaponStatProp { Id = 2, Code = "fire_rate", EnName = "Fire Rate", RuName = "Скорострельность" },
            new WeaponStatProp { Id = 3, Code = "recoil_control", EnName = "Recoil Control", RuName = "Контроль отдачи" },
            new WeaponStatProp { Id = 4, Code = "magazine_size", EnName = "Magazine Size", RuName = "Размер магазина" }
        );
    }
}
