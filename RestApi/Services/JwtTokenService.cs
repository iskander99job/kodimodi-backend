using System.Globalization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace RestApi.Services;

public sealed class JwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenDays;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Missing Jwt__Issuer.");

        _audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Missing Jwt__Audience.");

        _accessTokenDays = configuration.GetValue<int>("Jwt:AccessTokenDays");

        if (_accessTokenDays <= 0)
        {
            throw new InvalidOperationException(
                "Jwt__AccessTokenDays must be a positive integer.");
        }

        var encodedKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Missing Jwt__SigningKey.");

        var key = Convert.FromBase64String(encodedKey);

        if (key.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt__SigningKey must contain at least 32 bytes after Base64 decoding.");
        }

        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);
    }

    public string CreateAccessToken(long userId)
    {
        var now = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _issuer,
            Audience = _audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddDays(_accessTokenDays),
            SigningCredentials = _signingCredentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] =
                    userId.ToString(CultureInfo.InvariantCulture)
            }
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}