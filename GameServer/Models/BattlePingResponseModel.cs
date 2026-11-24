using MemoryPack;

namespace GameServer.Models;

[MemoryPackable]
public partial class BattlePingResponseModel
{
    public SpawnInfo[] Spawns { get; set; } = Array.Empty<SpawnInfo>();
    public string LivekitToken { get; set; }
    public string Error { get; set; }
    public string[] LocalWeaponCodes { get; set; } = [];
}

[MemoryPackable]
public partial class SpawnInfo
{
    public int Id { get; set; }
    public Vec3 Position { get; set; }
    public string Nickname { get; set; } = "";
    public string? RankCode { get; set; }
    public string? WeaponCode { get; set; }
}