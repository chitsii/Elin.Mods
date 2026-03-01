using System;
using System.Collections.Generic;

namespace Elin_QuestMod.Quest
{
    public static class QuestStateService
    {
        // Legacy constants kept for compatibility with old callers.
        public const string LegacyPrefix = "questmod";
        public const string KeyCurrentPhase = "questmod.quest.current_phase";
        public const string PrefixDone = "questmod.quest.done.";
        public const string PrefixActive = "questmod.quest.active.";

        private const string LocalCurrentPhase = "quest.current_phase";
        private const string LocalDonePrefix = "quest.done.";
        private const string LocalActivePrefix = "quest.active.";
        private const string LocalBootstrap = "quest.bootstrap";
        private const string LocalDispatch = "quest.dispatch";

        public static string GetDefaultPrefix()
        {
            string modId = Plugin.ModGuid;
            if (string.IsNullOrWhiteSpace(modId))
            {
                return LegacyPrefix;
            }

            return NormalizePrefix(modId);
        }

        public static string BuildFlagKey(string localKey)
        {
            return BuildFlagKey(localKey, null);
        }

        public static string BuildFlagKey(string localKey, string explicitPrefix)
        {
            string normalizedLocal = NormalizeLocalKey(localKey);
            if (string.IsNullOrEmpty(normalizedLocal))
            {
                return string.Empty;
            }

            string prefix = ResolvePrefix(explicitPrefix);
            return prefix + "." + normalizedLocal;
        }

        public static string GetBootstrapFlagKey()
        {
            return BuildFlagKey(LocalBootstrap);
        }

        public static string GetDispatchFlagKey()
        {
            return BuildFlagKey(LocalDispatch);
        }

        public static string GetCurrentPhaseKey()
        {
            return GetCurrentPhaseKey(null);
        }

        public static string GetCurrentPhaseKey(string explicitPrefix)
        {
            return BuildFlagKey(LocalCurrentPhase, explicitPrefix);
        }

        public static string GetQuestDoneKey(string questId)
        {
            return GetQuestDoneKey(questId, null);
        }

        public static string GetQuestDoneKey(string questId, string explicitPrefix)
        {
            questId = NormalizeQuestId(questId);
            if (string.IsNullOrEmpty(questId))
            {
                return string.Empty;
            }

            return BuildFlagKey(LocalDonePrefix + questId, explicitPrefix);
        }

        public static string GetQuestActiveKey(string questId)
        {
            return GetQuestActiveKey(questId, null);
        }

        public static string GetQuestActiveKey(string questId, string explicitPrefix)
        {
            questId = NormalizeQuestId(questId);
            if (string.IsNullOrEmpty(questId))
            {
                return string.Empty;
            }

            return BuildFlagKey(LocalActivePrefix + questId, explicitPrefix);
        }

        public static int CheckQuestsForDispatch(string outputFlagKey, string questIdsCsv)
        {
            return CheckQuestsForDispatch(outputFlagKey, questIdsCsv, null);
        }

        public static int CheckQuestsForDispatch(string outputFlagKey, string questIdsCsv, string explicitPrefix)
        {
            var flags = GetFlags();
            if (flags == null)
            {
                return 0;
            }

            if (string.IsNullOrEmpty(outputFlagKey))
            {
                ModLog.Warn("CheckQuestsForDispatch skipped: outputFlagKey is empty.");
                return 0;
            }

            int selected = 0;
            var questIds = ParseCsv(questIdsCsv);
            for (int i = 0; i < questIds.Count; i++)
            {
                string questId = questIds[i];
                if (IsQuestDispatchTarget(questId, explicitPrefix))
                {
                    selected = i + 1; // 1-based. 0 is fallback.
                    break;
                }
            }

            flags[outputFlagKey] = selected;
            return selected;
        }

        public static void StartQuest(string questId)
        {
            StartQuest(questId, null);
        }

        public static void StartQuest(string questId, string explicitPrefix)
        {
            questId = NormalizeQuestId(questId);
            if (string.IsNullOrEmpty(questId))
            {
                return;
            }

            var flags = GetFlags();
            if (flags == null)
            {
                return;
            }

            string activeKey = GetQuestActiveKey(questId, explicitPrefix);
            string doneKey = GetQuestDoneKey(questId, explicitPrefix);

            flags[activeKey] = 1;
            if (!flags.ContainsKey(doneKey))
            {
                flags[doneKey] = 0;
            }
        }

