namespace RestApi.Models;

public sealed class Weapon
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public long CategoryId { get; set; }
    public long MinRankId { get; set; }
    public bool IsStarter { get; set; }
    public int Price { get; set; }
    public int Sort { get; set; }
    public string? Preview { get; set; }
    public string? Icon { get; set; }
    public WeaponCategory Category { get; set; } = null!;
    public Rank MinRank { get; set; } = null!;
    public ICollection<WeaponStat> Stats { get; } = new List<WeaponStat>();
}