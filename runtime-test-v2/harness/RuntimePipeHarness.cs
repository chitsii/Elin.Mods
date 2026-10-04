using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Elin.RuntimeTestPipe
{
    // Temporary test infrastructure; never install as a product Mod.
    [BepInPlugin(Owner, "Runtime test pipe repair", "0.1.0")]
    [BepInDependency("elin.plugins.scripting")]
    public sealed class RuntimePipeHarness : BaseUnityPlugin
    {
        public const string Owner = "chitsii.elin.runtime_test_pipe";
        const string ExpectedKitHash = "707BBC730B9535198F968A7F47DB7DFBAB11152583B074E2A83073F8984F8852";
        static RuntimePipeHarness instance;
        static FieldInfo queueField, cancellationField;
        static MethodInfo notify;
        Harmony harmony;
        bool stopping;
        int mainThreadId;

        void Awake()
        {
            if (instance != null) { Logger.LogError("Duplicate harness rejected."); enabled = false; return; }
            string marker = Path.Combine(Path.GetDirectoryName(Info.Location), "enable.txt");
            if (!File.Exists(marker) || File.ReadAllText(marker).Trim() != "world_11")
            { Logger.LogWarning("Inactive: explicit world_11 marker is absent."); enabled = false; return; }
            try
            {
                if (!ExistingScriptingEnabled()) throw new InvalidOperationException("Existing CWL AllowScripting is disabled; no setting will be changed.");
                Type type = AccessTools.TypeByName("EModding.Components.EPipe");
                if (type == null || type.Assembly.GetName().Name != "ElinModdingKit" || type.Assembly.GetName().Version.ToString() != "1.4.0.30832")
                    throw new InvalidOperationException("Unsupported pipe implementation/version; no patch applied.");
                int copies = 0;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    if (assembly.GetName().Name == "ElinModdingKit") copies++;
                if (copies != 1) throw new InvalidOperationException("Duplicate ModdingKit assembly identity; no patch applied.");
                string expectedPath = Path.GetFullPath(Path.Combine(Paths.GameRootPath, "Package", "_ModdingKit", "ElinModdingKit.dll"));
                if (!string.Equals(Path.GetFullPath(type.Assembly.Location), expectedPath, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Loaded ModdingKit location mismatch; no patch applied.");
                using (var input = File.OpenRead(type.Assembly.Location))
                using (var hash = SHA256.Create())
                    if (BitConverter.ToString(hash.ComputeHash(input)).Replace("-", "") != ExpectedKitHash)
                        throw new InvalidOperationException("Installed ModdingKit hash changed; no patch applied.");
                if (UnityEngine.Object.FindObjectsOfType(type).Length != 0)
                    throw new InvalidOperationException("Pipe instance already exists; refusing to revive a live queue. Restart required.");
                MethodInfo method = type.GetMethod("ProcessCommands", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (method == null || method.ReturnType != typeof(IEnumerator) || method.GetParameters().Length != 0)
                    throw new InvalidOperationException("Pipe method signature mismatch.");
                queueField = type.GetField("_commands", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                cancellationField = type.GetField("_cts", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                notify = type.GetMethod("Notify", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new[] { typeof(NamedPipeServerStream), typeof(string) }, null);
                if (queueField == null || queueField.FieldType != typeof(ConcurrentQueue<(NamedPipeServerStream, string)>) ||
                    cancellationField == null || cancellationField.FieldType != typeof(CancellationTokenSource) || notify == null || notify.ReturnType.FullName != "Cysharp.Threading.Tasks.UniTaskVoid")
                    throw new InvalidOperationException("Pipe queue/teardown/reply signature mismatch.");
                var patches = Harmony.GetPatchInfo(method);
                if (patches != null)
                    foreach (var prefix in patches.Prefixes)
                        if (prefix.owner == Owner) throw new InvalidOperationException("Duplicate harness Harmony owner rejected.");
                instance = this;
                mainThreadId = Thread.CurrentThread.ManagedThreadId;
                harmony = new Harmony(Owner);
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(RuntimePipeHarness), nameof(ReplacePump)));
                Logger.LogInfo("Loaded kit=" + type.Assembly.FullName + " location=" + expectedPath + " sha256=" + ExpectedKitHash);
                Logger.LogInfo("Test-only EPipe repair installed before pipe startup; world_11 / RUNTIME_TEST / existing scripting guard required.");
            }
            catch (Exception error)
            { Logger.LogError(error); stopping = true; harmony?.UnpatchSelf(); if (instance == this) instance = null; enabled = false; }
        }

        static bool ReplacePump(object __instance, ref IEnumerator __result)
        {
            var owner = instance;
            if (owner == null || owner.stopping) return true;
            var queue = (ConcurrentQueue<(NamedPipeServerStream server, string cmd)>)queueField.GetValue(__instance);
            var cancellation = (CancellationTokenSource)cancellationField.GetValue(null);
            owner.Logger.LogInfo("Pump started on Unity thread " + Thread.CurrentThread.ManagedThreadId);
            __result = new CommandPump<(NamedPipeServerStream server, string cmd)>(queue,
                () => !owner.stopping && !cancellation.IsCancellationRequested,
                item => owner.Dispatch(item.cmd),
                (item, text) => { if (item.server.IsConnected) notify.Invoke(__instance, new object[] { item.server, text }); },
                error => owner.Logger.LogError(error)).Run();
            return false;
        }

        string Dispatch(string command)
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThreadId) throw new InvalidOperationException("Main-thread dispatch guard rejected.");
            string id = Game.id;
            string name = EClass.core?.game?.player?.chara?.Name;
            bool allowed = ExistingScriptingEnabled() && EScript.IsScriptingAvailable;
            if (!TestSessionGuard.Allows(id, name, allowed))
                return "(err): runtime_guard_rejected (world_11 / RUNTIME_TEST / enabled scripting required)";
            if (command != "cs.version" && !command.StartsWith("cs.eval ", StringComparison.Ordinal) && !command.StartsWith("cs.file ", StringComparison.Ordinal))
                return "(err): test harness only accepts cs.version, cs.eval, cs.file";
            Logger.LogInfo("Dispatch " + command.Split(' ')[0] + " save=" + id + " pc=" + name + " timeScale=" + Time.timeScale + " thread=" + mainThreadId);
            string result = ConsoleCommandHelper.EvaluateAsCommand(command, false);
            return string.IsNullOrEmpty(result) ? "(ok): command completed" : result;
        }

        static bool ExistingScriptingEnabled()
        {
            string path = Path.Combine(Paths.ConfigPath, "dk.elinplugins.customdialogloader.cfg");
            return File.Exists(path) && Regex.IsMatch(File.ReadAllText(path), @"(?m)^\s*Scripting\.AllowScripting\s*=\s*true\s*$", RegexOptions.IgnoreCase);
        }

        void OnApplicationQuit() { Stop(); }
        void OnDestroy() { Stop(); }
        void Stop()
        {
            if (instance != this || stopping) return;
            stopping = true;
            harmony?.UnpatchSelf();
            Logger.LogInfo("Test-only pump stopped; only harness Harmony owner removed.");
            instance = null;
        }
    }
}
