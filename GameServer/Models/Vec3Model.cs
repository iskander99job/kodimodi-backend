using MemoryPack;

namespace GameServer.Models;

[MemoryPackable]
public partial struct Vec3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }
}