namespace RestApi.Models;

public sealed class WeaponStatProp
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string EnName { get; set; }
    public required string RuName { get; set; }
}
