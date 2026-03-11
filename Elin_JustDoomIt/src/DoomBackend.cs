using BepInEx.Logging;
using DoomNetFrameworkEngine;
using DoomNetFrameworkEngine.Audio;
using DoomNetFrameworkEngine.DoomEntity;
using DoomNetFrameworkEngine.DoomEntity.Game;
using DoomNetFrameworkEngine.DoomEntity.MathUtils;
using DoomNetFrameworkEngine.DoomEntity.World;
using DoomNetFrameworkEngine.UserInput;
using DoomNetFrameworkEngine.Video;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Elin_JustDoomIt
{
    public struct DoomKillEvent
    {
        public int TotalKills;
        public int CurrentKillStreak;
        public string Enemy;
        public int Health;
        public int Armor;
        public string Weapon;
        public string MapCode;
        public string MapTitle;
        public int MapKills;
        public int MapKillTotal;
    }

    public struct DoomMapStartEvent
    {
        public int Episode;
        public int Map;
        public int Skill;
        public string MapCode;
        public string MapTitle;
    }

    public struct DoomDamageEvent
    {
        public int Health;
        public int Armor;
        public string MapCode;
        public string MapTitle;
    }

    public struct DoomDeathEvent
    {
        public int Episode;
        public int Map;
        public string MapCode;
        public string MapTitle;
    }

    public struct DoomSecretEvent
    {
        public int Count;
        public int SecretCount;
        public int TotalSecrets;
        public string MapCode;
        public string MapTitle;
    }

    public enum DoomBackendEventType
    {
        None = 0,
        MapStart = 1,
        Damage = 2,
        Death = 3,
        Kill = 4,
        Secret = 5
    }

    public readonly struct DoomBackendEvent
    {
        public DoomBackendEventType Type { get; }
        public DoomMapStartEvent MapStartEvent { get; }
        public DoomDamageEvent DamageEvent { get; }
        public DoomDeathEvent DeathEvent { get; }
        public DoomKillEvent KillEvent { get; }
        public DoomSecretEvent SecretEvent { get; }

        private DoomBackendEvent(
            DoomBackendEventType type,
            DoomMapStartEvent mapStartEvent,
            DoomDamageEvent damageEvent,
            DoomDeathEvent deathEvent,
            DoomKillEvent killEvent,
            DoomSecretEvent secretEvent)
        {
            Type = type;
            MapStartEvent = mapStartEvent;
            DamageEvent = damageEvent;
            DeathEvent = deathEvent;
            KillEvent = killEvent;
            SecretEvent = secretEvent;
        }

        public static DoomBackendEvent FromMapStart(DoomMapStartEvent mapStartEvent)
        {
            return new DoomBackendEvent(DoomBackendEventType.MapStart, mapStartEvent, default, default, default, default);
        }

        public static DoomBackendEvent FromDamage(DoomDamageEvent damageEvent)
        {
            return new DoomBackendEvent(DoomBackendEventType.Damage, default, damageEvent, default, default, default);
        }

        public static DoomBackendEvent FromDeath(DoomDeathEvent deathEvent)
        {
            return new DoomBackendEvent(DoomBackendEventType.Death, default, default, deathEvent, default, default);
        }

        public static DoomBackendEvent FromKill(DoomKillEvent killEvent)
        {
            return new DoomBackendEvent(DoomBackendEventType.Kill, default, default, default, killEvent, default);
        }

        public static DoomBackendEvent FromSecret(DoomSecretEvent secretEvent)
        {
            return new DoomBackendEvent(DoomBackendEventType.Secret, default, default, default, default, secretEvent);
        }
    }

    public struct DoomRunStats
    {
        public int TotalKills;
        public int MaxKillStreak;
        public int CurrentKillStreak;
        public int ClearEventCount;
        public int BossClearEventCount;
    }

    public interface IDoomBackend
    {
        int Width { get; }
        int Height { get; }
        bool IsRunning { get; }
        DoomRunStats Stats { get; }
        int PersistentTotalChips { get; set; }
        bool Initialize(DoomLaunchConfig launchConfig, ManualLogSource logger);
        void PrimeSessionState();
        void SubmitInput(DoomInputState input);
        bool TryDequeueEvent(out DoomBackendEvent backendEvent);
        void SavePersistentCheckpoint();
        void SavePersistentNow();
        void Tick(float deltaTime);
        Color32[] GetFrameBuffer();
        void Shutdown();
    }

    public sealed class ManagedDoomBackend : IDoomBackend
    {
        private readonly int _requestedWidth;
        private readonly int _requestedHeight;

        private Doom _doom;
        private ManagedDoomVideo _video;
        private ManagedDoomInput _input;
        private UnityDoomSound _sound;
        private GameContent _content;
        private Color32[] _frame;
        private DoomInputState _currentInput;
        private DoomPendingWeaponInput _pendingWeaponInput;
        private DoomWeaponCyclePlanner _weaponCyclePlanner;

        private int _fpsScale = 2;
        private int _frameCount = -1;
        private bool _running;
        private DoomRunStats _stats;
        private ManualLogSource _logger;
        private string _saveSlotKey;
        private string _engineSavePath;
        private bool _loadPersistentSaveOnStart;
        private bool _pendingSaveExport;
        private int _pendingSaveExportTicks;
        private int _loadedTotalPlaySeconds;
        private float _sessionPlaySeconds;
        private int _killStreak;
        private int _mapKillCount;
        private int _lastEpisode = -1;
        private int _lastMap = -1;
        private GameState _lastGameState = GameState.Level;
        private int _lastHealth = -1;
        private int _lastDamageCount = -1;
        private int _lastSecretCount = -1;
        private PlayerState _lastPlayerState;
        private bool _hasLastPlayerState;
        private int _persistentTotalChips;
        private readonly Queue<DoomBackendEvent> _orderedEvents = new Queue<DoomBackendEvent>();

        public int Width => _video?.Width ?? Mathf.Max(160, _requestedWidth);
        public int Height => _video?.Height ?? Mathf.Max(100, _requestedHeight);
        public bool IsRunning => _running;
        public DoomRunStats Stats => _stats;
        public int PersistentTotalChips
        {
            get => _persistentTotalChips;
            set => _persistentTotalChips = Mathf.Max(0, value);
        }

        public ManagedDoomBackend(int requestedWidth, int requestedHeight)
        {
            _requestedWidth = requestedWidth;
            _requestedHeight = requestedHeight;
        }

        public bool Initialize(DoomLaunchConfig launchConfig, ManualLogSource logger)
        {
            try
            {
                _logger = logger;
                if (string.IsNullOrWhiteSpace(launchConfig.IwadPath))
                {
                    logger.LogError("[JustDoomIt] Missing IWAD path.");
                    return false;
                }

                var cmdArgs = new List<string>
                {
                    "-iwad", launchConfig.IwadPath
                };

                if (launchConfig.PwadPaths != null && launchConfig.PwadPaths.Count > 0)
                {
                    cmdArgs.Add("-file");
                    cmdArgs.AddRange(launchConfig.PwadPaths);
                }

                cmdArgs.Add("-warp");
                cmdArgs.Add(Mathf.Clamp(launchConfig.Episode, 1, 4).ToString());
                cmdArgs.Add(Mathf.Clamp(launchConfig.Map, 1, 32).ToString());
                cmdArgs.Add("-skill");
                cmdArgs.Add(Mathf.Clamp(launchConfig.Skill, 1, 5).ToString());
                cmdArgs.Add("-nomusic");

                var args = new CommandLineArgs(cmdArgs.ToArray());

                var config = new Config
                {
                    video_highresolution = _requestedWidth >= 640 || _requestedHeight >= 360,
                    video_gammacorrection = Mathf.Clamp(ModConfig.DoomBrightness.Value, 0, 10),
                    video_displaymessage = true,
                    video_gamescreensize = 7,
                    video_fullscreen = false,
                    audio_musicvolume = 0,
                    audio_soundvolume = Mathf.Clamp(ModConfig.DoomSfxVolume.Value, 0, 15)
                };

                _content = new GameContent(args);
                _video = new ManagedDoomVideo(config, _content);
                _input = new ManagedDoomInput(() => _currentInput, config);
                _sound = new UnityDoomSound(launchConfig.IwadPath, config.audio_soundvolume);

                _doom = new Doom(
                    args,
                    config,
                    _content,
                    _video,
                    _sound,
                    NullMusic.GetInstance(),
                    _input);

                _doom.NewGame(ToGameSkill(Mathf.Clamp(launchConfig.Skill, 1, 5)), Mathf.Clamp(launchConfig.Episode, 1, 4), Mathf.Clamp(launchConfig.Map, 1, 32));
                _saveSlotKey = launchConfig.SaveSlotKey;
                _engineSavePath = GetEngineSavePath();
                _loadPersistentSaveOnStart = false;
                _pendingSaveExport = false;
                _pendingSaveExportTicks = 0;
                _loadedTotalPlaySeconds = 0;
                _sessionPlaySeconds = 0f;
                if (!string.IsNullOrWhiteSpace(_saveSlotKey) &&
                    DoomPersistentSaveStore.TryLoadSummary(_saveSlotKey, out var loadedSummary))
                {
                    _loadedTotalPlaySeconds = Mathf.Max(0, loadedSummary.TotalPlaySeconds);
                    _persistentTotalChips = Mathf.Max(0, loadedSummary.TotalChips);
                }
                else
                {
                    _persistentTotalChips = 0;
                }
                if (launchConfig.LoadExistingSave && !string.IsNullOrWhiteSpace(_saveSlotKey))
                {
                    if (DoomPersistentSaveStore.TryImportToEngineSlot(_saveSlotKey, _engineSavePath, out var importError))
                    {
                        _loadPersistentSaveOnStart = true;
                        _logger.LogInfo("[JustDoomIt] Imported persistent save for key=" + _saveSlotKey);
                    }
                    else if (!string.IsNullOrWhiteSpace(importError))
                    {
                        _logger.LogWarning("[JustDoomIt] Failed to import persistent save: " + importError);
                    }
                }

                _frame = new Color32[_video.Width * _video.Height];
                _stats = default;
                _pendingWeaponInput.Clear();
                _weaponCyclePlanner.Reset(0);
                SyncWeaponPlannerFromGameState();
                _killStreak = 0;
                _mapKillCount = 0;
                _lastEpisode = -1;
                _lastMap = -1;
                _lastGameState = GameState.Level;
                _lastHealth = -1;
                _lastDamageCount = -1;
                _lastSecretCount = -1;
                _lastPlayerState = default;
                _hasLastPlayerState = false;
                _orderedEvents.Clear();
                _running = true;
                logger.LogInfo("[JustDoomIt] Managed Doom initialized. saveKey=" + (_saveSlotKey ?? "(none)"));
                return true;
            }
            catch (System.Exception ex)
            {
                logger.LogError("[JustDoomIt] Managed Doom initialization failed: " + ex);
                Shutdown();
                return false;
            }
        }

        public void SubmitInput(DoomInputState input)
        {
            _currentInput = input;
            _pendingWeaponInput.Capture(ExtractWeaponSlot(input), input.WeaponCycleSteps);
        }

        public void PrimeSessionState()
        {
            if (!_running || _doom == null || _video == null)
            {
                return;
            }

            try
            {
                EnsurePersistentSaveLoaded();
                CaptureCurrentState(queueLevelStart: true);
                _video.Render(_doom, Fixed.Zero);
                _video.CopyFrame(_frame);
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] ManagedDoomBackend.PrimeSessionState failed.", ex);
                _running = false;
            }
        }

        public bool TryDequeueEvent(out DoomBackendEvent backendEvent)
        {
            if (_orderedEvents.Count > 0)
            {
                backendEvent = _orderedEvents.Dequeue();
                return true;
            }

            backendEvent = default;
            return false;
        }

        public void Tick(float deltaTime)
        {
            if (!_running || _doom == null || _video == null)
            {
                return;
            }

            try
            {
                EnsurePersistentSaveLoaded();

                _frameCount++;

                if (_frameCount % _fpsScale == 0)
                {
                    // One-shot inputs must survive until an actual DOOM simulation step runs.
                    ApplyPendingWeaponInputForTick();
                    if (_doom.Update() == UpdateResult.Completed)
                    {
                        _running = false;
                        return;
                    }
                }

                var frameFrac = Fixed.FromInt(_frameCount % _fpsScale + 1) / _fpsScale;
                _video.Render(_doom, frameFrac);
                _video.CopyFrame(_frame);
                _sessionPlaySeconds += Mathf.Max(0f, deltaTime);
                UpdateRunStats();
                UpdatePendingPersistentSaveExport();
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] ManagedDoomBackend.Tick failed.", ex);
                _running = false;
            }
        }

        public Color32[] GetFrameBuffer() => _frame;

        public void SavePersistentCheckpoint()
        {
            if (!_running || _doom == null || string.IsNullOrWhiteSpace(_saveSlotKey))
            {
                return;
            }

            try
            {
                if (_doom.SaveGame(0, "JustDoomIt"))
                {
                    _pendingSaveExport = true;
                    _pendingSaveExportTicks = 2;
                }
            }
            catch (System.Exception ex)
            {
                _logger?.LogWarning("[JustDoomIt] SavePersistentCheckpoint failed: " + ex.Message);
            }
        }

        public void SavePersistentNow()
        {
            if (string.IsNullOrWhiteSpace(_saveSlotKey))
            {
                return;
            }

            try
            {
                if (_running && _doom != null)
                {
                    _doom.SaveGame(0, "JustDoomIt");
                    // Flush game action quickly before shutdown.
                    for (var i = 0; i < 3; i++)
                    {
                        if (_doom.Update() == UpdateResult.Completed)
                        {
                            break;
                        }
                    }
                }

                ExportPersistentSaveImmediately();
            }
            catch (System.Exception ex)
            {
                _logger?.LogWarning("[JustDoomIt] SavePersistentNow failed: " + ex.Message);
            }
        }

        public void Shutdown()
        {
            _running = false;
            _pendingWeaponInput.Clear();
            _doom = null;
            _video = null;
            _input = null;
            _frame = null;
            _sound?.Dispose();
            _sound = null;
            _content?.Dispose();
            _content = null;
            _stats = default;
            _saveSlotKey = null;
            _engineSavePath = null;
            _loadPersistentSaveOnStart = false;
            _pendingSaveExport = false;
            _pendingSaveExportTicks = 0;
            _loadedTotalPlaySeconds = 0;
            _sessionPlaySeconds = 0f;
            _killStreak = 0;
            _mapKillCount = 0;
            _lastEpisode = -1;
            _lastMap = -1;
            _lastGameState = GameState.Level;
            _lastHealth = -1;
            _lastDamageCount = -1;
            _lastSecretCount = -1;
            _lastPlayerState = default;
            _hasLastPlayerState = false;
            _persistentTotalChips = 0;
            _orderedEvents.Clear();
        }

        private void ApplyPendingWeaponInputForTick()
        {
            _pendingWeaponInput.ConsumeOneTick(out var weaponSlot, out var weaponCycleSteps);
            if (weaponSlot >= 1 && weaponSlot <= 7)
            {
                ApplyWeaponSlotForTick(_weaponCyclePlanner.PlanDirectSlot(weaponSlot));
                return;
            }

            if (weaponCycleSteps != 0)
            {
                ApplyWeaponSlotForTick(_weaponCyclePlanner.PlanCycleStep(weaponCycleSteps));
                return;
            }

            ApplyWeaponSlotForTick(0);
        }

        private static int ExtractWeaponSlot(DoomInputState input)
        {
            if (input.Weapon1) return 1;
            if (input.Weapon2) return 2;
            if (input.Weapon3) return 3;
            if (input.Weapon4) return 4;
            if (input.Weapon5) return 5;
            if (input.Weapon6) return 6;
            if (input.Weapon7) return 7;
            return 0;
        }

        private static int ToWeaponSlot(WeaponType weapon)
        {
            switch (weapon)
            {
                case WeaponType.Fist:
                case WeaponType.Chainsaw:
                    return 1;
                case WeaponType.Pistol:
                    return 2;
                case WeaponType.Shotgun:
                case WeaponType.SuperShotgun:
                    return 3;
                case WeaponType.Chaingun:
                    return 4;
                case WeaponType.Missile:
                    return 5;
                case WeaponType.Plasma:
                    return 6;
                case WeaponType.Bfg:
                    return 7;
                default:
                    return 1;
            }
        }

        private void ApplyWeaponSlotForTick(int weaponSlot)
        {
            _currentInput.Weapon1 = weaponSlot == 1;
            _currentInput.Weapon2 = weaponSlot == 2;
            _currentInput.Weapon3 = weaponSlot == 3;
            _currentInput.Weapon4 = weaponSlot == 4;
            _currentInput.Weapon5 = weaponSlot == 5;
            _currentInput.Weapon6 = weaponSlot == 6;
            _currentInput.Weapon7 = weaponSlot == 7;
            _currentInput.WeaponCycleSteps = 0;
        }

        private void SyncWeaponPlannerFromGameState()
        {
            var readyWeapon = _doom?.Game?.World?.ConsolePlayer?.ReadyWeapon;
            if (readyWeapon.HasValue)
            {
                _weaponCyclePlanner.SyncActualReady(ToWeaponSlot(readyWeapon.Value));
            }
        }

        private void EnsurePersistentSaveLoaded()
        {
            if (!_loadPersistentSaveOnStart || _doom == null)
            {
                return;
            }

            _doom.LoadGame(0);
            _loadPersistentSaveOnStart = false;
            SyncWeaponPlannerFromGameState();
        }

        private void UpdatePendingPersistentSaveExport()
        {
            if (!_pendingSaveExport)
            {
                return;
            }

            if (_pendingSaveExportTicks > 0)
            {
                _pendingSaveExportTicks--;
                return;
            }

            ExportPersistentSaveImmediately();
        }

        private void ExportPersistentSaveImmediately()
        {
            _pendingSaveExport = false;
            _pendingSaveExportTicks = 0;
            if (string.IsNullOrWhiteSpace(_saveSlotKey) || string.IsNullOrWhiteSpace(_engineSavePath))
            {
                return;
            }

            var exported = DoomPersistentSaveStore.TryExportFromEngineSlot(_saveSlotKey, _engineSavePath, out var exportError);
            if (!exported && !string.IsNullOrWhiteSpace(exportError))
            {
                _logger?.LogWarning("[JustDoomIt] Failed to export persistent save: " + exportError);
            }

            if (exported)
            {
                var summary = BuildSaveSummary();
                if (!DoomPersistentSaveStore.TryStoreSummary(_saveSlotKey, summary, out var metaError) &&
                    !string.IsNullOrWhiteSpace(metaError))
                {
                    _logger?.LogWarning("[JustDoomIt] Failed to write save summary: " + metaError);
                }

                _logger?.LogInfo("[JustDoomIt] Exported persistent save for key=" + _saveSlotKey);
            }
        }

        private static string GetEngineSavePath()
        {
            var exeDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty) ?? string.Empty;
            return Path.Combine(exeDir, "doomsav0.dsg");
        }

        private static GameSkill ToGameSkill(int skill)
        {
            switch (Mathf.Clamp(skill, 1, 5))
            {
                case 1: return GameSkill.Baby;
                case 2: return GameSkill.Easy;
                case 3: return GameSkill.Medium;
                case 4: return GameSkill.Hard;
                case 5: return GameSkill.Nightmare;
                default: return GameSkill.Medium;
            }
        }

        private static int ToSkillNumber(GameSkill skill)
        {
            switch (skill)
            {
                case GameSkill.Baby: return 1;
                case GameSkill.Easy: return 2;
                case GameSkill.Medium: return 3;
                case GameSkill.Hard: return 4;
                case GameSkill.Nightmare: return 5;
                default: return 3;
            }
        }

        private void UpdateRunStats()
        {
            var game = _doom?.Game;
            var world = game?.World;
            var player = world?.ConsolePlayer;
            if (game == null || world == null || player == null)
            {
                return;
            }

            _input?.SetObservedWeapon(player.ReadyWeapon);
            _weaponCyclePlanner.SyncActualReady(ToWeaponSlot(player.ReadyWeapon));
            ApplyInvincibility(player);

            var episode = game.Options?.Episode ?? 1;
            var map = game.Options?.Map ?? 1;
            var skill = ToSkillNumber(game.Options?.Skill ?? GameSkill.Medium);
            var playerState = player.PlayerState;
            var mapChanged = episode != _lastEpisode || map != _lastMap;
            if (mapChanged)
            {
                _lastEpisode = episode;
                _lastMap = map;
                _killStreak = 0;
                _mapKillCount = 0;
                _lastHealth = player.Health;
                _lastDamageCount = player.DamageCount;
                _lastSecretCount = player.SecretCount;
            }

            if (_lastHealth < 0)
            {
                _lastHealth = player.Health;
            }

            if (_lastDamageCount < 0)
            {
                _lastDamageCount = player.DamageCount;
            }

            if (_lastSecretCount < 0)
            {
                _lastSecretCount = player.SecretCount;
            }

            if (game.State == GameState.Level &&
                playerState == PlayerState.Live &&
                (mapChanged || _lastGameState != GameState.Level || !_hasLastPlayerState || _lastPlayerState != PlayerState.Live))
            {
                EnqueueMapStartEvent(episode, map, skill, world.Map?.Title ?? string.Empty);
            }

            var invincible = ModConfig.InvincibleMode != null && ModConfig.InvincibleMode.Value;
            var tookDamage = !invincible &&
                (player.Health < _lastHealth || player.DamageCount > _lastDamageCount);
            if (playerState == PlayerState.Dead && (!_hasLastPlayerState || _lastPlayerState != PlayerState.Dead))
            {
                _killStreak = 0;
                _stats.CurrentKillStreak = 0;
                EnqueueDeathEvent(new DoomDeathEvent
                {
                    Episode = episode,
                    Map = map,
                    MapCode = "E" + episode + "M" + map,
                    MapTitle = world.Map?.Title ?? string.Empty
                });
            }
            else if (tookDamage)
            {
                _killStreak = 0;
                _stats.CurrentKillStreak = 0;
                EnqueueDamageEvent(new DoomDamageEvent
                {
                    Health = player.Health,
                    Armor = player.ArmorPoints,
                    MapCode = "E" + episode + "M" + map,
                    MapTitle = world.Map?.Title ?? string.Empty
                });
            }

            _lastHealth = player.Health;
            _lastDamageCount = player.DamageCount;
            _lastPlayerState = playerState;
            _hasLastPlayerState = true;

            if (player.SecretCount > _lastSecretCount)
            {
                EnqueueSecretEvent(new DoomSecretEvent
                {
                    Count = player.SecretCount - _lastSecretCount,
                    SecretCount = player.SecretCount,
                    TotalSecrets = world.TotalSecrets,
                    MapCode = "E" + episode + "M" + map,
                    MapTitle = world.Map?.Title ?? string.Empty
                });
            }
            _lastSecretCount = player.SecretCount;

            while (DoomKillFeed.TryDequeueEnemy(out var enemyName))
            {
                _killStreak++;
                _mapKillCount++;
                _stats.TotalKills++;
                _stats.CurrentKillStreak = _killStreak;
                if (_killStreak > _stats.MaxKillStreak)
                {
                    _stats.MaxKillStreak = _killStreak;
                }

                EnqueueKillEvent(new DoomKillEvent
                {
                    TotalKills = _stats.TotalKills,
                    CurrentKillStreak = _killStreak,
                    Enemy = enemyName ?? "Unknown",
                    Health = player.Health,
                    Armor = player.ArmorPoints,
                    Weapon = GetWeaponName(player.ReadyWeapon),
                    MapCode = "E" + episode + "M" + map,
                    MapTitle = world.Map?.Title ?? "",
                    MapKills = _mapKillCount,
                    MapKillTotal = world.TotalKills
                });
            }

            if (_lastGameState == GameState.Level && game.State == GameState.Intermission)
            {
                _stats.ClearEventCount++;
                if (IsBossMap(_lastMap))
                {
                    _stats.BossClearEventCount++;
                }
                _killStreak = 0;
                _mapKillCount = 0;
                _stats.CurrentKillStreak = 0;
            }

            _lastGameState = game.State;
        }

        private void CaptureCurrentState(bool queueLevelStart)
        {
            var game = _doom?.Game;
            var world = game?.World;
            var player = world?.ConsolePlayer;
            if (game == null || world == null || player == null)
            {
                return;
            }

            _input?.SetObservedWeapon(player.ReadyWeapon);
            _weaponCyclePlanner.SyncActualReady(ToWeaponSlot(player.ReadyWeapon));
            ApplyInvincibility(player);

            var episode = game.Options?.Episode ?? 1;
            var map = game.Options?.Map ?? 1;
            var skill = ToSkillNumber(game.Options?.Skill ?? GameSkill.Medium);
            _lastEpisode = episode;
            _lastMap = map;
            _lastHealth = player.Health;
            _lastDamageCount = player.DamageCount;
            _lastSecretCount = player.SecretCount;
            _lastGameState = game.State;
            _lastPlayerState = player.PlayerState;
            _hasLastPlayerState = true;
            _killStreak = 0;
            _mapKillCount = 0;
            _stats.CurrentKillStreak = 0;

            if (queueLevelStart && game.State == GameState.Level && player.PlayerState == PlayerState.Live)
            {
                EnqueueMapStartEvent(episode, map, skill, world.Map?.Title ?? string.Empty);
            }
        }

        private void EnqueueMapStartEvent(int episode, int map, int skill, string mapTitle)
        {
            _orderedEvents.Enqueue(DoomBackendEvent.FromMapStart(new DoomMapStartEvent
            {
                Episode = episode,
                Map = map,
                Skill = Mathf.Clamp(skill, 1, 5),
                MapCode = "E" + episode + "M" + map,
                MapTitle = mapTitle ?? string.Empty
            }));
        }

        private void EnqueueDamageEvent(DoomDamageEvent damageEvent)
        {
            _orderedEvents.Enqueue(DoomBackendEvent.FromDamage(damageEvent));
        }

        private void EnqueueDeathEvent(DoomDeathEvent deathEvent)
        {
            _orderedEvents.Enqueue(DoomBackendEvent.FromDeath(deathEvent));
        }

        private void EnqueueKillEvent(DoomKillEvent killEvent)
        {
            _orderedEvents.Enqueue(DoomBackendEvent.FromKill(killEvent));
        }

        private void EnqueueSecretEvent(DoomSecretEvent secretEvent)
        {
            _orderedEvents.Enqueue(DoomBackendEvent.FromSecret(secretEvent));
        }

        private DoomSaveSummary BuildSaveSummary()
        {
            var sessionSeconds = Mathf.Max(0, Mathf.RoundToInt(_sessionPlaySeconds));
            var totalSeconds = Mathf.Max(0, _loadedTotalPlaySeconds + sessionSeconds);
            return new DoomSaveSummary
            {
                SavedUtcTicks = DateTime.UtcNow.Ticks,
                TotalPlaySeconds = totalSeconds,
                LastSessionSeconds = sessionSeconds,
                TotalKills = _stats.TotalKills,
                MaxKillStreak = _stats.MaxKillStreak,
                TotalChips = _persistentTotalChips
            };
        }

        private static void ApplyInvincibility(DoomNetFrameworkEngine.DoomEntity.Game.Player player)
        {
            if (player == null || ModConfig.InvincibleMode == null)
            {
                return;
            }

            if (ModConfig.InvincibleMode.Value)
            {
                player.Cheats |= CheatFlags.GodMode;
            }
            else
            {
                player.Cheats &= ~CheatFlags.GodMode;
            }
        }

        private static bool IsBossMap(int map)
        {
            if (map <= 0)
            {
                return false;
            }

            // Doom-format episodes place bosses mainly on M8 (and optional secret finale on M9).
            return map == 8 || map == 9;
        }

        private static string GetWeaponName(WeaponType weapon)
        {
            switch (weapon)
            {
                case WeaponType.Fist: return "Fist";
                case WeaponType.Pistol: return "Pistol";
                case WeaponType.Shotgun: return "Shotgun";
                case WeaponType.Chaingun: return "Chaingun";
                case WeaponType.Missile: return "Rocket";
                case WeaponType.Plasma: return "Plasma";
                case WeaponType.Bfg: return "BFG";
                case WeaponType.Chainsaw: return "Chainsaw";
                case WeaponType.SuperShotgun: return "Super Shotgun";
                default: return weapon.ToString();
            }
        }
    }

    internal sealed class ManagedDoomVideo : IVideo
    {
        private readonly DoomNetFrameworkEngine.Video.Renderer _renderer;
        private readonly byte[] _frameBytes;

        public ManagedDoomVideo(Config config, GameContent content)
        {
            _renderer = new DoomNetFrameworkEngine.Video.Renderer(config, content);
            _frameBytes = new byte[_renderer.Width * _renderer.Height * 4];
        }

        public int Width => _renderer.Width;
        public int Height => _renderer.Height;

        public void Render(Doom doom, Fixed frameFrac)
        {
            _renderer.Render(doom, _frameBytes, frameFrac);
        }

        public void CopyFrame(Color32[] destination)
        {
            if (destination == null || destination.Length != Width * Height)
            {
                return;
            }

            // Renderer writes RGBA bytes in column-major order (x * Height + y).
            // Unity expects Color32 in row-major order (y * Width + x), RGBA.
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var srcPixel = x * Height + y;
                    var src = srcPixel * 4;
                    var dst = (Height - 1 - y) * Width + x;

                    var r = _frameBytes[src];
                    var g = _frameBytes[src + 1];
                    var b = _frameBytes[src + 2];
                    var a = _frameBytes[src + 3];

                    destination[dst] = new Color32(r, g, b, a);
                }
            }
        }

        public void InitializeWipe() => _renderer.InitializeWipe();
        public bool HasFocus() => true;
        public int MaxWindowSize => _renderer.MaxWindowSize;
        public int WindowSize { get => _renderer.WindowSize; set => _renderer.WindowSize = value; }
        public bool DisplayMessage { get => _renderer.DisplayMessage; set => _renderer.DisplayMessage = value; }
        public int MaxGammaCorrectionLevel => _renderer.MaxGammaCorrectionLevel;
        public int GammaCorrectionLevel { get => _renderer.GammaCorrectionLevel; set => _renderer.GammaCorrectionLevel = value; }
        public int WipeBandCount => _renderer.WipeBandCount;
        public int WipeHeight => _renderer.WipeHeight;
    }

    internal sealed class ManagedDoomInput : IUserInput
    {
        private readonly System.Func<DoomInputState> _stateProvider;
        private readonly Config _config;
        private int _turnHeld;
        private int _observedWeaponSlot = 1;

        public ManagedDoomInput(System.Func<DoomInputState> stateProvider, Config config)
        {
            _stateProvider = stateProvider;
            _config = config;
        }

        public void BuildTicCmd(TicCmd cmd)
        {
            var s = _stateProvider();
            cmd.Clear();

            // In this mod, Shift should consistently mean "run".
            var speed = s.Run ? 1 : 0;

            if (s.TurnLeft || s.TurnRight)
            {
                _turnHeld++;
            }
            else
            {
                _turnHeld = 0;
            }

            var turnSpeed = _turnHeld < PlayerBehavior.SlowTurnTics ? 2 : speed;
            if (s.TurnRight)
            {
                cmd.AngleTurn -= (short)PlayerBehavior.AngleTurn[turnSpeed];
            }
            if (s.TurnLeft)
            {
                cmd.AngleTurn += (short)PlayerBehavior.AngleTurn[turnSpeed];
            }

            if (Mathf.Abs(s.MouseDeltaX) > 0.001f)
            {
                var mouseTurn = Mathf.RoundToInt(s.MouseDeltaX * ModConfig.MouseTurnSensitivity.Value * 256f);
                mouseTurn = Mathf.Clamp(mouseTurn, -8192, 8192);
                cmd.AngleTurn -= (short)mouseTurn;
            }

            var forward = 0;
            var side = 0;

            if (s.MoveForward) forward += PlayerBehavior.ForwardMove[speed];
            if (s.MoveBackward) forward -= PlayerBehavior.ForwardMove[speed];
            if (s.StrafeRight) side += PlayerBehavior.SideMove[speed];
            if (s.StrafeLeft) side -= PlayerBehavior.SideMove[speed];

            forward = Mathf.Clamp(forward, -PlayerBehavior.MaxMove, PlayerBehavior.MaxMove);
            side = Mathf.Clamp(side, -PlayerBehavior.MaxMove, PlayerBehavior.MaxMove);

            cmd.ForwardMove += (sbyte)forward;
            cmd.SideMove += (sbyte)side;

            if (s.Fire)
            {
                cmd.Buttons |= TicCmdButtons.Attack;
            }
            if (s.Use)
            {
                cmd.Buttons |= TicCmdButtons.Use;
            }

            var weapon = GetWeaponIndex(s);
            if (weapon >= 0)
            {
                cmd.Buttons |= TicCmdButtons.Change;
                cmd.Buttons |= (byte)(weapon << TicCmdButtons.WeaponShift);
            }
        }

        public void SetObservedWeapon(WeaponType weapon)
        {
            _observedWeaponSlot = ToWeaponSlot(weapon);
        }

        private int GetWeaponIndex(DoomInputState s)
        {
            if (s.Weapon1) return 0;
            if (s.Weapon2) return 1;
            if (s.Weapon3) return 2;
            if (s.Weapon4) return 3;
            if (s.Weapon5) return 4;
            if (s.Weapon6) return 5;
            if (s.Weapon7) return 6;

            if (s.WeaponCycleSteps != 0)
            {
                var dir = s.WeaponCycleSteps > 0 ? 1 : -1;
                _observedWeaponSlot = WrapWeaponSlot(_observedWeaponSlot + dir);
                return _observedWeaponSlot - 1;
            }

            return -1;
        }

        private static int WrapWeaponSlot(int slot)
        {
            while (slot < 1)
            {
                slot += 7;
            }

            while (slot > 7)
            {
                slot -= 7;
            }

            return slot;
        }

        private static int ToWeaponSlot(WeaponType weapon)
        {
            switch (weapon)
            {
                case WeaponType.Fist:
                case WeaponType.Chainsaw:
                    return 1;
                case WeaponType.Pistol:
                    return 2;
                case WeaponType.Shotgun:
                case WeaponType.SuperShotgun:
                    return 3;
                case WeaponType.Chaingun:
                    return 4;
                case WeaponType.Missile:
                    return 5;
                case WeaponType.Plasma:
                    return 6;
                case WeaponType.Bfg:
                    return 7;
                default:
                    return 1;
            }
        }

        public void Reset()
        {
        }

        public void GrabMouse()
        {
        }

        public void ReleaseMouse()
        {
        }

        public int MaxMouseSensitivity => 15;

        public int MouseSensitivity
        {
            get => _config.mouse_sensitivity;
            set => _config.mouse_sensitivity = value;
        }
    }
}

