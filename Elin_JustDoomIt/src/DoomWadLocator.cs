using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace Elin_ModTemplate
{
    public sealed class DoomWadEntry
    {
        public string FileName;
        public string FullPath;
    }

    [Serializable]
    public sealed class DoomRuntimeLoadout
    {
        public string selectedIwadFile = "freedoom1.wad";
        public List<string> enabledModFiles = new List<string>();
        public int selectedSkill = 3;
    }

    public struct DoomLaunchConfig
    {
        public string IwadPath;
        public List<string> PwadPaths;
        public int Skill;
        public int Episode;
        public int Map;
        public string SaveSlotKey;
        public bool LoadExistingSave;
    }

    public static class DoomWadLocator
    {
        public static string GetProfileRoot()
        {
            return Path.Combine(Paths.ConfigPath, Plugin.ModGuid, "profiles");
        }

        public static string GetWadRoot()
        {
            var modDir = Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? string.Empty;
            var preferred = Path.Combine(modDir, "wad");
            if (Directory.Exists(preferred))
            {
                return preferred;
            }

            var pluginDir = Path.Combine(Paths.PluginPath, "Elin_JustDoomIt", "wad");
            if (Directory.Exists(pluginDir))
            {
                return pluginDir;
            }

            return preferred;
        }

        public static List<DoomWadEntry> FindIwads()
        {
            var result = new Dictionary<string, DoomWadEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in EnumerateDiscoveryRoots())
            {
                var iwadDir = Path.Combine(root, "iwads");
                // Keep curated IWADs in "wad/iwads" as higher priority than duplicates in root "wad".
                ScanDirForWads(iwadDir, result, overwriteExisting: true);
                ScanDirForWads(root, result, overwriteExisting: false);
            }

            // Steam rerelease IWAD auto-detection (non-destructive fallback):
            // fixed file paths only; never override curated/local IWAD entries.
            foreach (var root in EnumerateSteamIwadRoots())
            {
                ScanSteamFixedIwadFiles(root, result, overwriteExisting: false);
            }

            return result.Values.OrderBy(v => v.FileName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static List<DoomWadEntry> FindPwads()
        {
            var result = new Dictionary<string, DoomWadEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in EnumerateDiscoveryRoots())
            {
                var modsDir = Path.Combine(root, "mods");
                ScanDirForWads(modsDir, result);
            }

            return result.Values.OrderBy(v => v.FileName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static DoomModRuleRefreshResult RefreshPwadRules()
        {
            var pwads = FindPwads();
            return DoomModRuleStore.RefreshRules(pwads);
        }

        public static bool ReconcileRuntimeLoadout(DoomRuntimeLoadout loadout)
        {
            if (loadout == null)
            {
                return false;
            }

            var beforeIwad = loadout.selectedIwadFile ?? string.Empty;
            var beforeSkill = loadout.selectedSkill;
            var beforeMods = string.Join("\n", loadout.enabledModFiles ?? new List<string>());
            SanitizeLoadout(loadout, FindPwads());

            return !string.Equals(beforeIwad, loadout.selectedIwadFile, StringComparison.Ordinal) ||
                   beforeSkill != loadout.selectedSkill ||
                   !string.Equals(beforeMods, string.Join("\n", loadout.enabledModFiles ?? new List<string>()), StringComparison.Ordinal);
        }

        public static string GetIwadDisplayName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "(none)";
            }

            var n = Path.GetFileName(fileName).ToLowerInvariant();
            switch (n)
            {
                case "doom.wad":
                    return "DOOM: The Ultimate DOOM";
                case "doom2.wad":
                    return "DOOM II: Hell on Earth";
                case "tnt.wad":
                    return "Final DOOM: TNT - Evilution";
                case "plutonia.wad":
                    return "Final DOOM: The Plutonia Experiment";
                case "freedoom1.wad":
                    return "FreeDoom: Phase 1";
                case "freedoom2.wad":
                    return "FreeDoom: Phase 2";
                case "freedm.wad":
                    return "FreeDM";
                default:
                    return Path.GetFileName(fileName);
            }
        }

        public static DoomRuntimeLoadout LoadRuntimeLoadout()
        {
            var path = ResolveProfilePathForRead("runtime_loadout.json");
            try
            {
                if (File.Exists(path))
                {
                    var parsed = ParseLoadout(File.ReadAllLines(path));
                    return SanitizeLoadout(parsed ?? new DoomRuntimeLoadout(), FindPwads());
                }
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Failed to load runtime loadout: " + ex.Message);
            }

            return SanitizeLoadout(new DoomRuntimeLoadout(), FindPwads());
        }

        public static void SaveRuntimeLoadout(DoomRuntimeLoadout loadout)
        {
            var sanitized = SanitizeLoadout(loadout ?? new DoomRuntimeLoadout(), FindPwads());
            var path = GetRuntimeProfilePath();
            var dir = Path.GetDirectoryName(path) ?? string.Empty;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var lines = new List<string>
            {
                "selected_iwad=" + sanitized.selectedIwadFile,
                "enabled_mods=" + string.Join(";", sanitized.enabledModFiles ?? new List<string>()),
                "selected_skill=" + sanitized.selectedSkill
            };
            File.WriteAllLines(path, lines);
        }

        public static DoomLaunchConfig BuildLaunchConfig(DoomRuntimeLoadout loadout)
        {
            var iwads = FindIwads();
            var pwads = FindPwads();
            var sanitized = SanitizeLoadout(loadout ?? new DoomRuntimeLoadout(), pwads);

            var iwad = iwads.FirstOrDefault(i =>
                string.Equals(i.FileName, sanitized.selectedIwadFile, StringComparison.OrdinalIgnoreCase));
            if (iwad == null && iwads.Count > 0)
            {
                iwad = iwads[0];
            }

            var paths = new List<string>();
            var pwadNames = new List<string>();
            var selected = sanitized.enabledModFiles ?? new List<string>();
            for (var i = 0; i < selected.Count && paths.Count < 1; i++)
            {
                var file = selected[i];
                var match = pwads.FirstOrDefault(p =>
                    string.Equals(p.FileName, file, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    paths.Add(match.FullPath);
                    pwadNames.Add(match.FileName);
                }
            }

            var skill = Mathf.Clamp(sanitized.selectedSkill, 1, 5);
            return new DoomLaunchConfig
            {
                IwadPath = iwad?.FullPath,
                PwadPaths = paths,
                Skill = skill,
                Episode = 1,
                Map = 1,
                SaveSlotKey = DoomPersistentSaveStore.BuildSlotKey(iwad?.FileName, pwadNames, skill),
                LoadExistingSave = false
            };
        }

        private static string GetRuntimeProfilePath()
        {
            return GetMutableProfilePath("runtime_loadout.json");
        }

        internal static string GetMutableProfilePath(string fileName)
        {
            return Path.Combine(GetProfileRoot(), fileName);
        }

        internal static string ResolveProfilePathForRead(string fileName)
        {
            return GetMutableProfilePath(fileName);
        }

        private static void ScanDirForWads(string dir, IDictionary<string, DoomWadEntry> output, bool overwriteExisting = true)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                return;
            }

            foreach (var path in Directory.GetFiles(dir, "*.wad", SearchOption.TopDirectoryOnly))
            {
                var file = Path.GetFileName(path);
                if (string.IsNullOrWhiteSpace(file))
                {
                    continue;
                }

                if (!overwriteExisting && output.ContainsKey(file))
                {
                    continue;
                }

                output[file] = new DoomWadEntry
                {
                    FileName = file,
                    FullPath = path
                };
            }
        }

        private static void ScanSteamFixedIwadFiles(string root, IDictionary<string, DoomWadEntry> output, bool overwriteExisting)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                return;
            }

            // Fixed paths only (no recursive walk).
            var files = new[]
            {
                Path.Combine(root, "DOOM.WAD"),
                Path.Combine(root, "DOOM2.WAD"),
                Path.Combine(root, "TNT.WAD"),
                Path.Combine(root, "PLUTONIA.WAD")
            };

            for (var i = 0; i < files.Length; i++)
            {
                var fullPath = files[i];
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                var file = Path.GetFileName(fullPath);
                if (string.IsNullOrWhiteSpace(file))
                {
                    continue;
                }

                if (!overwriteExisting && output.ContainsKey(file))
                {
                    continue;
                }

                output[file] = new DoomWadEntry
                {
                    FileName = file,
                    FullPath = fullPath
                };
            }
        }

        private static DoomRuntimeLoadout SanitizeLoadout(DoomRuntimeLoadout loadout, IReadOnlyList<DoomWadEntry> pwads = null)
        {
            var availableMods = new HashSet<string>(
                (pwads ?? Array.Empty<DoomWadEntry>())
                    .Where(p => p != null && !string.IsNullOrWhiteSpace(p.FileName))
                    .Select(p => p.FileName),
                StringComparer.OrdinalIgnoreCase);

            loadout.enabledModFiles = loadout.enabledModFiles ?? new List<string>();
            loadout.enabledModFiles = loadout.enabledModFiles
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Where(s => availableMods.Contains(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(1)
                .ToList();

            loadout.selectedIwadFile = string.IsNullOrWhiteSpace(loadout.selectedIwadFile)
                ? "freedoom1.wad"
                : loadout.selectedIwadFile.Trim();

            loadout.selectedSkill = Mathf.Clamp(loadout.selectedSkill, 1, 5);
            return loadout;
        }

        private static IEnumerable<string> EnumerateDiscoveryRoots()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var primary = GetWadRoot();
            if (!string.IsNullOrWhiteSpace(primary))
            {
                set.Add(primary);
            }

            var pluginRoot = Path.Combine(Paths.PluginPath, "Elin_JustDoomIt", "wad");
            if (!string.IsNullOrWhiteSpace(pluginRoot))
            {
                set.Add(pluginRoot);
            }

            var streamingRoot = Path.Combine(Application.streamingAssetsPath, "doom");
            if (!string.IsNullOrWhiteSpace(streamingRoot))
            {
                set.Add(streamingRoot);
            }

            return set;
        }

        private static IEnumerable<string> EnumerateSteamIwadRoots()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var steamRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam",
                "steamapps",
                "common");

            // Strict path only (no guess-based variants).
            var candidates = new[]
            {
                Path.Combine(steamRoot, "Ultimate Doom", "rerelease")
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                var c = candidates[i];
                if (!string.IsNullOrWhiteSpace(c) && Directory.Exists(c))
                {
                    set.Add(c);
                }
            }

            return set;
        }

        private static DoomRuntimeLoadout ParseLoadout(string[] lines)
        {
            var loadout = new DoomRuntimeLoadout();
            if (lines == null)
            {
                return loadout;
            }

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var sep = line.IndexOf('=');
                if (sep <= 0 || sep >= line.Length - 1)
                {
                    continue;
                }

                var key = line.Substring(0, sep).Trim();
                var value = line.Substring(sep + 1).Trim();
                if (key.Equals("selected_iwad", StringComparison.OrdinalIgnoreCase))
                {
                    loadout.selectedIwadFile = value;
                }
                else if (key.Equals("enabled_mods", StringComparison.OrdinalIgnoreCase))
                {
                    loadout.enabledModFiles = value
                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => s.Length > 0)
                        .ToList();
                }
                else if (key.Equals("selected_skill", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, out var skill))
                    {
                        loadout.selectedSkill = skill;
                    }
                }
            }

            return loadout;
        }
    }
}
