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
        public int Reward;
        public string Enemy;
        public int Health;
        public int Armor;
        public string Weapon;
        public string MapCode;
        public string MapTitle;
        public int MapKills;
        public int MapKillTotal;
    }

    public struct DoomRunStats
    {
        public int TotalKills;
        public int MaxKillStreak;
        public int CurrentKillStreak;
        public int KillChipPayout;
        public int ClearEventCount;
        public int BossClearEventCount;
    }

    public interface IDoomBackend
    {
        int Width { get; }
        int Height { get; }
        bool IsRunning { get; }
        DoomRunStats Stats { get; }
        bool Initialize(DoomLaunchConfig launchConfig, ManualLogSource logger);
        void SubmitInput(DoomInputState input);
        bool TryDequeueKillEvent(out DoomKillEvent killEvent);
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
        private readonly Queue<DoomKillEvent> _killEvents = new Queue<DoomKillEvent>();

        public int Width => _video?.Width ?? Mathf.Max(160, _requestedWidth);
        public int Height => _video?.Height ?? Mathf.Max(100, _requestedHeight);
        public bool IsRunning => _running;
        public DoomRunStats Stats => _stats;

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
                _killStreak = 0;
                _mapKillCount = 0;
                _lastEpisode = -1;
                _lastMap = -1;
                _lastGameState = GameState.Level;
                _lastHealth = -1;
                _lastDamageCount = -1;
                _killEvents.Clear();
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
        }

        public bool TryDequeueKillEvent(out DoomKillEvent killEvent)
        {
            if (_killEvents.Count > 0)
            {
                killEvent = _killEvents.Dequeue();
                return true;
            }

            killEvent = default;
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
                if (_loadPersistentSaveOnStart)
                {
                    _doom.LoadGame(0);
                    _loadPersistentSaveOnStart = false;
                }

                _frameCount++;

                if (_frameCount % _fpsScale == 0)
                {
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
            _killEvents.Clear();
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
            ApplyInvincibility(player);

            var episode = game.Options?.Episode ?? 1;
            var map = game.Options?.Map ?? 1;
            var mapChanged = episode != _lastEpisode || map != _lastMap;
            if (mapChanged)
            {
                _lastEpisode = episode;
                _lastMap = map;
                _killStreak = 0;
                _mapKillCount = 0;
                _lastHealth = player.Health;
                _lastDamageCount = player.DamageCount;
            }

            if (_lastHealth < 0)
            {
                _lastHealth = player.Health;
            }

            if (_lastDamageCount < 0)
            {
                _lastDamageCount = player.DamageCount;
            }

            var invincible = ModConfig.InvincibleMode != null && ModConfig.InvincibleMode.Value;
            var tookDamage = !invincible &&
                (player.Health < _lastHealth || player.DamageCount > _lastDamageCount);
            if (tookDamage)
            {
                _killStreak = 0;
                _stats.CurrentKillStreak = 0;
            }

            _lastHealth = player.Health;
            _lastDamageCount = player.DamageCount;

            while (DoomKillFeed.TryDequeueEnemy(out var enemyName))
            {
                _killStreak++;
                _mapKillCount++;
                var reward = GetKillRewardByStreak(_killStreak);
                _stats.TotalKills++;
                _stats.KillChipPayout += reward;
                _stats.CurrentKillStreak = _killStreak;
                if (_killStreak > _stats.MaxKillStreak)
                {
                    _stats.MaxKillStreak = _killStreak;
                }

                _killEvents.Enqueue(new DoomKillEvent
                {
                    TotalKills = _stats.TotalKills,
                    CurrentKillStreak = _killStreak,
                    Reward = reward,
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
                TotalChips = _stats.KillChipPayout
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

        private static int GetKillRewardByStreak(int streak)
        {
            if (streak <= 1)
            {
                return 100;
            }

            // Use integer doubling with an explicit cap to avoid overflow on high streaks.
            var reward = 100;
            for (var i = 1; i < streak; i++)
            {
                if (reward >= 5000)
                {
                    return 5000;
                }

                reward *= 2;
            }

            return Mathf.Clamp(reward, 100, 5000);
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

