using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Elin_NiComment.Llm
{
    public class LlmReactionService : MonoBehaviour
    {
        private static LlmReactionService _instance;
        public static LlmReactionService Instance => _instance;

        private const string SystemPrompt =
            "あなたはニコニコ動画のゲーム実況配信の視聴者たちです。\n" +
            "以下のゲーム内イベントを見て、視聴者たちのリアクションをJSON形式で返してください。\n\n" +
            "出力形式:\n" +
            "{\n" +
            "  \"hype\": 1-5の盛り上がり度,\n" +
            "  \"comments\": [匿名コメントの配列],\n" +
            "  \"kotehan\": [コテハン付きコメントの配列],\n" +
            "  \"barrage\": [弾幕コメントの配列]\n" +
            "}\n\n" +
            "ルール:\n" +
            "- comments: 普通のコメント。1件ずつ順番に流れる\n" +
            "- kotehan: Elinの住人として投稿。{\"n\":\"キャラ名\",\"t\":\"本文\"}\n" +
            "- barrage: ツッコミどころへの一斉反応。全部同時に画面に出る（乱用しない）\n" +
            "- 地味なイベントはスルーしてよい。全イベントに反応する必要はない\n" +
            "- 面白い・意外なイベントにだけ厚く反応する\n" +
            "- 1コメント最大15文字。ナレーションをそのまま繰り返さない\n\n" +
            "--- 出力例 ---\n\n" +
            "イベント: あなたは畑に種を蒔いた / 雨が降り始めた / ロイターがパンを食べた\n" +
            "{\"hype\":1,\"comments\":[\"農業ゲー始まったな\"],\"kotehan\":[],\"barrage\":[]}\n\n" +
            "イベント: ゾンビに52ダメージ / 鉄のインゴットを手に入れた\n" +
            "{\"hype\":3,\"comments\":[\"ナイス撃破\",\"鉄は助かる\",\"結構削るやんけ\"]," +
            "\"kotehan\":[{\"n\":\"オパートス\",\"t\":\"いい戦いだ\"}],\"barrage\":[]}\n\n" +
            "イベント: 王が現れた！/ ロイターが演奏で人を殺した / 伝説の剣をドロップした\n" +
            "{\"hype\":5,\"comments\":[\"王様きたあああ\",\"ロイターさん容赦ねぇｗ\"," +
            "\"神ドロップktkr!!\",\"888888888\"]," +
            "\"kotehan\":[],\"barrage\":[\"ちょwww\",\"殺人演奏\",\"ヒエッ…\",\"死んでて草\"]}\n\n" +
            "イベント: 猫が爆発した / エーテル病悪化 / 自宅が炎上した\n" +
            "{\"hype\":5,\"comments\":[\"ファッ！？\",\"まーた猫が爆発してる…\"," +
            "\"自宅がああああ( ﾟдﾟ)\",\"カオスすぎて草\"]," +
            "\"kotehan\":[{\"n\":\"エヘカトル\",\"t\":\"猫ちゃん…にゃ\"}]," +
            "\"barrage\":[\"は？\",\"は？\",\"意味不明で草\",\"いつものElin\"]}\n\n" +
            "イベント: プチが死んだ！/ あなたはプチの墓を建てた\n" +
            "{\"hype\":2,\"comments\":[\"あああああああ\",\"うそだろ…\",\"RIP\",\"復活させようぜ\"]," +
            "\"kotehan\":[{\"n\":\"ジュア\",\"t\":\"安らかに…\"}]," +
            "\"barrage\":[\"(´；ω；`)\",\"えぇ...\",\"うわぁぁぁ\"]}\n\n" +
            "--- 例ここまで ---\n\n" +
            "--- コテハンで使えるキャラ ---\n" +
            "- エヘカトル: 幸運の女神。猫好き天然、語尾に「にゃ」\n" +
            "- ジュア: 癒しの女神。慈愛深く穏やか\n" +
            "- ルルウィ: 風の女神。高飛車でプライドが高い\n" +
            "- オパートス: 大地の神。豪快で戦い好き\n" +
            "- クミロミ: 収穫の神。内向的で植物のことばかり考えている\n" +
            "- マニ: 機械の神。知的でクール\n" +
            "- イツパロトル: 元素の神。荒々しく激情的\n" +
            "- ラーネイレ: エレアの女戦士。真面目で心優しい\n" +
            "- ロミアス: エレアの男性。皮肉屋だが仲間思い\n\n" +
            "ルール:\n" +
            "- コテハンは全コメントの3割程度。大半はcommentsの匿名コメントにする\n" +
            "- キャラの個性に合った反応にする\n" +
            "- barrageにはコテハンを入れない（commentsかkotehanに入れる）";

        private const int MaxQueueSize = 30;
        private const int MaxCommentLength = 30;

        private struct TimestampedEvent
        {
            public string Text;
            public float Time;
        }

        private const float DefaultBackoffSeconds = 60f;
        private const float StatsLogInterval = 60f;

        private ILlmProvider _provider;
        private readonly Queue<TimestampedEvent> _eventQueue = new Queue<TimestampedEvent>();
        private readonly Queue<CommentRequest> _dripQueue = new Queue<CommentRequest>();
        private float _batchTimer;
        private float _dripTimer;
        private float _currentHypeMultiplier = 1f;
        private bool _requestInFlight;
        private float _backoffUntil;

        // --- Call stats ---
        private readonly List<float> _callTimestamps = new List<float>();
        private int _totalCalls;
        private int _totalSuccess;
        private int _total429;
        private int _totalOtherErrors;
        private float _nextStatsLog;

        public bool IsActive => _provider != null && _provider.IsAvailable;

        public void Initialize(ILlmProvider provider)
        {
            _instance = this;
            _provider = provider;
            LlmConfig.OnProviderSettingsChanged += ReloadProvider;
            Debug.Log($"[NiComment] LLM initialized: {provider.Name} (available={provider.IsAvailable})");
        }

        private void OnDestroy()
        {
            LlmConfig.OnProviderSettingsChanged -= ReloadProvider;
            if (_instance == this) _instance = null;
        }

        private void ReloadProvider()
        {
            if (_requestInFlight)
            {
                Debug.Log("[NiComment] Config changed, will reload provider after current request");
                return;
            }

            var newProvider = LlmProviderFactory.Create();
            _provider = newProvider;
            _backoffUntil = 0f;
            Debug.Log($"[NiComment] Provider hot-swapped: {newProvider.Name} (available={newProvider.IsAvailable})");
        }

        public void EnqueueGameEvent(string text)
        {
            if (!IsActive) return;

            if (!EventFilter.TryFilter(text, out var cleaned))
            {
#if DEBUG
                Debug.Log($"[NiComment] Event filtered out: {text}");
#endif
                return;
            }

            while (_eventQueue.Count >= MaxQueueSize)
                _eventQueue.Dequeue();

            _eventQueue.Enqueue(new TimestampedEvent
            {
                Text = cleaned,
                Time = Time.unscaledTime
            });
#if DEBUG
            Debug.Log($"[NiComment] Event queued ({_eventQueue.Count}/{MaxQueueSize}): {cleaned}");
#endif
        }

        private void Update()
        {
            DripComments();
            LogStatsIfDue();

            if (!IsActive || _eventQueue.Count == 0) return;
            if (_requestInFlight) return;
            if (Time.unscaledTime < _backoffUntil) return;

            _batchTimer += Time.unscaledDeltaTime;
            if (_batchTimer < LlmConfig.BatchInterval.Value) return;

            FlushBatch();
        }

        private float _nextDripInterval;

        private void DripComments()
        {
            if (_dripQueue.Count == 0) return;

            _dripTimer += Time.unscaledDeltaTime;
            if (_dripTimer < _nextDripInterval) return;

            _dripTimer = 0f;
            var jitter = UnityEngine.Random.Range(0.6f, 1.4f);
            _nextDripInterval = LlmConfig.DripInterval.Value * _currentHypeMultiplier * jitter;

            var req = _dripQueue.Dequeue();
            NiCommentAPI.Send(req);
        }

        private void FlushBatch()
        {
            var now = Time.unscaledTime;
            var maxAge = LlmConfig.EventMaxAge.Value;
            var maxBatch = LlmConfig.MaxBatchSize.Value;
            var events = new List<string>();

            while (_eventQueue.Count > 0 && events.Count < maxBatch)
            {
                var ev = _eventQueue.Dequeue();
                if (now - ev.Time > maxAge)
                {
                    Debug.Log($"[NiComment] Discarding stale event ({now - ev.Time:F1}s old): {ev.Text}");
                    continue;
                }
                events.Add(ev.Text);
            }

            if (events.Count == 0) return;

            var userMessage = string.Join("\n", events);
            _requestInFlight = true;

#if DEBUG
            Debug.Log($"[NiComment] LLM prompt:\n{userMessage}");
#endif

            _provider.SendAsync(SystemPrompt, userMessage, (response, error) =>
            {
                _requestInFlight = false;
                _batchTimer = 0f;

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning($"[NiComment] LLM error: {error}");

                    if (Is429Error(error))
                    {
                        RecordCall("429");
                        var delay = ParseRetryDelay(error);
                        _backoffUntil = Time.unscaledTime + delay;
                        Debug.LogWarning($"[NiComment] Rate limited (429). Backing off for {delay:F0}s");
                        // No fallback on 429 — silence is correct for transient rate limits
                        return;
                    }

                    RecordCall("err");
                    SendFallbackComments(events.Count);
                    return;
                }

                RecordCall("ok");
#if DEBUG
                Debug.Log($"[NiComment] LLM response:\n{response}");
#endif
                ProcessLlmResponse(response);
            });
        }

        private void RecordCall(string result)
        {
            var now = Time.unscaledTime;
            _callTimestamps.Add(now);
            _totalCalls++;

            if (result == "ok") _totalSuccess++;
            else if (result == "429") _total429++;
            else _totalOtherErrors++;

            // Prune timestamps older than 1 hour
            while (_callTimestamps.Count > 0 && now - _callTimestamps[0] > 3600f)
                _callTimestamps.RemoveAt(0);

            var last1m = 0;
            var last5m = 0;
            for (int i = _callTimestamps.Count - 1; i >= 0; i--)
            {
                var age = now - _callTimestamps[i];
                if (age <= 60f) last1m++;
                if (age <= 300f) last5m++;
                else break;
            }

            Debug.Log($"[NiComment] API call #{_totalCalls} [{result}] | 1m:{last1m} 5m:{last5m} 1h:{_callTimestamps.Count} | total ok:{_totalSuccess} 429:{_total429} err:{_totalOtherErrors}");
        }

        private void LogStatsIfDue()
        {
            var now = Time.unscaledTime;
            if (now < _nextStatsLog) return;
            _nextStatsLog = now + StatsLogInterval;

            if (_totalCalls == 0) return;

            var last1m = 0;
            var last5m = 0;
            for (int i = _callTimestamps.Count - 1; i >= 0; i--)
            {
                var age = now - _callTimestamps[i];
                if (age <= 60f) last1m++;
                if (age <= 300f) last5m++;
                else break;
            }

            Debug.Log($"[NiComment] Stats | 1m:{last1m} 5m:{last5m} 1h:{_callTimestamps.Count} | total ok:{_totalSuccess} 429:{_total429} err:{_totalOtherErrors} | queue:{_eventQueue.Count} drip:{_dripQueue.Count} inflight:{_requestInFlight} backoff:{(_backoffUntil > now ? $"{_backoffUntil - now:F0}s" : "no")}");
        }

        private static bool Is429Error(string error)
        {
            return error != null && error.StartsWith("[429]");
        }

        private static float ParseRetryDelay(string error)
        {
            // Providers format errors as "[code] message". Try to extract retry-after hint.
            // For now, use default backoff since retry-after is not passed through the provider interface.
            return DefaultBackoffSeconds;
        }

        private void ProcessLlmResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return;

            try
            {
                var obj = JObject.Parse(response);
                var hype = obj["hype"]?.Value<int>() ?? 3;
                _currentHypeMultiplier = HypeToMultiplier(hype);

                // comments: anonymous strings, dripped
                var comments = obj["comments"] as JArray;
                // kotehan: {n, t} objects, dripped
                var kotehan = obj["kotehan"] as JArray;
                // barrage: strings, all sent immediately
                var barrage = obj["barrage"] as JArray;

#if DEBUG
                Debug.Log($"[NiComment] Parsed hype={hype}, comments={comments?.Count ?? 0}, kotehan={kotehan?.Count ?? 0}, barrage={barrage?.Count ?? 0}");
#endif

                if (comments != null)
                {
                    foreach (var item in comments)
                    {
                        var text = Truncate(item.ToString());
                        if (!string.IsNullOrEmpty(text))
                            _dripQueue.Enqueue(new CommentRequest(text));
                    }
                }

                if (kotehan != null)
                {
                    foreach (var item in kotehan)
                    {
                        var name = item["n"]?.ToString();
                        var text = Truncate(item["t"]?.ToString());
                        if (!string.IsNullOrEmpty(text))
                            _dripQueue.Enqueue(KotehanRegistry.ToRequest(name, text));
                    }
                }

                if (barrage != null)
                {
                    foreach (var item in barrage)
                    {
                        var text = Truncate(item.ToString());
                        if (!string.IsNullOrEmpty(text))
                            NiCommentAPI.Send(text);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NiComment] Failed to parse LLM JSON: {ex.Message}\nRaw: {response}");
                SalvageComments(response);
            }
        }

        // Regex to extract quoted Japanese strings from malformed JSON
        private static readonly Regex SalvageRegex =
            new Regex(@"""([^""]{1,30})""", RegexOptions.Compiled);

        private void SalvageComments(string response)
        {
            var matches = SalvageRegex.Matches(response);
            var count = 0;
            foreach (Match m in matches)
            {
                var text = m.Groups[1].Value;

                // Skip JSON keys and structural values
                if (text == "hype" || text == "comments" || text == "n" || text == "t")
                    continue;
                // Skip pure numbers
                if (int.TryParse(text, out _))
                    continue;

                var truncated = Truncate(text);
                if (!string.IsNullOrEmpty(truncated))
                {
                    _dripQueue.Enqueue(new CommentRequest(truncated));
                    count++;
                }
            }
#if DEBUG
            Debug.Log($"[NiComment] Salvaged {count} comments from malformed JSON");
#endif
        }

        private static string Truncate(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length > MaxCommentLength ? text.Substring(0, MaxCommentLength) : text;
        }

        private static float HypeToMultiplier(int hype)
        {
            if (hype <= 2) return 1.5f;
            if (hype >= 4) return 0.5f;
            return 1f;
        }

        private void SendFallbackComments(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var text = GetRandomFallbackText();
                if (text != null)
                    _dripQueue.Enqueue(new CommentRequest(text));
            }
        }

        private static readonly string[] FallbackEventIds =
        {
            CommentTexts.NpcEat, CommentTexts.NpcMusic, CommentTexts.BigDamage
        };

        private string GetRandomFallbackText()
        {
            var eventId = FallbackEventIds[UnityEngine.Random.Range(0, FallbackEventIds.Length)];
            return CommentTexts.GetRandom(eventId);
        }
    }
}
