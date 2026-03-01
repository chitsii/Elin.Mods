using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Elin_BottleNeckFinder
{
    public static class PatchRegistry
    {
        public struct PatchEntry
        {
            public MethodBase Method;
            public string MethodFullName;
            public List<string> OwnerIds;
        }

        private static readonly Dictionary<string, string> _modNames
            = new Dictionary<string, string>();

        private static readonly Dictionary<string, string> _asmToHarmonyId
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<PatchEntry> _entries = new List<PatchEntry>();

        private static readonly Dictionary<MethodBase, PatchEntry> _methodLookup
            = new Dictionary<MethodBase, PatchEntry>();

        public static IReadOnlyList<PatchEntry> Entries => _entries;
        public static IReadOnlyDictionary<string, string> ModNames => _modNames;
        public static IReadOnlyDictionary<string, string> AsmToHarmonyId => _asmToHarmonyId;

        public static int TotalPatches { get; private set; }
        public static int TranspilerMethodCount { get; private set; }

        public static void Build()
        {
            _modNames.Clear();
            _asmToHarmonyId.Clear();
            _entries.Clear();
            _methodLookup.Clear();
            TotalPatches = 0;
            TranspilerMethodCount = 0;

            BuildModNameMap();
            CollectPatches();

            Debug.Log($"[BNF] PatchRegistry: {_entries.Count} methods, "
                + $"{TotalPatches} patches, {_modNames.Count} mods");
        }

        private static void BuildModNameMap()
        {
            foreach (var obj in ModManager.ListPluginObject)
            {
                var plugin = obj as BaseUnityPlugin;
                if (plugin == null) continue;

                var meta = plugin.Info.Metadata;
                _modNames[meta.GUID] = meta.Name;

                var asmName = plugin.GetType().Assembly.GetName().Name;
                if (!_asmToHarmonyId.ContainsKey(asmName))
                    _asmToHarmonyId[asmName] = meta.GUID;
            }
        }

        private static void CollectPatches()
        {
            foreach (var method in Harmony.GetAllPatchedMethods())
            {
                try
                {
                    var info = Harmony.GetPatchInfo(method);
                    if (info == null) continue;

                    var owners = info.Owners.ToList();
                    if (owners.Count == 0) continue;

                    int count = (info.Prefixes?.Count ?? 0)
                              + (info.Postfixes?.Count ?? 0)
                              + (info.Transpilers?.Count ?? 0)
                              + (info.Finalizers?.Count ?? 0);

                    TotalPatches += count;

                    if ((info.Transpilers?.Count ?? 0) > 0)
                        TranspilerMethodCount++;

                    var entry = new PatchEntry
                    {
                        Method = method,
                        MethodFullName = $"{method.DeclaringType?.Name}.{method.Name}",
                        OwnerIds = owners
                    };
                    _entries.Add(entry);
                    _methodLookup[method] = entry;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BNF] Failed to inspect patch: {ex.Message}");
                }
            }
        }

        public static PatchEntry? FindByMethod(MethodBase method)
        {
            return _methodLookup.TryGetValue(method, out var e) ? e : (PatchEntry?)null;
        }

        public static string GetModName(string harmonyId)
        {
            if (_modNames.TryGetValue(harmonyId, out var name))
                return name;
            return harmonyId;
        }

        public static string ResolveModFromAsm(string assemblyName)
        {
            if (_asmToHarmonyId.TryGetValue(assemblyName, out var hid))
                return GetModName(hid);
            return null;
        }

        public static List<string> GetOwnerNamesForMethod(MethodBase method)
        {
            if (!_methodLookup.TryGetValue(method, out var entry))
                return new List<string>();

            var names = new List<string>(entry.OwnerIds.Count);
            foreach (var id in entry.OwnerIds)
            {
                string name = GetModName(id);
                if (!names.Contains(name))
                    names.Add(name);
            }
            return names;
        }

        public static List<PatchEntry> GetMultiModEntries()
        {
            var result = new List<PatchEntry>();
            foreach (var entry in _entries)
            {
                if (entry.OwnerIds.Count >= 2)
                    result.Add(entry);
            }
            return result;
        }

        private static Dictionary<string, List<string>> _methodNameToOwners;

        public static List<string> GetOwnerNamesByMethodName(string methodFullName)
        {
            if (_methodNameToOwners == null)
            {
                _methodNameToOwners = new Dictionary<string, List<string>>();
                foreach (var entry in _entries)
                {
                    if (_methodNameToOwners.ContainsKey(entry.MethodFullName))
                        continue;
                    var names = new List<string>();
                    foreach (var id in entry.OwnerIds)
                    {
                        string name = GetModName(id);
                        if (!names.Contains(name))
                            names.Add(name);
                    }
                    _methodNameToOwners[entry.MethodFullName] = names;
                }
            }
            return _methodNameToOwners.TryGetValue(methodFullName, out var result)
                ? result : null;
        }
    }
}
