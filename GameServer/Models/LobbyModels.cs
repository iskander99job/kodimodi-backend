using MemoryPack;

namespace GameServer.Models;

[MemoryPackable]
public partial class LobbyJoinModel
{
    public string RoomId { get; set; }
    public string Nickname { get; set; }
    public string HostToken { get; set; }   // только у создателя
}

[MemoryPackable]
public partial class LobbyPlayerModel
{
    public int Id { get; set; }
    public string Nickname { get; set; }
}

[MemoryPackable]
public partial class LobbyStateModel
{
    public string Code { get; set; }
    public int MaxPlayers { get; set; }
    public int HostId { get; set; }
    public int YourId { get; set; }
    public LobbyPlayerModel[] Players { get; set; } = Array.Empty<LobbyPlayerModel>();
}

[MemoryPackable]
public partial class MatchStartedModel
{
    public string RoomId { get; set; }
    public int PlayerId { get; set; }
    public string BattleToken { get; set; } = "";
}

[MemoryPackable]
public partial class RoomClosedModel
{
    // "cancelled" | "host_left" | "room_not_found" | "full" | "started" | "duplicate" | "closed"
    public string Reason { get; set; }
}