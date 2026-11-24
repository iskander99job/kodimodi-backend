using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RestApi.Data;
using RestApi.Errors;

namespace RestApi.Routes.Arsenal;

public static class ArsenalRoute
{
    public static void MapArsenal(this WebApplication app)
    {
        var arsenal = app.MapGroup("/arsenal").RequireAuthorization();

        arsenal.MapGet("", GetArsenalAsync);
        arsenal.MapPost("/{code}/equip", EquipWeaponAsync);
    }

    private static async Task<IResult> GetArsenalAsync(
        string? category,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Results.Unauthorized();
        }

        var userExists = await db.Users.AnyAsync(
            user => user.Id == userId,
            cancellationToken);

        if (!userExists)
        {
            return Results.NotFound(new { error = ErrorCodes.UserNotFound });
        }

        var query =
            from ownership in db.UserWeapons.AsNoTracking()
            join weapon in db.Weapons.AsNoTracking()
                on ownership.WeaponId equals weapon.Id
            where ownership.UserId == userId
            select new
            {
                weapon.Code,
                weapon.Name,
                CategoryCode = weapon.Category.Code,
                weapon.Sort,
                ownership.Equipped
            };

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryCode = category.Trim().ToLowerInvariant();
            query = query.Where(item => item.CategoryCode == categoryCode);
        }

        var weapons = await query
            .OrderBy(item => item.Sort)
            .ThenBy(item => item.Code)
            .Select(item => new
            {
                code = item.Code,
                name = item.Name,
                categoryCode = item.CategoryCode.ToUpper(),
                equipped = item.Equipped
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(new { weapons });
    }

    private static async Task<IResult> EquipWeaponAsync(
        string code,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
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
                item.CategoryId,
                CategoryCode = item.Category.Code
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (weapon is null)
        {
            return Results.NotFound(new { error = ErrorCodes.WeaponNotFound });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var lockedUsers = await db.Users
            .FromSql($"SELECT * FROM users WHERE \"Id\" = {userId} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (lockedUsers.Count == 0)
        {
            return Results.NotFound(new { error = ErrorCodes.UserNotFound });
        }

        var categoryWeapons = await db.UserWeapons
            .AsTracking()
            .Where(ownership =>
                ownership.UserId == userId &&
                db.Weapons.Any(item =>
                    item.Id == ownership.WeaponId &&
                    item.CategoryId == weapon.CategoryId))
            .ToListAsync(cancellationToken);

        if (!categoryWeapons.Any(ownership => ownership.WeaponId == weapon.Id))
        {
            return Results.Json(
                new { error = ErrorCodes.WeaponNotOwned },
                statusCode: StatusCodes.Status403Forbidden);
        }

        foreach (var ownership in categoryWeapons)
        {
            ownership.Equipped = ownership.WeaponId == weapon.Id;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Results.Ok(new
        {
            code = weapon.Code,
            categoryCode = weapon.CategoryCode.ToUpperInvariant(),
            equipped = true
        });
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out long userId)
    {
        var subject = principal.FindFirst("sub")?.Value;

        return long.TryParse(subject, out userId) && userId > 0;
    }
}