        public static void CompleteQuest(string questId, int nextPhase = -1)
        {
            CompleteQuest(questId, nextPhase, null);
        }

        public static void CompleteQuest(string questId, int nextPhase, string explicitPrefix)
        {
            questId = NormalizeQuestId(questId);
            if (string.IsNullOrEmpty(questId))
            {
                return;
            }

            var flags = GetFlags();
            if (flags == null)
            {
                return;
            }

            string doneKey = GetQuestDoneKey(questId, explicitPrefix);
            string activeKey = GetQuestActiveKey(questId, explicitPrefix);

            flags[doneKey] = 1;
            flags[activeKey] = 0;
            if (nextPhase >= 0)
            {
                flags[GetCurrentPhaseKey(explicitPrefix)] = nextPhase;
            }
        }

        public static bool IsQuestCompleted(string questId)
        {
            return IsQuestCompleted(questId, null);
        }

        public static bool IsQuestCompleted(string questId, string explicitPrefix)
        {
            questId = NormalizeQuestId(questId);
            if (string.IsNullOrEmpty(questId))
            {
                return false;
            }

            string doneKey = GetQuestDoneKey(questId, explicitPrefix);
            return GetFlagInt(doneKey, 0) == 1;
        }

        public static bool IsQuestActive(string questId)
        {
            return IsQuestActive(questId, null);
        }

        public static bool IsQuestActive(string questId, string explicitPrefix)
        {
            questId = NormalizeQuestId(questId);
            if (string.IsNullOrEmpty(questId))
            {
                return false;
            }

            string activeKey = GetQuestActiveKey(questId, explicitPrefix);
            return GetFlagInt(activeKey, 0) == 1;
        }

        public static int GetCurrentPhase()
        {
            return GetCurrentPhase(null);
        }

        public static int GetCurrentPhase(string explicitPrefix)
        {
            return GetFlagInt(GetCurrentPhaseKey(explicitPrefix), 0);
        }

        public static void SetCurrentPhase(int phase)
        {
            SetCurrentPhase(phase, null);
        }

        public static void SetCurrentPhase(int phase, string explicitPrefix)
        {
            SetFlagInt(GetCurrentPhaseKey(explicitPrefix), phase);
        }

        public static int GetFlagInt(string key, int defaultValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                return defaultValue;
            }

            var flags = GetFlags();
            if (flags == null)
            {
                return defaultValue;
            }

            if (flags.TryGetValue(key, out int value))
            {
                return value;
            }

            return defaultValue;
        }

        public static void SetFlagInt(string key, int value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            var flags = GetFlags();
            if (flags == null)
            {
                return;
            }

            flags[key] = value;
        }

        private static bool IsQuestDispatchTarget(string questId, string explicitPrefix)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return false;
            }

            if (IsQuestCompleted(questId, explicitPrefix))
            {
                return false;
            }

            return IsQuestActive(questId, explicitPrefix);
        }

        private static string NormalizeQuestId(string questId)
        {
            if (questId == null)
            {
                return string.Empty;
            }

            return questId.Trim();
        }

        private static List<string> ParseCsv(string csv)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(csv))
            {
                return result;
            }

            var items = csv.Split(',');
            for (int i = 0; i < items.Length; i++)
            {
                var normalized = NormalizeQuestId(items[i]);
                if (!string.IsNullOrEmpty(normalized))
                {
                    result.Add(normalized);
                }
            }

            return result;
        }

        private static string ResolvePrefix(string explicitPrefix)
        {
            if (!string.IsNullOrWhiteSpace(explicitPrefix))
            {
                return NormalizePrefix(explicitPrefix);
            }

            return GetDefaultPrefix();
        }

        private static string NormalizePrefix(string prefix)
        {
            string normalized = prefix.Trim().Trim('.');
            if (string.IsNullOrEmpty(normalized))
            {
                return LegacyPrefix;
            }

            return normalized.ToLowerInvariant();
        }

        private static string NormalizeLocalKey(string localKey)
        {
            if (localKey == null)
            {
                return string.Empty;
            }

            return localKey.Trim().Trim('.');
        }

        private static Dictionary<string, int> GetFlags()
        {
            return EClass.player?.dialogFlags;
        }
    }
}
