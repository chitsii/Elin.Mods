using BepInEx.Logging;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Elin_ModTemplate
{
    public sealed class DoomSessionManager : MonoBehaviour
    {
        private const float KillVoiceDelaySeconds = 0.18f;
        private const string DoomPlaylistId = "Zone_Casino";

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
        private const float KillVoiceSampleGain = 2.6f;
        private int _nextKillVoiceIndex;
        private int _processedKillCount;
        private int _processedKillPayout;
        private int _processedClearEvents;
        private int _processedBossClearEvents;
        private int _sessionCoinsEarned;
        private int _lastAnnouncedStreak;
        private int _lastPopupStreak;
        private bool _isSfxDucked;
        private bool _exitConfirmOpen;

        public static void Ensure(ManualLogSource logger)
        {
            if (Instance != null) return;

            var go = new GameObject("JustDoomIt_Session");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<DoomSessionManager>();
            Instance._logger = logger;
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

                var wad = DoomWadLocator.FindWad();
                if (string.IsNullOrWhiteSpace(wad))
                {
                    DoomDiagnostics.Warn("[JustDoomIt] No WAD found. Keep vanilla TV behavior.");
                    result = false;
                    return false;
                }

                StopSession();
                _overlay = DoomOverlayDisplay.Create();
                _backend = new ManagedDoomBackend(ModConfig.DoomWidth.Value, ModConfig.DoomHeight.Value);
                if (!_backend.Initialize(wad, _logger))
                {
                    DoomDiagnostics.Error("[JustDoomIt] Doom backend initialization failed.");
                    StopSession();
                    result = false;
                    return true;
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
                _isSfxDucked = false;
                DoomKillFeed.Reset();
                EnsureKillVoiceSource();
                EnsureKillVoiceClipsLoaded();
                StartDoomPlaylist();
                SetCursorCaptured(true);
                EInput.Consume(consumeAxis: true, _skipFrame: 2);
                DoomDiagnostics.Info("[JustDoomIt] DOOM session started: " + wad);
                LogRandomStartLine();
                LogRewardRules();
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
                    RequestExitConfirmation();
                    EInput.Consume(consumeAxis: true, _skipFrame: 1);
                    return;
                }

                if (_exitConfirmOpen)
                {
                    _backend.SubmitInput(default);
                    return;
                }

                if (_active)
                {
                    SetCursorCaptured(true);
                    EInput.Consume(consumeAxis: true, _skipFrame: 1);
                    var input = DoomInputState.ReadFromUnity();
                    _backend.SubmitInput(input);
                }
                else
                {
                    _backend.SubmitInput(default);
                }

                _backend.Tick(Time.unscaledDeltaTime);
                _overlay?.Upload(_backend.GetFrameBuffer());
                ProcessChipRewards(_backend.Stats);
                UpdateKillVoicePlayback();
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] Session Update failed. Stopping session.", ex);
                StopSession();
            }
        }

        public void StopSession()
        {
            if (_sessionCoinsEarned > 0)
            {
                DoomDiagnostics.Info("[JustDoomIt] " + Localize(
                    "DOOMカジノ報酬: 合計" + _sessionCoinsEarned + "枚のチップを獲得。",
                    "DOOM casino payout: earned " + _sessionCoinsEarned + " chips.",
                    "DOOM赌场奖励：本局共获得" + _sessionCoinsEarned + "枚筹码。"));
            }

            if (_backend != null)
            {
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
            _isSfxDucked = false;
            _killVoiceQueue.Clear();
            DoomKillFeed.Reset();
            _exitConfirmOpen = false;
            if (_killVoiceSource != null)
            {
                _killVoiceSource.Stop();
            }
            StopDoomPlaylist();
            _backend?.SetSfxDucking(false);
        }

        private void RequestExitConfirmation()
        {
            if (_exitConfirmOpen)
            {
                return;
            }

            _exitConfirmOpen = true;
            SetCursorCaptured(false);
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

            var line = lines[Random.Range(0, lines.Length)];
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
                GrantCasinoChips(add);
                ShowCoinPopup(add, stats.CurrentKillStreak);
            }

            while (_processedClearEvents < stats.ClearEventCount)
            {
                _processedClearEvents++;
                const int clearBonus = 5000;
                GrantCasinoChips(clearBonus);
                ShowCoinPopup(clearBonus, 0);
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
                ShowStreakPopup(stats.CurrentKillStreak);
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
                if (!_isSfxDucked)
                {
                    _backend?.SetSfxDucking(true);
                    _isSfxDucked = true;
                }
                return;
            }
            else if (_isSfxDucked)
            {
                _backend?.SetSfxDucking(false);
                _isSfxDucked = false;
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
            if (!_isSfxDucked)
            {
                _backend?.SetSfxDucking(true);
                _isSfxDucked = true;
            }
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

        private static void StartDoomPlaylist()
        {
            try
            {
                if (EClass.Sound == null)
                {
                    return;
                }

                var playlist = EClass.Sound.GetPlaylist(DoomPlaylistId);

                if (playlist == null)
                {
                    DoomDiagnostics.Warn("[JustDoomIt] Playlist not found: " + DoomPlaylistId);
                    return;
                }

                EClass.Sound.SetBGMPlaylist(playlist);
                EClass.Sound.NextBGM();
                DoomDiagnostics.Info("[JustDoomIt] Playlist started: " + DoomPlaylistId);
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Failed to start playlist: " + ex.Message);
            }
        }

        private static void StopDoomPlaylist()
        {
            try
            {
                EClass.Sound?.StopBGM();
                EClass.Sound?.ResetPlaylist();
                DoomDiagnostics.Info("[JustDoomIt] Playlist stopped.");
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Failed to stop playlist: " + ex.Message);
            }
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
                "報酬ルール: 撃破でチップ獲得。連続キルは撃破ごとに1.5倍、被弾でリセット。クリア+5000、ボス+10000。",
                "Reward rules: kills grant chips; each consecutive kill scales by x1.5 and resets when you take damage. Clear +5000, boss +10000.",
                "奖励规则：击杀得筹码；连杀每次按1.5倍递增，受伤即重置。通关+5000，Boss关+10000。"));
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

        private void ShowStreakPopup(int streak)
        {
            if (streak < 2)
            {
                return;
            }

            var text = Localize(
                "連続キル x" + streak,
                "Kill streak x" + streak,
                "连杀 x" + streak);
            ShowProgressText(text, FontColor.Great);
        }

        private void ShowProgressText(string text, FontColor color)
        {
            if (text.IsEmpty())
            {
                return;
            }

            WidgetPopText.Say(text, color);
            Msg.SayRaw(text);
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
