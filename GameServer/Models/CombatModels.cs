using MemoryPack;

namespace GameServer.Models
{
    [MemoryPackable]
    public partial class PlayerHealthModel
    {
        public int Id { get; set; }
        public float Health { get; set; }
    }

    [MemoryPackable]
    public partial class PlayerDiedModel
    {
        public int VictimId { get; set; }
        public int KillerId { get; set; }
    }

    [MemoryPackable]
    public partial class PlayerRespawnedModel
    {
        public int Id { get; set; }
        public Vec3 Position { get; set; }
        public float Health { get; set; }
    }

    [MemoryPackable]
    public partial class PlayerStatsModel
    {
        public int Id { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
    }

    [MemoryPackable]
    public partial class ScoreboardModel
    {
        public int KillLimit { get; set; }
        public int KillerId { get; set; }   // 0, если это просто обновление таблицы
        public int VictimId { get; set; }
        public PlayerStatsModel[] Players { get; set; } = Array.Empty<PlayerStatsModel>();
    }

    [MemoryPackable]
    public partial class MatchEndedModel
    {
        public int WinnerId { get; set; }
        public PlayerStatsModel[] Players { get; set; } = Array.Empty<PlayerStatsModel>();
    }
}