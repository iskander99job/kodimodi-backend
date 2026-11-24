namespace RestApi.Models;

public sealed class UserWeapon
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long WeaponId { get; set; }
    public bool Equipped { get; set; }
}