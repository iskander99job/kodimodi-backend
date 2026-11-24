using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

namespace GameServer.Models;

public class PlayerModel
{
    public float Health { get; set; } = 100f;
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public bool IsAlive { get; set; } = true;
    public Vec3 LastPosition { get; set; }   // нужна, чтобы возрождать подальше от живых
    
    public int Id { get; init; }
    public long? AccountId { get; init; }
    public string? RankCode { get; init; }
    public string[] WeaponCodes { get; init; } = [];
    public string? ActiveWeaponCode { get; set; }
    public string Nickname { get; set; } = "";
    public WebSocket Socket { get; init; } = default!;

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    // Этот токен передаём только конкретному владельцу игрока.
    public string BattleToken { get; } =
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public bool MatchesBattleToken(string? token)
    {
        return token != null
               && token.Length == BattleToken.Length
               && CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(token),
                   Encoding.UTF8.GetBytes(BattleToken));
    }
    
    public bool IsOpen => Socket.State == WebSocketState.Open;

    public async Task SendAsync(byte[] data, CancellationToken ct = default)
    {
        if (!IsOpen) return;

        await _sendLock.WaitAsync(ct);
        try
        {
            if (IsOpen)
                await Socket.SendAsync(data, WebSocketMessageType.Binary, true, ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }
}