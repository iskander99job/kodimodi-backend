using MemoryPack;

namespace GameServer.Models
{
    [MemoryPackable]
    public partial class PlayerReloadModel
    {
        public int Id { get; set; }
        public bool IsReloading { get; set; }
    }
}