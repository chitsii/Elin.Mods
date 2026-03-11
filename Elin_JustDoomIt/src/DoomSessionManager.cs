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
        private const int BossClearBonus = 10000;

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
        private int _processedClearEvents;
        private int _processedBossClearEvents;
        private int _sessionCoinsEarned;
        private int _lastAnnouncedStreak;
        private int _sessionSkill;
        private DoomRewardRate _currentRate;
        private int _currentPool;
        private int _currentMultiplierStage;
        private bool _roundActive;
        private bool _rateSelectionOpen;
        private int _rateSelectionCursor;
        private DoomRewardRateSelectionMode _rewardRateSelectionMode;
        private string _currentMapCode = string.Empty;
        private string _currentMapTitle = string.Empty;
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
            OpenArcadeMenu(user, forceMenu: false);
        }

        private void OpenArcadeMenu(Chara user, bool forceMenu)
        {
            EInput.Consume(consumeAxis: true, _skipFrame: 2);
            StartCoroutine(OpenArcadeMenuNextFrame(user, forceMenu));
        }

        private IEnumerator OpenArcadeMenuNextFrame(Chara user, bool forceMenu)
        {
            yield return null;
            var loadout = DoomWadLocator.LoadRuntimeLoadout();
            var menu = DoomArcadeMenuUI.Create();
            menu.Show(
                loadout,
                user,
                onPlay: (loadExisting) => StartSessionDirect(loadout, loadExisting),
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
                        () => OpenArcadeMenu(EClass.pc, forceMenu: true),
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
            _active = false;
            _processedKillCount = 0;
            _processedClearEvents = 0;
            _processedBossClearEvents = 0;
            _sessionCoinsEarned = 0;
            _lastAnnouncedStreak = 0;
            _sessionSkill = 3;
            _rewardRateSelectionMode = launch.RewardRateSelectionMode;
            _doomTickAccumulator = 0f;
            _nextGlobalStatsFlushAt = Time.unscaledTime + GlobalStatsFlushIntervalSeconds;
            ResetRoundState();
            DoomKillFeed.Reset();
            EnsureKillVoiceSource();
            EnsureKillVoiceClipsLoaded();
            StartDoomBgm();
            EInput.Consume(consumeAxis: true, _skipFrame: 2);
            _backend.PrimeSessionState();
            _overlay?.Upload(_backend.GetFrameBuffer());
            if (!ProcessBackendEvents(_backend.Stats))
            {
                return;
            }

            _active = true;
            SetCursorCaptured(!_rateSelectionOpen);
            RefreshHud();

            var modCount = launch.PwadPaths != null ? launch.PwadPaths.Count : 0;
            DoomDiagnostics.Info("[JustDoomIt] DOOM session started. IWAD=" + launch.IwadPath + " PWADs=" + modCount);
            LogRandomStartLine();
            LogRewardRules();
        }

        private void Update()
        {
            try
            {
                if (_backend == null)
                {
                    return;
                }

                if (!_backend.IsRunning)
                {
                    StopSession();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Escape) || EInput.isCancel)
                {
                    if (_exitConfirmOpen)
                    {
                        _exitConfirmOpen = false;
                        AbandonCurrentRound(showPopup: true);
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

                if (_rateSelectionOpen)
                {
                    HandleRateSelectionInput();
                    EInput.Consume(consumeAxis: true, _skipFrame: 1);
                    _backend.SubmitInput(default);
                    RefreshHud();
                    return;
                }

                DoomInputState input = default;
                SetCursorCaptured(true);
                EInput.Consume(consumeAxis: true, _skipFrame: 1);
                input = DoomInputState.ReadFromUnity();

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
                    if (!ProcessBackendEvents(_backend.Stats))
                    {
                        return;
                    }
                    AccumulateGlobalPlaytime(ticks * DoomTickStep);
                }
                RefreshHud();
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
            _processedClearEvents = 0;
            _processedBossClearEvents = 0;
            _sessionCoinsEarned = 0;
            _lastAnnouncedStreak = 0;
            _doomTickAccumulator = 0f;
            _killVoiceQueue.Clear();
            DoomKillFeed.Reset();
            _exitConfirmOpen = false;
            _rateSelectionOpen = false;
            ResetRoundState();
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
            var prompt = _currentPool > 0
                ? Localize(
                    "DOOMプレイを停止しますか？\n現在の未確定プールは失われます。",
                    "Stop DOOM play?\nYour current unbanked pool will be lost.",
                    "要停止DOOM游玩吗？\n当前未兑现奖池会全部损失。")
                : Localize(
                    "DOOMプレイを停止しますか？",
                    "Stop DOOM play?",
                    "要停止DOOM游玩吗？");
            Dialog.YesNo(
                prompt,
                () =>
                {
                    _exitConfirmOpen = false;
                    AbandonCurrentRound(showPopup: true);
                    StopSession();
                },
                () =>
                {
                    _exitConfirmOpen = false;
                    _overlay?.SetVisible(true);
                    SetCursorCaptured(!_rateSelectionOpen);
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

        private bool ProcessBackendEvents(DoomRunStats stats)
        {
            while (_backend != null && _backend.TryDequeueEvent(out var backendEvent))
            {
                switch (backendEvent.Type)
                {
                    case DoomBackendEventType.MapStart:
                        HandleMapStart(backendEvent.MapStartEvent);
                        break;
                    case DoomBackendEventType.Death:
                        HandleDeath(backendEvent.DeathEvent);
                        break;
                    case DoomBackendEventType.Damage:
                        HandleDamage(backendEvent.DamageEvent);
                        break;
                    case DoomBackendEventType.Secret:
                        HandleSecret(backendEvent.SecretEvent);
                        break;
                    case DoomBackendEventType.Kill:
                        _processedKillCount++;
                        EnqueueNextKillVoice();
                        HandleKill(backendEvent.KillEvent);
                        break;
                }
            }

            while (_processedKillCount < stats.TotalKills)
            {
                _processedKillCount++;
                EnqueueNextKillVoice();
            }

            while (_processedClearEvents < stats.ClearEventCount)
            {
                _processedClearEvents++;
                var isBossClear = _processedBossClearEvents < stats.BossClearEventCount;
                if (isBossClear)
                {
                    _processedBossClearEvents++;
                }

                HandleMapClear(isBossClear);
            }

            if (stats.MaxKillStreak >= 3 && stats.MaxKillStreak > _lastAnnouncedStreak)
            {
                _lastAnnouncedStreak = stats.MaxKillStreak;
                DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                    "連続キル最高記録 x" + _lastAnnouncedStreak + "！",
                    "New best kill streak x" + _lastAnnouncedStreak + "!",
                    "连杀新纪录 x" + _lastAnnouncedStreak + "！"));
            }

            return _backend != null;
        }

        private void HandleMapStart(DoomMapStartEvent mapEvent)
        {
            _currentMapCode = mapEvent.MapCode ?? string.Empty;
            _currentMapTitle = mapEvent.MapTitle ?? string.Empty;
            _sessionSkill = Mathf.Clamp(mapEvent.Skill, 1, 5);
            ResetRoundState();
            var configuredRate = DoomLaunchPreferences.ResolveConfiguredRate(_rewardRateSelectionMode);
            _rateSelectionCursor = RateToCursor(configuredRate == DoomRewardRate.None ? DoomRewardRate.Low : configuredRate);
            if (configuredRate != DoomRewardRate.None && TryBeginRound(configuredRate, showInsufficientPopup: true))
            {
                var fixedRateCode = DoomRewardRoundLogic.GetConfig(configuredRate).Code;
                DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                    "ラウンド開始: " + BuildMapLabel(_currentMapCode, _currentMapTitle) + " を固定RATE " + fixedRateCode + " で開始。",
                    "Round start: auto-started " + BuildMapLabel(_currentMapCode, _currentMapTitle) + " with fixed rate " + fixedRateCode + ".",
                    "回合开始：" + BuildMapLabel(_currentMapCode, _currentMapTitle) + " 已按固定RATE " + fixedRateCode + " 自动开始。"));
                return;
            }

            _rateSelectionOpen = true;
            SetCursorCaptured(false);
            RefreshRateSelectionOverlay();
            RefreshHud();
            DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                "ラウンド開始: " + BuildMapLabel(_currentMapCode, _currentMapTitle) + " でレート選択待ち。",
                "Round start: waiting for rate selection on " + BuildMapLabel(_currentMapCode, _currentMapTitle) + ".",
                "回合开始：" + BuildMapLabel(_currentMapCode, _currentMapTitle) + "，等待选择RATE。"));
        }

        private void HandleKill(DoomKillEvent killEvent)
        {
            if (!_roundActive || _currentRate == DoomRewardRate.None)
            {
                return;
            }

            var usedStage = _currentMultiplierStage;
            var add = DoomRewardRoundLogic.CalculateKillPoolGain(_currentRate, _sessionSkill, usedStage);
            if (add <= 0)
            {
                return;
            }

            _currentPool += add;
            var multiText = DoomRewardRoundLogic.FormatMultiplier(_currentRate, usedStage);
            var suffix = usedStage > 0 ? " " + multiText : string.Empty;
            ShowChipDeltaText("+" + add + " KILL" + suffix, FontColor.Good);
            LogKillCommentary(killEvent, add, multiText);
            _currentMultiplierStage = DoomRewardRoundLogic.AdvanceMultiplierStage(_currentMultiplierStage);
            RefreshHud();
        }

        private void HandleSecret(DoomSecretEvent secretEvent)
        {
            if (!_roundActive || _currentRate == DoomRewardRate.None)
            {
                return;
            }

            var count = Mathf.Max(1, secretEvent.Count);
            var add = DoomRewardRoundLogic.GetSecretPoolGain() * count;
            _currentPool += add;
            var suffix = count > 1 ? " x" + count : string.Empty;
            ShowChipDeltaText("+" + add + " SECRET" + suffix, FontColor.Great);
            DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                BuildMapLabel(secretEvent.MapCode, secretEvent.MapTitle) + " でシークレット発見 +" + add + suffix + " (" + secretEvent.SecretCount + "/" + secretEvent.TotalSecrets + ")",
                "Secret found on " + BuildMapLabel(secretEvent.MapCode, secretEvent.MapTitle) + ": +" + add + suffix + " (" + secretEvent.SecretCount + "/" + secretEvent.TotalSecrets + ")",
                "在 " + BuildMapLabel(secretEvent.MapCode, secretEvent.MapTitle) + " 发现秘密：+" + add + suffix + " (" + secretEvent.SecretCount + "/" + secretEvent.TotalSecrets + ")"));
            RefreshHud();
        }

        private void HandleDamage(DoomDamageEvent damageEvent)
        {
            if (!_roundActive || _currentRate == DoomRewardRate.None)
            {
                return;
            }

            var loss = DoomRewardRoundLogic.CalculateHitLoss(_currentRate, _currentPool);
            var lossPercent = DoomRewardRoundLogic.GetHitLossPercent(_currentRate);
            if (loss > 0)
            {
                _currentPool = Mathf.Max(0, _currentPool - loss);
                ShowChipDeltaText("-" + loss + " HIT LOSS (" + lossPercent + "%)", FontColor.Bad);
            }

            if (_currentMultiplierStage > 0)
            {
                ShowProgressText(Localize("連続ボーナス リセット", "KILL BONUS RESET", "连杀加成重置"), FontColor.Bad);
            }

            _currentMultiplierStage = DoomRewardRoundLogic.ResetMultiplierStage();
            RefreshHud();
        }

        private void HandleMapClear(bool isBossClear)
        {
            var cashOut = _currentPool;
            var mapLabel = BuildMapLabel(_currentMapCode, _currentMapTitle);
            ResetRoundState();
            RefreshHud();

            if (cashOut > 0)
            {
                GrantConfirmedReward(cashOut);
                ShowProgressText("CASH OUT +" + cashOut, FontColor.Good);
            }

            if (isBossClear)
            {
                GrantConfirmedReward(BossClearBonus);
                ShowProgressText("BOSS BONUS +" + BossClearBonus, FontColor.Great);
            }

            _backend?.SavePersistentNow();
            DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                BuildMapLabel(_currentMapCode, _currentMapTitle) + " をクリア。精算 +" + cashOut + (isBossClear ? " / boss +" + BossClearBonus : string.Empty),
                "Cleared " + mapLabel + ". Cash out +" + cashOut + (isBossClear ? " / boss +" + BossClearBonus : string.Empty),
                "已通关 " + mapLabel + "。兑现 +" + cashOut + (isBossClear ? " / boss +" + BossClearBonus : string.Empty)));
        }

        private void HandleDeath(DoomDeathEvent deathEvent)
        {
            var cashOut = _currentPool;
            ResetRoundState();
            RefreshHud();

            if (cashOut > 0)
            {
                GrantConfirmedReward(cashOut);
                ShowProgressText(
                    Localize("死亡精算 +" + cashOut, "DEATH CASH OUT +" + cashOut, "死亡兑现 +" + cashOut),
                    FontColor.Good);
                _backend?.SavePersistentNow();
            }

            DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                cashOut > 0
                    ? "死亡により未精算チップを精算 +" + cashOut + "。"
                    : "死亡時に精算できる未精算チップはありませんでした。",
                cashOut > 0
                    ? "Death cashed out uncashed chips +" + cashOut + "."
                    : "No uncashed chips were available to cash out on death.",
                cashOut > 0
                    ? "死亡时已兑现未结算筹码 +" + cashOut + "。"
                    : "死亡时没有可兑现的未结算筹码。"));
            RefreshHud();
        }

        private void HandleRateSelectionInput()
        {
            if (!_rateSelectionOpen)
            {
                return;
            }

            var hoveredIndex = _overlay?.GetRateSelectionHoverIndex(Input.mousePosition) ?? -1;
            if (hoveredIndex >= 0 && hoveredIndex != _rateSelectionCursor)
            {
                _rateSelectionCursor = hoveredIndex;
                RefreshRateSelectionOverlay();
            }

            if (hoveredIndex >= 0 && Input.GetMouseButtonDown(0))
            {
                _rateSelectionCursor = hoveredIndex;
                ConfirmRateSelection();
                return;
            }

            var axis = EInput.axis;
            if (axis.y > 0f)
            {
                _rateSelectionCursor = (_rateSelectionCursor + 2) % 3;
                RefreshRateSelectionOverlay();
                return;
            }

            if (axis.y < 0f)
            {
                _rateSelectionCursor = (_rateSelectionCursor + 1) % 3;
                RefreshRateSelectionOverlay();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                _rateSelectionCursor = 0;
                ConfirmRateSelection();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                _rateSelectionCursor = 1;
                ConfirmRateSelection();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                _rateSelectionCursor = 2;
                ConfirmRateSelection();
                return;
            }

            if (EInput.isConfirm || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ConfirmRateSelection();
            }
        }

        private void ConfirmRateSelection()
        {
            var rate = CursorToRate(_rateSelectionCursor);
            if (!TryBeginRound(rate, showInsufficientPopup: true))
            {
                RefreshRateSelectionOverlay();
                return;
            }
        }

        private bool TryBeginRound(DoomRewardRate rate, bool showInsufficientPopup)
        {
            var entryCost = DoomRewardRoundLogic.GetEntryCost(rate);
            if (!TrySpendCasinoCoins(entryCost))
            {
                if (showInsufficientPopup)
                {
                    ShowProgressText(Localize("チップ不足", "NOT ENOUGH CHIPS", "筹码不足"), FontColor.Bad);
                }

                return false;
            }

            _currentRate = rate;
            _currentPool = 0;
            _currentMultiplierStage = DoomRewardRoundLogic.ResetMultiplierStage();
            _roundActive = true;
            _rateSelectionOpen = false;
            _active = true;
            _overlay?.HideRateSelection();
            SetCursorCaptured(true);
            ShowProgressText(Localize("賭け: ", "Wager: ", "赌法：") + GetRewardRateDisplayName(rate), FontColor.Great);
            RefreshHud();
            return true;
        }

        private void ResetRoundState()
        {
            _currentRate = DoomRewardRate.None;
            _currentPool = 0;
            _currentMultiplierStage = DoomRewardRoundLogic.ResetMultiplierStage();
            _roundActive = false;
        }

        private void RefreshHud()
        {
            if (_overlay == null)
            {
                return;
            }

            string betText;
            string rewardText;
            string poolLabelText;
            string poolText;
            var poolValue = 0;
            var riskLoss = 0;
            var roundActive = false;

            if (_roundActive && _currentRate != DoomRewardRate.None)
            {
                betText = Localize("賭け: ", "Wager: ", "赌法：") + GetRewardRateDisplayName(_currentRate);
                rewardText = BuildKillRewardDisplay(_currentRate, _sessionSkill, _currentMultiplierStage);
                poolLabelText = Localize("未精算チップ", "Uncashed Chips", "未结算筹码");
                poolText = _currentPool.ToString();
                poolValue = _currentPool;
                riskLoss = DoomRewardRoundLogic.CalculateHitLoss(_currentRate, _currentPool);
                roundActive = true;
            }
            else if (_rateSelectionOpen)
            {
                betText = Localize("賭けを選択", "SELECT WAGER", "选择赌法");
                rewardText = Localize("1キル報酬 -", "Per-Kill Payout -", "每杀奖励 -");
                poolLabelText = Localize("未精算チップ", "Uncashed Chips", "未结算筹码");
                poolText = "-";
            }
            else
            {
                betText = Localize("賭け: -", "Wager: -", "赌法：-");
                rewardText = Localize("1キル報酬 -", "Per-Kill Payout -", "每杀奖励 -");
                poolLabelText = Localize("未精算チップ", "Uncashed Chips", "未结算筹码");
                poolText = "-";
            }

            _overlay.SetHud(betText, rewardText, poolLabelText, poolText, poolValue, riskLoss, roundActive);
        }

        private void RefreshRateSelectionOverlay()
        {
            if (_overlay == null)
            {
                return;
            }

            var rate = CursorToRate(_rateSelectionCursor);
            var config = DoomRewardRoundLogic.GetConfig(rate);
            var bonusCap = Mathf.RoundToInt(DoomRewardRoundLogic.GetKillBonusCapPercent(rate));
            var difficultyMultiplier = DoomRewardRoundLogic.GetDifficultyMultiplier(_sessionSkill);
            var options = BuildRateSelectionOptions();
            var helper = Localize(
                "現在難易度補正 x" + difficultyMultiplier.ToString("0.00") + "。参加 " + config.EntryCost + " を払って開始。連続キルによる報酬ブーストは最大 +" + bonusCap + "%。未精算チップを育ててクリアか死亡で精算。",
                "Current difficulty modifier x" + difficultyMultiplier.ToString("0.00") + ". Pay " + config.EntryCost + " to start. The streak reward boost reaches up to +" + bonusCap + "%. Build up uncashed chips and cash out on clear or death.",
                "当前难度补正 x" + difficultyMultiplier.ToString("0.00") + "。支付 " + config.EntryCost + " 开始。连杀带来的奖励加成最高可达 +" + bonusCap + "%。积累未结算筹码并在通关或死亡时兑现。");

            _overlay.ShowRateSelection(Localize("賭けを選択", "SELECT WAGER", "选择赌法"), helper, options, _rateSelectionCursor);
        }

        private string[] BuildRateSelectionOptions()
        {
            var chips = EClass.pc?.GetCurrency(CasinoCoinId) ?? 0;
            var rates = new[] { DoomRewardRate.Low, DoomRewardRate.Mid, DoomRewardRate.High };
            var options = new string[rates.Length];
            for (var i = 0; i < rates.Length; i++)
            {
                var config = DoomRewardRoundLogic.GetConfig(rates[i]);
                var bonusCap = Mathf.RoundToInt(DoomRewardRoundLogic.GetKillBonusCapPercent(rates[i]));
                var option = Localize(
                    GetRewardRateDisplayName(rates[i]) + "\n<size=18>参加 " + config.EntryCost +
                    "   1キル " + config.BaseReward +
                    "   最大 +" + bonusCap + "%" +
                    "   被弾 -" + config.HitLossPercent + "%</size>",
                    GetRewardRateDisplayName(rates[i]) + "\n<size=18>ENTRY " + config.EntryCost +
                    "   PAYOUT " + config.BaseReward +
                    "   MAX +" + bonusCap + "%" +
                    "   HIT -" + config.HitLossPercent + "%</size>",
                    GetRewardRateDisplayName(rates[i]) + "\n<size=18>入场 " + config.EntryCost +
                    "   每杀 " + config.BaseReward +
                    "   最大 +" + bonusCap + "%" +
                    "   受击 -" + config.HitLossPercent + "%</size>");
                if (chips < config.EntryCost)
                {
                    option += Localize("  <size=18>  不足</size>", "  <size=18>  LOCK</size>", "  <size=18>  不足</size>");
                }

                options[i] = option;
            }

            return options;
        }

        private static string GetRewardRateDisplayName(DoomRewardRate rate)
        {
            switch (rate)
            {
                case DoomRewardRate.Low:
                    return Localize("安全重視", "Safe Play", "稳扎稳打");
                case DoomRewardRate.Mid:
                    return Localize("標準勝負", "Standard Play", "标准胜负");
                case DoomRewardRate.High:
                    return Localize("大勝負", "High Stakes", "放手一搏");
                default:
                    return "-";
            }
        }

        private static string BuildKillRewardDisplay(DoomRewardRate rate, int skill, int multiplierStage)
        {
            if (rate == DoomRewardRate.None)
            {
                return Localize("1キル報酬 -", "Per-Kill Payout -", "每杀奖励 -");
            }

            var reward = DoomRewardRoundLogic.CalculateKillPoolGain(rate, skill, multiplierStage);
            var bonusPercent = DoomRewardRoundLogic.GetDisplayedKillBonusPercent(rate, multiplierStage);
            if (bonusPercent <= 0)
            {
                return Localize(
                    "1キル報酬 " + reward,
                    "Per-Kill Payout " + reward,
                    "每杀奖励 " + reward);
            }

            return Localize(
                "1キル報酬 " + reward + " (+" + bonusPercent + "%)",
                "Per-Kill Payout " + reward + " (+" + bonusPercent + "%)",
                "每杀奖励 " + reward + " (+" + bonusPercent + "%)");
        }

        private static DoomRewardRate CursorToRate(int cursor)
        {
            switch (Mathf.Clamp(cursor, 0, 2))
            {
                case 0: return DoomRewardRate.Low;
                case 1: return DoomRewardRate.Mid;
                default: return DoomRewardRate.High;
            }
        }

        private static int RateToCursor(DoomRewardRate rate)
        {
            switch (rate)
            {
                case DoomRewardRate.High:
                    return 2;
                case DoomRewardRate.Mid:
                    return 1;
                default:
                    return 0;
            }
        }

        private bool TrySpendCasinoCoins(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (EClass.pc == null || EClass.pc.GetCurrency(CasinoCoinId) < amount)
            {
                return false;
            }

            EClass.pc.ModCurrency(-amount, CasinoCoinId);
            return true;
        }

        private void AbandonCurrentRound(bool showPopup)
        {
            var lost = _currentPool;
            var hadRoundState = _currentRate != DoomRewardRate.None || _currentPool > 0 || _roundActive;
            ResetRoundState();
            RefreshHud();
            if (showPopup && hadRoundState)
            {
                ShowProgressText(
                    lost > 0
                        ? Localize("未精算チップ喪失 " + lost, "UNCASHED CHIPS LOST " + lost, "未结算筹码损失 " + lost)
                        : Localize("未精算チップ喪失", "UNCASHED CHIPS LOST", "未结算筹码损失"),
                    FontColor.Bad);
            }
        }

        private void GrantConfirmedReward(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            GrantCasinoChips(amount);
            if (_backend != null)
            {
                _backend.PersistentTotalChips += amount;
            }
        }

        private static string BuildMapLabel(string mapCode, string mapTitle)
        {
            if (string.IsNullOrWhiteSpace(mapCode))
            {
                return mapTitle ?? string.Empty;
            }

            return string.IsNullOrWhiteSpace(mapTitle) ? mapCode : (mapCode + " " + mapTitle);
        }

        private static void LogKillCommentary(DoomKillEvent e, int poolAdd, string multiplierText)
        {
            var mapPart = BuildMapLabel(e.MapCode, e.MapTitle);
            var streakPart = "x" + e.CurrentKillStreak;
            var weapon = LocalizeWeapon(e.Weapon);
            var enemy = LocalizeEnemy(e.Enemy);
            var rewardPart = Localize(
                "+" + poolAdd + " 未精算",
                "+" + poolAdd + " UNCASHED",
                "+" + poolAdd + " 未结算") + (string.IsNullOrWhiteSpace(multiplierText) ? string.Empty : " " + multiplierText);

            var line = Localize(
                "【DOOM " + mapPart + "】" + enemy + "に" + weapon + "を向けた！ " + enemy + "をミンチにした！ キルストリーク" + streakPart + "！ " + rewardPart,
                "[DOOM " + mapPart + "] Lined up " + weapon + " on " + enemy + "! Turned " + enemy + " into mince! Kill streak " + streakPart + "! " + rewardPart,
                "【DOOM " + mapPart + "】用" + weapon + "瞄准了" + enemy + "！ 把" + enemy + "打成了肉酱！ 连杀" + streakPart + "！ " + rewardPart);

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

            EClass.pc.ModCurrency(amount, CasinoCoinId);
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
                "報酬ルール: 安全重視は堅実、標準勝負は基準、大勝負は夢枠。各マップ開始時に賭けを選択。撃破報酬とシークレット発見は未精算チップへ、被弾でチップ減少+連続ボーナスリセット、クリア時または死亡時に精算、ESCで全損。連続ボーナス上限は安全重視+400%、標準勝負+600%、大勝負+999%。シークレットは固定+500、ボスは追加+10000。",
                "Reward rules: Safe Play is the steady lane, Standard Play is the baseline, and High Stakes is the dream lane. Pick a wager at each map start. Kills and secret finds add to uncashed chips, hits shave the chips and reset the kill bonus, clears and deaths cash out, and ESC loses the lot. Kill-bonus caps are +400% for Safe Play, +600% for Standard Play, and +999% for High Stakes. Secrets are a flat +500, and boss clears add +10000.",
                "奖励规则：稳扎稳打偏稳健，标准胜负是基准，放手一搏是梦想档。每张地图开始时选择赌法。击杀与秘密发现都会累积到未结算筹码，受伤会扣筹码并重置连杀加成，通关或死亡时兑现，ESC会全部损失。连杀加成上限分别为稳扎稳打+400%、标准胜负+600%、放手一搏+999%。秘密固定+500，Boss通关额外+10000。"));
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

        private void ShowChipDeltaText(string text, FontColor color)
        {
            if (text.IsEmpty())
            {
                return;
            }

            WidgetPopText.Say(text, color);
            _overlay?.ShowHudDelta(text, GetOverlayColor(color));
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

        private static string FormatSigned(int value)
        {
            if (value > 0)
            {
                return "+" + value;
            }

            return value.ToString();
        }
    }
}

