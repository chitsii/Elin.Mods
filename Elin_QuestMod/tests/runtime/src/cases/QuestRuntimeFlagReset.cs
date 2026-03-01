using System;
using System.Collections.Generic;
using System.Reflection;

internal static class QuestRuntimeFlagReset
{
    public static int ClearAllFlagsForDefaultPrefix(Type serviceType)
    {
        RuntimeAssertions.Require(serviceType != null, "serviceType is null.");
        RuntimeAssertions.Require(EClass.player != null, "EClass.player is null.");
        RuntimeAssertions.Require(EClass.player.dialogFlags != null, "dialogFlags is null.");

        string prefix = InvokeString(serviceType, "GetDefaultPrefix", new object[0]);
        RuntimeAssertions.Require(!string.IsNullOrEmpty(prefix), "QuestStateService.GetDefaultPrefix returned empty.");

        string scopedPrefix = prefix + ".";
        var flags = EClass.player.dialogFlags;
        var keysToRemove = new List<string>();

        foreach (var kv in flags)
        {
            string key = kv.Key;
            if (key != null && key.StartsWith(scopedPrefix, StringComparison.Ordinal))
            {
                keysToRemove.Add(key);
            }
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            flags.Remove(keysToRemove[i]);
        }

        return keysToRemove.Count;
    }

    private static string InvokeString(Type type, string methodName, object[] args)
    {
        var argTypes = GetArgumentTypes(args);
        MethodInfo method = ModRuntimeReflection.RequireStaticMethod(type, methodName, argTypes);
        object raw = ModRuntimeReflection.InvokeStatic(method, args);
        RuntimeAssertions.Require(raw is string, "Expected string return from " + methodName);
        return (string)raw;
    }

    private static Type[] GetArgumentTypes(object[] args)
    {
        if (args == null || args.Length == 0)
        {
            return Type.EmptyTypes;
        }

        var types = new Type[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            types[i] = args[i] != null ? args[i].GetType() : typeof(object);
        }

        return types;
    }
}
