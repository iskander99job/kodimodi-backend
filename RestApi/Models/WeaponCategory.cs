namespace RestApi.Models;

public sealed class WeaponCategory
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string EnName { get; set; }
    public required string RuName { get; set; }
    public string? Icon { get; set; }
    public int Sort { get; set; }
}
