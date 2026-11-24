namespace RestApi.Models;

public sealed class WeaponStat
{
    public long Id { get; set; }
    public long WeaponId { get; set; }
    public long StatPropId { get; set; }
    public WeaponStatProp StatProp { get; set; } = null!;
    public int Value { get; set; }
}