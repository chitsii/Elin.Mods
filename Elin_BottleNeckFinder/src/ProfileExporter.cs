using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace Elin_BottleNeckFinder
{
    public static class ProfileExporter
    {
        private static readonly string ExportPath = Path.Combine(
            Application.persistentDataPath, "BNF_profile_report.md");

        public static string LastExportPath => ExportPath;

        private const int HighFreqMethodCount = 10;

        public static bool Export(float fps, float frameMs)
        {
            try
            {
                var sb = new StringBuilder();
                var now = DateTime.Now;
                var ranking = ProfilingData.Ranking;
                bool hasData = ranking != null && ranking.Count > 0;

                sb.AppendLine("# BottleNeckFinder Profile Report");
                sb.AppendLine();
                sb.AppendLine($"Exported: {now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                // == Environment ==
                WriteEnvironment(sb);

                // == Profiling Session ==
                WriteProfilingSession(sb, fps, frameMs);

                // == Important Notes ==
                WriteImportantNotes(sb);

                // == Mod Performance (avg) ==
                if (hasData)
                    WriteModPerformance(sb, ranking);
                else
                    WriteNoData(sb);

                // == Spike Ranking (peak) ==
                if (hasData)
                    WriteSpikeRanking(sb, ranking);

                // == High-Frequency Methods ==
                if (hasData)
                    WriteHighFrequencyMethods(sb, ranking);

                // == Skipped Methods (Transpiler) ==
                WriteSkippedMethods(sb);

                // == Skipped Methods (Text Pipeline) ==
                WriteSkippedTextPipelineMethods(sb);

                // == Multi-Mod Patched Methods ==
                WriteMultiModMethods(sb);

                // == Recent Errors ==
                WriteErrors(sb);

                // == Patch Registry ==
                WritePatchRegistry(sb);

                File.WriteAllText(ExportPath, sb.ToString(), Encoding.UTF8);
                Debug.Log($"[BNF] Profile exported to: {ExportPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BNF] Export failed: {ex.Message}");
                return false;
            }
        }

        private static void WriteEnvironment(StringBuilder sb)
        {
            sb.AppendLine("## Environment");
            sb.AppendLine();

            string gameVersion = "Unknown";
            try
            {
                var core = EClass.core;
                if (core?.version != null)
                    gameVersion = core.version.GetText();
            }
            catch { }

            int loadedMods = 0;
            try { loadedMods = ModManager.ListPluginObject.Count; }
            catch { }

            sb.AppendLine($"- Game: EA {gameVersion}");
            sb.AppendLine($"- Unity: {Application.unityVersion}");
            sb.AppendLine($"- Platform: {Application.platform}");
            sb.AppendLine($"- Loaded mods: {loadedMods}");
            sb.AppendLine();
        }

        private static void WriteProfilingSession(StringBuilder sb,
            float fps, float frameMs)
        {
            sb.AppendLine("## Profiling Session");
            sb.AppendLine();

            float elapsed = Time.realtimeSinceStartup - Plugin.ProfilingStartTime;
            int minutes = (int)(elapsed / 60f);
            int seconds = (int)(elapsed % 60f);

            sb.AppendLine($"- Duration: {minutes}m {seconds}s");
            sb.AppendLine($"- Frames sampled: {ProfilingData.TotalFrames}");
            sb.AppendLine($"- Sample interval: every {ModConfig.SampleInterval.Value} frame(s)");
            sb.AppendLine($"- FPS at export: {fps:F0} ({frameMs:F1}ms/frame)");
            sb.AppendLine($"- Instrumented methods: {HarmonyProfiler.InstrumentedMethodCount}");
            sb.AppendLine($"- Skipped (transpiler): {HarmonyProfiler.SkippedTranspilerCount}");
            sb.AppendLine($"- Skipped (text pipeline): {HarmonyProfiler.SkippedTextPipelineCount}");
            sb.AppendLine($"- Patch failures detected: {ErrorMonitor.PatchFailureCount}");
            sb.AppendLine();
        }

        private static void WriteImportantNotes(StringBuilder sb)
        {
            sb.AppendLine("## Important Notes");
            sb.AppendLine();
            sb.AppendLine("### Performance Context");
            sb.AppendLine();
            sb.AppendLine("This is a turn-based 2D game running at 60 FPS. Unlike real-time action games where frame drops directly affect gameplay responsiveness, turn-based games are inherently more tolerant of occasional performance variations. A mod that shows high performance impact in this report may not necessarily cause noticeable lag during actual gameplay.");
            sb.AppendLine();
            sb.AppendLine("**Regarding Spike Metrics:** High peak values (e.g., 100ms+) typically occur during zone transitions, save/load operations, or scene initialization—moments where the game naturally pauses and players expect brief loading times. The Peak/Avg ratio is useful for identifying sporadic heavy operations, but should be interpreted with this context in mind rather than treated as a critical issue.");
            sb.AppendLine();
            sb.AppendLine("### Data Limitations");
            sb.AppendLine();
            sb.AppendLine("**Inactive Code Paths:** Mods that were not actively triggered during the profiling session will show minimal or no performance data. This does not indicate they have zero impact—it simply means their code paths were not executed. A combat-focused mod may show negligible impact during town exploration, and vice versa.");
            sb.AppendLine();
            sb.AppendLine("**Transpiler Patches:** Methods modified by transpiler patches are excluded from instrumentation. Mods using transpilers may have their actual performance impact underestimated in this report.");
            sb.AppendLine();
            sb.AppendLine("### Recommended Usage");
            sb.AppendLine();
            sb.AppendLine("This profile report should be treated as **one diagnostic tool among many**, not as absolute truth. For best results:");
            sb.AppendLine();
            sb.AppendLine("- Combine this data with your own observations of when lag actually occurs");
            sb.AppendLine("- Consider re-profiling during specific gameplay scenarios to capture different code paths");
            sb.AppendLine("- Prioritize investigating mods that cause noticeable in-game stutter, rather than focusing solely on high peak values during non-interactive moments");
            sb.AppendLine();
        }

        private static void WriteModPerformance(StringBuilder sb,
            IReadOnlyList<ProfilingData.ModProfile> ranking)
        {
            sb.AppendLine("## Mod Performance (sorted by avg ms/frame)");
            sb.AppendLine();

            for (int i = 0; i < ranking.Count; i++)
            {
                var mod = ranking[i];
                if (mod.AvgMs < 0.001) continue;

                sb.AppendLine($"### {i + 1}. {mod.ModName} ({mod.HarmonyId}) — avg:{mod.AvgMs:F2} peak:{mod.PeakMs:F1} ms/frame");
                sb.AppendLine();

                var allMethods = ProfilingData.BuildAllMethodsForMod(mod.HarmonyId);
                if (allMethods.Count > 0)
                {
                    sb.AppendLine("Bottleneck methods:");
                    foreach (var m in allMethods)
                    {
                        string baseModInfo;
                        if (m.PatchOwnerNames == null || m.PatchOwnerNames.Count == 0)
                            baseModInfo = "(vanilla)";
                        else
                            baseModInfo = $"base:{m.AvgBaseMs:F3}ms mod:{m.AvgModMs:F3}ms";

                        string callInfo = m.AvgCallCount > 0
                            ? $", {m.AvgCallCount} calls/frame" : "";

                        double pct = mod.AvgMs > 0
                            ? m.AvgMs / mod.AvgMs * 100.0
                            : 0;
                        string peakInfo = m.PeakMs >= 0.01
                            ? $", peak:{m.PeakMs:F1}ms" : "";
                        sb.AppendLine($"- `{m.MethodName}` — avg:{m.AvgMs:F3}ms{peakInfo} ({pct:F1}%) [{baseModInfo}{callInfo}]");

                        if (m.PatchOwnerNames != null && m.PatchOwnerNames.Count > 0)
                            sb.AppendLine($"  - Patched by: {string.Join(", ", m.PatchOwnerNames)}");
                    }
                }
                else
                {
                    sb.AppendLine("(No method-level data available)");
                }
                sb.AppendLine();
            }
        }

        private static void WriteNoData(StringBuilder sb)
        {
            sb.AppendLine("## No profiling data available");
            sb.AppendLine();
            sb.AppendLine("Start the profiler from the overlay and wait a few seconds before exporting.");
            sb.AppendLine();
        }

        private static void WriteSpikeRanking(StringBuilder sb,
            IReadOnlyList<ProfilingData.ModProfile> ranking)
        {
            var sorted = ranking
                .Where(m => m.PeakMs >= 0.01)
                .OrderByDescending(m => m.PeakMs)
                .ToList();
            if (sorted.Count == 0) return;

            sb.AppendLine("## Spike Ranking (sorted by peak ms)");
            sb.AppendLine();
            sb.AppendLine("Mods sorted by worst single-frame spike. High Peak/Avg ratio indicates intermittent lag spikes.");
            sb.AppendLine();
            sb.AppendLine("| Rank | Mod | Avg ms | Peak ms | Peak/Avg |");
            sb.AppendLine("|------|-----|--------|---------|----------|");

            for (int i = 0; i < sorted.Count; i++)
            {
                var mod = sorted[i];
                double ratio = mod.AvgMs > 0 ? mod.PeakMs / mod.AvgMs : 0;
                sb.AppendLine($"| {i + 1} | {mod.ModName} | {mod.AvgMs:F2} | {mod.PeakMs:F1} | {ratio:F1}x |");
            }
            sb.AppendLine();
        }

        private static void WriteHighFrequencyMethods(StringBuilder sb,
            IReadOnlyList<ProfilingData.ModProfile> ranking)
        {
            // Collect all methods across all mods
            var allMethods = new List<(string ModName, ProfilingData.MethodProfile Method)>();
            foreach (var mod in ranking)
            {
                var methods = ProfilingData.BuildAllMethodsForMod(mod.HarmonyId);
                foreach (var m in methods)
                {
                    if (m.AvgCallCount > 0)
                        allMethods.Add((mod.ModName, m));
                }
            }

            if (allMethods.Count == 0) return;

            var sorted = allMethods
                .OrderByDescending(x => x.Method.AvgCallCount)
                .Take(HighFreqMethodCount)
                .ToList();

            sb.AppendLine("## High-Frequency Methods");
            sb.AppendLine();
            sb.AppendLine("Methods with the most calls per frame. High call count amplifies per-call cost.");
            sb.AppendLine();
            sb.AppendLine("| Method | Calls/Frame | Avg ms/call | Total ms/frame | Mod |");
            sb.AppendLine("|--------|-------------|-------------|----------------|-----|");

            foreach (var (modName, m) in sorted)
            {
                double perCall = m.AvgCallCount > 0 ? m.AvgMs / m.AvgCallCount : 0;
                sb.AppendLine($"| `{m.MethodName}` | {m.AvgCallCount} | {perCall:F4} | {m.AvgMs:F3} | {modName} |");
            }
            sb.AppendLine();
        }

        private static void WriteSkippedMethods(StringBuilder sb)
        {
            if (HarmonyProfiler.SkippedTranspilerCount == 0) return;

            // Collect methods that have transpiler patches
            var skipped = new List<(string MethodName, List<string> TranspilerOwners)>();
            foreach (var entry in PatchRegistry.Entries)
            {
                try
                {
                    var patchInfo = Harmony.GetPatchInfo(entry.Method);
                    if (patchInfo?.Transpilers == null || patchInfo.Transpilers.Count == 0)
                        continue;

                    // Skip our own patches
                    if (entry.OwnerIds.Contains(Plugin.ModGuid)
                     || entry.OwnerIds.Contains("tishi.bnf.profiler.harmony"))
                        continue;

                    var owners = new List<string>();
                    foreach (var t in patchInfo.Transpilers)
                    {
                        string name = PatchRegistry.GetModName(t.owner);
                        if (!owners.Contains(name))
                            owners.Add(name);
                    }
                    skipped.Add((entry.MethodFullName, owners));
                }
                catch { }
            }

            if (skipped.Count == 0) return;

            sb.AppendLine("## Skipped Methods (Transpiler)");
            sb.AppendLine();
            sb.AppendLine("These methods have Transpiler patches and were excluded from profiling.");
            sb.AppendLine("Their performance impact is NOT reflected in this report.");
            sb.AppendLine();

            foreach (var (methodName, owners) in skipped)
            {
                sb.AppendLine($"- `{methodName}` — Transpiler by: {string.Join(", ", owners)}");
            }
            sb.AppendLine();
        }

        private static void WriteSkippedTextPipelineMethods(StringBuilder sb)
        {
            if (HarmonyProfiler.SkippedTextPipelineCount == 0) return;

            var skipped = new List<string>();
            foreach (var entry in PatchRegistry.Entries)
            {
                try
                {
                    if (entry.OwnerIds.Contains(Plugin.ModGuid)
                     || entry.OwnerIds.Contains("tishi.bnf.profiler.harmony"))
                        continue;

                    var declaringType = entry.Method.DeclaringType;
                    if (declaringType == null) continue;

                    var typeName = declaringType.IsGenericType
                        ? declaringType.GetGenericTypeDefinition().Name.Split('`')[0]
                        : declaringType.Name;

                    if (typeName == "Lang" || typeName == "GameLang" || typeName == "Msg")
                        skipped.Add(entry.MethodFullName);
                }
                catch { }
            }

            if (skipped.Count == 0) return;

            sb.AppendLine("## Skipped Methods (Text Pipeline)");
            sb.AppendLine();
            sb.AppendLine("These methods belong to the text evaluation pipeline (Lang, GameLang, Msg)");
            sb.AppendLine("and were excluded to prevent breaking text variable substitution.");
            sb.AppendLine();

            foreach (var methodName in skipped)
            {
                sb.AppendLine($"- `{methodName}`");
            }
            sb.AppendLine();
        }

        private static void WriteMultiModMethods(StringBuilder sb)
        {
            var multiModEntries = PatchRegistry.GetMultiModEntries();
            if (multiModEntries.Count == 0) return;

            sb.AppendLine("## Multi-Mod Patched Methods (Potential Conflicts)");
            sb.AppendLine();
            sb.AppendLine("Methods patched by 2+ mods. Check if mod interactions cause performance issues.");
            sb.AppendLine();
            sb.AppendLine("| Method | Mods | Avg Total | Base | Mod Overhead | Calls/Frame |");
            sb.AppendLine("|--------|------|-----------|------|--------------|-------------|");

            foreach (var entry in multiModEntries)
            {
                var modNames = PatchRegistry.GetOwnerNamesForMethod(entry.Method);
                string modsStr = string.Join(", ", modNames);

                var mp = FindMethodProfile(entry.MethodFullName);
                string totalStr = mp.HasValue ? $"{mp.Value.AvgMs:F2}ms" : "-";
                string baseStr = mp.HasValue ? $"{mp.Value.AvgBaseMs:F2}ms" : "-";
                string modStr = mp.HasValue ? $"{mp.Value.AvgModMs:F2}ms" : "-";
                string callsStr = mp.HasValue && mp.Value.AvgCallCount > 0
                    ? mp.Value.AvgCallCount.ToString() : "-";

                sb.AppendLine($"| `{entry.MethodFullName}` | {modsStr} | {totalStr} | {baseStr} | {modStr} | {callsStr} |");
            }
            sb.AppendLine();
        }

        private static void WriteErrors(StringBuilder sb)
        {
            var errors = ErrorMonitor.Errors;
            if (errors == null || errors.Count == 0) return;

            sb.AppendLine("## Recent Errors");
            sb.AppendLine();
            foreach (var err in errors)
            {
                string mod = err.ModName ?? "Unknown";
                string tag = err.IsPatchFailure ? "[PATCH FAILURE]" : "[RUNTIME]";
                sb.AppendLine($"- {tag} **{mod}**: {err.Summary}");
            }
            sb.AppendLine();
        }

        private static void WritePatchRegistry(StringBuilder sb)
        {
            sb.AppendLine("## Patch Registry");
            sb.AppendLine();
            sb.AppendLine("Which mods patch which methods:");
            sb.AppendLine();

            var modPatches = new Dictionary<string, List<string>>();
            foreach (var entry in PatchRegistry.Entries)
            {
                foreach (var owner in entry.OwnerIds)
                {
                    string modName = PatchRegistry.GetModName(owner);
                    if (!modPatches.TryGetValue(modName, out var list))
                    {
                        list = new List<string>();
                        modPatches[modName] = list;
                    }
                    if (!list.Contains(entry.MethodFullName))
                        list.Add(entry.MethodFullName);
                }
            }

            foreach (var kv in modPatches.OrderBy(x => x.Key))
            {
                sb.AppendLine($"**{kv.Key}** ({kv.Value.Count} methods):");
                foreach (var method in kv.Value.OrderBy(x => x))
                {
                    sb.AppendLine($"  - `{method}`");
                }
                sb.AppendLine();
            }
        }

        private static ProfilingData.MethodProfile? FindMethodProfile(string methodFullName)
        {
            var allRanking = ProfilingData.Ranking;
            if (allRanking == null) return null;
            foreach (var modProfile in allRanking)
            {
                var allMethods = ProfilingData.BuildAllMethodsForMod(modProfile.HarmonyId);
                foreach (var m in allMethods)
                {
                    if (m.MethodName == methodFullName)
                        return m;
                }
            }
            return null;
        }
    }
}
