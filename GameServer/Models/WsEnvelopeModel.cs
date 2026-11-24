using MemoryPack;

namespace GameServer.Models;

[MemoryPackable]
public partial class WsEnvelopeModel
{
    public WsMessageType Type { get; set; }
    public byte[] Payload { get; set; }
}
    
public enum WsMessageType : byte
{
    // /battle
    BattlePingRequest = 1,
    BattlePingResponse = 2,
    PlayerBodyPosition = 3,
    Shoot = 4,
    // /lobby
    LobbyJoin = 5,    // клиент → сервер
    LobbyState = 6,   // сервер → все
    StartMatch = 7,   // хост → сервер
    CancelMatch = 8,  // хост → сервер
    RoomClosed = 9,   // сервер → все, также причина отказа при входе
    MatchStarted = 10, // сервер → все
    PlayerHealth = 11,      // сервер → все
    PlayerDied = 12,        // сервер → все
    PlayerRespawned = 13,   // сервер → все
    Scoreboard = 14,        // сервер → все
    MatchEnded = 15,
    PlayerLeft = 16,
    BattleClosed = 17,
    BattleHeartbeat = 18,
    BattleHeartbeatAck = 19,
    PlayerReload = 20,

    BattleReady = 21,
    BattleCountdown = 22,
    BattleStarted = 23,
    PlayerWeaponChanged = 24
}