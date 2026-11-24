using MemoryPack;

namespace GameServer.Models
{
    [MemoryPackable]
    public partial class PlayerLeftModel
    {
        public int Id { get; set; }
    }

    [MemoryPackable]
    public partial class BattleClosedModel
    {
        public string Reason { get; set; } = "";
    }
}