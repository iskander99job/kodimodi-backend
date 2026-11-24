namespace RestApi.Models;

public enum LoginType
{
    Local = 0
}

public sealed class User
{
    public long Id { get; set; }
    public required string Nickname { get; set; }
    public required string NormalizedNickname { get; set; }
    public string? PasswordHash { get; set; }
    public int Coins { get; set; }
    public int Xp { get; set; }
    public string? Email { get; set; }
    public bool Banned { get; set; }
    public LoginType LoginType { get; set; } = LoginType.Local;
}