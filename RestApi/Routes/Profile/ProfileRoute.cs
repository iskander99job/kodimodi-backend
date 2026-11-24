using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RestApi.Data;
using RestApi.Errors;

namespace RestApi.Routes.Profile;

public static class ProfileRoute
{
    public static void MapProfile(this WebApplication app)
    {
        app.MapGet("/profile", GetProfileAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetProfileAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var subject = principal.FindFirst("sub")?.Value;

        if (!long.TryParse(subject, out var userId) || userId <= 0)
        {
            return Results.Unauthorized();
        }

        var profile = await db.Users
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                id = user.Id,
                nickname = user.Nickname,
                coins = user.Coins,
                xp = user.Xp,
                rankCode = db.Ranks
                    .Where(rank => rank.MinXp <= user.Xp)
                    .OrderByDescending(rank => rank.MinXp)
                    .Select(rank => rank.Code)
                    .FirstOrDefault(),
                email = user.Email
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return Results.NotFound(new { error = ErrorCodes.UserNotFound });
        }

        return Results.Ok(profile);
    }
}