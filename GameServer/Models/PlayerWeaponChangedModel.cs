using MemoryPack;

namespace GameServer.Models;

[MemoryPackable]
public partial class PlayerWeaponChangedModel
{
    public int Id { get; set; }
    public string WeaponCode { get; set; } = "";
}
