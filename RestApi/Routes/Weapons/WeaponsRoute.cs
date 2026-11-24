using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RestApi.Data;
using RestApi.Errors;
using RestApi.Models;
using RestApi.Services;

namespace RestApi.Routes.Weapons;

public static class WeaponsRoute
{
    public static void MapWeapons(this WebApplication app)
    {
        var weapons = app.MapGroup("/weapons");

        weapons.MapGet("", GetWeaponsAsync);
        weapons.MapGet("/starter", GetStarterWeaponAsync);
        weapons.MapGet("/{code}", GetWeaponDetailsAsync);

        weapons.MapPost("/{code}/buy", BuyWeaponAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetStarterWeaponAsync(
        StarterWeaponCatalog starterWeapons,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var weapon = await starterWeapons.FindAsync(db, cancellationToken);
        if (weapon is null)
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        return Results.Ok(new { code = weapon.Code, name = weapon.Name, categoryCode = weapon.Category.Code.ToUpperInvariant() });
    }

    private static async Task<IResult> GetWeaponsAsync(
        string? category,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.Weapons.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryCode = category.Trim().ToLowerInvariant();
            query = query.Where(weapon => weapon.Category.Code == categoryCode);
        }

        var weapons = await query
            .OrderBy(weapon => weapon.Sort)
            .ThenBy(weapon => weapon.Code)
            .Select(weapon => new
            {
                code = weapon.Code,
                name = weapon.Name,
                categoryCode = weapon.Category.Code.ToUpper(),
                price = weapon.Price,
                minRank = new
                {
                    code = weapon.MinRank.Code,
                    englishName = weapon.MinRank.EnName,
                    minXp = weapon.MinRank.MinXp
                }
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(new { weapons });
    }

    private static async Task<IResult> GetWeaponDetailsAsync(
        string code,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var weapon = await db.Weapons
            .AsNoTracking()
            .Where(weapon => weapon.Code == normalizedCode)
            .Select(weapon => new
            {
                code = weapon.Code,
                name = weapon.Name,
                categoryCode = weapon.Category.Code.ToUpper(),
                price = weapon.Price,
                minRank = new
                {
                    code = weapon.MinRank.Code,
                    englishName = weapon.MinRank.EnName,
                    minXp = weapon.MinRank.MinXp
                },
                stats = weapon.Stats
                    .OrderBy(stat => stat.StatProp.Code)
                    .Select(stat => new
                    {
                        statCode = stat.StatProp.Code.ToUpper(),
                        value = stat.Value
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (weapon is null)
        {
            return Results.NotFound(new { error = ErrorCodes.WeaponNotFound });
        }

        return Results.Ok(weapon);
    }

    private static async Task<IResult> BuyWeaponAsync(
        string code,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var subject = principal.FindFirst("sub")?.Value;

        if (!long.TryParse(subject, out var userId) || userId <= 0)
        {
            return Results.Unauthorized();
        }

        var normalizedCode = code.Trim().ToUpperInvariant();

        var weapon = await db.Weapons
            .AsNoTracking()
            .Where(item => item.Code == normalizedCode)
            .Select(item => new
            {
                item.Id,
                item.Code,
                item.Price,
                MinXp = item.MinRank.MinXp
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (weapon is null)
        {
            return Results.NotFound(new { error = ErrorCodes.WeaponNotFound });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var users = await db.Users
            .FromSql($"SELECT * FROM users WHERE \"Id\" = {userId} FOR UPDATE")
            .AsTracking()
            .ToListAsync(cancellationToken);

        var user = users.SingleOrDefault();

        if (user is null)
        {
            return Results.NotFound(new { error = ErrorCodes.UserNotFound });
        }

        var alreadyOwned = await db.UserWeapons.AnyAsync(
            item => item.UserId == userId && item.WeaponId == weapon.Id,
            cancellationToken);

        if (alreadyOwned)
        {
            return Results.Conflict(new { error = ErrorCodes.WeaponAlreadyOwned });
        }

        if (user.Xp < weapon.MinXp)
        {
            return Results.Json(
                new { error = ErrorCodes.RankRequired },
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (user.Coins < weapon.Price)
        {
            return Results.Conflict(new { error = ErrorCodes.InsufficientCoins });
        }

        user.Coins -= weapon.Price;

        db.UserWeapons.Add(new UserWeapon
        {
            UserId = userId,
            WeaponId = weapon.Id,
            Equipped = false
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: UserWeaponConfiguration.OwnershipIndexName
            })
        {
            await transaction.RollbackAsync(cancellationToken);

            return Results.Conflict(new { error = ErrorCodes.WeaponAlreadyOwned });
        }

        await transaction.CommitAsync(cancellationToken);

        return Results.Ok(new
        {
            code = weapon.Code,
            coins = user.Coins
        });
    }
}