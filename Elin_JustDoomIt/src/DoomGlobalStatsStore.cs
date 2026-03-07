using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace Elin_JustDoomIt
{
    public static class DoomGlobalStatsStore
    {
        private static readonly object Sync = new object();
        private static bool _loaded;
        private static int _totalPlaySeconds;
        private static bool _dirty;

        public static void EnsureLoaded()
        {
            lock (Sync)
            {
                if (_loaded)
                {
                    return;
                }

                _loaded = true;
                _totalPlaySeconds = 0;
                _dirty = false;

                try
                {
                    var path = GetStatsPath();
                    if (!File.Exists(path))
                    {
                        return;
                    }

                    var lines = File.ReadAllLines(path);
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

                        map[line.Substring(0, sep).Trim()] = line.Substring(sep + 1).Trim();
                    }

                    if (map.TryGetValue("total_play_seconds", out var secs) && int.TryParse(secs, out var parsed))
                    {
                        _totalPlaySeconds = Math.Max(0, parsed);
                    }
                }
                catch (Exception ex)
                {
                    DoomDiagnostics.Warn("[JustDoomIt] Failed to load global stats: " + ex.Message);
                }
            }
        }

        public static int GetTotalPlaySeconds()
        {
            EnsureLoaded();
            lock (Sync)
            {
                return _totalPlaySeconds;
            }
        }

        public static void AddPlaySeconds(int seconds)
        {
            if (seconds <= 0)
            {
                return;
            }

            EnsureLoaded();
            lock (Sync)
            {
                _totalPlaySeconds = Math.Max(0, _totalPlaySeconds + seconds);
                _dirty = true;
            }
        }

        public static void Flush()
        {
            EnsureLoaded();
            lock (Sync)
            {
                if (!_dirty)
                {
                    return;
                }

                try
                {
                    var path = GetStatsPath();
                    var dir = Path.GetDirectoryName(path) ?? string.Empty;
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var tmp = path + ".tmp";
                    var lines = new[]
                    {
                        "format=v2",
                        "saved_utc_ticks=" + DateTime.UtcNow.Ticks,
                        "total_play_seconds=" + _totalPlaySeconds
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

                    _dirty = false;
                }
                catch (Exception ex)
                {
                    DoomDiagnostics.Warn("[JustDoomIt] Failed to flush global stats: " + ex.Message);
                }
            }
        }

        private static string GetStatsPath()
        {
            return Path.Combine(Paths.ConfigPath, Plugin.ModGuid, "stats", "doom_global_stats.txt");
        }
    }
}


