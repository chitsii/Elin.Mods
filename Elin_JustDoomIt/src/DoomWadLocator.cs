using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace Elin_ModTemplate
{
    public static class DoomWadLocator
    {
        private const string FixedWadName = "freedoom1.wad";

        public static string FindWad()
        {
            var candidates = new List<string>();
            var modDir = Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? "";
            candidates.Add(Path.Combine(modDir, "wad"));
            candidates.Add(Path.Combine(Paths.PluginPath, "Elin_JustDoomIt", "wad"));
            candidates.Add(Path.Combine(UnityEngine.Application.streamingAssetsPath, "doom"));

            foreach (var dir in candidates)
            {
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                {
                    continue;
                }

                var p = Path.Combine(dir, FixedWadName);
                if (File.Exists(p))
                {
                    return p;
                }
            }

            return null;
        }
    }
}
