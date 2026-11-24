using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestApi.Data;
using RestApi.Errors;
using RestApi.Models;
using RestApi.Services;

namespace RestApi.Routes.Auth;

public static class LoginRoute
{
    public static void MapLogin(this RouteGroupBuilder auth)
    {
        auth.MapPost("/login", LoginAsync);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        JwtTokenService jwtTokenService,
        StarterWeaponCatalog starterWeapons,
        CancellationToken cancellationToken)
    {
        var nickname = request.Nickname;
        var password = request.Password;

        if (nickname is null || nickname.Length is < 4 or > 16 ||
            password is null || password.Length is < 6 or > 30)
        {
            return InvalidCredentials();
        }

        var normalizedNickname = nickname.ToUpperInvariant();

        var user = await db.Users.SingleOrDefaultAsync(
            user => user.NormalizedNickname == normalizedNickname &&
                    user.LoginType == LoginType.Local,
            cancellationToken);

        if (user is null || user.PasswordHash is null)
        {
            return InvalidCredentials();
        }

        var result = passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
        {
            return InvalidCredentials();
        }

        if (!await starterWeapons.EnsureForExistingUserAsync(db, user.Id, cancellationToken))
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        var accessToken = jwtTokenService.CreateAccessToken(user.Id);

        return Results.Ok(new { accessToken });
    }

    private static IResult InvalidCredentials()
    {
        return Results.Json(
            new { error = ErrorCodes.InvalidCredentials },
            statusCode: StatusCodes.Status401Unauthorized);
    }
}

public sealed record LoginRequest(string? Nickname, string? Password);