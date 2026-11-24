using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GameServer.Services;

public sealed class MatchLoadout(HttpClient client)
{
    private static readonly string[] Slots = ["pistol", "ar", "smg", "shotgun"];

    public async Task<Result> ResolveAsync(string? token, CancellationToken cancellationToken)
    {
        if (client.BaseAddress is null)
            return new(null, [], "arsenal_unavailable");

        try
        {
            Profile? profile = null;
            var equipped = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(token))
            {
                using var profileResponse = await GetAsync("profile", token, cancellationToken);
                if (profileResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    return new(null, [], "invalid_session");

                profile = await ReadAsync<Profile>(profileResponse, cancellationToken);
                if (profile is null || profile.Id <= 0)
                    return new(null, [], "arsenal_unavailable");

                using var arsenalResponse = await GetAsync("arsenal", token, cancellationToken);
                if (arsenalResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    return new(null, [], "invalid_session");

                var arsenal = await ReadAsync<Arsenal>(arsenalResponse, cancellationToken);
                if (arsenal?.Weapons is null)
                    return new(null, [], "arsenal_unavailable");

                foreach (var weapon in arsenal.Weapons)
                {
                    if (weapon.Equipped && !string.IsNullOrWhiteSpace(weapon.CategoryCode) &&
                        !string.IsNullOrWhiteSpace(weapon.Code))
                        equipped.TryAdd(weapon.CategoryCode, weapon.Code);
                }
            }

            if (!equipped.ContainsKey("pistol"))
            {
                using var response = await GetAsync("weapons/starter", null, cancellationToken);
                var starter = await ReadAsync<Weapon>(response, cancellationToken);
                if (string.IsNullOrWhiteSpace(starter?.Code) ||
                    !string.Equals(starter.CategoryCode, "pistol", StringComparison.OrdinalIgnoreCase))
                    return new(null, [], "arsenal_unavailable");

                equipped.Add("pistol", starter.Code);
            }

            return new(profile?.Id, Slots.Where(equipped.ContainsKey).Select(slot => equipped[slot]).ToArray(),
                null, profile?.Nickname, profile?.RankCode);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            return new(null, [], "arsenal_unavailable");
        }
    }

    private async Task<HttpResponseMessage> GetAsync(string path, string? token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request, cancellationToken);
    }

    private static async Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        where T : class
    {
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            : null;
    }

    public sealed record Result(long? AccountId, string[] WeaponCodes, string? Error,
        string? Nickname = null, string? RankCode = null);
    private sealed record Profile(long Id, string? Nickname, string? RankCode);
    private sealed record Arsenal(Weapon[] Weapons);
    private sealed record Weapon(string Code, string CategoryCode, bool Equipped);
}
