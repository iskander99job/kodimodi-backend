using GameServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GameServer.Routes;

public class CreateRoomRequest
{
    public int MaxPlayers { get; set; }
}

public class CreateRoomResponse
{
    public string Code { get; set; } = "";
    public int MaxPlayers { get; set; }
    public string HostToken { get; set; } = "";
}

public static class RoomRoute
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/rooms", (CreateRoomRequest req) =>
        {
            var room = RoomManager.Create(req.MaxPlayers);
            if (room == null)
                return Results.BadRequest(new
                {
                    error = $"maxPlayers must be {RoomManager.MinPlayers}..{RoomManager.MaxPlayersLimit}"
                });

            return Results.Ok(new CreateRoomResponse
            {
                Code = room.Code,
                MaxPlayers = room.MaxPlayers,
                HostToken = room.HostToken
            });
        });

        app.MapGet("/rooms/{code}", (string code) =>
        {
            var room = RoomManager.Get(code);
            if (room == null) return Results.NotFound(new { error = "room_not_found" });
            if (!room.CanJoin) return Results.BadRequest(new { error = "room_unavailable" });
            return Results.Ok(new { code = room.Code, maxPlayers = room.MaxPlayers });
        });
        
        app.MapGet("/rooms/{code}/preview", (HttpContext context, string code) =>
        {
            context.Response.Headers["Cache-Control"] = "no-store";

            var room = RoomManager.Get(code);

            if (room == null)
                return Results.NotFound(new { error = "room_not_found" });

            return Results.Ok(room.BuildPublicPreview());
        });
    }
}