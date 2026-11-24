using Livekit.Server.Sdk.Dotnet;

namespace GameServer.Services;

public static class Livekit
{
    private const string Key = "mykey";
    private const string Secret = "yyRr+TTPnXV2kRZ8UcCgbO1iSy9WoyA4ox9hbsFZkOs=";
    
    public static string GetToken(int id, string room)
    {
        var token = new AccessToken(Key, Secret)
            .WithIdentity(id.ToString())
            .WithGrants(new VideoGrants{ RoomJoin = true, Room = room })
            .WithTtl(TimeSpan.FromMinutes(30));

        var jwt = token.ToJwt();
        
        return jwt;
    }
}

// livekit-server --keys "mykey: yyRr+TTPnXV2kRZ8UcCgbO1iSy9WoyA4ox9hbsFZkOs=" --bind 0.0.0.0