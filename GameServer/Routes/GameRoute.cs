using System.Net.WebSockets;
using GameServer.Data;
using GameServer.Models;
using GameServer.Services;
using MemoryPack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Routes;

public static class GameRoute
{
    public static void Map(WebApplication app)
{
    app.Map("/battle", async (HttpContext context) =>
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
                using var receiveTimeout = new CancellationTokenSource(
                    TimeSpan.FromSeconds(room == null ? 10 : 30));

                var data = await WsIo.ReceiveAsync(ws, receiveTimeout.Token);

                if (data == null) return;

                try
                {
                    var model = WsProtocol.Unpack(data);

                    if (room == null)
                    {
                        if (model.Type != WsMessageType.BattlePingRequest)
                        {
                            await Reject(ws, "expected_battle_join");
                            return;
                        }

                        var req =
                            WsProtocol.UnpackPayload<BattlePingRequestModel>(model);

                        var target = RoomManager.Get(req.RoomId);

                        if (target == null)
                        {
                            await Reject(ws, "room_not_found");
                            return;
                        }

                        var validation = target.CheckBattleJoin(req.id, req.BattleToken);
                        if (validation != AttachResult.Ok)
                        {
                            await Reject(ws, AttachError(validation));
                            return;
                        }

                        var loadout = await context.RequestServices.GetRequiredService<MatchLoadout>()
                            .ResolveAsync(req.AccessToken, context.RequestAborted);
                        if (loadout.Error is not null)
                        {
                            await Reject(ws, loadout.Error);
                            return;
                        }

                        await target.BattleEvents.WaitAsync();

                        try
                        {
                            var attach = target.TryAttachBattle(
                                req.id,
                                req.BattleToken,
                                ws,
                                loadout,
                                out var player,
                                out var startList);

                            if (attach != AttachResult.Ok)
                            {
                                await Reject(ws, AttachError(attach));

                                return;
                            }

                            me = player;
                            room = target;

                            if (startList != null)
                                await PrepareBattle(room, startList);
                        }
                        finally
                        {
                            target.BattleEvents.Release();
                        }

                        continue;
                    }

                    await room.BattleEvents.WaitAsync();

                    try
                    {
                        if (model.Type == WsMessageType.BattleHeartbeat)
                        {
                            var ack = MemoryPackSerializer.Serialize(
                                new WsEnvelopeModel
                                {
                                    Type = WsMessageType.BattleHeartbeatAck,
                                    Payload = Array.Empty<byte>()
                                });

                            await TrySend(me!, ack);
                            continue;
                        }
                        
                        if (model.Type == WsMessageType.BattleReady)
                        {
                            var ready = WsProtocol.UnpackPayload<BattleReadyModel>(model);

                            var countdown = room.SetPlayerReady(me!, ready.Ready);

                            if (countdown != null)
                            {
                                if (countdown.StartsAtUnixMs > 0)
                                {
                                    // Таймер начинает идти до рассылки.
                                    // Сама активация проходит через BattleEvents.
                                    _ = StartAfterCountdownAsync(room, countdown.Version);
                                }

                                await BroadcastToAll(
                                    room,
                                    WsProtocol.Pack(
                                        WsMessageType.BattleCountdown,
                                        countdown));
                            }

                            continue;
                        }
                        
                        // После завершения игровые действия игнорируются.
                        if (!room.IsBattleActive) continue;

                        if (model.Type == WsMessageType.PlayerBodyPosition)
                        {
                            var req =
                                WsProtocol.UnpackPayload<PlayerBodyPositionModel>(
                                    model);

                            req.Id = me!.Id;
                            me.LastPosition = req.Position;

                            await BroadcastToOthers(
                                room,
                                me,
                                WsProtocol.Pack(
                                    WsMessageType.PlayerBodyPosition,
                                    req));
                        }

                        if (model.Type == WsMessageType.Shoot)
                        {
                            var req =
                                WsProtocol.UnpackPayload<ShootModel>(model);

                            req.Id = me!.Id;

                            if (req.Damage > 0f)
                            {
                                var hit = room.ApplyHit(
                                    me,
                                    req.HitPlayerId,
                                    req.Damage);

                                if (hit != null)
                                    await HandleHit(room, hit);
                            }
                            else
                            {
                                await BroadcastToOthers(
                                    room,
                                    me,
                                    WsProtocol.Pack(WsMessageType.Shoot, req));
                            }
                        }
                        
                        if (model.Type == WsMessageType.PlayerWeaponChanged)
                        {
                            var request = WsProtocol.UnpackPayload<PlayerWeaponChangedModel>(model);
                            var weaponCode = Array.Find(me!.WeaponCodes, code =>
                                string.Equals(code, request.WeaponCode, StringComparison.OrdinalIgnoreCase));

                            if (weaponCode != null && weaponCode != me.ActiveWeaponCode)
                            {
                                me.ActiveWeaponCode = weaponCode;
                                await BroadcastToOthers(room, me, WsProtocol.Pack(
                                    WsMessageType.PlayerWeaponChanged,
                                    new PlayerWeaponChangedModel { Id = me.Id, WeaponCode = weaponCode }));
                            }

                            continue;
                        }

                        if (model.Type == WsMessageType.PlayerReload)
                        {
                            var req =
                                WsProtocol.UnpackPayload<PlayerReloadModel>(model);

                            // Используем ID подключённого игрока.
                            req.Id = me!.Id;

                            await BroadcastToOthers(
                                room,
                                me,
                                WsProtocol.Pack(
                                    WsMessageType.PlayerReload,
                                    req));
                        }
                    }
                    finally
                    {
                        room.BattleEvents.Release();
                    }
                }
                catch (MemoryPackSerializationException ex)
                {
                    Console.WriteLine($"Плохое сообщение: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Battle socket error: {ex.Message}");
        }
        finally
        {
            if (room != null && me != null)
            {
                await room.BattleEvents.WaitAsync();

                try
                {
                    bool cancelled = room.TryCancelLoading();
                    bool remove = room.LeaveBattle(me, out var ended);

                    if (cancelled)
                    {
                        await CloseLoadingUnderGate(
                            room,
                            "player_left_loading");
                    }
                    else if (!remove)
                    {
                        await BroadcastToAll(
                            room,
                            WsProtocol.Pack(
                                WsMessageType.PlayerLeft,
                                new PlayerLeftModel { Id = me.Id }));

                        await BroadcastToAll(
                            room,
                            WsProtocol.Pack(
                                WsMessageType.Scoreboard,
                                room.BuildBoard()));

                        if (ended != null)
                        {
                            await BroadcastToAll(
                                room,
                                WsProtocol.Pack(
                                    WsMessageType.MatchEnded,
                                    ended));
                        }
                    }

                    if (remove)
                        RoomManager.Remove(room.Code);
                }
                finally
                {
                    room.BattleEvents.Release();
                }
            }
        }
    });
}

    public static async Task WatchLoadingAsync(RoomModel room)
    {
        try
        {
            while (true)
            {
                TimeSpan delay;

                await room.BattleEvents.WaitAsync();

                try
                {
                    if (!room.NeedsLoadingWatch)
                        return;

                    delay = room.LoadingDeadlineUtc - DateTime.UtcNow;

                    if (delay <= TimeSpan.Zero)
                    {
                        if (room.TryCancelLoading())
                        {
                            await CloseLoadingUnderGate(
                                room,
                                "loading_timeout");
                        }

                        return;
                    }
                }
                finally
                {
                    room.BattleEvents.Release();
                }

                await Task.Delay(delay);

                // Проверяем срок заново:
                // за время ожидания могла начаться подготовка камер.
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Loading timeout error: {ex.Message}");
        }
    }

    private static async Task StartAfterCountdownAsync(
        RoomModel room,
        int version)
    {
        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(RoomModel.CountdownSeconds));

            await room.BattleEvents.WaitAsync();

            try
            {
                // Проверяем, что отсчёт не отменили и игроки ещё готовы.
                if (!room.TryActivateBattle(version))
                    return;

                await BroadcastToAll(
                    room,
                    WsProtocol.Pack(
                        WsMessageType.BattleStarted,
                        new BattleStartedModel
                        {
                            Version = version,
                            ServerNowUnixMs =
                                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        }));
            }
            finally
            {
                room.BattleEvents.Release();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Battle countdown error: {ex.Message}");
        }
    }
    
    private static async Task CloseLoadingUnderGate(
        RoomModel room,
        string reason)
    {
        RoomManager.Remove(room.Code);

        var players = room.BattleSnapshot();

        await BroadcastToAll(
            room,
            WsProtocol.Pack(
                WsMessageType.BattleClosed,
                new BattleClosedModel { Reason = reason }));

        foreach (var player in players)
        {
            try
            {
                using var timeout =
                    new CancellationTokenSource(
                        TimeSpan.FromSeconds(5));

                await player.Socket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    reason,
                    timeout.Token);
            }
            catch
            {
                player.Socket.Abort();
            }
        }
    }
    
    private static string AttachError(AttachResult result) => result == AttachResult.LoadingTimeout
        ? "loading_timeout"
        : result.ToString().ToLowerInvariant();

    private static async Task PrepareBattle(RoomModel room, PlayerModel[] players)
    {
        var spawns = SpawnPositions.Shuffled();

        var all = new SpawnInfo[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            all[i] = new SpawnInfo
            {
                Id = players[i].Id,
                Position = spawns[i % spawns.Length],
                Nickname = players[i].Nickname,
                RankCode = players[i].RankCode,
                WeaponCode = players[i].ActiveWeaponCode
            };
            players[i].LastPosition = all[i].Position;
        }

        foreach (var p in players)
        {
            var resp = new BattlePingResponseModel
            {
                Spawns = all,
                LocalWeaponCodes = p.WeaponCodes,
                // комната LiveKit = код матча, голос разных матчей не пересекается
                LivekitToken = Services.Livekit.GetToken(p.Id, room.Code)
            };
            await TrySend(p, WsProtocol.Pack(WsMessageType.BattlePingResponse, resp));
        }

        await BroadcastToAll(room, WsProtocol.Pack(WsMessageType.Scoreboard, room.BuildBoard()));
    }

    private static async Task HandleHit(RoomModel room, RoomModel.HitResult hit)
    {
        await BroadcastToAll(room, WsProtocol.Pack(WsMessageType.PlayerHealth,
            new PlayerHealthModel { Id = hit.VictimId, Health = hit.Health }));

        if (!hit.Killed) return;

        await BroadcastToAll(room, WsProtocol.Pack(WsMessageType.PlayerDied,
            new PlayerDiedModel { VictimId = hit.VictimId, KillerId = hit.KillerId }));

        await BroadcastToAll(room, WsProtocol.Pack(WsMessageType.Scoreboard, hit.Board!));

        if (hit.Ended != null)
        {
            await BroadcastToAll(room, WsProtocol.Pack(WsMessageType.MatchEnded, hit.Ended));
            return;
        }

        _ = RespawnLater(room, hit.VictimId);
    }

    private static async Task RespawnLater(RoomModel room, int playerId)
    {
        await Task.Delay(
            TimeSpan.FromSeconds(RoomModel.RespawnDelaySeconds));

        await room.BattleEvents.WaitAsync();

        try
        {
            if (!room.TryRespawn(playerId, out var position))
                return;

            await BroadcastToAll(
                room,
                WsProtocol.Pack(
                    WsMessageType.PlayerRespawned,
                    new PlayerRespawnedModel
                    {
                        Id = playerId,
                        Position = position,
                        Health = RoomModel.MaxHealth
                    }));
        }
        finally
        {
            room.BattleEvents.Release();
        }
    }

    private static async Task BroadcastToOthers(RoomModel room, PlayerModel sender, byte[] bytes)
    {
        foreach (var p in room.BattleSnapshot())
        {
            if (ReferenceEquals(p, sender)) continue;
            await TrySend(p, bytes);
        }
    }

    private static async Task BroadcastToAll(RoomModel room, byte[] bytes)
    {
        foreach (var p in room.BattleSnapshot())
            await TrySend(p, bytes);
    }

    // сбой отправки одному игроку не должен ломать остальных
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

    private static async Task Reject(WebSocket ws, string error)
    {
        var bytes = WsProtocol.Pack(
            WsMessageType.BattlePingResponse,
            new BattlePingResponseModel { Error = error });

        using var timeout =
            new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await ws.SendAsync(
            bytes,
            WebSocketMessageType.Binary,
            true,
            timeout.Token);

        await ws.CloseOutputAsync(
            WebSocketCloseStatus.PolicyViolation,
            error,
            timeout.Token);
    }
}