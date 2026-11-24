using Microsoft.EntityFrameworkCore;
using RestApi.Models;

namespace RestApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Rank> Ranks => Set<Rank>();
    public DbSet<Weapon> Weapons => Set<Weapon>();
    public DbSet<WeaponCategory> WeaponCategories => Set<WeaponCategory>();
    public DbSet<WeaponStatProp> WeaponStatProps => Set<WeaponStatProp>();
    public DbSet<WeaponStat> WeaponStats => Set<WeaponStat>();
    public DbSet<UserWeapon> UserWeapons => Set<UserWeapon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();

        user.ToTable("users");

        user.Property(u => u.Nickname).HasMaxLength(15);
        user.Property(u => u.NormalizedNickname).HasMaxLength(15);

        user.Property(u => u.Xp)
            .HasDefaultValue(0);

        user.HasIndex(u => u.NormalizedNickname).IsUnique();
        
        modelBuilder.ApplyConfiguration(new RankConfiguration());
        modelBuilder.ApplyConfiguration(new WeaponCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new WeaponStatPropConfiguration());
        modelBuilder.ApplyConfiguration(new WeaponConfiguration());
        modelBuilder.ApplyConfiguration(new WeaponStatConfiguration());
        modelBuilder.ApplyConfiguration(new UserWeaponConfiguration());
    }
}