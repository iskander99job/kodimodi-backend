using GameServer.Services;
using System.Net.WebSockets;
using System.Security.Cryptography;
using GameServer.Data;

namespace GameServer.Models;

public enum RoomState
{
    Lobby,
    Started,
    Closed
}

public enum JoinResult
{
    Ok,
    Full,
    Started,
    Duplicate,
    Closed
}

public enum LeaveOutcome
{
    Nothing,
    Changed,
    RoomClosed
}

public enum AttachResult
{
    Ok,
    NotStarted,
    NotMember,
    Duplicate,
    Closed,
    Unauthorized,
    LoadingTimeout
}

public class RoomModel
{
    public const int MinToStart = 2;

    public string Code { get; }
    public int MaxPlayers { get; }
    public string HostToken { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    private readonly List<PlayerModel> _lobby = new();
    private readonly Dictionary<int, PlayerModel> _battle = new();
    private readonly HashSet<int> _members = new(); // кто участвует в матче
    private readonly object _lock = new();

    private RoomState _state = RoomState.Lobby;
    private int _hostId = -1;
    private int _nextId;
    private DateTime _touched = DateTime.UtcNow;

    public const int KillLimit = 10;
    public const float MaxHealth = 100f;
    public const float MaxHitDamage = 100f; // защита от абсурдных значений
    public const int RespawnDelaySeconds = 3;

    private bool _battleStarted;
    private bool _matchEnded;
    private bool _spawnsIssued;
    private readonly HashSet<int> _readyPlayers = new();

    private int _countdownVersion;
    private bool _countdownRunning;

    private DateTime _readyDeadlineUtc;

    public const int BattleReadyTimeoutSeconds = 30;
    public const int CountdownSeconds = 3;

    // Храним результаты даже после отключения игрока.
    private readonly Dictionary<int, PlayerModel> _participants = new();

// Упорядочивает обработку и рассылку событий боя.
    public SemaphoreSlim BattleEvents { get; } = new(1, 1);

    public const int BattleLoadTimeoutSeconds = 30;

    public DateTime LoadingDeadlineUtc
    {
        get
        {
            lock (_lock)
            {
                return _spawnsIssued
                    ? _readyDeadlineUtc
                    : _touched.AddSeconds(BattleLoadTimeoutSeconds);
            }
        }
    }

    public bool TryCancelLoading()
    {
        lock (_lock)
        {
            if (_state != RoomState.Started || _battleStarted)
                return false;

            _state = RoomState.Closed;
            return true;
        }
    }

    public bool IsBattleActive
    {
        get
        {
            lock (_lock)
                return _battleStarted
                       && !_matchEnded
                       && _state == RoomState.Started;
        }
    }

    public RoomModel(string code, int maxPlayers)
    {
        Code = code;
        MaxPlayers = maxPlayers;
    }

    public bool CanJoin
    {
        get
        {
            lock (_lock)
            {
                if (_state != RoomState.Lobby)
                    return false;

                int guestCapacity = _hostId == -1
                    ? MaxPlayers - 1
                    : MaxPlayers;

                return _lobby.Count < guestCapacity;
            }
        }
    }

    public object BuildPublicPreview()
    {
        lock (_lock)
        {
            int guestCapacity = _hostId == -1
                ? MaxPlayers - 1
                : MaxPlayers;

            return new
            {
                code = Code,
                maxPlayers = MaxPlayers,
                state = _state.ToString().ToLowerInvariant(),
                canJoin = _state == RoomState.Lobby
                          && _lobby.Count < guestCapacity,

                players = _lobby.Select(p => new
                {
                    id = p.Id,
                    nickname = p.Nickname
                }).ToArray()
            };
        }
    }

    // ---------- Лобби ----------

    public JoinResult TryJoinLobby(WebSocket ws, string nickname, bool asHost,
        out PlayerModel? player, out PlayerModel[]? startList)
    {
        player = null;
        startList = null;
        lock (_lock)
        {
            if (_state == RoomState.Closed) return JoinResult.Closed;
            if (_state == RoomState.Started) return JoinResult.Started;
            if (asHost && _hostId != -1) return JoinResult.Duplicate;
            if (_lobby.Count >= MaxPlayers)
                return JoinResult.Full;

// Пока хост не подключился, последнее место доступно только ему.
            if (!asHost
                && _hostId == -1
                && _lobby.Count >= MaxPlayers - 1)
            {
                return JoinResult.Full;
            }

            player = new PlayerModel { Id = ++_nextId, Socket = ws, Nickname = nickname };
            _lobby.Add(player);
            if (asHost) _hostId = player.Id;

            // Комната заполнилась: стартуем сами, но только если создатель уже в лобби
            if (_hostId != -1 && _lobby.Count == MaxPlayers)
            {
                _state = RoomState.Started;
                _touched = DateTime.UtcNow;
                startList = _lobby.ToArray();
                foreach (var p in startList) _members.Add(p.Id);
            }

            return JoinResult.Ok;
        }
    }

    public LeaveOutcome LeaveLobby(PlayerModel p)
    {
        lock (_lock)
        {
            if (_state != RoomState.Lobby) return LeaveOutcome.Nothing; // после старта лобби уже не важно
            if (!_lobby.Remove(p)) return LeaveOutcome.Nothing;

            if (_lobby.Count == 0 || p.Id == _hostId)
            {
                _state = RoomState.Closed;
                return LeaveOutcome.RoomClosed;
            }

            return LeaveOutcome.Changed;
        }
    }

    public bool TryStart(PlayerModel requester, out PlayerModel[] players)
    {
        lock (_lock)
        {
            players = Array.Empty<PlayerModel>();
            if (_state != RoomState.Lobby) return false;
            if (requester.Id != _hostId) return false;
            if (_lobby.Count < MinToStart) return false;

            _state = RoomState.Started;
            _touched = DateTime.UtcNow;
            players = _lobby.ToArray();
            foreach (var p in players) _members.Add(p.Id);
            return true;
        }
    }

    public bool TryCancel(PlayerModel requester)
    {
        lock (_lock)
        {
            if (_state != RoomState.Lobby) return false;
            if (requester.Id != _hostId) return false;
            _state = RoomState.Closed;
            return true;
        }
    }

    public LobbyStateModel BuildLobbyState(int yourId)
    {
        lock (_lock)
            return new LobbyStateModel
            {
                Code = Code,
                MaxPlayers = MaxPlayers,
                HostId = _hostId,
                YourId = yourId,
                Players = _lobby
                    .Select(x => new LobbyPlayerModel { Id = x.Id, Nickname = x.Nickname })
                    .ToArray()
            };
    }

    public PlayerModel[] LobbySnapshot()
    {
        lock (_lock) return _lobby.ToArray();
    }

    // ---------- Бой ----------

    // Если подключился последний игрок, в startList вернутся все
    public AttachResult TryAttachBattle(
        int id,
        string? battleToken,
        WebSocket ws,
        MatchLoadout.Result loadout,
        out PlayerModel? player,
        out PlayerModel[]? startList)
    {
        player = null;
        startList = null;

        lock (_lock)
        {
            var validation = ValidateBattleJoin(id, battleToken, out _);
            if (validation != AttachResult.Ok)
                return validation;

            player = new PlayerModel
            {
                Id = id,
                Socket = ws,
                Nickname = string.IsNullOrWhiteSpace(loadout.Nickname) ? "Guest" : loadout.Nickname,
                AccountId = loadout.AccountId,
                RankCode = loadout.RankCode,
                WeaponCodes = loadout.WeaponCodes,
                ActiveWeaponCode = loadout.WeaponCodes.FirstOrDefault()
            };

            _participants[id] = player;
            _battle[id] = player;

            if (_battle.Count == _members.Count)
            {
                // Персонажей можно создавать, но бой ещё не разрешён.
                _spawnsIssued = true;

                _readyDeadlineUtc = DateTime.UtcNow.AddSeconds(
                    BattleReadyTimeoutSeconds);

                startList = _battle.Values.ToArray();
            }

            return AttachResult.Ok;
        }
    }

    public AttachResult CheckBattleJoin(int id, string? battleToken)
    {
        lock (_lock) return ValidateBattleJoin(id, battleToken, out _);
    }

    private AttachResult ValidateBattleJoin(int id, string? battleToken, out PlayerModel? member)
    {
        member = null;
        if (_state == RoomState.Closed)
            return AttachResult.Closed;

        if (_state == RoomState.Lobby)
            return AttachResult.NotStarted;

        if (_spawnsIssued || _matchEnded)
            return AttachResult.Closed;

        if (DateTime.UtcNow >=
            _touched.AddSeconds(BattleLoadTimeoutSeconds))
        {
            return AttachResult.LoadingTimeout;
        }

        if (!_members.Contains(id))
            return AttachResult.Unauthorized;

        member = _lobby.FirstOrDefault(p => p.Id == id);

        if (member == null || !member.MatchesBattleToken(battleToken))
            return AttachResult.Unauthorized;

        if (_battle.ContainsKey(id))
            return AttachResult.Duplicate;

        return AttachResult.Ok;
    }

    public bool NeedsLoadingWatch
    {
        get
        {
            lock (_lock)
            {
                return _state == RoomState.Started &&
                       !_battleStarted &&
                       !_matchEnded;
            }
        }
    }

// Возвращает сообщение только при запуске или отмене отсчёта.
// Вызывать под BattleEvents.
    public BattleCountdownModel? SetPlayerReady(
        PlayerModel player,
        bool ready)
    {
        lock (_lock)
        {
            if (_state != RoomState.Started ||
                !_spawnsIssued ||
                _battleStarted ||
                _matchEnded)
            {
                return null;
            }

            if (!_battle.TryGetValue(player.Id, out var current) ||
                !ReferenceEquals(current, player))
            {
                return null;
            }

            if (ready)
                _readyPlayers.Add(player.Id);
            else
                _readyPlayers.Remove(player.Id);

            bool allReady =
                _battle.Count == _members.Count &&
                _battle.Values.All(p =>
                    p.IsOpen && _readyPlayers.Contains(p.Id));

            if (!allReady && _countdownRunning)
            {
                _countdownRunning = false;
                _countdownVersion++;

                return new BattleCountdownModel
                {
                    Version = _countdownVersion,
                    ServerNowUnixMs =
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    StartsAtUnixMs = 0
                };
            }

            if (!allReady || _countdownRunning)
                return null;

            // Нельзя запускать отсчёт после истечения срока готовности.
            if (DateTime.UtcNow >= _readyDeadlineUtc)
                return null;

            _countdownRunning = true;
            _countdownVersion++;

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            return new BattleCountdownModel
            {
                Version = _countdownVersion,
                ServerNowUnixMs = now,
                StartsAtUnixMs = now + CountdownSeconds * 1000L
            };
        }
    }

// Вызывать после серверной задержки и под BattleEvents.
    public bool TryActivateBattle(int countdownVersion)
    {
        lock (_lock)
        {
            if (_state != RoomState.Started ||
                _battleStarted ||
                _matchEnded ||
                !_countdownRunning ||
                _countdownVersion != countdownVersion)
            {
                return false;
            }

            if (_battle.Count != _members.Count ||
                !_battle.Values.All(p =>
                    p.IsOpen && _readyPlayers.Contains(p.Id)))
            {
                return false;
            }

            if (DateTime.UtcNow >= _readyDeadlineUtc)
                return false;

            _countdownRunning = false;
            _battleStarted = true;

            return true;
        }
    }

    // true, если в бою никого не осталось и комнату надо удалить
    public bool LeaveBattle(PlayerModel p, out MatchEndedModel? ended)
    {
        ended = null;

        lock (_lock)
        {
            if (!_battle.TryGetValue(p.Id, out var current)
                || !ReferenceEquals(current, p))
            {
                return false;
            }

            _battle.Remove(p.Id);

            // Никого не осталось — удалить комнату.
            if (_battle.Count == 0)
            {
                _state = RoomState.Closed;
                return true;
            }

            // Проверяем только после фактического начала боя.
            if (_battleStarted && !_matchEnded && _battle.Count == 1)
                ended = FinishMatch(_battle.Values.First().Id);

            return false;
        }
    }

    private MatchEndedModel? FinishMatch(int winnerId)
    {
        if (_matchEnded) return null;

        _matchEnded = true;

        return new MatchEndedModel
        {
            WinnerId = winnerId,
            Players = BuildBoard(0, 0).Players
        };
    }

    public PlayerModel[] BattleSnapshot()
    {
        lock (_lock) return _battle.Values.ToArray();
    }

    // ---------- Чистка ----------

    public bool IsAbandoned(TimeSpan maxAge)
    {
        lock (_lock)
        {
            if (_state == RoomState.Closed) return true;
            var idle = DateTime.UtcNow - _touched > maxAge;
            if (_state == RoomState.Lobby) return _lobby.Count == 0 && idle;
            return _battle.Count == 0 && idle; // Started, но никто не подключился к /battle
        }
    }

    // null, если попадание недопустимо (не тот игрок, мёртв, матч не идёт)
    public HitResult? ApplyHit(PlayerModel shooter, int victimId, float rawDamage)
    {
        lock (_lock)
        {
            if (!_battleStarted || _matchEnded || _state != RoomState.Started)
                return null;

            if (!_battle.TryGetValue(shooter.Id, out var current)
                || !ReferenceEquals(current, shooter))
            {
                return null;
            }

            if (shooter.Id == victimId || !shooter.IsAlive) return null;
            if (!_battle.TryGetValue(victimId, out var victim) || !victim.IsAlive) return null;

            float damage = Math.Clamp(rawDamage, 0f, MaxHitDamage);
            if (damage <= 0f) return null;

            victim.Health = Math.Max(0f, victim.Health - damage);

            if (victim.Health > 0f)
                return new HitResult { VictimId = victimId, Health = victim.Health };

            victim.IsAlive = false;
            victim.Deaths++;
            shooter.Kills++;

            var board = BuildBoard(shooter.Id, victim.Id);

            MatchEndedModel? ended = null;
            if (shooter.Kills >= KillLimit)
            {
                ended = FinishMatch(shooter.Id);
            }

            return new HitResult
            {
                VictimId = victimId,
                Health = 0f,
                Killed = true,
                KillerId = shooter.Id,
                Board = board,
                Ended = ended
            };
        }
    }

    public bool TryRespawn(int playerId, out Vec3 position)
    {
        position = default;
        lock (_lock)
        {
            if (!_battleStarted || _matchEnded) return false;
            if (!_battle.TryGetValue(playerId, out var p) || p.IsAlive) return false;

            var others = _battle.Values
                .Where(x => x.Id != playerId && x.IsAlive)
                .Select(x => x.LastPosition);

            position = SpawnPositions.GetFarthestFrom(others);

            p.Health = MaxHealth;
            p.IsAlive = true;
            p.LastPosition = position;
            return true;
        }
    }

    public ScoreboardModel BuildBoard()
    {
        lock (_lock) return BuildBoard(0, 0);
    }

    private ScoreboardModel BuildBoard(int killerId, int victimId) => new()
    {
        KillLimit = KillLimit,
        KillerId = killerId,
        VictimId = victimId,
        Players = _participants.Values
            .OrderBy(p => p.Id)
            .Select(p => new PlayerStatsModel { Id = p.Id, Kills = p.Kills, Deaths = p.Deaths })
            .ToArray()
    };

    public sealed class HitResult
    {
        public int VictimId { get; init; }
        public float Health { get; init; }
        public bool Killed { get; init; }
        public int KillerId { get; init; }
        public ScoreboardModel? Board { get; init; }
        public MatchEndedModel? Ended { get; init; }
    }
}