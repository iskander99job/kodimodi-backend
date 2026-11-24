using Microsoft.EntityFrameworkCore;
using RestApi.Data;
using RestApi.Models;

namespace RestApi.Services;

public sealed class StarterWeaponCatalog
{
    public Task<Weapon?> FindAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        return db.Weapons.AsNoTracking()
            .Include(weapon => weapon.Category)
            .SingleOrDefaultAsync(weapon => weapon.IsStarter, cancellationToken);
    }

    public async Task<bool> EnsureForExistingUserAsync(
        AppDbContext db, long userId, CancellationToken cancellationToken)
    {
        if (await db.UserWeapons.AnyAsync(item => item.UserId == userId, cancellationToken))
            return true;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var users = await db.Users
            .FromSql($"SELECT * FROM users WHERE \"Id\" = {userId} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
            return false;

        if (await db.UserWeapons.AnyAsync(item => item.UserId == userId, cancellationToken))
            return true;

        var weapon = await FindAsync(db, cancellationToken);
        if (weapon is null)
            return false;

        db.UserWeapons.Add(new UserWeapon { UserId = userId, WeaponId = weapon.Id, Equipped = true });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
