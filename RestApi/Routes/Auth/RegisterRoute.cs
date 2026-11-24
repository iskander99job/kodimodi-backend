using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RestApi.Data;
using RestApi.Errors;
using RestApi.Models;
using RestApi.Services;

namespace RestApi.Routes.Auth;

public static class RegisterRoute
{
    public static void MapRegister(this RouteGroupBuilder auth)
    {
        auth.MapPost("/register", RegisterAsync);
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        JwtTokenService jwtTokenService,
        StarterWeaponCatalog starterWeapons,
        CancellationToken cancellationToken)
    {
        var nickname = request.Nickname;
        var password = request.Password;

        if (nickname is null ||
            !Regex.IsMatch(nickname, @"\A[A-Za-z0-9_]{4,16}\z"))
        {
            return Results.BadRequest(new { error = ErrorCodes.InvalidNickname });
        }

        if (password is null || password.Length is < 6 or > 30)
        {
            return Results.BadRequest(new { error = ErrorCodes.InvalidPassword });
        }

        var normalizedNickname = nickname.ToUpperInvariant();

        var nicknameTaken = await db.Users.AnyAsync(
            user => user.NormalizedNickname == normalizedNickname,
            cancellationToken);

        if (nicknameTaken)
        {
            return Results.Conflict(new { error = ErrorCodes.NicknameTaken });
        }

        var starterWeapon = await starterWeapons.FindAsync(db, cancellationToken);
        if (starterWeapon is null)
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        var user = new User
        {
            Nickname = nickname,
            NormalizedNickname = normalizedNickname,
            LoginType = LoginType.Local
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        db.Users.Add(user);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            db.UserWeapons.Add(new UserWeapon
            {
                UserId = user.Id,
                WeaponId = starterWeapon.Id,
                Equipped = true
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_users_NormalizedNickname"
            })
        {
            return Results.Conflict(new { error = ErrorCodes.NicknameTaken });
        }

        var accessToken = jwtTokenService.CreateAccessToken(user.Id);

        return Results.Json(
            new { accessToken },
            statusCode: StatusCodes.Status201Created);
    }
}

public sealed record RegisterRequest(string? Nickname, string? Password);