using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApi.Models;

namespace RestApi.Data;

internal sealed class WeaponStatConfiguration : IEntityTypeConfiguration<WeaponStat>
{
    public void Configure(EntityTypeBuilder<WeaponStat> stat)
    {
        stat.ToTable("weapon_stats");

        stat.HasKey(s => s.Id);

        stat.Property(s => s.Id)
            .HasColumnName("id")
            .HasIdentityOptions(startValue: 100);

        stat.Property(s => s.WeaponId)
            .HasColumnName("weapon_id");

        stat.Property(s => s.StatPropId).HasColumnName("stat_prop_id");

        stat.Property(s => s.Value)
            .HasColumnName("value");

        stat.HasIndex(s => new { s.WeaponId, s.StatPropId }).IsUnique();

        stat.HasOne<Weapon>()
            .WithMany(w => w.Stats)
            .HasForeignKey(s => s.WeaponId)
            .OnDelete(DeleteBehavior.Cascade);

        stat.HasOne(s => s.StatProp)
            .WithMany()
            .HasForeignKey(s => s.StatPropId)
            .OnDelete(DeleteBehavior.Restrict);

        stat.HasData(
            new WeaponStat { Id = 1, WeaponId = 1, StatPropId = 1, Value = 32 },
            new WeaponStat { Id = 2, WeaponId = 1, StatPropId = 2, Value = 82 },
            new WeaponStat { Id = 4, WeaponId = 1, StatPropId = 3, Value = 70 },
            new WeaponStat { Id = 5, WeaponId = 1, StatPropId = 4, Value = 30 },
            new WeaponStat { Id = 6, WeaponId = 2, StatPropId = 1, Value = 46 },
            new WeaponStat { Id = 7, WeaponId = 2, StatPropId = 2, Value = 57 },
            new WeaponStat { Id = 8, WeaponId = 2, StatPropId = 3, Value = 64 },
            new WeaponStat { Id = 9, WeaponId = 2, StatPropId = 4, Value = 12 },
            new WeaponStat { Id = 10, WeaponId = 3, StatPropId = 1, Value = 28 },
            new WeaponStat { Id = 11, WeaponId = 3, StatPropId = 2, Value = 91 },
            new WeaponStat { Id = 12, WeaponId = 3, StatPropId = 3, Value = 76 },
            new WeaponStat { Id = 13, WeaponId = 3, StatPropId = 4, Value = 24 }
        );
    }
}