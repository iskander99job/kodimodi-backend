using MemoryPack;

namespace GameServer.Models;

[MemoryPackable]
public partial class BattleReadyModel
{
    public bool Ready { get; set; }
}

[MemoryPackable]
public partial class BattleCountdownModel
{
    // Номер отсчёта, чтобы различать отмены и повторные запуски.
    public int Version { get; set; }

    public long ServerNowUnixMs { get; set; }

    // 0 означает отмену отсчёта.
    public long StartsAtUnixMs { get; set; }
}

[MemoryPackable]
public partial class BattleStartedModel
{
    public int Version { get; set; }
    public long ServerNowUnixMs { get; set; }
}