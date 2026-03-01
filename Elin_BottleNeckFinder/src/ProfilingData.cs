using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Elin_BottleNeckFinder
{
    public static class ProfilingData
    {
        public struct MethodProfile
        {
            public string MethodName;
            public double AvgMs;
            public double PeakMs;
            public double AvgBaseMs;
            public double AvgModMs;
            public int AvgCallCount;
            public List<string> PatchOwnerNames;
        }

        public struct ModProfile
        {
            public string ModName;
            public string HarmonyId;
            public double AvgMs;
            public double PeakMs;
            public double CurrentMs;
            public List<MethodProfile> TopMethods;
        }

        private static int _totalFrames;

        // Cumulative totals per mod (harmonyId → total ms)
        private static readonly Dictionary<string, double> _modTotalMs
            = new Dictionary<string, double>();

        // Per-method per-mod cumulative: "harmonyId|methodName" → total ms
        private static readonly Dictionary<string, double> _methodTotalMs
            = new Dictionary<string, double>();

        private static readonly Dictionary<string, double> _methodBaseTotalMs
            = new Dictionary<string, double>();

        private static readonly Dictionary<string, double> _methodCallTotal
            = new Dictionary<string, double>();

        // Peak (max single-frame) values
        private static readonly Dictionary<string, double> _modPeakMs
            = new Dictionary<string, double>();

        private static readonly Dictionary<string, double> _methodPeakMs
            = new Dictionary<string, double>();

        // Current frame values for display
        private static readonly Dictionary<string, double> _modCurrentMs
            = new Dictionary<string, double>();

        private const int TopMethodCount = 3;

        private static readonly List<ModProfile> _ranking = new List<ModProfile>();
        public static IReadOnlyList<ModProfile> Ranking => _ranking;
        public static int TotalFrames => _totalFrames;

        private static readonly double _ticksToMs = 1000.0 / Stopwatch.Frequency;

        public static void Update()
        {
            _totalFrames++;

            var modFrame = new Dictionary<string, double>();
            var methodFrame = new Dictionary<string, double>();
            var methodBaseFrame = new Dictionary<string, double>();
            var methodCallFrame = new Dictionary<string, int>();

            if (HarmonyProfiler.IsActive)
            {
                foreach (var kv in HarmonyProfiler.FrameTicks)
                {
                    double ms = kv.Value * _ticksToMs;
                    var entry = PatchRegistry.FindByMethod(kv.Key);
                    if (entry == null) continue;

                    double perOwner = ms / entry.Value.OwnerIds.Count;
                    string methodName = entry.Value.MethodFullName;

                    foreach (var owner in entry.Value.OwnerIds)
                    {
                        if (modFrame.TryGetValue(owner, out double existing))
                            modFrame[owner] = existing + perOwner;
                        else
                            modFrame[owner] = perOwner;

                        string methodKey = owner + "|" + methodName;
                        if (methodFrame.TryGetValue(methodKey, out double mExisting))
                            methodFrame[methodKey] = mExisting + perOwner;
                        else
                            methodFrame[methodKey] = perOwner;

                        // Base time
                        if (HarmonyProfiler.FrameBaseTicks.TryGetValue(kv.Key, out long baseTicks))
                        {
                            double baseMs = baseTicks * _ticksToMs;
                            double basePerOwner = baseMs / entry.Value.OwnerIds.Count;
                            if (methodBaseFrame.TryGetValue(methodKey, out double bExisting))
                                methodBaseFrame[methodKey] = bExisting + basePerOwner;
                            else
                                methodBaseFrame[methodKey] = basePerOwner;
                        }

                        // Call count
                        if (HarmonyProfiler.FrameCallCounts.TryGetValue(kv.Key, out int calls))
                        {
                            if (!methodCallFrame.ContainsKey(methodKey))
                                methodCallFrame[methodKey] = calls;
                        }
                    }
                }
            }

            if (UpdateProfiler.IsActive)
            {
                foreach (var kv in UpdateProfiler.FrameTicks)
                {
                    double ms = kv.Value * _ticksToMs;
                    if (modFrame.TryGetValue(kv.Key, out double existing))
                        modFrame[kv.Key] = existing + ms;
                    else
                        modFrame[kv.Key] = ms;

                    string methodKey = kv.Key + "|Update()";
                    if (methodFrame.TryGetValue(methodKey, out double mExisting))
                        methodFrame[methodKey] = mExisting + ms;
                    else
                        methodFrame[methodKey] = ms;
                }
            }

            // Accumulate mod totals + peak
            _modCurrentMs.Clear();
            foreach (var kv in modFrame)
            {
                _modCurrentMs[kv.Key] = kv.Value;

                if (_modTotalMs.TryGetValue(kv.Key, out double existing))
                    _modTotalMs[kv.Key] = existing + kv.Value;
                else
                    _modTotalMs[kv.Key] = kv.Value;

                if (!_modPeakMs.TryGetValue(kv.Key, out double peak) || kv.Value > peak)
                    _modPeakMs[kv.Key] = kv.Value;
            }

            // Accumulate method totals + peak
            foreach (var kv in methodFrame)
            {
                if (_methodTotalMs.TryGetValue(kv.Key, out double existing))
                    _methodTotalMs[kv.Key] = existing + kv.Value;
                else
                    _methodTotalMs[kv.Key] = kv.Value;

                if (!_methodPeakMs.TryGetValue(kv.Key, out double peak) || kv.Value > peak)
                    _methodPeakMs[kv.Key] = kv.Value;
            }

            foreach (var kv in methodBaseFrame)
            {
                if (_methodBaseTotalMs.TryGetValue(kv.Key, out double existing))
                    _methodBaseTotalMs[kv.Key] = existing + kv.Value;
                else
                    _methodBaseTotalMs[kv.Key] = kv.Value;
            }

            foreach (var kv in methodCallFrame)
            {
                string key = kv.Key;
                if (_methodCallTotal.TryGetValue(key, out double existing))
                    _methodCallTotal[key] = existing + kv.Value;
                else
                    _methodCallTotal[key] = kv.Value;
            }

            // Build ranking
            _ranking.Clear();
            if (_totalFrames == 0) return;

            foreach (var kv in _modTotalMs)
            {
                double avg = kv.Value / _totalFrames;
                _modCurrentMs.TryGetValue(kv.Key, out double current);
                _modPeakMs.TryGetValue(kv.Key, out double peak);

                var topMethods = BuildTopMethods(kv.Key);

                _ranking.Add(new ModProfile
                {
                    HarmonyId = kv.Key,
                    ModName = PatchRegistry.GetModName(kv.Key),
                    AvgMs = avg,
                    PeakMs = peak,
                    CurrentMs = current,
                    TopMethods = topMethods
                });
            }
            _ranking.Sort((a, b) => b.AvgMs.CompareTo(a.AvgMs));
        }

        public static void Clear()
        {
            _totalFrames = 0;
            _modTotalMs.Clear();
            _modPeakMs.Clear();
            _methodTotalMs.Clear();
            _methodPeakMs.Clear();
            _methodBaseTotalMs.Clear();
            _methodCallTotal.Clear();
            _modCurrentMs.Clear();
            _ranking.Clear();
        }

        public static List<MethodProfile> BuildAllMethodsForMod(string harmonyId)
        {
            return BuildMethodsForMod(harmonyId, int.MaxValue);
        }

        private static List<MethodProfile> BuildTopMethods(string harmonyId)
        {
            return BuildMethodsForMod(harmonyId, TopMethodCount);
        }

        private static List<MethodProfile> BuildMethodsForMod(
            string harmonyId, int limit)
        {
            if (_totalFrames == 0) return new List<MethodProfile>();

            string prefix = harmonyId + "|";
            var methods = new List<MethodProfile>();

            foreach (var kv in _methodTotalMs)
            {
                if (!kv.Key.StartsWith(prefix)) continue;

                string methodName = kv.Key.Substring(prefix.Length);
                double avg = kv.Value / _totalFrames;

                double avgBase = 0;
                if (_methodBaseTotalMs.TryGetValue(kv.Key, out double baseTotal))
                    avgBase = baseTotal / _totalFrames;

                int avgCalls = 0;
                if (_methodCallTotal.TryGetValue(kv.Key, out double callTotal))
                    avgCalls = (int)(callTotal / _totalFrames);

                List<string> ownerNames = PatchRegistry.GetOwnerNamesByMethodName(methodName);

                double peakMs = 0;
                if (_methodPeakMs.TryGetValue(kv.Key, out double mp))
                    peakMs = mp;

                methods.Add(new MethodProfile
                {
                    MethodName = methodName,
                    AvgMs = avg,
                    PeakMs = peakMs,
                    AvgBaseMs = avgBase,
                    AvgModMs = Math.Max(0, avg - avgBase),
                    AvgCallCount = avgCalls,
                    PatchOwnerNames = ownerNames
                });
            }

            methods.Sort((a, b) => b.AvgMs.CompareTo(a.AvgMs));
            if (methods.Count > limit)
                methods.RemoveRange(limit, methods.Count - limit);
            return methods;
        }
    }
}
