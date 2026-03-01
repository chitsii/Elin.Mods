using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Elin_BottleNeckFinder
{
    public static class HarmonyProfiler
    {
        private static Harmony _harmony;
        private static bool _patchesApplied;
        private static volatile bool _enabled;

        private static readonly Dictionary<MethodBase, long> _frameTicks
            = new Dictionary<MethodBase, long>();

        private static readonly Dictionary<MethodBase, long> _frameBaseTicks
            = new Dictionary<MethodBase, long>();

        private static readonly Dictionary<MethodBase, int> _frameCallCounts
            = new Dictionary<MethodBase, int>();

        [ThreadStatic] private static Stack<long> _tsStack;
        [ThreadStatic] private static Stack<long> _tsInnerStack;

        private static readonly HashSet<string> TextPipelineTypes = new HashSet<string>
        {
            "Lang", "GameLang", "Msg"
        };

        public static bool IsActive => _enabled;
        public static int SkippedTranspilerCount { get; private set; }
        public static int SkippedTextPipelineCount { get; private set; }
        public static int InstrumentedMethodCount { get; private set; }
        public static IReadOnlyDictionary<MethodBase, long> FrameTicks => _frameTicks;
        public static IReadOnlyDictionary<MethodBase, long> FrameBaseTicks => _frameBaseTicks;
        public static IReadOnlyDictionary<MethodBase, int> FrameCallCounts => _frameCallCounts;

        public static void Start()
        {
            if (_enabled) return;

            if (!_patchesApplied)
            {
                _harmony = new Harmony("tishi.bnf.profiler.harmony");
                int patched = 0;

                int skippedTranspiler = 0;
                int skippedTextPipeline = 0;

                foreach (var entry in PatchRegistry.Entries)
                {
                    try
                    {
                        if (entry.OwnerIds.Contains(Plugin.ModGuid)
                         || entry.OwnerIds.Contains("tishi.bnf.profiler.harmony"))
                            continue;

                        // Skip text pipeline types - re-patching breaks
                        // text variable evaluation (#1, #player, [key], etc.)
                        var declaringType = entry.Method.DeclaringType;
                        if (declaringType != null && IsTextPipelineType(declaringType))
                        {
                            skippedTextPipeline++;
                            continue;
                        }

                        // Skip methods with transpilers - re-patching can break
                        // transpiled IL and corrupt game text evaluation etc.
                        var patchInfo = Harmony.GetPatchInfo(entry.Method);
                        if (patchInfo?.Transpilers != null && patchInfo.Transpilers.Count > 0)
                        {
                            skippedTranspiler++;
                            continue;
                        }

                        _harmony.Patch(
                            entry.Method,
                            prefix: new HarmonyMethod(typeof(HarmonyProfiler),
                                nameof(TimingPrefix)) { priority = Priority.First },
                            postfix: new HarmonyMethod(typeof(HarmonyProfiler),
                                nameof(TimingPostfix)) { priority = Priority.Last }
                        );
                        _harmony.Patch(
                            entry.Method,
                            prefix: new HarmonyMethod(typeof(HarmonyProfiler),
                                nameof(BaseTimingPrefix)) { priority = Priority.Last },
                            postfix: new HarmonyMethod(typeof(HarmonyProfiler),
                                nameof(BaseTimingPostfix)) { priority = Priority.First }
                        );
                        patched++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[BNF] Failed to add profiler to "
                            + $"{entry.MethodFullName}: {ex.Message}");
                    }
                }

                _patchesApplied = true;
                InstrumentedMethodCount = patched;
                SkippedTranspilerCount = skippedTranspiler;
                SkippedTextPipelineCount = skippedTextPipeline;
                Debug.Log($"[BNF] HarmonyProfiler: {patched} instrumented, "
                    + $"{skippedTranspiler} transpiled skipped, "
                    + $"{skippedTextPipeline} text pipeline skipped");
            }

            _enabled = true;
            Debug.Log("[BNF] HarmonyProfiler started");
        }

        public static void Stop()
        {
            if (!_enabled) return;

            // Do NOT call UnpatchSelf() - it breaks MonoMod/CWL hook chains
            // and causes NotSupportedException. Patches stay applied but are
            // no-ops when _enabled is false.
            _enabled = false;
            _frameTicks.Clear();
            _frameBaseTicks.Clear();
            _frameCallCounts.Clear();

            Debug.Log("[BNF] HarmonyProfiler stopped");
        }

        public static void BeginFrame()
        {
            _frameTicks.Clear();
            _frameBaseTicks.Clear();
            _frameCallCounts.Clear();
        }

        public static void TimingPrefix(MethodBase __originalMethod)
        {
            if (!_enabled) return;
            if (_tsStack == null) _tsStack = new Stack<long>();
            _tsStack.Push(Stopwatch.GetTimestamp());
        }

        public static void TimingPostfix(MethodBase __originalMethod)
        {
            if (!_enabled) return;
            if (_tsStack == null || _tsStack.Count == 0) return;

            long start = _tsStack.Pop();
            long elapsed = Stopwatch.GetTimestamp() - start;

            if (_frameTicks.TryGetValue(__originalMethod, out long existing))
                _frameTicks[__originalMethod] = existing + elapsed;
            else
                _frameTicks[__originalMethod] = elapsed;

            if (_frameCallCounts.TryGetValue(__originalMethod, out int count))
                _frameCallCounts[__originalMethod] = count + 1;
            else
                _frameCallCounts[__originalMethod] = 1;
        }

        public static void BaseTimingPrefix(MethodBase __originalMethod)
        {
            if (!_enabled) return;
            if (_tsInnerStack == null) _tsInnerStack = new Stack<long>();
            _tsInnerStack.Push(Stopwatch.GetTimestamp());
        }

        public static void BaseTimingPostfix(MethodBase __originalMethod)
        {
            if (!_enabled) return;
            if (_tsInnerStack == null || _tsInnerStack.Count == 0) return;

            long start = _tsInnerStack.Pop();
            long elapsed = Stopwatch.GetTimestamp() - start;

            if (_frameBaseTicks.TryGetValue(__originalMethod, out long existing))
                _frameBaseTicks[__originalMethod] = existing + elapsed;
            else
                _frameBaseTicks[__originalMethod] = elapsed;
        }

        private static bool IsTextPipelineType(Type type)
        {
            var name = type.IsGenericType
                ? type.GetGenericTypeDefinition().Name.Split('`')[0]
                : type.Name;
            return TextPipelineTypes.Contains(name);
        }
    }
}
