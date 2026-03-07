using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace Elin_JustDoomIt
{
    public enum DoomModEntryState
    {
        Hidden = 0,
        ReadySingle = 1,
        SetupNeeded = 2,
        ReadyMulti = 3,
        ErrorConfig = 4,
        ErrorUnsupported = 5,
        ErrorLayout = 6
    }

    [DataContract]
    public sealed class DoomModEntryConfig
    {
        [DataMember(EmitDefaultValue = false)]
        public string display_name;

        [DataMember(EmitDefaultValue = false)]
        public string main_wad_file;

        [DataMember(EmitDefaultValue = false)]
        public string[] wad_order;
    }

    public sealed class DoomModEntryDefinition
    {
        public string EntryId;
        public string DisplayName;
        public string FolderPath;
        public string ContentRootPath;
        public DoomModEntryState State;
        public List<string> DetectedWadFiles = new List<string>();
        public List<string> LaunchWadFiles = new List<string>();
        public string MainWadFile;
        public string ManifestHash;
        public string BaseRequiredIwadFamily = "unknown";
        public string EffectiveRequiredIwadFamily = "unknown";
        public string ErrorReason;
    }

    public static class DoomModEntryCore
    {
        private static readonly string[] AllowedGuideExtensions = { ".txt", ".md", ".nfo", ".diz", ".jpg", ".png" };
        private static readonly string[] UnsupportedLaunchExtensions = { ".deh", ".bex", ".pk3", ".pk7" };

        public static List<DoomModEntryDefinition> DiscoverEntries(string modsRoot, string configRoot)
        {
            var results = new List<DoomModEntryDefinition>();
            if (string.IsNullOrWhiteSpace(modsRoot) || !Directory.Exists(modsRoot))
            {
                return results;
            }

            var directories = Directory.GetDirectories(modsRoot, "*", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);

            foreach (var folder in directories)
            {
                var entryId = Path.GetFileName(folder);
                if (string.IsNullOrWhiteSpace(entryId) || IsReservedFolder(entryId))
                {
                    continue;
                }

                var entry = DiscoverEntry(folder, configRoot);
                if (entry.State != DoomModEntryState.Hidden)
                {
                    results.Add(entry);
                }
            }

            return results;
        }

        public static DoomModEntryConfig LoadEntryConfig(string configRoot, string entryId)
        {
            var path = GetEntryConfigPath(configRoot, entryId);
            if (!File.Exists(path))
            {
                return null;
            }

            using (var stream = File.OpenRead(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(DoomModEntryConfig));
                return serializer.ReadObject(stream) as DoomModEntryConfig;
            }
        }

        public static void SaveEntryConfig(string configRoot, string entryId, DoomModEntryConfig config)
        {
            if (string.IsNullOrWhiteSpace(entryId))
            {
                throw new ArgumentException("entryId is required.", nameof(entryId));
            }

            var path = GetEntryConfigPath(configRoot, entryId);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var stream = File.Create(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(DoomModEntryConfig));
                serializer.WriteObject(stream, config ?? new DoomModEntryConfig());
            }
        }

        public static void ResetEntryConfig(string configRoot, string entryId)
        {
            var path = GetEntryConfigPath(configRoot, entryId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static string GetEntryConfigPath(string configRoot, string entryId)
        {
            return Path.Combine(configRoot ?? string.Empty, (entryId ?? string.Empty) + ".json");
        }

        public static string ComputeManifestHash(IEnumerable<string> launchFiles)
        {
            var files = (launchFiles ?? Enumerable.Empty<string>())
                .Where(File.Exists)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            using (var sha1 = SHA1.Create())
            {
                var raw = new StringBuilder();
                for (var i = 0; i < files.Length; i++)
                {
                    var path = files[i];
                    var info = new FileInfo(path);
                    raw.Append(Path.GetFileName(path).ToLowerInvariant())
                        .Append('|')
                        .Append(info.Length)
                        .Append('|')
                        .Append(info.LastWriteTimeUtc.Ticks)
                        .Append('|')
                        .Append(ComputeFileSha1(path))
                        .Append('\n');
                }

                var bytes = Encoding.UTF8.GetBytes(raw.ToString());
                var hash = sha1.ComputeHash(bytes);
                return ToHex(hash);
            }
        }

        public static string InferRequiredIwadFamily(IEnumerable<string> launchFiles)
        {
            var hasMapXx = false;
            var hasEpisodeMap = false;
            var anyParsed = false;

            foreach (var file in launchFiles ?? Enumerable.Empty<string>())
            {
                var result = InferFamilyFromSingleWad(file);
                if (result == "doom2")
                {
                    hasMapXx = true;
                    anyParsed = true;
                }
                else if (result == "doom1")
                {
                    hasEpisodeMap = true;
                    anyParsed = true;
                }
                else if (result == "any")
                {
                    hasMapXx = true;
                    hasEpisodeMap = true;
                    anyParsed = true;
                }
            }

            if (hasMapXx && hasEpisodeMap) return "any";
            if (hasMapXx) return "doom2";
            if (hasEpisodeMap) return "doom1";
            return anyParsed ? "unknown" : "unknown";
        }

        private static DoomModEntryDefinition DiscoverEntry(string folder, string configRoot)
        {
            var entry = new DoomModEntryDefinition
            {
                EntryId = Path.GetFileName(folder),
                DisplayName = Path.GetFileName(folder),
                FolderPath = folder
            };

            string contentRoot;
            string layoutError;
            if (!TryResolveContentRoot(folder, out contentRoot, out layoutError))
            {
                if (HasAnyWads(folder))
                {
                    entry.State = DoomModEntryState.ErrorLayout;
                    entry.ErrorReason = layoutError ?? "Folder structure is not supported";
                    return entry;
                }

                entry.State = DoomModEntryState.Hidden;
                return entry;
            }

            entry.ContentRootPath = contentRoot;
            entry.DetectedWadFiles = Directory.GetFiles(contentRoot, "*.wad", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (entry.DetectedWadFiles.Count == 0)
            {
                entry.State = DoomModEntryState.Hidden;
                return entry;
            }

            if (HasUnsupportedDirectInputs(contentRoot))
            {
                entry.State = DoomModEntryState.ErrorUnsupported;
                entry.ErrorReason = "This mod includes non-WAD files that v1 does not support";
                return entry;
            }

            if (entry.DetectedWadFiles.Count == 1)
            {
                entry.LaunchWadFiles = new List<string>(entry.DetectedWadFiles);
                entry.MainWadFile = entry.DetectedWadFiles[0];
                entry.ManifestHash = ComputeManifestHash(entry.LaunchWadFiles.Select(f => Path.Combine(contentRoot, f)));
                entry.BaseRequiredIwadFamily = InferRequiredIwadFamily(entry.LaunchWadFiles.Select(f => Path.Combine(contentRoot, f)));
                entry.EffectiveRequiredIwadFamily = entry.BaseRequiredIwadFamily;
                entry.State = DoomModEntryState.ReadySingle;
                return entry;
            }

            DoomModEntryConfig config = null;
            try
            {
                config = LoadEntryConfig(configRoot, entry.EntryId);
            }
            catch
            {
                entry.State = DoomModEntryState.ErrorConfig;
                entry.ErrorReason = "Failed to read saved setup";
                return entry;
            }

            if (config == null)
            {
                entry.State = DoomModEntryState.SetupNeeded;
                return entry;
            }

            if (!TryApplyConfig(entry, config))
            {
                entry.State = DoomModEntryState.ErrorConfig;
                if (string.IsNullOrWhiteSpace(entry.ErrorReason))
                {
                    entry.ErrorReason = "Saved setup is invalid";
                }
                return entry;
            }

            entry.ManifestHash = ComputeManifestHash(entry.LaunchWadFiles.Select(f => Path.Combine(contentRoot, f)));
            entry.BaseRequiredIwadFamily = InferRequiredIwadFamily(entry.LaunchWadFiles.Select(f => Path.Combine(contentRoot, f)));
            entry.EffectiveRequiredIwadFamily = entry.BaseRequiredIwadFamily;
            entry.State = DoomModEntryState.ReadyMulti;
            return entry;
        }

        private static bool TryApplyConfig(DoomModEntryDefinition entry, DoomModEntryConfig config)
        {
            if (entry == null || config == null)
            {
                return false;
            }

            var detected = new HashSet<string>(entry.DetectedWadFiles, StringComparer.OrdinalIgnoreCase);
            var order = (config.wad_order ?? Array.Empty<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList();

            if (order.Count == 0)
            {
                entry.ErrorReason = "Saved setup has no launch files";
                return false;
            }

            if (order.Count != order.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                entry.ErrorReason = "Saved setup contains duplicate launch files";
                return false;
            }

            for (var i = 0; i < order.Count; i++)
            {
                if (!detected.Contains(order[i]))
                {
                    entry.ErrorReason = "Saved setup references missing file: " + order[i];
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(config.main_wad_file))
            {
                entry.ErrorReason = "Saved setup is missing main_wad_file";
                return false;
            }

            if (!order.Contains(config.main_wad_file, StringComparer.OrdinalIgnoreCase))
            {
                entry.ErrorReason = "Saved setup main_wad_file is not in wad_order";
                return false;
            }

            entry.LaunchWadFiles = order;
            entry.MainWadFile = order.First(v => string.Equals(v, config.main_wad_file, StringComparison.OrdinalIgnoreCase));
            entry.DisplayName = string.IsNullOrWhiteSpace(config.display_name) ? entry.EntryId : config.display_name.Trim();
            return true;
        }

        private static bool TryResolveContentRoot(string folder, out string contentRoot, out string error)
        {
            contentRoot = null;
            error = null;

            if (GetDirectWadCount(folder) > 0)
            {
                contentRoot = folder;
                return true;
            }

            var childDirectories = Directory.GetDirectories(folder, "*", SearchOption.TopDirectoryOnly);
            var directFiles = Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly);
            if (childDirectories.Length == 1 && directFiles.All(IsAllowedGuideOrMetadataFile))
            {
                var child = childDirectories[0];
                if (GetDirectWadCount(child) > 0)
                {
                    contentRoot = child;
                    return true;
                }
            }

            error = "Folder structure is not supported";
            return false;
        }

        private static bool IsReservedFolder(string entryId)
        {
            return entryId.StartsWith(".", StringComparison.Ordinal) ||
                   entryId.StartsWith("_", StringComparison.Ordinal) ||
                   string.Equals(entryId, "__MACOSX", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetDirectWadCount(string folder)
        {
            return Directory.GetFiles(folder, "*.wad", SearchOption.TopDirectoryOnly).Length;
        }

        private static bool HasAnyWads(string folder)
        {
            return Directory.GetFiles(folder, "*.wad", SearchOption.AllDirectories).Length > 0;
        }

        private static bool IsAllowedGuideOrMetadataFile(string path)
        {
            var ext = Path.GetExtension(path) ?? string.Empty;
            return AllowedGuideExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        private static bool HasUnsupportedDirectInputs(string contentRoot)
        {
            var files = Directory.GetFiles(contentRoot, "*", SearchOption.TopDirectoryOnly);
            for (var i = 0; i < files.Length; i++)
            {
                var ext = Path.GetExtension(files[i]) ?? string.Empty;
                if (UnsupportedLaunchExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string InferFamilyFromSingleWad(string path)
        {
            if (!File.Exists(path))
            {
                return "unknown";
            }

            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var header = new byte[12];
                    if (stream.Read(header, 0, header.Length) != header.Length)
                    {
                        return "unknown";
                    }

                    var magic = Encoding.ASCII.GetString(header, 0, 4);
                    if (!string.Equals(magic, "IWAD", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(magic, "PWAD", StringComparison.OrdinalIgnoreCase))
                    {
                        return "unknown";
                    }

                    var lumpCount = BitConverter.ToInt32(header, 4);
                    var dirOffset = BitConverter.ToInt32(header, 8);
                    if (lumpCount < 0 || dirOffset < 0)
                    {
                        return "unknown";
                    }

                    var dirSize = lumpCount * 16L;
                    if (dirOffset + dirSize > stream.Length)
                    {
                        return "unknown";
                    }

                    stream.Seek(dirOffset, SeekOrigin.Begin);
                    var hasMapXx = false;
                    var hasEpisodeMap = false;
                    var entry = new byte[16];
                    for (var i = 0; i < lumpCount; i++)
                    {
                        if (stream.Read(entry, 0, entry.Length) != entry.Length)
                        {
                            return "unknown";
                        }

                        var lump = DecodeLumpName(entry, 8);
                        if (IsMapXx(lump))
                        {
                            hasMapXx = true;
                        }
                        else if (IsEpisodeMap(lump))
                        {
                            hasEpisodeMap = true;
                        }
                    }

                    if (hasMapXx && hasEpisodeMap) return "any";
                    if (hasMapXx) return "doom2";
                    if (hasEpisodeMap) return "doom1";
                }
            }
            catch
            {
                return "unknown";
            }

            return "unknown";
        }

        private static string DecodeLumpName(byte[] entry, int offset)
        {
            var chars = new char[8];
            var count = 0;
            for (var i = 0; i < 8; i++)
            {
                var b = entry[offset + i];
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

        private static string ComputeFileSha1(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha1 = SHA1.Create())
            {
                return ToHex(sha1.ComputeHash(stream));
            }
        }

        private static string ToHex(byte[] hash)
        {
            var sb = new StringBuilder(hash.Length * 2);
            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }
}

