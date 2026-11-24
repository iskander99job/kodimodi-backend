using GameServer.Models;

namespace GameServer.Data;

public class SpawnPositions
{
    public static readonly Vec3[] Positions =
    [
        new Vec3(3f, 2.639f,  26f),
        new Vec3(21f, 2.658f, 18f)
    ];

    private static readonly Random Rng = new();

    public static Vec3 GetRandom()
        => Positions[Rng.Next(Positions.Length)];

    public static Vec3 GetByIndex(int index)
        => Positions[index];

    public static int Count => Positions.Length;
    
    // возвращает НОВЫЙ перемешанный массив, исходный не трогает
    public static Vec3[] Shuffled()
    {
        var copy = (Vec3[])Positions.Clone();
        Rng.Shuffle(copy);
        return copy;
    }
    
    // Точка, максимально далёкая от ближайшего из переданных игроков
    public static Vec3 GetFarthestFrom(IEnumerable<Vec3> others)
    {
        var list = others.ToList();
        if (list.Count == 0) return GetRandom();

        Vec3 best = Positions[0];
        float bestDist = -1f;

        foreach (var p in Positions)
        {
            float nearest = list.Min(o => DistSq(p, o));
            if (nearest > bestDist) { bestDist = nearest; best = p; }
        }
        return best;
    }

    private static float DistSq(Vec3 a, Vec3 b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }
}