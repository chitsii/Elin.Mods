using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace Elin_ModTemplate
{
    public sealed class DoomModRulePrompt
    {
        public string Sha1;
        public string FileName;
        public string SuggestedFamily;
        public string Confidence;
        public string ReasonCode;
    }

    public sealed class DoomModRuleRefreshResult
    {
        public readonly List<DoomModRulePrompt> Prompts = new List<DoomModRulePrompt>();
        public int ProcessedCount;
        public int AutoAcceptedCount;
        public int ReusedCount;
        public int StaleRemovedCount;
    }

    public struct DoomModRuleInfo
    {
        public bool Exists;
        public string Family;
        public string Source;
        public string Confidence;
        public string ReasonCode;
    }

    [Serializable]
    public sealed class DoomModRuleEntry
    {
        public string sha1;
        public string file_name;
        public long file_size;
        public long last_write_utc;
        public string family;
        public string source;
        public string confidence;
        public string reason_code;
    }

    [Serializable]
    internal sealed class DoomModRuleFile
    {
        public int version = 1;
        public List<DoomModRuleEntry> entries = new List<DoomModRuleEntry>();
    }

    internal struct DoomModInferenceResult
    {
        public string Family;
        public string Confidence;
        public string ReasonCode;
    }

    public static class DoomModRuleStore
    {
        private const long MaxParseBytes = 128L * 1024L * 1024L;
        private const int ParseTimeoutMs = 2000;

        private static readonly Dictionary<string, string> BuiltinRules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "NEIS.WAD", "doom1" },
            { "AV.WAD", "doom2" },
            { "AVMOVFIX.WAD", "doom2" },
            { "MM.WAD", "doom2" },
            { "MMMUS.WAD", "doom2" }
        };

        private static readonly object SyncRoot = new object();
        private static DoomModRuleFile _data;
        private static Dictionary<string, DoomModRuleEntry> _byHash;
        private static Dictionary<string, DoomModRuleEntry> _byFileName;
        private static bool _loaded;

        public static string GetRulesPath()
        {
            return DoomWadLocator.GetMutableProfilePath("mod_rules.json");
        }

        public static void EnsureLoaded()
        {
            lock (SyncRoot)
            {
                if (_loaded)
                {
                    return;
                }

                LoadUnsafe();
            }
        }

        public static DoomModRuleInfo GetRuleInfo(string pwadFileName)
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                var key = Path.GetFileName(pwadFileName ?? string.Empty);
                if (string.IsNullOrWhiteSpace(key))
                {
                    return default;
                }

                if (_byFileName.TryGetValue(key, out var rule))
                {
                    return ToInfo(rule, exists: true);
                }

                if (BuiltinRules.TryGetValue(key, out var builtinFamily))
                {
                    return new DoomModRuleInfo
                    {
                        Exists = true,
                        Family = builtinFamily,
                        Source = "builtin",
                        Confidence = "high",
                        ReasonCode = "builtin_known_mod"
                    };
                }

                return default;
            }
        }

        public static string GetFamilyOrUnknown(string pwadFileName)
        {
            var info = GetRuleInfo(pwadFileName);
            if (!info.Exists)
            {
                return "unknown";
            }

            return NormalizeFamily(info.Family);
        }

        public static bool SetManualFamily(string pwadFileName, string family, string reasonCode = "manual_override")
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                var normalizedFamily = NormalizeFamily(family);
                if (string.IsNullOrWhiteSpace(pwadFileName))
                {
                    return false;
                }

                var file = DoomWadLocator.FindPwads().FirstOrDefault(p =>
                    string.Equals(p.FileName, pwadFileName, StringComparison.OrdinalIgnoreCase));
                if (file == null || string.IsNullOrWhiteSpace(file.FullPath) || !File.Exists(file.FullPath))
                {
                    return false;
                }

                var info = new FileInfo(file.FullPath);
                var sha1 = ResolveSha1ForFileUnsafe(file.FileName, file.FullPath, info.Length, info.LastWriteTimeUtc.Ticks);
                if (string.IsNullOrWhiteSpace(sha1))
                {
                    return false;
                }

                var changed = false;
                if (!_byHash.TryGetValue(sha1, out var entry))
                {
                    entry = new DoomModRuleEntry { sha1 = sha1 };
                    _data.entries.Add(entry);
                    _byHash[sha1] = entry;
                    changed = true;
                }

                changed |= AssignEntryMetadata(entry, file.FileName, info.Length, info.LastWriteTimeUtc.Ticks, normalizedFamily, "manual", "high", reasonCode);
                _byFileName[file.FileName] = entry;

                if (changed)
                {
                    SaveUnsafe();
                }

                return true;
            }
        }

        public static DoomModRuleRefreshResult RefreshRules(IReadOnlyList<DoomWadEntry> pwads)
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                var result = new DoomModRuleRefreshResult();
                var changed = false;
                var existingFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (pwads != null)
                {
                    foreach (var file in pwads.OrderBy(p => p.FileName, StringComparer.OrdinalIgnoreCase))
                    {
                        if (file == null || string.IsNullOrWhiteSpace(file.FileName) || string.IsNullOrWhiteSpace(file.FullPath))
                        {
                            continue;
                        }

                        if (!File.Exists(file.FullPath))
                        {
                            continue;
                        }

                        existingFiles.Add(file.FileName);
                        result.ProcessedCount++;
                        var finfo = new FileInfo(file.FullPath);
                        var sha1 = ResolveSha1ForFileUnsafe(file.FileName, file.FullPath, finfo.Length, finfo.LastWriteTimeUtc.Ticks);
                        if (string.IsNullOrWhiteSpace(sha1))
                        {
                            DoomDiagnostics.Warn("[JustDoomIt] Failed to hash PWAD: " + file.FileName);
                            continue;
                        }

                        if (_byHash.TryGetValue(sha1, out var existing))
                        {
                            result.ReusedCount++;
                            if (AssignEntryMetadata(existing, file.FileName, finfo.Length, finfo.LastWriteTimeUtc.Ticks,
                                existing.family, existing.source, existing.confidence, existing.reason_code))
                            {
                                changed = true;
                            }

                            _byFileName[file.FileName] = existing;
                            continue;
                        }

                        DoomModRuleEntry entry;
                        if (BuiltinRules.TryGetValue(file.FileName, out var builtinFamily))
                        {
                            entry = new DoomModRuleEntry
                            {
                                sha1 = sha1,
                                file_name = file.FileName,
                                file_size = finfo.Length,
                                last_write_utc = finfo.LastWriteTimeUtc.Ticks,
                                family = builtinFamily,
                                source = "builtin",
                                confidence = "high",
                                reason_code = "builtin_known_mod"
                            };
                            result.AutoAcceptedCount++;
                        }
                        else
                        {
                            var inference = InferFromWad(file.FullPath);
                            entry = new DoomModRuleEntry
                            {
                                sha1 = sha1,
                                file_name = file.FileName,
                                file_size = finfo.Length,
                                last_write_utc = finfo.LastWriteTimeUtc.Ticks,
                                family = NormalizeFamily(inference.Family),
                                source = "auto",
                                confidence = NormalizeConfidence(inference.Confidence),
                                reason_code = string.IsNullOrWhiteSpace(inference.ReasonCode) ? "unknown" : inference.ReasonCode
                            };

                            if (entry.confidence == "high" && entry.family != "unknown")
                            {
                                result.AutoAcceptedCount++;
                            }
                            else
                            {
                                result.Prompts.Add(new DoomModRulePrompt
                                {
                                    Sha1 = entry.sha1,
                                    FileName = entry.file_name,
                                    SuggestedFamily = entry.family,
                                    Confidence = entry.confidence,
                                    ReasonCode = entry.reason_code
                                });
                            }
                        }

                        _data.entries.Add(entry);
                        _byHash[entry.sha1] = entry;
                        _byFileName[entry.file_name] = entry;
                        changed = true;
                    }
                }

                if (_data.entries.Count > 0)
                {
                    var keep = new List<DoomModRuleEntry>(_data.entries.Count);
                    for (var i = 0; i < _data.entries.Count; i++)
                    {
                        var e = _data.entries[i];
                        if (e == null || string.IsNullOrWhiteSpace(e.file_name))
                        {
                            changed = true;
                            continue;
                        }

                        if (!existingFiles.Contains(e.file_name))
                        {
                            result.StaleRemovedCount++;
                            changed = true;
                            continue;
                        }

                        keep.Add(e);
                    }

                    if (keep.Count != _data.entries.Count)
                    {
                        _data.entries = keep;
                        RebuildIndexesUnsafe();
                    }
                }

                if (changed)
                {
                    SaveUnsafe();
                }

                return result;
            }
        }

        public static string GetReasonText(string reasonCode, string family)
        {
            var code = (reasonCode ?? string.Empty).Trim().ToLowerInvariant();
            var fam = NormalizeFamily(family).ToUpperInvariant();
            var isCn = Lang.langCode == "CN";
            var isJp = Lang.isJP && !isCn;
            switch (code)
            {
                case "builtin_known_mod":
                    return isCn ? "内置规则" : (isJp ? "内蔵ルール" : "Built-in rule");
                case "mapxx_only":
                    return isCn ? "检测到MAPxx关卡标记 (" + fam + ")" : (isJp ? "MAPxxマップ定義を検出 (" + fam + ")" : "Found MAPxx lumps (" + fam + ")");
                case "exmy_only":
                    return isCn ? "检测到E#M#关卡标记 (" + fam + ")" : (isJp ? "E#M#マップ定義を検出 (" + fam + ")" : "Found E#M# lumps (" + fam + ")");
                case "mixed_map_formats":
                    return isCn ? "同时检测到MAPxx与E#M#" : (isJp ? "MAPxxとE#M#の両方を検出" : "Found both MAPxx and E#M#");
                case "no_map_lumps":
                    return isCn ? "未检测到关卡标记" : (isJp ? "マップ定義が見つかりません" : "No map lumps found");
                case "file_too_large":
                    return isCn ? "文件过大（>128MB）" : (isJp ? "ファイルサイズ超過（>128MB）" : "File too large (>128MB)");
                case "parse_timeout":
                    return isCn ? "解析超时（>2秒）" : (isJp ? "解析タイムアウト（>2秒）" : "Parser timeout (>2s)");
                case "parse_error":
                    return isCn ? "WAD解析失败" : (isJp ? "WAD解析失敗" : "Failed to parse WAD");
                case "manual_override":
                    return isCn ? "手动覆盖" : (isJp ? "手動設定" : "Manual override");
                default:
                    return isCn ? "不明" : (isJp ? "不明" : "Unknown");
            }
        }

        private static DoomModRuleInfo ToInfo(DoomModRuleEntry rule, bool exists)
        {
            return new DoomModRuleInfo
            {
                Exists = exists,
                Family = NormalizeFamily(rule.family),
                Source = NormalizeSource(rule.source),
                Confidence = NormalizeConfidence(rule.confidence),
                ReasonCode = string.IsNullOrWhiteSpace(rule.reason_code) ? "unknown" : rule.reason_code
            };
        }

        private static void LoadUnsafe()
        {
            _loaded = true;
            _data = new DoomModRuleFile();
            var path = DoomWadLocator.ResolveProfilePathForRead("mod_rules.json");
            try
            {
                if (File.Exists(path))
                {
                    var parsed = IO.LoadFile<DoomModRuleFile>(path, compress: false);
                    if (parsed != null && parsed.entries != null)
                    {
                        _data = parsed;
                    }
                }
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Failed to read mod_rules.json: " + ex.Message);
                _data = new DoomModRuleFile();
            }

            if (_data.entries == null)
            {
                _data.entries = new List<DoomModRuleEntry>();
            }

            RebuildIndexesUnsafe();
        }

        private static void SaveUnsafe()
        {
            var path = GetRulesPath();
            var dir = Path.GetDirectoryName(path) ?? string.Empty;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            IO.SaveFile(path, _data, compress: false);
        }

        private static void RebuildIndexesUnsafe()
        {
            _byHash = new Dictionary<string, DoomModRuleEntry>(StringComparer.OrdinalIgnoreCase);
            _byFileName = new Dictionary<string, DoomModRuleEntry>(StringComparer.OrdinalIgnoreCase);
            if (_data?.entries == null)
            {
                return;
            }

            for (var i = 0; i < _data.entries.Count; i++)
            {
                var e = _data.entries[i];
                if (e == null || string.IsNullOrWhiteSpace(e.sha1))
                {
                    continue;
                }

                e.family = NormalizeFamily(e.family);
                e.source = NormalizeSource(e.source);
                e.confidence = NormalizeConfidence(e.confidence);
                if (string.IsNullOrWhiteSpace(e.reason_code))
                {
                    e.reason_code = "unknown";
                }

                _byHash[e.sha1] = e;
                if (!string.IsNullOrWhiteSpace(e.file_name))
                {
                    if (_byFileName.TryGetValue(e.file_name, out var existing))
                    {
                        if (ComparePriority(e, existing) >= 0)
                        {
                            _byFileName[e.file_name] = e;
                        }
                    }
                    else
                    {
                        _byFileName[e.file_name] = e;
                    }
                }
            }
        }

        private static int ComparePriority(DoomModRuleEntry a, DoomModRuleEntry b)
        {
            return ScoreSource(a.source).CompareTo(ScoreSource(b.source));
        }

        private static int ScoreSource(string source)
        {
            switch (NormalizeSource(source))
            {
                case "manual": return 3;
                case "builtin": return 2;
                case "auto": return 1;
                default: return 0;
            }
        }

        private static bool AssignEntryMetadata(
            DoomModRuleEntry entry,
            string fileName,
            long fileSize,
            long lastWriteUtc,
            string family,
            string source,
            string confidence,
            string reasonCode)
        {
            var changed = false;
            if (!string.Equals(entry.file_name, fileName, StringComparison.OrdinalIgnoreCase))
            {
                entry.file_name = fileName;
                changed = true;
            }

            if (entry.file_size != fileSize)
            {
                entry.file_size = fileSize;
                changed = true;
            }

            if (entry.last_write_utc != lastWriteUtc)
            {
                entry.last_write_utc = lastWriteUtc;
                changed = true;
            }

            var nFamily = NormalizeFamily(family);
            if (!string.Equals(entry.family, nFamily, StringComparison.OrdinalIgnoreCase))
            {
                entry.family = nFamily;
                changed = true;
            }

            var nSource = NormalizeSource(source);
            if (!string.Equals(entry.source, nSource, StringComparison.OrdinalIgnoreCase))
            {
                entry.source = nSource;
                changed = true;
            }

            var nConfidence = NormalizeConfidence(confidence);
            if (!string.Equals(entry.confidence, nConfidence, StringComparison.OrdinalIgnoreCase))
            {
                entry.confidence = nConfidence;
                changed = true;
            }

            var nReason = string.IsNullOrWhiteSpace(reasonCode) ? "unknown" : reasonCode.Trim();
            if (!string.Equals(entry.reason_code, nReason, StringComparison.OrdinalIgnoreCase))
            {
                entry.reason_code = nReason;
                changed = true;
            }

            return changed;
        }

        private static string ResolveSha1ForFileUnsafe(string fileName, string fullPath, long fileSize, long lastWriteUtc)
        {
            if (_byFileName.TryGetValue(fileName, out var entry))
            {
                if (entry.file_size == fileSize && entry.last_write_utc == lastWriteUtc && !string.IsNullOrWhiteSpace(entry.sha1))
                {
                    return entry.sha1;
                }
            }

            return ComputeFileSha1(fullPath);
        }

        private static string ComputeFileSha1(string path)
        {
            try
            {
                using (var fs = File.OpenRead(path))
                using (var sha1 = SHA1.Create())
                {
                    var hash = sha1.ComputeHash(fs);
                    var sb = new StringBuilder(hash.Length * 2);
                    for (var i = 0; i < hash.Length; i++)
                    {
                        sb.Append(hash[i].ToString("x2"));
                    }

                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] SHA1 failed for " + path + ": " + ex.Message);
                return null;
            }
        }

        private static DoomModInferenceResult InferFromWad(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    return NewInference("unknown", "low", "parse_error");
                }

                if (info.Length > MaxParseBytes)
                {
                    return NewInference("unknown", "low", "file_too_large");
                }

                var sw = Stopwatch.StartNew();
                using (var fs = File.OpenRead(path))
                {
                    if (sw.ElapsedMilliseconds > ParseTimeoutMs)
                    {
                        return NewInference("unknown", "low", "parse_timeout");
                    }

                    var header = new byte[12];
                    if (fs.Read(header, 0, header.Length) != header.Length)
                    {
                        return NewInference("unknown", "low", "parse_error");
                    }

                    var magic = Encoding.ASCII.GetString(header, 0, 4);
                    if (!string.Equals(magic, "IWAD", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(magic, "PWAD", StringComparison.OrdinalIgnoreCase))
                    {
                        return NewInference("unknown", "low", "parse_error");
                    }

                    var lumpCount = BitConverter.ToInt32(header, 4);
                    var dirOffset = BitConverter.ToInt32(header, 8);
                    if (lumpCount < 0 || dirOffset < 0)
                    {
                        return NewInference("unknown", "low", "parse_error");
                    }

                    var dirSize = lumpCount * 16L;
                    if (dirOffset + dirSize > fs.Length)
                    {
                        return NewInference("unknown", "low", "parse_error");
                    }

                    fs.Seek(dirOffset, SeekOrigin.Begin);
                    var hasMapXx = false;
                    var hasEpisodeMap = false;
                    var dir = new byte[16];
                    for (var i = 0; i < lumpCount; i++)
                    {
                        if (sw.ElapsedMilliseconds > ParseTimeoutMs)
                        {
                            return NewInference("unknown", "low", "parse_timeout");
                        }

                        if (fs.Read(dir, 0, dir.Length) != dir.Length)
                        {
                            return NewInference("unknown", "low", "parse_error");
                        }

                        var name = DecodeLumpName(dir, 8);
                        if (IsMapXx(name))
                        {
                            hasMapXx = true;
                        }
                        else if (IsEpisodeMap(name))
                        {
                            hasEpisodeMap = true;
                        }
                    }

                    if (hasMapXx && !hasEpisodeMap)
                    {
                        return NewInference("doom2", "high", "mapxx_only");
                    }

                    if (hasEpisodeMap && !hasMapXx)
                    {
                        return NewInference("doom1", "high", "exmy_only");
                    }

                    if (hasMapXx && hasEpisodeMap)
                    {
                        return NewInference("any", "medium", "mixed_map_formats");
                    }

                    return NewInference("unknown", "low", "no_map_lumps");
                }
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] PWAD inference failed for " + path + ": " + ex.Message);
                return NewInference("unknown", "low", "parse_error");
            }
        }

        private static DoomModInferenceResult NewInference(string family, string confidence, string reason)
        {
            return new DoomModInferenceResult
            {
                Family = NormalizeFamily(family),
                Confidence = NormalizeConfidence(confidence),
                ReasonCode = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason
            };
        }

        private static string DecodeLumpName(byte[] dir, int offset)
        {
            var chars = new char[8];
            var count = 0;
            for (var i = 0; i < 8; i++)
            {
                var b = dir[offset + i];
                if (b == 0)
                {
                    break;
                }

                chars[count++] = char.ToUpperInvariant((char)b);
            }

            return new string(chars, 0, count);
        }

        private static bool IsMapXx(string lump)
        {
            return lump.Length == 5 &&
                   lump[0] == 'M' &&
                   lump[1] == 'A' &&
                   lump[2] == 'P' &&
                   char.IsDigit(lump[3]) &&
                   char.IsDigit(lump[4]);
        }

        private static bool IsEpisodeMap(string lump)
        {
            return lump.Length == 4 &&
                   lump[0] == 'E' &&
                   char.IsDigit(lump[1]) &&
                   lump[2] == 'M' &&
                   char.IsDigit(lump[3]);
        }

        public static string NormalizeFamily(string family)
        {
            if (string.IsNullOrWhiteSpace(family))
            {
                return "unknown";
            }

            switch (family.Trim().ToLowerInvariant())
            {
                case "doom1":
                    return "doom1";
                case "doom2":
                    return "doom2";
                case "any":
                    return "any";
                default:
                    return "unknown";
            }
        }

        public static string NormalizeSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return "auto";
            }

            switch (source.Trim().ToLowerInvariant())
            {
                case "manual":
                    return "manual";
                case "builtin":
                    return "builtin";
                default:
                    return "auto";
            }
        }

        public static string NormalizeConfidence(string confidence)
        {
            if (string.IsNullOrWhiteSpace(confidence))
            {
                return "low";
            }

            switch (confidence.Trim().ToLowerInvariant())
            {
                case "high":
                    return "high";
                case "medium":
                    return "medium";
                default:
                    return "low";
            }
        }
    }
}
