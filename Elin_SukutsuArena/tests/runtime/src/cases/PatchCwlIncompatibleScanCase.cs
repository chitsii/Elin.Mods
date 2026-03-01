using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

// Active CWL compatibility scan.
// This case explicitly invokes CWL TestIncompatibleIl and should be run with -CaseId.
public sealed class ZzzPatchCwlIncompatibleScanCase : RuntimeCaseBase
{
    private const string PatchTypePrefix = "Elin_SukutsuArena";

    public override string Id => "patch.compat.cwl_incompatible_scan";

    public override IReadOnlyList<string> Tags => new[] { "compat", "active" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        CwlCompatFacade.ClearIncompatibleCalls();

        var patchMethods = CollectSukutsuPatchMethods();
        RuntimeAssertions.Require(patchMethods.Count > 0, "No Sukutsu Harmony patch methods were discovered.");
        ctx.Set("patchMethods", patchMethods);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var patchMethods = ctx.Get<List<MethodInfo>>("patchMethods");
        var incompatible = new List<string>();
        var unavailable = new List<string>();
        int checkedCount = 0;

        for (int i = 0; i < patchMethods.Count; i++)
        {
            var patchMethod = patchMethods[i];
            if (patchMethod == null)
                continue;

            if (!CwlCompatFacade.TryTestIncompatibleIl(patchMethod, out bool isIncompatible, out string reason))
            {
                unavailable.Add(FormatMethod(patchMethod) + " => " + reason);
                continue;
            }

            checkedCount++;
            if (isIncompatible)
                incompatible.Add(FormatMethod(patchMethod));
        }

        ctx.Set("checkedCount", checkedCount);
        ctx.Set("incompatible", incompatible);
        ctx.Set("unavailable", unavailable);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        int checkedCount = ctx.Get<int>("checkedCount");
        var incompatible = ctx.Get<List<string>>("incompatible");
        var unavailable = ctx.Get<List<string>>("unavailable");
        var patchMethods = ctx.Get<List<MethodInfo>>("patchMethods");

        RuntimeAssertions.Require(
            unavailable.Count == 0,
            "CWL TestIncompatibleIl unavailable: " + Summarize(unavailable));
        RuntimeAssertions.Require(
            checkedCount > 0,
            "No patch methods were checked by CWL TestIncompatibleIl.");
        RuntimeAssertions.Require(
            incompatible.Count == 0,
            "CWL reported INCOMPATIBLE patch methods: " + Summarize(incompatible));

        ctx.Log(
            "CWL active incompatible scan passed: discovered="
            + patchMethods.Count
            + ", checked="
            + checkedCount);
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }

    private static List<MethodInfo> CollectSukutsuPatchMethods()
    {
        var patchMethods = new List<MethodInfo>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        var patchedMethods = Harmony.GetAllPatchedMethods();
        if (patchedMethods == null)
            return patchMethods;

        foreach (var target in patchedMethods)
        {
            if (target == null)
                continue;

            var patchInfo = HarmonyCompatFacade.GetPatchInfo(target);
            if (patchInfo == null)
                continue;

            CollectFromBucket(patchInfo, "prefix", patchMethods, seenKeys);
            CollectFromBucket(patchInfo, "postfix", patchMethods, seenKeys);
            CollectFromBucket(patchInfo, "transpiler", patchMethods, seenKeys);
            CollectFromBucket(patchInfo, "finalizer", patchMethods, seenKeys);
        }

        return patchMethods;
    }

    private static void CollectFromBucket(
        object patchInfo,
        string bucket,
        List<MethodInfo> output,
        HashSet<string> seenKeys)
    {
        var patchList = GetPatchBucket(patchInfo, bucket);
        var enumerable = ReflectionCompat.AsEnumerable(patchList);
        if (enumerable == null)
            return;

        foreach (var patch in enumerable)
        {
            var patchMethod = GetPatchMethod(patch);
            if (patchMethod == null)
                continue;

            var declaringType = patchMethod.DeclaringType;
            if (declaringType == null)
                continue;

            string fullName = declaringType.FullName ?? string.Empty;
            if (!fullName.StartsWith(PatchTypePrefix, StringComparison.Ordinal))
                continue;

            string key = GetMethodKey(patchMethod);
            if (!seenKeys.Add(key))
                continue;

            output.Add(patchMethod);
        }
    }

    private static object GetPatchBucket(object patchInfo, string bucket)
    {
        if (patchInfo == null || string.IsNullOrEmpty(bucket))
            return null;

        var known = HarmonyCompatFacade.GetPatchBucket(patchInfo, bucket);
        if (known != null)
            return known;

        if (bucket.IndexOf("final", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return ReflectionCompat.GetFieldOrPropertyValue(
                patchInfo,
                "Finalizers",
                "finalizers",
                "finalizer");
        }

        return null;
    }

    private static MethodInfo GetPatchMethod(object patch)
    {
        if (patch == null)
            return null;

        if (patch is MethodInfo direct)
            return direct;

        var methodObj = ReflectionCompat.GetFieldOrPropertyValue(patch, "PatchMethod", "patchMethod", "method");
        return methodObj as MethodInfo;
    }

    private static string GetMethodKey(MethodBase method)
    {
        if (method == null)
            return string.Empty;

        try
        {
            return method.Module.Name + ":" + method.MetadataToken;
        }
        catch
        {
            return FormatMethod(method);
        }
    }

    private static string FormatMethod(MethodBase method)
    {
        if (method == null)
            return "(null)";

        string typeName = method.DeclaringType != null
            ? (method.DeclaringType.FullName ?? method.DeclaringType.Name)
            : "(global)";

        var parameters = method.GetParameters();
        var parts = new string[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var pType = parameters[i].ParameterType;
            parts[i] = pType != null ? (pType.FullName ?? pType.Name) : "?";
        }

        return typeName + "." + method.Name + "(" + string.Join(",", parts) + ")";
    }

    private static string Summarize(IReadOnlyList<string> items, int maxItems = 6)
    {
        if (items == null || items.Count == 0)
            return "(none)";

        int take = items.Count < maxItems ? items.Count : maxItems;
        var shown = new string[take];
        for (int i = 0; i < take; i++)
            shown[i] = items[i];

        string suffix = items.Count > take ? " (+" + (items.Count - take) + " more)" : string.Empty;
        return string.Join("; ", shown) + suffix;
    }
}
