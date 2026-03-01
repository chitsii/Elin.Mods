using System;
using System.Collections.Generic;

namespace Elin_QuestMod.Drama
{
    /// <summary>
    /// Drama-side eval entry points.
    /// Scenario scripts call this runtime through DramaBuilder helpers.
    /// </summary>
    public static class DramaRuntime
    {
        private const int RawMinutesPerDay = 1440;
        private static IDramaDependencyResolver _resolver = new NullDramaDependencyResolver();

        public static void ConfigureResolver(IDramaDependencyResolver resolver)
        {
            _resolver = resolver ?? new NullDramaDependencyResolver();
        }

        public static void ResolveFlag(string key, string targetFlagKey)
        {
            if (string.IsNullOrEmpty(targetFlagKey))
            {
                return;
            }

            if (!_resolver.TryResolveBool(key, out bool value))
            {
                ModLog.Warn("DramaRuntime.ResolveFlag: unresolved key '" + key + "'");
                return;
            }

            if (EClass.player?.dialogFlags == null)
            {
                ModLog.Warn(
                    "DramaRuntime.ResolveFlag: skipped (dialogFlags unavailable), key="
                    + key
                    + ", target="
                    + targetFlagKey);
                return;
            }

            EClass.player.dialogFlags[targetFlagKey] = value ? 1 : 0;
            ModLog.Info(
                "DramaRuntime.ResolveFlag: key="
                + key
                + " -> "
                + targetFlagKey
                + "="
                + (value ? "1" : "0"));
        }

        public static void ResolveRun(string key)
        {
            if (!_resolver.TryExecute(key))
            {
                ModLog.Warn("DramaRuntime.ResolveRun: unresolved key '" + key + "'");
                return;
            }

            ModLog.Info("DramaRuntime.ResolveRun: executed key=" + key);
        }

        /// <summary>
        /// Evaluate multiple dialogFlags with ALL semantics and write 1/0 to target.
        /// Empty source list is treated as true (same semantics as QuestCondition.All()).
        /// </summary>
        public static void ResolveFlagsAll(string sourceFlagsCsv, string targetFlagKey)
        {
            ResolveFlags(sourceFlagsCsv, targetFlagKey, requireAll: true);
        }

        /// <summary>
        /// Evaluate multiple dialogFlags with ANY semantics and write 1/0 to target.
        /// Empty source list is treated as false (same semantics as QuestCondition.Any()).
        /// </summary>
        public static void ResolveFlagsAny(string sourceFlagsCsv, string targetFlagKey)
        {
            ResolveFlags(sourceFlagsCsv, targetFlagKey, requireAll: false);
        }

        /// <summary>
        /// Resolve elapsed-time gate in raw minutes and write 1/0 to target.
        /// If lastRaw <= 0 or threshold <= 0, resolves true.
        /// </summary>
        public static void ResolveCooldownElapsedRaw(
            string lastRawFlagKey,
            string targetFlagKey,
            int thresholdRawMinutes)
        {
            if (string.IsNullOrEmpty(targetFlagKey))
            {
                return;
            }

            var flags = EClass.player?.dialogFlags;
            if (flags == null)
            {
                ModLog.Warn(
                    "DramaRuntime.ResolveCooldownElapsedRaw: skipped (dialogFlags unavailable), target="
                    + targetFlagKey);
                return;
            }

            if (EClass.world?.date == null)
            {
                ModLog.Warn(
                    "DramaRuntime.ResolveCooldownElapsedRaw: skipped (world/date unavailable), target="
                    + targetFlagKey);
                return;
            }

            int lastRaw = 0;
            if (!string.IsNullOrEmpty(lastRawFlagKey))
            {
                flags.TryGetValue(lastRawFlagKey, out lastRaw);
            }

            int currentRaw = EClass.world.date.GetRaw();
            bool elapsed = true;
            if (thresholdRawMinutes > 0 && lastRaw > 0)
            {
                elapsed = (currentRaw - lastRaw) >= thresholdRawMinutes;
            }

            flags[targetFlagKey] = elapsed ? 1 : 0;
            ModLog.Info(
                "DramaRuntime.ResolveCooldownElapsedRaw: lastRawFlag="
                + (lastRawFlagKey ?? string.Empty)
                + ", lastRaw="
                + lastRaw
                + ", currentRaw="
                + currentRaw
                + ", thresholdRaw="
                + thresholdRawMinutes
                + " -> "
                + targetFlagKey
                + "="
                + (elapsed ? "1" : "0"));
        }

