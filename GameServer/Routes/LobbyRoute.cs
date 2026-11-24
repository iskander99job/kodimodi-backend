using System.Net.WebSockets;
using GameServer.Models;
using GameServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GameServer.Routes;

public static class LobbyRoute
{
    public static void Map(WebApplication app)
    {
        app.Map("/lobby", async (HttpContext context) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            using var ws = await context.WebSockets.AcceptWebSocketAsync();

            PlayerModel? me = null;
            RoomModel? room = null;

            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var data = await WsIo.ReceiveAsync(ws);
                    if (data == null) return;

                    WsEnvelopeModel model;
                    try { model = WsProtocol.Unpack(data); }
                    catch { continue; }

                    // первое сообщение обязано быть входом в комнату
                    if (room == null)
                    {
                        if (model.Type != WsMessageType.LobbyJoin) continue;

                        var req = WsProtocol.UnpackPayload<LobbyJoinModel>(model);
                        var target = RoomManager.Get(req.RoomId);
                        if (target == null) { await Reject(ws, "room_not_found"); return; }

                        bool asHost = !string.IsNullOrEmpty(req.HostToken)
                                      && req.HostToken == target.HostToken;

                        var join = target.TryJoinLobby(ws, Sanitize(req.Nickname), asHost, out var player, out var startList);
                        if (join != JoinResult.Ok)
                        {
                            await Reject(ws, join.ToString().ToLowerInvariant());
                            return;
                        }

                        me = player;
                        room = target;
                        await BroadcastLobby(room);

                        if (startList != null)
                            await NotifyMatchStarted(room, startList);   // комната заполнена, стартуем

                        continue;
                    }

                    switch (model.Type)
                    {
                        case WsMessageType.StartMatch:
                            if (room.TryStart(me!, out var players))
                                await NotifyMatchStarted(room, players);
                            break;

                        case WsMessageType.CancelMatch:
                            if (room.TryCancel(me!))
                                await CloseRoom(room, "cancelled");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lobby socket error: {ex.Message}");
            }
            finally
            {
                if (room != null && me != null)
                {
                    switch (room.LeaveLobby(me))
                    {
                        case LeaveOutcome.Changed:
                            await BroadcastLobby(room);
                            break;
                        case LeaveOutcome.RoomClosed:
                            await CloseRoom(room, "host_left");
                            break;
                    }
                }
            }
        });
    }

    private static async Task BroadcastLobby(RoomModel room)
    {
        foreach (var p in room.LobbySnapshot())
        {
            var bytes = WsProtocol.Pack(WsMessageType.LobbyState, room.BuildLobbyState(p.Id));
            await TrySend(p, bytes);
        }
    }

    private static async Task NotifyMatchStarted(RoomModel room, PlayerModel[] players)
    {
        _ = GameRoute.WatchLoadingAsync(room);
        
        foreach (var p in players)
        {
            var bytes = WsProtocol.Pack(WsMessageType.MatchStarted,
                new MatchStartedModel
                {
                    RoomId = room.Code,
                    PlayerId = p.Id,
                    BattleToken = p.BattleToken
                });
            await TrySend(p, bytes);
            await TryCloseOutput(p, "match_started");
        }
    }

    private static async Task CloseRoom(RoomModel room, string reason)
    {
        var bytes = WsProtocol.Pack(WsMessageType.RoomClosed, new RoomClosedModel { Reason = reason });

        foreach (var p in room.LobbySnapshot())
        {
            await TrySend(p, bytes);
            await TryCloseOutput(p, reason);
        }

        RoomManager.Remove(room.Code);
    }

    private static async Task TrySend(PlayerModel p, byte[] bytes)
    {
        using var timeout =
            new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            await p.SendAsync(bytes, timeout.Token);
        }
        catch
        {
            p.Socket.Abort();
        }
    }

    private static async Task TryCloseOutput(
        PlayerModel p,
        string reason)
    {
        try
        {
            using var timeout =
                new CancellationTokenSource(TimeSpan.FromSeconds(5));

            await p.Socket.CloseOutputAsync(
                WebSocketCloseStatus.NormalClosure,
                reason,
                timeout.Token);
        }
        catch
        {
        }
    }

    private static async Task Reject(WebSocket ws, string reason)
    {
        var bytes = WsProtocol.Pack(WsMessageType.RoomClosed, new RoomClosedModel { Reason = reason });
        await ws.SendAsync(bytes, WebSocketMessageType.Binary, true, CancellationToken.None);
        await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, reason, CancellationToken.None);
    }

    private static string Sanitize(string? nick)
    {
        nick = (nick ?? "").Trim();
        if (nick.Length == 0) nick = "Guest";
        return nick.Length > 20 ? nick[..20] : nick;
    }
}