using MemoryPack;

namespace GameServer.Models;

[MemoryPackable]
public partial class BattlePingRequestModel
{
    public int id;
    public string RoomId { get; set; }
    public string BattleToken { get; set; } = "";
    public string? AccessToken { get; set; }
}