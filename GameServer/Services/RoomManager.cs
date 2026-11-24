using System.Collections.Concurrent;
using System.Security.Cryptography;
using GameServer.Models;

namespace GameServer.Services;

public static class RoomManager
{
    public const int MinPlayers = 2;
    public const int MaxPlayersLimit = 6;

    private const int CodeLength = 6;
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private static readonly ConcurrentDictionary<string, RoomModel> Rooms = new();

    private static readonly Timer Sweeper =
        new(_ => Sweep(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

    public static RoomModel? Create(int maxPlayers)
    {
        if (maxPlayers < MinPlayers || maxPlayers > MaxPlayersLimit) return null;

        for (int i = 0; i < 20; i++)
        {
            var room = new RoomModel(GenerateCode(), maxPlayers);
            if (Rooms.TryAdd(room.Code, room)) return room;
        }
        return null;
    }

    public static RoomModel? Get(string? code)
        => string.IsNullOrWhiteSpace(code)
            ? null
            : Rooms.GetValueOrDefault(code.Trim().ToUpperInvariant());

    public static void Remove(string code) => Rooms.TryRemove(code, out _);

    private static void Sweep()
    {
        foreach (var room in Rooms.Values)
            if (room.IsAbandoned(TimeSpan.FromMinutes(10)))
                Rooms.TryRemove(room.Code, out _);
    }

    private static string GenerateCode()
    {
        Span<char> chars = stackalloc char[CodeLength];
        for (int i = 0; i < CodeLength; i++)
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(chars);
    }
}