using MemoryPack;

namespace GameServer.Models
{
    [MemoryPackable]
    public partial class PlayerBodyPositionModel
    {
        public int Id { get; set; }
        public Vec3 Position { get; set; }
        public Vec3 Rotation { get; set; }
        public bool IsJumping { get; set; }
        public float H { get; set; }
        public float V { get; set; }
        public double SentAt { get; set; }
    }
}