namespace RestApi.Models;

public sealed class Rank
{
    public Rank(long id, string code, string enName, string ruName, int minXp)
    {
        Id = id;
        Code = code;
        EnName = enName;
        RuName = ruName;
        MinXp = minXp;
    }

    public long Id { get; private set; }
    public string Code { get; private set; }
    public string EnName { get; private set; }
    public string RuName { get; private set; }
    public int MinXp { get; private set; }
}