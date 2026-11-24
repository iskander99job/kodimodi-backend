using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApi.Models;

namespace RestApi.Data;

internal sealed class UserWeaponConfiguration : IEntityTypeConfiguration<UserWeapon>
{
    internal const string OwnershipIndexName = "ux_user_weapons_user_id_weapon_id";

    public void Configure(EntityTypeBuilder<UserWeapon> ownership)
    {
        ownership.ToTable("user_weapons");

        ownership.HasKey(item => item.Id);

        ownership.Property(item => item.Id).HasColumnName("id");
        ownership.Property(item => item.UserId).HasColumnName("user_id");
        ownership.Property(item => item.WeaponId).HasColumnName("weapon_id");

        ownership.Property(item => item.Equipped)
            .HasColumnName("equipped")
            .HasDefaultValue(false);

        ownership.HasIndex(item => new { item.UserId, item.WeaponId })
            .IsUnique()
            .HasDatabaseName(OwnershipIndexName);

        ownership.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        ownership.HasOne<Weapon>()
            .WithMany()
            .HasForeignKey(item => item.WeaponId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}