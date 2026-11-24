using MemoryPack;

namespace GameServer.Models
{
    [MemoryPackable]
    public partial class ShootModel
    {
        public int Id { get; set; }
        public int HitPlayerId { get; set; }
        public float Damage { get; set; }
    }
}