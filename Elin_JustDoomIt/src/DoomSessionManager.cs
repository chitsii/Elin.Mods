using BepInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Elin_JustDoomIt
{
    public sealed class DoomSessionManager : MonoBehaviour
    {
        private const float KillVoiceDelaySeconds = 0.18f;
        private const float DoomTickStep = 1f / 60f;
        private const int MaxDoomTicksPerFrame = 8;
        private const float GlobalStatsFlushIntervalSeconds = 15f;
        private const string CasinoCoinId = "casino_coin";

        private struct KillVoiceRequest
        {
            public int ClipIndex;
            public float ReadyAt;
        }

        private static readonly string[] StartHypeLinesEn =
        {
            "Wake up. Lock in. Clear the room.",
            "No mercy run starts now.",
            "Steel nerves, steady aim, full send.",
            "Push forward and do not stop.",
            "One cabinet. One legend. Go.",
            "Crank the pressure. Own the run.",
            "Frame perfect or flame out. Fight.",
            "You are live. Make this run count."
        };
        private static readonly string[] StartHypeLinesJp =
        {
            "目を覚ませ。集中しろ。敵を掃討だ。",
            "容赦なしのランが始まる。",
            "神経を研ぎ澄ませ。照準はぶらすな。",
            "前進あるのみ。止まるな。",
            "筐体ひとつ。伝説ひとつ。行け。",
            "圧を上げろ。このランを支配しろ。",
            "一瞬の判断が明暗を分ける。戦え。",
            "本番開始だ。この一走に刻め。"
        };
        private static readonly string[] StartHypeLinesCn =
        {
            "醒来，锁定目标，清空全场。",
            "无情模式，现在开局。",
            "稳住神经，准星别抖，狠狠干。",
            "只管向前，别停下。",
            "一台机器，一段传奇，出发。",
            "把压力拉满，主宰这一局。",
            "一帧见生死，狠狠干到底。",
            "实战开始，让这把载入史册。"
        };
        private static readonly string[] DoomBgmIds =
        {
            "BGM/doom_themed_alien",
            "BGM/doom_themed_boss",
            "BGM/doom_themed_hell",
            "BGM/doom_themed_industrial",
            "BGM/doom_themed_labo"
        };

        public static DoomSessionManager Instance { get; private set; }

        private ManualLogSource _logger;
        private IDoomBackend _backend;
        private DoomOverlayDisplay _overlay;
        private bool _active;
        private AudioSource _killVoiceSource;
        private readonly Queue<KillVoiceRequest> _killVoiceQueue = new Queue<KillVoiceRequest>();
        private readonly List<AudioClip> _killVoiceClips = new List<AudioClip>();
        private bool _killVoiceLoading;
        private bool _killVoiceLoaded;
        private int _nextDoomBgmIndex;
        private bool _doomBgmActive;
        private float _nextDoomBgmRetryAt;
        private const float KillVoiceSampleGain = 2.6f;
        private int _nextKillVoiceIndex;
        private int _processedKillCount;
        private int _processedKillPayout;
        private int _processedClearEvents;
        private int _processedBossClearEvents;
        private int _sessionCoinsEarned;
        private int _lastAnnouncedStreak;
        private int _lastPopupStreak;
        private int _lastKillReward;
        private bool _exitConfirmOpen;
        private float _doomTickAccumulator;
        private float _globalPlaySecondsAccumulator;
        private float _nextGlobalStatsFlushAt;

        public static void Ensure(ManualLogSource logger)
        {
            if (Instance != null) return;

            var go = new GameObject("JustDoomIt_Session");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<DoomSessionManager>();
            Instance._logger = logger;
            DoomGlobalStatsStore.EnsureLoaded();
        }

        public bool TryHandleMachineUse(Card machine, Chara user, ref bool result)
        {
            try
            {
                if (machine == null)
                {
                    result = false;
                    return true;
                }

                if (_backend != null && _backend.IsRunning)
                {
                    StopSession();
                    result = false;
                    return true;
                }

                if (DoomWadLocator.FindIwads().Count == 0)
                {
                    DoomDiagnostics.Warn("[JustDoomIt] No IWAD found. Keep vanilla machine behavior.");
                    result = false;
                    return false;
                }

                OpenArcadeMenu(user);
                result = false;
                return true;
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] TryHandleTvUse failed.", ex);
                StopSession();
                result = false;
                return false;
            }
        }

        private void OpenArcadeMenu(Chara user)
        {
            EInput.Consume(consumeAxis: true, _skipFrame: 2);
            StartCoroutine(OpenArcadeMenuNextFrame(user));
        }

        private IEnumerator OpenArcadeMenuNextFrame(Chara user)
        {
            yield return null;
            var loadout = DoomWadLocator.LoadRuntimeLoadout();
            var menu = DoomArcadeMenuUI.Create();
            menu.Show(
                loadout,
                user,
                onPlay: (loadExisting) => StartSessionDirect(DoomWadLocator.LoadRuntimeLoadout(), loadExisting),
                onClose: () => { });
        }

        private void StartSessionDirect(DoomRuntimeLoadout loadout, bool loadExisting)
        {
            ModConfig.ReloadRuntimeConfig();
            var launch = DoomWadLocator.BuildLaunchConfig(loadout);
            if (string.IsNullOrWhiteSpace(launch.IwadPath))
            {
                DoomDiagnostics.Warn("[JustDoomIt] No valid IWAD found.");
                EClass.pc?.Say(Localize(
                    "IWADが見つかりません。wad/iwads または wad に配置してください。",
                    "No valid IWAD found. Put WADs in wad/iwads or wad.",
                    "未找到可用IWAD。请放到 wad/iwads 或 wad。"));
                return;
            }

            var resolvedIwadFile = Path.GetFileName(launch.IwadPath);
            var removed = RemoveIncompatibleModsForIwad(loadout, resolvedIwadFile);
            if (removed > 0)
            {
                DoomWadLocator.SaveRuntimeLoadout(loadout);
                EClass.pc?.Say(Localize(
                    "IWAD不一致のMODを " + removed + " 件無効化しました。",
                    "Disabled " + removed + " incompatible mod(s) for this IWAD.",
                    "已禁用 " + removed + " 个与该IWAD不兼容的MOD。"));
                launch = DoomWadLocator.BuildLaunchConfig(loadout);
                if (string.IsNullOrWhiteSpace(launch.IwadPath))
                {
                    DoomDiagnostics.Warn("[JustDoomIt] No valid IWAD found after mod pruning.");
                    return;
                }
            }

            launch.LoadExistingSave = loadExisting;
            var selectedMod = loadout.selectedModId;
            if (!string.IsNullOrWhiteSpace(selectedMod))
            {
                var selectedEntry = DoomWadLocator.FindModEntries().FirstOrDefault(e =>
                    string.Equals(e.EntryId, selectedMod, StringComparison.OrdinalIgnoreCase));
                var family = DoomArcadeMenuUI.GetRequiredIwadFamilyForEntry(selectedEntry);
                if (string.Equals(family, "unknown", StringComparison.OrdinalIgnoreCase))
                {
                    Dialog.YesNo(
                        Localize(
                            "選択中のMODは依存先が不明です。\nこのまま起動しますか？",
                            "Selected MOD dependency is unknown.\nLaunch anyway?",
                            "当前MOD依赖未知。\n仍要启动吗？"),
                        () => StartSessionInternal(launch),
                        () => OpenArcadeMenu(EClass.pc),
                        Localize("起動する", "Launch", "启动"),
                        Localize("戻る", "Back", "返回"));
                    return;
                }
            }

            StartSessionInternal(launch);
        }

        private static int RemoveIncompatibleModsForIwad(DoomRuntimeLoadout loadout, string iwadFile)
        {
            return DoomArcadeMenuUI.RemoveIncompatibleModsForIwad(loadout, iwadFile);
        }

        private void StartSessionInternal(DoomLaunchConfig launch)
        {
            StopSession();
            _overlay = DoomOverlayDisplay.Create();
            _backend = new ManagedDoomBackend(ModConfig.DoomWidth.Value, ModConfig.DoomHeight.Value);
            if (!_backend.Initialize(launch, _logger))
            {
                DoomDiagnostics.Error("[JustDoomIt] Doom backend initialization failed.");
                StopSession();
                return;
            }

            _overlay.Initialize(_backend.Width, _backend.Height);
            _active = true;
            _processedKillCount = 0;
            _processedKillPayout = 0;
            _processedClearEvents = 0;
            _processedBossClearEvents = 0;
            _sessionCoinsEarned = 0;
            _lastAnnouncedStreak = 0;
            _lastPopupStreak = 0;
            _lastKillReward = 0;
            _doomTickAccumulator = 0f;
            _nextGlobalStatsFlushAt = Time.unscaledTime + GlobalStatsFlushIntervalSeconds;
            DoomKillFeed.Reset();
            EnsureKillVoiceSource();
            EnsureKillVoiceClipsLoaded();
            StartDoomBgm();
            SetCursorCaptured(true);
            EInput.Consume(consumeAxis: true, _skipFrame: 2);

            var modCount = launch.PwadPaths != null ? launch.PwadPaths.Count : 0;
            DoomDiagnostics.Info("[JustDoomIt] DOOM session started. IWAD=" + launch.IwadPath + " PWADs=" + modCount);
            LogRandomStartLine();
            LogRewardRules();
        }

        private void Update()
        {
            try
            {
                if (_backend == null || !_backend.IsRunning)
                {
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Escape) || EInput.isCancel)
                {
                    if (_exitConfirmOpen)
                    {
                        _exitConfirmOpen = false;
                        StopSession();
                        EInput.Consume(consumeAxis: true, _skipFrame: 1);
                        return;
                    }

                    RequestExitConfirmation();
                    EInput.Consume(consumeAxis: true, _skipFrame: 1);
                    return;
                }

                if (_exitConfirmOpen)
                {
                    _backend.SubmitInput(default);
                    return;
                }

                if (!_active)
                {
                    _backend.SubmitInput(default);
                    return;
                }

                SetCursorCaptured(true);
                EInput.Consume(consumeAxis: true, _skipFrame: 1);
                var input = DoomInputState.ReadFromUnity();
                _backend.SubmitInput(input);

                _doomTickAccumulator += Time.unscaledDeltaTime;
                if (_doomTickAccumulator > DoomTickStep * MaxDoomTicksPerFrame)
                {
                    _doomTickAccumulator = DoomTickStep * MaxDoomTicksPerFrame;
                }

                var ticks = 0;
                while (_doomTickAccumulator >= DoomTickStep && ticks < MaxDoomTicksPerFrame)
                {
                    _backend.Tick(DoomTickStep);
                    _doomTickAccumulator -= DoomTickStep;
                    ticks++;
                }

                if (ticks > 0)
                {
                    _overlay?.Upload(_backend.GetFrameBuffer());
                    ProcessChipRewards(_backend.Stats);
                    AccumulateGlobalPlaytime(ticks * DoomTickStep);
                }
                UpdateKillVoicePlayback();
                UpdateDoomBgmPlayback();
                FlushGlobalStatsIfNeeded();
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] Session Update failed. Stopping session.", ex);
                StopSession();
            }
        }

        public void StopSession()
        {
            CommitGlobalPlaytimeAndFlush();

            if (_sessionCoinsEarned > 0)
            {
                DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                    "DOOMカジノ報酬: 合計" + _sessionCoinsEarned + "枚のチップを獲得。",
                    "DOOM casino payout: earned " + _sessionCoinsEarned + " chips.",
                    "DOOM赌场奖励：本局共获得" + _sessionCoinsEarned + "枚筹码。"));
            }

            if (_backend != null)
            {
                _backend.SavePersistentNow();
                _backend.Shutdown();
                _backend = null;
            }

            if (_overlay != null)
            {
                Destroy(_overlay.gameObject);
                _overlay = null;
            }

            _active = false;
            SetCursorCaptured(false);
            _processedKillCount = 0;
            _processedKillPayout = 0;
            _processedClearEvents = 0;
            _processedBossClearEvents = 0;
            _sessionCoinsEarned = 0;
            _lastAnnouncedStreak = 0;
            _lastPopupStreak = 0;
            _lastKillReward = 0;
            _doomTickAccumulator = 0f;
            _killVoiceQueue.Clear();
            DoomKillFeed.Reset();
            _exitConfirmOpen = false;
            if (_killVoiceSource != null)
            {
                _killVoiceSource.Stop();
            }
            StopDoomBgm();
        }

        private void RequestExitConfirmation()
        {
            if (_exitConfirmOpen)
            {
                return;
            }

            _exitConfirmOpen = true;
            SetCursorCaptured(false);
            _overlay?.SetVisible(false);
            Dialog.YesNo(
                Localize(
                    "DOOMプレイを停止しますか？",
                    "Stop DOOM play?",
                    "要停止DOOM游玩吗？"),
                () =>
                {
                    _exitConfirmOpen = false;
                    StopSession();
                },
                () =>
                {
                    _exitConfirmOpen = false;
                    _overlay?.SetVisible(true);
                    SetCursorCaptured(true);
                    EInput.Consume(consumeAxis: true, _skipFrame: 1);
                },
                Localize("はい", "Yes", "是"),
                Localize("いいえ", "No", "否"));
        }

        private void OnDestroy()
        {
            StopSession();
            Instance = null;
        }

        private void AccumulateGlobalPlaytime(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            _globalPlaySecondsAccumulator += seconds;
            var whole = Mathf.FloorToInt(_globalPlaySecondsAccumulator);
            if (whole <= 0)
            {
                return;
            }

            _globalPlaySecondsAccumulator -= whole;
            DoomGlobalStatsStore.AddPlaySeconds(whole);
        }

        private void CommitGlobalPlaytimeAndFlush()
        {
            var whole = Mathf.FloorToInt(_globalPlaySecondsAccumulator);
            if (whole > 0)
            {
                _globalPlaySecondsAccumulator -= whole;
                DoomGlobalStatsStore.AddPlaySeconds(whole);
            }

            DoomGlobalStatsStore.Flush();
        }

        private void FlushGlobalStatsIfNeeded()
        {
            if (Time.unscaledTime < _nextGlobalStatsFlushAt)
            {
                return;
            }

            _nextGlobalStatsFlushAt = Time.unscaledTime + GlobalStatsFlushIntervalSeconds;
            DoomGlobalStatsStore.Flush();
        }

        private static void SetCursorCaptured(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }

        private static void LogRandomStartLine()
        {
            var lines = ResolveStartLines();
            if (lines.Length == 0)
            {
                return;
            }

            var line = lines[UnityEngine.Random.Range(0, lines.Length)];
            DoomDiagnostics.Info("[JustDoomIt] " + line);
        }

        private static string[] ResolveStartLines()
        {
            if (Lang.langCode == "CN")
            {
                return StartHypeLinesCn;
            }

            return Lang.isJP ? StartHypeLinesJp : StartHypeLinesEn;
        }

        private void ProcessChipRewards(DoomRunStats stats)
        {
            while (_backend != null && _backend.TryDequeueKillEvent(out var killEvent))
            {
                _processedKillCount++;
                _lastKillReward = killEvent.Reward;
                EnqueueNextKillVoice();
                LogKillCommentary(killEvent);
            }

            while (_processedKillCount < stats.TotalKills)
            {
                _processedKillCount++;
                EnqueueNextKillVoice();
            }

            if (_processedKillPayout < stats.KillChipPayout)
            {
                var add = stats.KillChipPayout - _processedKillPayout;
                _processedKillPayout = stats.KillChipPayout;
                if (add > 0)
                {
                    _lastKillReward = add;
                }
                GrantCasinoChips(add);
                ShowCoinPopup(add, stats.CurrentKillStreak);
            }

            while (_processedClearEvents < stats.ClearEventCount)
            {
                _processedClearEvents++;
                const int clearBonus = 5000;
                GrantCasinoChips(clearBonus);
                ShowCoinPopup(clearBonus, 0);
                _backend?.SavePersistentNow();
                DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                    "ステージクリア！ボーナス +" + clearBonus + " チップ",
                    "Stage clear! Bonus +" + clearBonus + " chips",
                    "关卡通关！奖励 +" + clearBonus + " 筹码"));
            }

            while (_processedBossClearEvents < stats.BossClearEventCount)
            {
                _processedBossClearEvents++;
                const int bossBonus = 10000;
                GrantCasinoChips(bossBonus);
                ShowCoinPopup(bossBonus, 0);
                DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                    "ボスマップ制覇！追加 +" + bossBonus + " チップ",
                    "Boss map cleared! Extra +" + bossBonus + " chips",
                    "Boss关卡完成！额外 +" + bossBonus + " 筹码"));
            }

            if (stats.MaxKillStreak >= 3 && stats.MaxKillStreak > _lastAnnouncedStreak)
            {
                _lastAnnouncedStreak = stats.MaxKillStreak;
                DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                    "連続キル最高記録 x" + _lastAnnouncedStreak + "！",
                    "New best kill streak x" + _lastAnnouncedStreak + "!",
                    "连杀新纪录 x" + _lastAnnouncedStreak + "！"));
            }

            if (stats.CurrentKillStreak >= 2 && stats.CurrentKillStreak != _lastPopupStreak)
            {
                _lastPopupStreak = stats.CurrentKillStreak;
                ShowStreakPopup(stats.CurrentKillStreak, _lastKillReward);
            }
            else if (stats.CurrentKillStreak <= 1)
            {
                _lastPopupStreak = stats.CurrentKillStreak;
            }
        }

        private static void LogKillCommentary(DoomKillEvent e)
        {
            var mapPart = string.IsNullOrWhiteSpace(e.MapTitle) ? e.MapCode : (e.MapCode + " " + e.MapTitle);
            var streakPart = "x" + e.CurrentKillStreak;
            var weapon = LocalizeWeapon(e.Weapon);
            var enemy = LocalizeEnemy(e.Enemy);

            var line = Localize(
                "【DOOM " + mapPart + "】" + enemy + "に" + weapon + "を向けた！ " + enemy + "をミンチにした！ キルストリーク" + streakPart + "！ +" + e.Reward + "チップ",
                "[DOOM " + mapPart + "] Lined up " + weapon + " on " + enemy + "! Turned " + enemy + " into mince! Kill streak " + streakPart + "! +" + e.Reward + " chips",
                "【DOOM " + mapPart + "】用" + weapon + "瞄准了" + enemy + "！ 把" + enemy + "打成了肉酱！ 连杀" + streakPart + "！ +" + e.Reward + "筹码");

            DoomDiagnostics.Info("[JustDoomIt] " + line);
            Msg.SayRaw(line);
        }

        private void EnsureKillVoiceSource()
        {
            if (_killVoiceSource != null)
            {
                return;
            }

            var go = new GameObject("JustDoomIt_KillVoice");
            DontDestroyOnLoad(go);
            _killVoiceSource = go.AddComponent<AudioSource>();
            _killVoiceSource.playOnAwake = false;
            _killVoiceSource.loop = false;
            _killVoiceSource.spatialBlend = 0f;
            _killVoiceSource.ignoreListenerPause = true;
            _killVoiceSource.volume = 1f;
        }

        private void EnsureKillVoiceClipsLoaded()
        {
            if (_killVoiceLoaded || _killVoiceLoading)
            {
                return;
            }

            StartCoroutine(LoadKillVoiceClips());
        }

        private IEnumerator LoadKillVoiceClips()
        {
            _killVoiceLoading = true;
            _killVoiceLoaded = false;
            _killVoiceClips.Clear();

            var dir = ResolveKillVoiceDirectory();
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                DoomDiagnostics.Warn("[JustDoomIt] Kill voice directory not found: " + dir);
                _killVoiceLoading = false;
                yield break;
            }

            var files = Directory.GetFiles(dir, "*.ogg");
            System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);

            foreach (var path in files)
            {
                var url = "file:///" + path.Replace("\\", "/");
                using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
                {
                    yield return req.SendWebRequest();
                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        DoomDiagnostics.Warn("[JustDoomIt] Failed to load kill voice: " + path + " (" + req.error + ")");
                        continue;
                    }

                    var clip = DownloadHandlerAudioClip.GetContent(req);
                    if (clip != null)
                    {
                        clip.name = Path.GetFileName(path);
                        AmplifyClip(clip, KillVoiceSampleGain);
                        _killVoiceClips.Add(clip);
                    }
                }
            }

            _killVoiceLoaded = _killVoiceClips.Count > 0;
            _killVoiceLoading = false;
            if (_killVoiceLoaded)
            {
                DoomDiagnostics.Info("[JustDoomIt] Kill voice clips loaded: " + _killVoiceClips.Count);
            }
            else
            {
                DoomDiagnostics.Warn("[JustDoomIt] No kill voice clips loaded.");
            }
        }


        private void EnqueueNextKillVoice()
        {
            EnsureKillVoiceClipsLoaded();
            if (_killVoiceClips.Count == 0)
            {
                return;
            }

            var index = _nextKillVoiceIndex % _killVoiceClips.Count;
            _nextKillVoiceIndex = (_nextKillVoiceIndex + 1) % _killVoiceClips.Count;
            _killVoiceQueue.Enqueue(new KillVoiceRequest
            {
                ClipIndex = index,
                ReadyAt = Time.unscaledTime + KillVoiceDelaySeconds
            });
        }

        private void UpdateKillVoicePlayback()
        {
            if (_killVoiceSource == null)
            {
                return;
            }

            if (_killVoiceSource.isPlaying)
            {
                return;
            }

            if (_killVoiceQueue.Count == 0)
            {
                return;
            }

            if (_killVoiceClips.Count == 0)
            {
                return;
            }

            var req = _killVoiceQueue.Peek();
            if (Time.unscaledTime < req.ReadyAt)
            {
                return;
            }

            _killVoiceQueue.Dequeue();
            var index = req.ClipIndex;
            if (index < 0 || index >= _killVoiceClips.Count)
            {
                return;
            }

            _killVoiceSource.clip = _killVoiceClips[index];
            _killVoiceSource.Play();
        }

        private static void AmplifyClip(AudioClip clip, float gain)
        {
            if (clip == null || gain <= 1f)
            {
                return;
            }

            try
            {
                var data = new float[clip.samples * clip.channels];
                if (!clip.GetData(data, 0))
                {
                    return;
                }

                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = Mathf.Clamp(data[i] * gain, -1f, 1f);
                }

                clip.SetData(data, 0);
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Failed to amplify kill voice clip: " + ex.Message);
            }
        }

        private static string ResolveKillVoiceDirectory()
        {
            var modDir = Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? "";
            var candidates = new[]
            {
                Path.Combine(modDir, "Sound", "KillStreak"),
                Path.Combine(BepInEx.Paths.PluginPath, "Elin_JustDoomIt", "Sound", "KillStreak")
            };

            foreach (var c in candidates)
            {
                if (Directory.Exists(c))
                {
                    return c;
                }
            }

            return candidates[0];
        }

        private void StartDoomBgm()
        {
            try
            {
                _doomBgmActive = true;
                _nextDoomBgmIndex = 0;
                _nextDoomBgmRetryAt = 0f;
                EClass.Sound?.StopBGM();
                PlayNextDoomBgm();
                DoomDiagnostics.Info("[JustDoomIt] DOOM BGM sequence started.");
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Failed to start DOOM BGM sequence: " + ex.Message);
            }
        }

        private void StopDoomBgm()
        {
            try
            {
                _doomBgmActive = false;
                EClass.Sound?.StopBGM();
                EClass._zone?.RefreshBGM();
                DoomDiagnostics.Info("[JustDoomIt] DOOM BGM sequence stopped.");
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Failed to stop DOOM BGM sequence: " + ex.Message);
            }
        }

        private void UpdateDoomBgmPlayback()
        {
            if (!_active || !_doomBgmActive || EClass.Sound == null)
            {
                return;
            }

            if (Time.unscaledTime < _nextDoomBgmRetryAt)
            {
                return;
            }

            var hasPlayingBgm = EClass.Sound.sourceBGM != null && EClass.Sound.sourceBGM.isPlaying;
            var currentId = EClass.Sound.currentBGM != null ? EClass.Sound.currentBGM.id : string.Empty;
            var isDoomBgm = IsDoomBgmId(currentId);

            if (hasPlayingBgm && isDoomBgm)
            {
                return;
            }

            if (hasPlayingBgm && !isDoomBgm)
            {
                DoomDiagnostics.Info("[JustDoomIt] Replacing non-DOOM BGM during session: " + currentId);
            }

            PlayNextDoomBgm();
        }

        private void PlayNextDoomBgm()
        {
            if (!_doomBgmActive || EClass.Sound == null || DoomBgmIds.Length == 0)
            {
                return;
            }

            var id = DoomBgmIds[_nextDoomBgmIndex % DoomBgmIds.Length];
            _nextDoomBgmIndex = (_nextDoomBgmIndex + 1) % DoomBgmIds.Length;
            var bgm = EClass.Sound.PlayBGM(id);
            if (bgm == null)
            {
                DoomDiagnostics.Warn("[JustDoomIt] PlayBGM failed for id: " + id);
                _nextDoomBgmRetryAt = Time.unscaledTime + 1.0f;
            }
            else
            {
                _nextDoomBgmRetryAt = Time.unscaledTime + 0.5f;
            }
        }

        private static bool IsDoomBgmId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            var normalized = NormalizeBgmId(id);
            for (var i = 0; i < DoomBgmIds.Length; i++)
            {
                if (string.Equals(normalized, NormalizeBgmId(DoomBgmIds[i]), System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeBgmId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            var s = id.Trim().Replace("\\", "/");

            const string bgmPrefix = "BGM/";
            if (s.StartsWith(bgmPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(bgmPrefix.Length);
            }

            if (s.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(0, s.Length - 4);
            }

            return s;
        }

        private void GrantCasinoChips(int amount)
        {
            if (amount <= 0 || EClass.pc == null)
            {
                return;
            }

            var chips = ThingGen.Create("casino_coin");
            if (chips == null)
            {
                DoomDiagnostics.Warn("[JustDoomIt] casino_coin item is missing. Reward skipped.");
                return;
            }

            chips.SetNum(amount);
            EClass.pc.AddThing(chips);
            _sessionCoinsEarned += amount;
        }

        private static string Localize(string jp, string en, string cn)
        {
            if (Lang.langCode == "CN")
            {
                return cn;
            }

            return Lang.isJP ? jp : en;
        }

        private static string LocalizeWeapon(string weapon)
        {
            if (!Lang.isJP && Lang.langCode != "CN")
            {
                return weapon;
            }

            switch (weapon)
            {
                case "Fist": return Lang.langCode == "CN" ? "拳头" : "拳";
                case "Pistol": return Lang.langCode == "CN" ? "手枪" : "ピストル";
                case "Shotgun": return Lang.langCode == "CN" ? "霰弹枪" : "ショットガン";
                case "Chaingun": return Lang.langCode == "CN" ? "机枪" : "チェインガン";
                case "Rocket": return Lang.langCode == "CN" ? "火箭炮" : "ロケット";
                case "Plasma": return Lang.langCode == "CN" ? "等离子枪" : "プラズマ";
                case "BFG": return "BFG";
                case "Chainsaw": return Lang.langCode == "CN" ? "电锯" : "チェーンソー";
                case "Super Shotgun": return Lang.langCode == "CN" ? "超级霰弹枪" : "スーパーショットガン";
                default: return weapon;
            }
        }

        private static string LocalizeEnemy(string enemy)
        {
            if (!Lang.isJP && Lang.langCode != "CN")
            {
                return enemy;
            }

            switch (enemy)
            {
                case "Zombieman": return Lang.langCode == "CN" ? "僵尸士兵" : "ゾンビマン";
                case "Shotgun Guy": return Lang.langCode == "CN" ? "霰弹枪僵尸" : "ショットガンガイ";
                case "Heavy Weapon Dude": return Lang.langCode == "CN" ? "重机枪兵" : "ヘビーウェポンデュード";
                case "Imp": return Lang.langCode == "CN" ? "小恶魔" : "インプ";
                case "Demon": return Lang.langCode == "CN" ? "恶魔" : "デーモン";
                case "Spectre": return Lang.langCode == "CN" ? "幽灵恶魔" : "スペクター";
                case "Cacodemon": return Lang.langCode == "CN" ? "卡考恶魔" : "カコデーモン";
                case "Baron of Hell": return Lang.langCode == "CN" ? "地狱男爵" : "ヘルバロン";
                case "Hell Knight": return Lang.langCode == "CN" ? "地狱骑士" : "ヘルナイト";
                case "Lost Soul": return Lang.langCode == "CN" ? "失魂" : "ロストソウル";
                case "Spider Mastermind": return Lang.langCode == "CN" ? "蜘蛛首脑" : "スパイダーマスターマインド";
                case "Arachnotron": return Lang.langCode == "CN" ? "蛛魔" : "アラクノトロン";
                case "Cyberdemon": return Lang.langCode == "CN" ? "机械巨魔" : "サイバーデーモン";
                case "Pain Elemental": return Lang.langCode == "CN" ? "痛苦元素" : "ペインエレメンタル";
                case "Arch-vile": return Lang.langCode == "CN" ? "大恶灵" : "アークバイル";
                case "Revenant": return Lang.langCode == "CN" ? "亡魂战士" : "レヴナント";
                case "Mancubus": return Lang.langCode == "CN" ? "肥魔" : "マンキュバス";
                case "Wolfenstein SS": return Lang.langCode == "CN" ? "党卫军" : "SS兵";
                case "Commander Keen": return Lang.langCode == "CN" ? "指挥官Keen" : "コマンダー・キーン";
                case "Icon of Sin": return Lang.langCode == "CN" ? "罪恶之像" : "アイコン・オブ・シン";
                case "Unknown": return Lang.langCode == "CN" ? "不明" : "不明";
                default: return enemy;
            }
        }

        private static void LogRewardRules()
        {
            DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                "報酬ルール: 撃破でチップ獲得。初回100、連続キルは撃破ごとに2倍（1キル最大5000）、被弾でリセット。クリア+5000、ボス+10000。",
                "Reward rules: kills grant chips. First kill is 100, each consecutive kill doubles (max 5000 per kill), and streak resets when you take damage. Clear +5000, boss +10000.",
                "奖励规则：击杀得筹码。首杀100，之后每次连杀奖励翻倍（单次击杀上限5000），受伤即重置。通关+5000，Boss关+10000。"));
        }

        private void ShowCoinPopup(int amount, int streak)
        {
            if (amount <= 0)
            {
                return;
            }

            var text = Localize(
                "カジノチップ +" + amount + (streak >= 2 ? "  (x" + streak + ")" : ""),
                "Casino chips +" + amount + (streak >= 2 ? "  (x" + streak + ")" : ""),
                "赌场筹码 +" + amount + (streak >= 2 ? "  (x" + streak + ")" : ""));
            ShowProgressText(text, FontColor.Good);
        }

        private void ShowStreakPopup(int streak, int reward)
        {
            if (streak < 2)
            {
                return;
            }

            var text = Localize(
                "連続キル x" + streak + (reward > 0 ? "  チップ +" + reward : ""),
                "Kill streak x" + streak + (reward > 0 ? "  chips +" + reward : ""),
                "连杀 x" + streak + (reward > 0 ? "  筹码 +" + reward : ""));
            ShowProgressText(text, FontColor.Great);
        }

        private void ShowProgressText(string text, FontColor color)
        {
            if (text.IsEmpty())
            {
                return;
            }

            WidgetPopText.Say(text, color);
            _overlay?.ShowNotice(text, GetOverlayColor(color));
        }

        private static Color GetOverlayColor(FontColor color)
        {
            if (color == FontColor.Great)
            {
                return new Color(1f, 0.95f, 0.35f, 1f);
            }
            if (color == FontColor.Good)
            {
                return new Color(0.45f, 1f, 0.65f, 1f);
            }
            if (color == FontColor.Bad)
            {
                return new Color(1f, 0.45f, 0.45f, 1f);
            }
            return Color.white;
        }
    }
}

