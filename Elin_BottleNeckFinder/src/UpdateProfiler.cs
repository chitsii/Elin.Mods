using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Elin_BottleNeckFinder
{
    public static class UpdateProfiler
    {
        private static Harmony _harmony;
        private static bool _patchesApplied;
        private static volatile bool _enabled;

        private static readonly Dictionary<string, long> _frameTicks
            = new Dictionary<string, long>();

        private static readonly Dictionary<Type, string> _typeToGuid
            = new Dictionary<Type, string>();

        [ThreadStatic] private static Stack<long> _tsStack;

        public static bool IsActive => _enabled;
        public static IReadOnlyDictionary<string, long> FrameTicks => _frameTicks;

        private static readonly string[] UpdateMethodNames =
            { "Update", "LateUpdate", "FixedUpdate" };

        public static void Start()
        {
            if (_enabled) return;

            if (!_patchesApplied)
            {
                _harmony = new Harmony("tishi.bnf.profiler.update");
                int patched = 0;

                foreach (var obj in ModManager.ListPluginObject)
                {
                    var plugin = obj as BaseUnityPlugin;
                    if (plugin == null) continue;

                    var meta = plugin.Info.Metadata;
                    if (meta.GUID == Plugin.ModGuid) continue;

                    var pluginType = plugin.GetType();
                    _typeToGuid[pluginType] = meta.GUID;

                    foreach (var methodName in UpdateMethodNames)
                    {
                        var method = pluginType.GetMethod(methodName,
                            BindingFlags.Instance | BindingFlags.Public
                            | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (method == null) continue;

                        try
                        {
                            _harmony.Patch(method,
                                prefix: new HarmonyMethod(typeof(UpdateProfiler),
                                    nameof(Prefix)) { priority = Priority.First },
                                postfix: new HarmonyMethod(typeof(UpdateProfiler),
                                    nameof(Postfix)) { priority = Priority.Last }
                            );
                            patched++;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[BNF] Failed to profile "
                                + $"{pluginType.Name}.{methodName}: {ex.Message}");
                        }
                    }
                }

                _patchesApplied = true;
                Debug.Log($"[BNF] UpdateProfiler: {patched} methods instrumented");
            }

            _enabled = true;
            Debug.Log("[BNF] UpdateProfiler started");
        }

        public static void Stop()
        {
            if (!_enabled) return;

            // Do NOT call UnpatchSelf() - it breaks MonoMod/CWL hook chains.
            // Patches stay applied but are no-ops when _enabled is false.
            _enabled = false;
            _frameTicks.Clear();

            Debug.Log("[BNF] UpdateProfiler stopped");
        }

        public static void BeginFrame()
        {
            _frameTicks.Clear();
        }

        public static void Prefix(MonoBehaviour __instance)
        {
            if (!_enabled) return;
            if (_tsStack == null) _tsStack = new Stack<long>();
            _tsStack.Push(Stopwatch.GetTimestamp());
        }

        public static void Postfix(MonoBehaviour __instance)
        {
            if (!_enabled) return;
            if (_tsStack == null || _tsStack.Count == 0) return;

            long start = _tsStack.Pop();
            long elapsed = Stopwatch.GetTimestamp() - start;

            var type = __instance.GetType();
            if (!_typeToGuid.TryGetValue(type, out var guid)) return;

            if (_frameTicks.TryGetValue(guid, out long existing))
                _frameTicks[guid] = existing + elapsed;
            else
                _frameTicks[guid] = elapsed;
        }
    }
}
