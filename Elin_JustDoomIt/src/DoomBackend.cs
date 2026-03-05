using BepInEx.Logging;
using DoomNetFrameworkEngine;
using DoomNetFrameworkEngine.Audio;
using DoomNetFrameworkEngine.DoomEntity;
using DoomNetFrameworkEngine.DoomEntity.Game;
using DoomNetFrameworkEngine.DoomEntity.MathUtils;
using DoomNetFrameworkEngine.DoomEntity.World;
using DoomNetFrameworkEngine.UserInput;
using DoomNetFrameworkEngine.Video;
using System.Collections.Generic;
using UnityEngine;

namespace Elin_ModTemplate
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
        bool Initialize(string wadPath, ManualLogSource logger);
        void SubmitInput(DoomInputState input);
        bool TryDequeueKillEvent(out DoomKillEvent killEvent);
        void SetSfxDucking(bool ducked);
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
        private int _lastKillCount = -1;
        private int _killStreak;
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

        public bool Initialize(string wadPath, ManualLogSource logger)
        {
            try
            {
                var args = new CommandLineArgs(new[]
                {
                    "-iwad", wadPath,
                    "-warp", "1", "1",
                    "-skill", "3",
                    "-nomusic"
                });

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
                _sound = new UnityDoomSound(wadPath, config.audio_soundvolume);

                _doom = new Doom(
                    args,
                    config,
                    _content,
                    _video,
                    _sound,
                    NullMusic.GetInstance(),
                    _input);

                _doom.NewGame(GameSkill.Medium, 1, 1);

                _frame = new Color32[_video.Width * _video.Height];
                _stats = default;
                _lastKillCount = -1;
                _killStreak = 0;
                _lastEpisode = -1;
                _lastMap = -1;
                _lastGameState = GameState.Level;
                _lastHealth = -1;
                _lastDamageCount = -1;
                _killEvents.Clear();
                _running = true;
                logger.LogInfo("[JustDoomIt] Managed Doom initialized.");
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

        public void SetSfxDucking(bool ducked)
        {
            if (_sound == null)
            {
                return;
            }

            _sound.ExternalVolumeScale = ducked ? 0.10f : 1f;
        }

        public void Tick(float deltaTime)
        {
            if (!_running || _doom == null || _video == null)
            {
                return;
            }

            try
            {
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
                UpdateRunStats();
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] ManagedDoomBackend.Tick failed.", ex);
                _running = false;
            }
        }

        public Color32[] GetFrameBuffer() => _frame;

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
            _lastKillCount = -1;
            _killStreak = 0;
            _lastEpisode = -1;
            _lastMap = -1;
            _lastGameState = GameState.Level;
            _lastHealth = -1;
            _lastDamageCount = -1;
            _killEvents.Clear();
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
                _lastKillCount = player.KillCount;
                _killStreak = 0;
                _lastHealth = player.Health;
                _lastDamageCount = player.DamageCount;
            }
            else if (_lastKillCount < 0)
            {
                _lastKillCount = player.KillCount;
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
            }

            _lastHealth = player.Health;
            _lastDamageCount = player.DamageCount;

            var currentKillCount = player.KillCount;
            if (currentKillCount > _lastKillCount)
            {
                var gained = currentKillCount - _lastKillCount;
                for (var i = 0; i < gained; i++)
                {
                    _killStreak++;
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
                        Enemy = DoomKillFeed.TryDequeueEnemy(out var enemyName) ? enemyName : "Unknown",
                        Health = player.Health,
                        Armor = player.ArmorPoints,
                        Weapon = GetWeaponName(player.ReadyWeapon),
                        MapCode = "E" + episode + "M" + map,
                        MapTitle = world.Map?.Title ?? "",
                        MapKills = player.KillCount,
                        MapKillTotal = world.TotalKills
                    });
                }
            }
            _lastKillCount = currentKillCount;

            if (_lastGameState == GameState.Level && game.State == GameState.Intermission)
            {
                _stats.ClearEventCount++;
                if (IsBossMap(_lastMap))
                {
                    _stats.BossClearEventCount++;
                }
                _killStreak = 0;
                _stats.CurrentKillStreak = 0;
            }

            _lastGameState = game.State;
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

            // Renderer writes BGRA bytes in column-major order (x * Height + y).
            // Unity expects Color32 in row-major order (y * Width + x), RGBA.
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var srcPixel = x * Height + y;
                    var src = srcPixel * 4;
                    var dst = (Height - 1 - y) * Width + x;

                    var b = _frameBytes[src];
                    var g = _frameBytes[src + 1];
                    var r = _frameBytes[src + 2];
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
