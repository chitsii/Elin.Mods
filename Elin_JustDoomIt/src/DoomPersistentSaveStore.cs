using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx;

namespace Elin_ModTemplate
{
    public struct DoomSaveSummary
    {
        public long SavedUtcTicks;
        public int TotalPlaySeconds;
        public int LastSessionSeconds;
        public int TotalKills;
        public int MaxKillStreak;
        public int TotalChips;
    }

    public static class DoomPersistentSaveStore
    {
        private const string SavePrefix = "slot_";
        private const string SaveExt = ".sav";
        private const string SaveBakExt = ".sav.bak";
        private const string SaveMetaExt = ".meta";

        public static string BuildSlotKey(string iwadFileName, IReadOnlyList<string> pwadFileNames, int skill)
        {
            var iwad = (iwadFileName ?? string.Empty).Trim().ToLowerInvariant();
            var parts = new List<string> { "iwad=" + iwad, "skill=" + Math.Max(1, Math.Min(5, skill)) };
            if (pwadFileNames != null && pwadFileNames.Count > 0)
            {
                for (var i = 0; i < pwadFileNames.Count; i++)
                {
                    var pwad = Path.GetFileName(pwadFileNames[i] ?? string.Empty).Trim().ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(pwad))
                    {
                        parts.Add("pwad" + i + "=" + pwad);
                    }
                }
            }

            var raw = string.Join("|", parts);
            return ToSha1Hex(raw);
        }

        public static bool HasSave(string slotKey)
        {
            if (string.IsNullOrWhiteSpace(slotKey))
            {
                return false;
            }

            return File.Exists(GetSlotPath(slotKey));
        }

        public static bool TryLoadSummary(string slotKey, out DoomSaveSummary summary)
        {
            summary = default;
            try
            {
                if (string.IsNullOrWhiteSpace(slotKey))
                {
                    return false;
                }

                var metaPath = GetSlotMetaPath(slotKey);
                if (!File.Exists(metaPath))
                {
                    return false;
                }

                var lines = File.ReadAllLines(metaPath);
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var sep = line.IndexOf('=');
                    if (sep <= 0)
                    {
                        continue;
                    }

                    var key = line.Substring(0, sep).Trim();
                    var value = line.Substring(sep + 1);
                    map[key] = value;
                }

                summary = new DoomSaveSummary
                {
                    SavedUtcTicks = ParseLong(map, "saved_utc_ticks", 0),
                    TotalPlaySeconds = ParseInt(map, "total_play_seconds", 0),
                    LastSessionSeconds = ParseInt(map, "last_session_seconds", 0),
                    TotalKills = ParseInt(map, "total_kills", 0),
                    MaxKillStreak = ParseInt(map, "max_kill_streak", 0),
                    TotalChips = ParseInt(map, "total_chips", 0)
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryStoreSummary(string slotKey, DoomSaveSummary summary, out string error)
        {
            error = null;
            try
            {
                if (string.IsNullOrWhiteSpace(slotKey))
                {
                    return false;
                }

                EnsureSaveRoot();
                var path = GetSlotMetaPath(slotKey);
                var tmp = path + ".tmp";
                var lines = new[]
                {
                    "saved_utc_ticks=" + summary.SavedUtcTicks,
                    "total_play_seconds=" + summary.TotalPlaySeconds,
                    "last_session_seconds=" + summary.LastSessionSeconds,
                    "total_kills=" + summary.TotalKills,
                    "max_kill_streak=" + summary.MaxKillStreak,
                    "total_chips=" + summary.TotalChips
                };

                File.WriteAllLines(tmp, lines);
                if (File.Exists(path))
                {
                    File.Replace(tmp, path, null, true);
                }
                else
                {
                    File.Move(tmp, path);
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryImportToEngineSlot(string slotKey, string engineSavePath, out string error)
        {
            error = null;
            try
            {
                var src = GetSlotPath(slotKey);
                if (!File.Exists(src))
                {
                    return false;
                }

                var engineDir = Path.GetDirectoryName(engineSavePath) ?? string.Empty;
                if (!Directory.Exists(engineDir))
                {
                    Directory.CreateDirectory(engineDir);
                }

                File.Copy(src, engineSavePath, true);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryExportFromEngineSlot(string slotKey, string engineSavePath, out string error)
        {
            error = null;
            try
            {
                if (string.IsNullOrWhiteSpace(slotKey) || !File.Exists(engineSavePath))
                {
                    return false;
                }

                var dst = GetSlotPath(slotKey);
                var bak = GetSlotBakPath(slotKey);
                var tmp = dst + ".tmp";

                EnsureSaveRoot();
                File.Copy(engineSavePath, tmp, true);

                if (File.Exists(dst))
                {
                    File.Copy(dst, bak, true);
                }

                if (File.Exists(dst))
                {
                    File.Replace(tmp, dst, bak, true);
                }
                else
                {
                    File.Move(tmp, dst);
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static string GetSaveRoot()
        {
            return Path.Combine(Paths.ConfigPath, Plugin.ModGuid, "saves");
        }

        private static string GetSlotPath(string slotKey)
        {
            return Path.Combine(GetSaveRoot(), SavePrefix + slotKey + SaveExt);
        }

        private static string GetSlotBakPath(string slotKey)
        {
            return Path.Combine(GetSaveRoot(), SavePrefix + slotKey + SaveBakExt);
        }

        private static string GetSlotMetaPath(string slotKey)
        {
            return Path.Combine(GetSaveRoot(), SavePrefix + slotKey + SaveMetaExt);
        }

        private static void EnsureSaveRoot()
        {
            var root = GetSaveRoot();
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
        }

        private static string ToSha1Hex(string input)
        {
            using (var sha1 = SHA1.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
                var hash = sha1.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static int ParseInt(Dictionary<string, string> map, string key, int defaultValue)
        {
            return map.TryGetValue(key, out var v) && int.TryParse(v, out var parsed) ? parsed : defaultValue;
        }

        private static long ParseLong(Dictionary<string, string> map, string key, long defaultValue)
        {
            return map.TryGetValue(key, out var v) && long.TryParse(v, out var parsed) ? parsed : defaultValue;
        }

    }
}
