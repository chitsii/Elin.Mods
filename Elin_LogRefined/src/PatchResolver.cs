using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Elin_LogRefined
{
    public static class PatchResolver
    {
        public static MethodBase FindDeclaredMethod(Type type, string name, Func<MethodInfo, bool> predicate, string patchName)
        {
            return FindDeclaredMethod(type, name, predicate, null, patchName);
        }

        public static MethodBase FindDeclaredMethod(
            Type type,
            string name,
            Func<MethodInfo, bool> strictPredicate,
            Func<MethodInfo, bool> fallbackPredicate,
            string patchName)
        {
            try
            {
                var methods = AccessTools.GetDeclaredMethods(type)
                    .Where(m => m.Name == name)
                    .OfType<MethodInfo>()
                    .OrderByDescending(m => m.GetParameters().Length)
                    .ToArray();

                foreach (var method in methods)
                {
                    if (!strictPredicate(method))
                        continue;

                    Debug.Log($"[LogRefined] {patchName}: strict resolved {DescribeMethod(method)}");
                    return method;
                }

                if (fallbackPredicate != null)
                {
                    foreach (var method in methods)
                    {
                        if (!fallbackPredicate(method))
                            continue;

                        Debug.LogWarning($"[LogRefined] {patchName}: strict target not found, fallback resolved {DescribeMethod(method)}");
                        return method;
                    }
                }

                Debug.LogError($"[LogRefined] {patchName}: target not found (strict/fallback)");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LogRefined] {patchName}: resolver failed ({ex.Message})");
                return null;
            }
        }

        public static string DescribeMethod(MethodBase method)
        {
            if (method == null)
                return "null";

            var ps = method.GetParameters();
            var sig = string.Join(", ", ps.Select(p => $"{p.ParameterType.Name} {p.Name}"));
            var returnType = method is MethodInfo mi ? mi.ReturnType.Name : "void";
            return $"{method.DeclaringType?.Name}.{method.Name}({sig}) -> {returnType}";
        }
    }
}
