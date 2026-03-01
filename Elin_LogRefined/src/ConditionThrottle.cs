using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elin_LogRefined
{
    /// <summary>
    /// Time-based throttle for condition log messages.
    /// Shared by all condition-related patches (PhaseMsg, Kill, PatchChara).
    /// Key = (conditionId, ownerUid); phase is intentionally excluded so that
    /// the entire Kill/Re-Add cycle is collapsed into one message window.
    ///
    /// Two access patterns:
    ///   ShouldThrottle - check + record (used by PatchChara, the authoritative gate)
    ///   IsThrottled    - check only     (used by PhaseMsg / Kill, which fire before PatchChara)
    /// </summary>
    public static class ConditionThrottle
    {
        private static readonly Dictionary<(int, int), float> _recent = new Dictionary<(int, int), float>();
        private static int _callCount;

        /// <summary>
        /// Check-and-record: returns true if within cooldown (suppress).
        /// On first call for a key, records timestamp and returns false.
        /// Used by PatchChara postfix (fires after AddCondition returns).
        /// </summary>
        public static bool ShouldThrottle(int conditionId, int ownerUid)
        {
            if (!ModConfig.ThrottleConditionLog.Value)
                return false;

            float cooldown = ModConfig.ConditionThrottleCooldown.Value;
            var key = (conditionId, ownerUid);
            float now = Time.time;

            if (_recent.TryGetValue(key, out float last) && now - last < cooldown)
                return true;

            _recent[key] = now;
            CleanupIfNeeded(now, cooldown);
            return false;
        }

        /// <summary>
        /// Check-only: returns true if a record exists within cooldown.
        /// Does NOT create or update records.
        /// Used by PhaseMsg / Kill prefixes (fire inside AddCondition, before PatchChara).
        /// </summary>
        public static bool IsThrottled(int conditionId, int ownerUid)
        {
            if (!ModConfig.ThrottleConditionLog.Value)
                return false;

            float cooldown = ModConfig.ConditionThrottleCooldown.Value;
            var key = (conditionId, ownerUid);
            float now = Time.time;

            return _recent.TryGetValue(key, out float last) && now - last < cooldown;
        }

        /// <summary>
        /// Purge stale entries every 100 calls to prevent unbounded growth.
        /// </summary>
        private static void CleanupIfNeeded(float now, float cooldown)
        {
            if (++_callCount < 100)
                return;

            _callCount = 0;
            float threshold = cooldown * 2f;
            var stale = _recent.Where(kv => now - kv.Value > threshold).Select(kv => kv.Key).ToList();
            foreach (var k in stale)
                _recent.Remove(k);
        }
    }
}
