using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace Elin_JustDoomIt
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
        public string selectedModId = string.Empty;
        public int selectedSkill = 3;

        // Legacy in-memory carry-over only. Do not persist in new format.
        public List<string> enabledModFiles = new List<string>();
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
        public DoomModEntryDefinition SelectedEntry;
    }

    public static class DoomWadLocator
    {
        private static bool _startupGcDone;

        public static string GetProfileRoot()
        {
            return Path.Combine(Paths.ConfigPath, Plugin.ModGuid, "profiles");
        }

        public static string GetModEntryConfigRoot()
        {
            return Path.Combine(GetProfileRoot(), "mod_entry_configs");
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
                ScanDirForWads(iwadDir, result, overwriteExisting: true);
                ScanDirForWads(root, result, overwriteExisting: false);
            }

            foreach (var root in EnumerateSteamIwadRoots())
            {
                ScanSteamFixedIwadFiles(root, result, overwriteExisting: false);
            }

            return result.Values.OrderBy(v => v.FileName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static List<DoomModEntryDefinition> FindModEntries()
        {
            EnsureStartupConfigGc();

            var result = new Dictionary<string, DoomModEntryDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in EnumerateDiscoveryRoots())
            {
                var modsDir = Path.Combine(root, "mods");
                foreach (var entry in DoomModEntryCore.DiscoverEntries(modsDir, GetModEntryConfigRoot()))
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.EntryId))
                    {
                        continue;
                    }

                    if (!result.ContainsKey(entry.EntryId))
                    {
                        result[entry.EntryId] = entry;
                    }
                }
            }

            return result.Values.OrderBy(v => v.DisplayName ?? v.EntryId, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static bool ReconcileRuntimeLoadout(DoomRuntimeLoadout loadout)
        {
            if (loadout == null)
            {
                return false;
            }

            var beforeIwad = loadout.selectedIwadFile ?? string.Empty;
            var beforeSkill = loadout.selectedSkill;
            var beforeMod = loadout.selectedModId ?? string.Empty;
            SanitizeLoadout(loadout, FindModEntries());

            return !string.Equals(beforeIwad, loadout.selectedIwadFile, StringComparison.Ordinal) ||
                   beforeSkill != loadout.selectedSkill ||
                   !string.Equals(beforeMod, loadout.selectedModId ?? string.Empty, StringComparison.Ordinal);
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
                    return SanitizeLoadout(parsed ?? new DoomRuntimeLoadout(), FindModEntries());
                }
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Failed to load runtime loadout: " + ex.Message);
            }

            return SanitizeLoadout(new DoomRuntimeLoadout(), FindModEntries());
        }

        public static void SaveRuntimeLoadout(DoomRuntimeLoadout loadout)
        {
            var sanitized = SanitizeLoadout(loadout ?? new DoomRuntimeLoadout(), FindModEntries());
            var path = GetRuntimeProfilePath();
            var dir = Path.GetDirectoryName(path) ?? string.Empty;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var lines = new List<string>
            {
                "selected_iwad=" + sanitized.selectedIwadFile,
                "selected_mod_id=" + (sanitized.selectedModId ?? string.Empty),
                "selected_skill=" + sanitized.selectedSkill
            };
            File.WriteAllLines(path, lines);
        }

        public static DoomLaunchConfig BuildLaunchConfig(DoomRuntimeLoadout loadout)
        {
            var iwads = FindIwads();
            var entries = FindModEntries();
            var sanitized = SanitizeLoadout(loadout ?? new DoomRuntimeLoadout(), entries);

            var iwad = iwads.FirstOrDefault(i =>
                string.Equals(i.FileName, sanitized.selectedIwadFile, StringComparison.OrdinalIgnoreCase));
            if (iwad == null && iwads.Count > 0)
            {
                iwad = iwads[0];
            }

            DoomModEntryDefinition selectedEntry = null;
            if (!string.IsNullOrWhiteSpace(sanitized.selectedModId))
            {
                selectedEntry = entries.FirstOrDefault(e =>
                    string.Equals(e.EntryId, sanitized.selectedModId, StringComparison.OrdinalIgnoreCase));
            }

            var paths = new List<string>();
            var manifestHash = string.Empty;
            if (selectedEntry != null &&
                (selectedEntry.State == DoomModEntryState.ReadySingle || selectedEntry.State == DoomModEntryState.ReadyMulti))
            {
                var launchFiles = selectedEntry.LaunchWadFiles ?? new List<string>();
                for (var i = 0; i < launchFiles.Count; i++)
                {
                    var fullPath = Path.Combine(selectedEntry.ContentRootPath ?? string.Empty, launchFiles[i] ?? string.Empty);
                    if (File.Exists(fullPath))
                    {
                        paths.Add(fullPath);
                    }
                }

                manifestHash = selectedEntry.ManifestHash ?? string.Empty;
            }

            var skill = Mathf.Clamp(sanitized.selectedSkill, 1, 5);
            return new DoomLaunchConfig
            {
                IwadPath = iwad?.FullPath,
                PwadPaths = paths,
                Skill = skill,
                Episode = 1,
                Map = 1,
                SaveSlotKey = DoomPersistentSaveStore.BuildSlotKey(iwad?.FileName, manifestHash),
                LoadExistingSave = false,
                SelectedEntry = selectedEntry
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

        private static void EnsureStartupConfigGc()
        {
            if (_startupGcDone)
            {
                return;
            }

            _startupGcDone = true;
            try
            {
                RunEntryConfigGc();
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Startup mod_entry_configs GC failed: " + ex.Message);
            }
        }

        private static void RunEntryConfigGc()
        {
            var configRoot = GetModEntryConfigRoot();
            if (!Directory.Exists(configRoot))
            {
                return;
            }

            var validIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in EnumerateDiscoveryRoots())
            {
                var modsDir = Path.Combine(root, "mods");
                if (!Directory.Exists(modsDir))
                {
                    continue;
                }

                var directories = Directory.GetDirectories(modsDir, "*", SearchOption.TopDirectoryOnly);
                for (var i = 0; i < directories.Length; i++)
                {
                    var entryId = Path.GetFileName(directories[i]);
                    if (!string.IsNullOrWhiteSpace(entryId))
                    {
                        validIds.Add(entryId);
                    }
                }
            }

            foreach (var path in Directory.GetFiles(configRoot, "*.json", SearchOption.TopDirectoryOnly))
            {
                var entryId = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(entryId) || validIds.Contains(entryId))
                {
                    continue;
                }

                File.Delete(path);
                DoomDiagnostics.Info("[JustDoomIt] config_delete reason=startup_gc entry_id=" + entryId);
            }
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

        private static DoomRuntimeLoadout SanitizeLoadout(DoomRuntimeLoadout loadout, IReadOnlyList<DoomModEntryDefinition> entries)
        {
            loadout.selectedIwadFile = string.IsNullOrWhiteSpace(loadout.selectedIwadFile)
                ? "freedoom1.wad"
                : loadout.selectedIwadFile.Trim();

            if (string.IsNullOrWhiteSpace(loadout.selectedModId) &&
                loadout.enabledModFiles != null &&
                loadout.enabledModFiles.Count > 0)
            {
                loadout.selectedModId = loadout.enabledModFiles[0] ?? string.Empty;
            }

            loadout.enabledModFiles = new List<string>();
            var available = new HashSet<string>(
                (entries ?? Array.Empty<DoomModEntryDefinition>())
                    .Where(e => e != null && !string.IsNullOrWhiteSpace(e.EntryId))
                    .Where(e => e.State == DoomModEntryState.ReadySingle || e.State == DoomModEntryState.ReadyMulti)
                    .Select(e => e.EntryId),
                StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(loadout.selectedModId) || !available.Contains(loadout.selectedModId.Trim()))
            {
                loadout.selectedModId = string.Empty;
            }
            else
            {
                loadout.selectedModId = loadout.selectedModId.Trim();
            }

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
                else if (key.Equals("selected_mod_id", StringComparison.OrdinalIgnoreCase))
                {
                    loadout.selectedModId = value;
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