        /// <summary>
        /// Resolve elapsed-time gate in days and write 1/0 to target.
        /// </summary>
        public static void ResolveCooldownElapsedDays(
            string lastRawFlagKey,
            string targetFlagKey,
            int cooldownDays = 1)
        {
            int thresholdRawMinutes = cooldownDays <= 0 ? 0 : cooldownDays * RawMinutesPerDay;
            ResolveCooldownElapsedRaw(lastRawFlagKey, targetFlagKey, thresholdRawMinutes);
        }

        /// <summary>
        /// Persist current world raw time into a dialogFlag.
        /// </summary>
        public static void StampCurrentRawTime(string targetFlagKey)
        {
            if (string.IsNullOrEmpty(targetFlagKey))
            {
                return;
            }

            var flags = EClass.player?.dialogFlags;
            if (flags == null)
            {
                ModLog.Warn(
                    "DramaRuntime.StampCurrentRawTime: skipped (dialogFlags unavailable), target="
                    + targetFlagKey);
                return;
            }

            if (EClass.world?.date == null)
            {
                ModLog.Warn(
                    "DramaRuntime.StampCurrentRawTime: skipped (world/date unavailable), target="
                    + targetFlagKey);
                return;
            }

            int currentRaw = EClass.world.date.GetRaw();
            flags[targetFlagKey] = currentRaw;
            ModLog.Info(
                "DramaRuntime.StampCurrentRawTime: "
                + targetFlagKey
                + "="
                + currentRaw);
        }

        private static void ResolveFlags(string sourceFlagsCsv, string targetFlagKey, bool requireAll)
        {
            if (string.IsNullOrEmpty(targetFlagKey))
            {
                return;
            }

            var flags = EClass.player?.dialogFlags;
            if (flags == null)
            {
                ModLog.Warn(
                    "DramaRuntime.ResolveFlags: skipped (dialogFlags unavailable), target="
                    + targetFlagKey);
                return;
            }

            List<string> sourceFlags = ParseCsv(sourceFlagsCsv);

            bool result;
            if (sourceFlags.Count == 0)
            {
                result = requireAll;
            }
            else if (requireAll)
            {
                result = true;
                for (int i = 0; i < sourceFlags.Count; i++)
                {
                    string key = sourceFlags[i];
                    bool ok = flags.TryGetValue(key, out int value) && value != 0;
                    if (!ok)
                    {
                        result = false;
                        break;
                    }
                }
            }
            else
            {
                result = false;
                for (int i = 0; i < sourceFlags.Count; i++)
                {
                    string key = sourceFlags[i];
                    if (flags.TryGetValue(key, out int value) && value != 0)
                    {
                        result = true;
                        break;
                    }
                }
            }

            flags[targetFlagKey] = result ? 1 : 0;
            ModLog.Info(
                "DramaRuntime.ResolveFlags"
                + (requireAll ? "All" : "Any")
                + ": sourceCount="
                + sourceFlags.Count
                + " -> "
                + targetFlagKey
                + "="
                + (result ? "1" : "0"));
        }

        private static List<string> ParseCsv(string csv)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(csv))
            {
                return result;
            }

            string[] values = csv.Split(',');
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i]?.Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    result.Add(value);
                }
            }

            return result;
        }

        // Backward-compatible entry points.
        public static void ResolveAndSetFlag(string depKey, string targetFlagKey)
        {
            ResolveFlag(depKey, targetFlagKey);
        }

        public static void RunDependencyCommand(string depKey)
        {
            ResolveRun(depKey);
        }
    }
}
