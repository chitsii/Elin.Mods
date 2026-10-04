using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

internal static class PatchRegistrationTests
{
    private static int Main(string[] args)
    {
        if (args.Length != 3) throw new ArgumentException("Expected product DLL, managed directory, BepInEx core directory.");
        string[] search = { Path.GetDirectoryName(Path.GetFullPath(args[0])), args[1], args[2] };
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
        {
            string name = new AssemblyName(request.Name).Name + ".dll";
            foreach (string directory in search)
            {
                string path = Path.Combine(directory, name);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        Run(Path.GetFullPath(args[0]));
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(string productPath)
    {
        Assembly product = Assembly.LoadFrom(productPath);
        Type sourceClass = product.GetType("Elin_AutoOfferingAlter.PatchOfferingBoxSourceInit", throwOnError: true);
        Type craftClass = product.GetType("Elin_AutoOfferingAlter.PatchOfferingBoxCraft", throwOnError: true);
        const BindingFlags declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
        // No Unity object, EClass state, SourceManager, recipe, Thing or plugin instance is created.
        foreach (Type type in product.GetTypes())
        {
            if (!type.IsDefined(typeof(HarmonyPatch), false)) continue;
            bool bulk = type.GetMethods(declared).Any(method => method.Name == "TargetMethod" || method.Name == "TargetMethods"
                || method.IsDefined(typeof(HarmonyTargetMethod), false) || method.IsDefined(typeof(HarmonyTargetMethods), false))
                || type.IsDefined(typeof(HarmonyPatchAll), false);
            if (!bulk) continue;
            Require(!type.GetMethods(declared).Any(method => method.IsDefined(typeof(HarmonyPatch), false)),
                type.FullName + " mixes bulk targeting with individual annotations.");
            foreach (HarmonyPatch patch in type.GetCustomAttributes(typeof(HarmonyPatch), false))
                Require(patch.info.methodName == null, type.FullName + " mixes TargetMethod with a class method annotation.");
        }
        Console.WriteLine("PASS: every compiled product patch class avoids bulk/individual annotation mixing");

        MethodBase init = (MethodBase)sourceClass.GetMethod("TargetMethod").Invoke(null, null);
        Require(init.DeclaringType == typeof(SourceManager) && init.Name == "Init" && !init.IsGenericMethod
            && init.GetParameters().Length == 0, "Source patch must target declared SourceManager.Init().");
        Type[] parameters = { typeof(BlessedState), typeof(bool), typeof(List<Thing>), typeof(TraitCrafter), typeof(bool) };
        MethodInfo craft = AccessTools.DeclaredMethod(typeof(RecipeCard), "Craft", parameters);
        MethodInfo baseCraft = AccessTools.DeclaredMethod(typeof(Recipe), "Craft", parameters);
        Require(craft != null && craft.ReturnType == typeof(Thing) && craft.IsVirtual && !craft.IsGenericMethod
            && craft.GetBaseDefinition() == baseCraft, "Actual RecipeCard.Craft signature/override changed.");
        HarmonyPatch craftAttribute = (HarmonyPatch)craftClass.GetCustomAttributes(typeof(HarmonyPatch), false).Single();
        Require(craftAttribute.info.declaringType == typeof(RecipeCard) && craftAttribute.info.methodName == "Craft"
            && craftAttribute.info.argumentTypes.SequenceEqual(parameters), "Craft annotation must select the exact native override.");
        MethodInfo postfix = craftClass.GetMethod("Postfix_Craft");
        Require(postfix.IsDefined(typeof(HarmonyPostfix), false)
            && postfix.GetParameters()[0].ParameterType == typeof(RecipeCard)
            && postfix.GetParameters()[1].ParameterType == typeof(Thing).MakeByRefType(), "Craft postfix signature mismatch.");
        Console.WriteLine("PASS: real DLL target signatures and compiled Craft override annotation");

        string owner = "offline.pr8.patch_registration." + Guid.NewGuid().ToString("N");
        var harmony = new Harmony(owner);
        try
        {
            // Prove this test rejects the exact registration failure seen in the Player.log.
            bool rejected = false;
            try { harmony.CreateClassProcessor(typeof(InvalidMixedPatch)).Patch(); }
            catch (Exception ex)
            {
                rejected = ex.ToString().Contains("You cannot combine TargetMethod");
                if (!rejected) throw;
            }
            Require(rejected, "Known invalid mixed targeting unexpectedly passed Harmony processing.");
            Console.WriteLine("PASS: Harmony rejects the known mixed TargetMethod/Craft regression");

            // Same assembly-wide registration entrypoint as Plugin.Awake, without constructing the plugin.
            harmony.PatchAll(product);
            Require(Harmony.GetPatchInfo(init).Prefixes.Count(patch => patch.owner == owner) == 1,
                "Source Init prefix was not registered exactly once.");
            Require(Harmony.GetPatchInfo(craft).Postfixes.Count(patch => patch.owner == owner && patch.PatchMethod == postfix) == 1,
                "RecipeCard.Craft postfix was not registered exactly once.");
            var basePatches = Harmony.GetPatchInfo(baseCraft);
            Require(basePatches == null || !basePatches.Owners.Contains(owner), "Test owner unexpectedly patched base Recipe.Craft.");
            Console.WriteLine("PASS: actual Harmony.PatchAll registers the product assembly and both native targets; original bodies never invoked");
        }
        finally
        {
            harmony.UnpatchSelf();
            foreach (MethodBase method in Harmony.GetAllPatchedMethods())
                Require(!Harmony.GetPatchInfo(method).Owners.Contains(owner), "Offline Harmony owner leaked.");
        }
        Console.WriteLine("PASS: offline patch cleanup; game/coldboot/craft execution remains unverified");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static class OfflineTarget
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Init() { }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Craft() { }
    }
    [HarmonyPatch]
    private static class InvalidMixedPatch
    {
        private static MethodBase TargetMethod() { return AccessTools.DeclaredMethod(typeof(OfflineTarget), "Init"); }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OfflineTarget), "Craft")]
        private static void Postfix() { }
    }
}
