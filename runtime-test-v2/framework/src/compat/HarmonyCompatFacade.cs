using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

// Runtime Test V2 Harmony compatibility facade.
public static class HarmonyCompatFacade
{
    public static object GetPatchInfo(MethodBase method)
    {
        if (method == null)
            return null;
        return Harmony.GetPatchInfo(method);
    }

    public static bool HasPatch(object patchInfo, string bucket, string declaringTypeFullName, string methodName)
    {
        if (patchInfo == null)
            return false;

        var patchList = GetPatchBucket(patchInfo, bucket);
        var enumerable = ReflectionCompat.AsEnumerable(patchList);
        if (enumerable == null)
            return false;

        foreach (var patch in enumerable)
        {
            var patchMethod = GetPatchMethod(patch);
            if (patchMethod == null)
                continue;
            if (!string.Equals(patchMethod.Name, methodName, StringComparison.Ordinal))
                continue;

            var type = patchMethod.DeclaringType;
            if (type == null)
                continue;

            if (string.Equals(type.FullName, declaringTypeFullName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static object GetPatchBucket(object patchInfo, string bucket)
    {
        if (patchInfo == null || string.IsNullOrEmpty(bucket))
            return null;

        if (bucket.IndexOf("pre", StringComparison.OrdinalIgnoreCase) >= 0)
            return ReflectionCompat.GetFieldOrPropertyValue(patchInfo, "Prefixes", "prefixes", "prefix");
        if (bucket.IndexOf("post", StringComparison.OrdinalIgnoreCase) >= 0)
            return ReflectionCompat.GetFieldOrPropertyValue(patchInfo, "Postfixes", "postfixes", "postfix");
        if (bucket.IndexOf("trans", StringComparison.OrdinalIgnoreCase) >= 0)
            return ReflectionCompat.GetFieldOrPropertyValue(patchInfo, "Transpilers", "transpilers", "transpiler");

        return null;
    }

    private static MethodInfo GetPatchMethod(object patch)
    {
        if (patch == null)
            return null;

        if (patch is MethodInfo direct)
            return direct;

        var method = ReflectionCompat.GetFieldOrPropertyValue(patch, "PatchMethod", "patchMethod", "method");
        return method as MethodInfo;
    }
}
