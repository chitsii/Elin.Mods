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
        private const int StageClearBonus = 1000;

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
        private bool _sessionTookOverWorldBgm;
        private float _nextDoomBgmRetryAt;
        private const float KillVoiceSampleGain = 2.6f;
        private int _nextKillVoiceIndex;
        private int _processedKillCount;
        private int _processedClearEvents;
        private int _processedBossClearEvents;
        private int _sessionCoinsEarned;
        private int _lastAnnouncedStreak;
        private readonly DoomRewardRoundController _roundController = new DoomRewardRoundController();
        private int _rateSelectionCursor;
        private bool _exitConfirmOpen;
        private bool _sessionEntryFeeResolved;
        private bool _sessionRewardsEnabled;
        private float _doomTickAccumulator;
        private float _globalPlaySecondsAccumulator;
        private float _nextGlobalStatsFlushAt;

        private DoomRewardRoundState CurrentRoundState => _roundController.State;

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
            _doomTickAccumulator = 0f;
            _nextGlobalStatsFlushAt = Time.unscaledTime + GlobalStatsFlushIntervalSeconds;
            _sessionEntryFeeResolved = false;
            _sessionRewardsEnabled = false;
            _roundController.ResetSession();
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
            SetCursorCaptured(!CurrentRoundState.RateSelectionOpen);
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

                if (CurrentRoundState.RateSelectionOpen)
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
            _sessionEntryFeeResolved = false;
            _sessionRewardsEnabled = false;
            _killVoiceQueue.Clear();
            DoomKillFeed.Reset();
            _exitConfirmOpen = false;
            _roundController.ResetSession();
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
            var prompt = CurrentRoundState.RoundActive
                ? Localize(
                    "DOOMプレイを停止しますか？\n獲得済みの報酬はそのまま残ります。",
                    "Stop DOOM play?\nRewards already earned will remain.",
                    "要停止DOOM游玩吗？\n已获得的奖励会保留。")
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
                    SetCursorCaptured(!CurrentRoundState.RateSelectionOpen);
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
            _roundController.HandleMapStart(mapEvent.Skill, mapEvent.MapCode, mapEvent.MapTitle);
            var state = CurrentRoundState;
            var fixedRate = DoomRewardRoundLogic.GetFixedRate();
            _rateSelectionCursor = RateToCursor(fixedRate);

            if (!_sessionEntryFeeResolved)
            {
                _sessionEntryFeeResolved = true;
                if (TryBeginRound(fixedRate, showInsufficientPopup: false, chargeEntryFee: true))
                {
                    _sessionRewardsEnabled = true;
                    var entryCost = DoomRewardRoundLogic.GetEntryCost(fixedRate);
                    DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                        "セッション開始: " + BuildMapLabel(state.MapCode, state.MapTitle) + " から固定参加費 " + entryCost + " を支払って報酬を開始。",
                        "Session start: rewards enabled from " + BuildMapLabel(state.MapCode, state.MapTitle) + " after paying fixed entry " + entryCost + ".",
                        "会话开始：从 " + BuildMapLabel(state.MapCode, state.MapTitle) + " 起支付固定入场费 " + entryCost + " 并启用奖励。"));
                    return;
                }

                _sessionRewardsEnabled = false;
                SetCursorCaptured(true);
                RefreshHud();
                ShowProgressText(Localize("チップ不足: このセッションは報酬なし", "NOT ENOUGH CHIPS: NO REWARDS THIS SESSION", "筹码不足：本次会话无奖励"), FontColor.Bad);
                DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                    "セッション開始: 固定参加費を払えないため " + BuildMapLabel(state.MapCode, state.MapTitle) + " からこのセッションは報酬なし。",
                    "Session start: no rewards for this session from " + BuildMapLabel(state.MapCode, state.MapTitle) + " because the fixed entry fee could not be paid.",
                    "会话开始：由于无法支付固定入场费，从 " + BuildMapLabel(state.MapCode, state.MapTitle) + " 起本次会话无奖励。"));
                return;
            }

            if (!_sessionRewardsEnabled)
            {
                SetCursorCaptured(true);
                RefreshHud();
                DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                    "ラウンド開始: " + BuildMapLabel(state.MapCode, state.MapTitle) + " は報酬なしセッションのため報酬加算なしで進行。",
                    "Round start: " + BuildMapLabel(state.MapCode, state.MapTitle) + " continues with no reward payouts because this session has no rewards.",
                    "回合开始：" + BuildMapLabel(state.MapCode, state.MapTitle) + " 因本次会话无奖励，不启用奖励发放。"));
                return;
            }

            if (TryBeginRound(fixedRate, showInsufficientPopup: false, chargeEntryFee: false))
            {
                var entryCost = DoomRewardRoundLogic.GetEntryCost(fixedRate);
                DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                    "ラウンド開始: " + BuildMapLabel(state.MapCode, state.MapTitle) + " を追加参加費なしで開始。セッション参加費 " + entryCost + " は支払い済み。",
                    "Round start: auto-started " + BuildMapLabel(state.MapCode, state.MapTitle) + " with no extra entry fee. Session entry " + entryCost + " was already paid.",
                    "回合开始：" + BuildMapLabel(state.MapCode, state.MapTitle) + " 已自动开始且不再追加入场费。会话入场费 " + entryCost + " 已支付。"));
                return;
            }

            SetCursorCaptured(true);
            RefreshHud();
            DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                "ラウンド開始: " + BuildMapLabel(state.MapCode, state.MapTitle) + " の報酬ラウンド再開に失敗。",
                "Round start: failed to re-open the reward round on " + BuildMapLabel(state.MapCode, state.MapTitle) + ".",
                "回合开始：" + BuildMapLabel(state.MapCode, state.MapTitle) + " 的奖励回合重新开启失败。"));
        }

        private void HandleKill(DoomKillEvent killEvent)
        {
            var result = _roundController.HandleKill();
            if (result.PoolDelta <= 0)
            {
                return;
            }

            GrantConfirmedReward(result.PoolDelta);
            var state = CurrentRoundState;
            var bonusText = DoomRewardRoundLogic.FormatKillBonus(state.Skill, result.UsedMultiplierStage);
            var suffix = string.IsNullOrWhiteSpace(bonusText) ? string.Empty : " " + bonusText;
            ShowChipDeltaText("+" + result.PoolDelta + " KILL" + suffix, FontColor.Good);
            LogKillCommentary(killEvent, result.PoolDelta, bonusText);
            RefreshHud();
        }

        private void HandleSecret(DoomSecretEvent secretEvent)
        {
            var count = Mathf.Max(1, secretEvent.Count);
            var result = _roundController.HandleSecret(count);
            if (result.PoolDelta <= 0)
            {
                return;
            }

            GrantConfirmedReward(result.PoolDelta);
            var suffix = count > 1 ? " x" + count : string.Empty;
            ShowChipDeltaText("+" + result.PoolDelta + " SECRET" + suffix, FontColor.Great);
            DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                BuildMapLabel(secretEvent.MapCode, secretEvent.MapTitle) + " でシークレット発見 +" + result.PoolDelta + suffix + " (" + secretEvent.SecretCount + "/" + secretEvent.TotalSecrets + ")",
                "Secret found on " + BuildMapLabel(secretEvent.MapCode, secretEvent.MapTitle) + ": +" + result.PoolDelta + suffix + " (" + secretEvent.SecretCount + "/" + secretEvent.TotalSecrets + ")",
                "在 " + BuildMapLabel(secretEvent.MapCode, secretEvent.MapTitle) + " 发现秘密：+" + result.PoolDelta + suffix + " (" + secretEvent.SecretCount + "/" + secretEvent.TotalSecrets + ")"));
            RefreshHud();
        }

        private void HandleDamage(DoomDamageEvent damageEvent)
        {
            var result = _roundController.HandleDamage();
            if (!result.BonusReset)
            {
                return;
            }

            if (result.BonusReset)
            {
                ShowProgressText(Localize("連続ボーナス リセット", "KILL BONUS RESET", "连杀加成重置"), FontColor.Bad);
            }

            RefreshHud();
        }

        private void HandleMapClear(bool isBossClear)
        {
            var state = CurrentRoundState;
            var mapLabel = BuildMapLabel(state.MapCode, state.MapTitle);
            var result = _roundController.HandleClear(StageClearBonus);
            RefreshHud();

            if (result.BonusAmount > 0)
            {
                GrantConfirmedReward(result.BonusAmount);
                ShowProgressText("CLEAR BONUS +" + result.BonusAmount, FontColor.Great);
            }

            _backend?.SavePersistentNow();
            DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                BuildMapLabel(state.MapCode, state.MapTitle) + " をクリア。マップ獲得 +" + state.CurrentPool + (result.BonusAmount > 0 ? " / clear +" + result.BonusAmount : string.Empty),
                "Cleared " + mapLabel + ". Map earned +" + state.CurrentPool + (result.BonusAmount > 0 ? " / clear +" + result.BonusAmount : string.Empty),
                "已通关 " + mapLabel + "。本图获得 +" + state.CurrentPool + (result.BonusAmount > 0 ? " / clear +" + result.BonusAmount : string.Empty)));
        }

        private void HandleDeath(DoomDeathEvent deathEvent)
        {
            var state = CurrentRoundState;
            var result = _roundController.HandleDeath();
            RefreshHud();
            _backend?.SavePersistentNow();

            DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                "死亡。マップ獲得分 +" + state.CurrentPool + " はすでに支払い済み。",
                "Death. Map earnings +" + state.CurrentPool + " were already paid instantly.",
                "死亡。本图获得的 +" + state.CurrentPool + " 已即时发放。"));
            RefreshHud();
        }

        private void HandleRateSelectionInput()
        {
            if (!CurrentRoundState.RateSelectionOpen)
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

        private bool TryBeginRound(DoomRewardRate rate, bool showInsufficientPopup, bool chargeEntryFee = true)
        {
            var entryCost = chargeEntryFee ? DoomRewardRoundLogic.GetEntryCost(rate) : 0;
            var result = _roundController.TryBeginRound(rate, CanSpendCasinoCoins(entryCost), chargeEntryFee);
            if (result.InsufficientFunds)
            {
                if (showInsufficientPopup)
                {
                    ShowProgressText(Localize("チップ不足", "NOT ENOUGH CHIPS", "筹码不足"), FontColor.Bad);
                }

                RefreshHud();
                return false;
            }

            if (!TrySpendCasinoCoins(result.EntryCost))
            {
                if (showInsufficientPopup)
                {
                    ShowProgressText(Localize("チップ不足", "NOT ENOUGH CHIPS", "筹码不足"), FontColor.Bad);
                }

                _roundController.HandleMapStart(CurrentRoundState.Skill, CurrentRoundState.MapCode, CurrentRoundState.MapTitle);
                RefreshHud();
                return false;
            }

            _active = true;
            _overlay?.HideRateSelection();
            SetCursorCaptured(true);
            if (result.EntryCost > 0)
            {
                ShowProgressText(Localize("開始料 ", "SESSION ENTRY ", "会话入场 ") + result.EntryCost, FontColor.Great);
            }
            RefreshHud();
            return true;
        }

        private void RefreshHud()
        {
            if (_overlay == null)
            {
                return;
            }

            _overlay.SetHud(BuildHudViewModel());
        }

        private void RefreshRateSelectionOverlay()
        {
            if (_overlay == null)
            {
                return;
            }

            _overlay.ShowRateSelection(BuildRateSelectionViewModel());
        }

        private DoomHudViewModel BuildHudViewModel()
        {
            return DoomRewardPresentationBuilder.BuildHudViewModel(CurrentRoundState, Localize);
        }

        private DoomRateSelectionViewModel BuildRateSelectionViewModel()
        {
            return DoomRewardPresentationBuilder.BuildRateSelectionViewModel(
                CurrentRoundState,
                _rateSelectionCursor,
                EClass.pc?.GetCurrency(CasinoCoinId) ?? 0,
                Localize);
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

        private static bool CanSpendCasinoCoins(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            return EClass.pc != null && EClass.pc.GetCurrency(CasinoCoinId) >= amount;
        }

        private void AbandonCurrentRound(bool showPopup)
        {
            var state = CurrentRoundState;
            _roundController.HandleAbort();
            var hadRoundState = state.CurrentRate != DoomRewardRate.None || state.CurrentPool > 0 || state.RoundActive;
            RefreshHud();
            if (showPopup && hadRoundState)
            {
                ShowProgressText(
                    Localize("ラウンド終了", "ROUND ENDED", "回合结束"),
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
                "+" + poolAdd + " チップ",
                "+" + poolAdd + " CHIPS",
                "+" + poolAdd + " 筹码") + (string.IsNullOrWhiteSpace(multiplierText) ? string.Empty : " " + multiplierText);

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
                _sessionTookOverWorldBgm = true;
                _doomBgmActive = true;
                _nextDoomBgmIndex = 0;
                _nextDoomBgmRetryAt = 0f;
                DoomBgmRuntime.TryStopCurrentBgm("[JustDoomIt] Failed to stop existing BGM before DOOM session: ");
                PlayNextDoomBgm();
                DoomDiagnostics.Info("[JustDoomIt] DOOM session music: custom soundtrack.");
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Failed to start DOOM BGM sequence: " + ex.Message);
            }
        }

        private void StopDoomBgm()
        {
            var restoreWorldBgm = _sessionTookOverWorldBgm;
            _sessionTookOverWorldBgm = false;
            if (!_doomBgmActive && !restoreWorldBgm)
            {
                return;
            }

            if (_doomBgmActive)
            {
                _doomBgmActive = false;
                DoomBgmRuntime.TryStopCurrentBgm("[JustDoomIt] Failed to stop DOOM BGM playback: ");
            }

            if (restoreWorldBgm)
            {
                DoomBgmRuntime.TryRefreshZoneBgm("[JustDoomIt] Failed to restore zone BGM after DOOM session: ");
            }

            DoomDiagnostics.Info("[JustDoomIt] DOOM session music stopped.");
        }

        private void UpdateDoomBgmPlayback()
        {
            if (!_active || (!_doomBgmActive && !_sessionTookOverWorldBgm))
            {
                return;
            }

            var sound = DoomBgmRuntime.TryGetSoundManager();
            if (sound == null)
            {
                return;
            }

            if (Time.unscaledTime < _nextDoomBgmRetryAt)
            {
                return;
            }

            var hasPlayingBgm = sound.sourceBGM != null && sound.sourceBGM.isPlaying;
            var currentId = sound.currentBGM != null ? sound.currentBGM.id : string.Empty;
            var isDoomBgm = IsDoomBgmId(currentId);

            if (_doomBgmActive && hasPlayingBgm && isDoomBgm)
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
            var sound = DoomBgmRuntime.TryGetSoundManager();
            if (!_doomBgmActive || sound == null || DoomBgmIds.Length == 0)
            {
                return;
            }

            var id = DoomBgmIds[_nextDoomBgmIndex % DoomBgmIds.Length];
            _nextDoomBgmIndex = (_nextDoomBgmIndex + 1) % DoomBgmIds.Length;
            var bgm = sound.PlayBGM(id);
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
            var entry = DoomRewardRoundLogic.GetEntryCost(DoomRewardRoundLogic.GetFixedRate());
            DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                "報酬ルール: 新規開始またはCONTINUE時に固定参加費 " + entry + " を1回だけ支払う。撃破報酬とシークレット報酬は即時支給され、連続キルごとに +35 ずつ伸び、上限は難易度で決まる。被弾すると連キルボーナスだけがリセットされ、クリア時は追加+1000。死亡やESCでの追加精算や没収はない。",
                "Reward rules: START OVER and CONTINUE pay the fixed entry fee of " + entry + " once per session. Kill and secret rewards are paid instantly, growing by +35 per consecutive kill up to a difficulty-based cap. Taking damage only resets the kill-streak bonus, and clears add +1000. Death and ESC do not trigger extra cash-out or loss.",
                "奖励规则：每次新开或CONTINUE时只支付一次固定入场费 " + entry + "。击杀和秘密奖励会即时发放，每次连杀额外 +35，上限取决于难度。受伤只会重置连杀奖励，通关额外 +1000。死亡或ESC都不会再结算或没收额外奖励。"));
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

