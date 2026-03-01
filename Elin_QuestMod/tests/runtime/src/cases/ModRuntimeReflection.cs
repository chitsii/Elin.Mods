using System;
using System.Reflection;

// Reflection helpers for runtime smoke tests.
public static class ModRuntimeReflection
{
    public const string ModAssemblyName = "Elin_QuestMod";

    public static Assembly RequireModAssembly()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            var asm = assemblies[i];
            if (asm == null)
            {
                continue;
            }

            string name;
            try
            {
                name = asm.GetName().Name ?? string.Empty;
            }
            catch
            {
                continue;
            }

            if (string.Equals(name, ModAssemblyName, StringComparison.Ordinal))
            {
                return asm;
            }
        }

        RuntimeAssertions.Require(false, "Mod assembly is not loaded: " + ModAssemblyName);
        return null;
    }

    public static Type RequireType(string fullName)
    {
        var asm = RequireModAssembly();
        var type = asm.GetType(fullName, false);
        RuntimeAssertions.Require(type != null, "Type not found: " + fullName);
        return type;
    }

    public static MethodInfo RequireStaticMethod(Type ownerType, string methodName, Type[] parameterTypes)
    {
        RuntimeAssertions.Require(ownerType != null, "ownerType is null.");
        RuntimeAssertions.Require(!string.IsNullOrEmpty(methodName), "methodName is empty.");

        var binding = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo method = null;

        if (parameterTypes != null)
        {
            method = ownerType.GetMethod(methodName, binding, null, parameterTypes, null);
        }

        if (method == null)
        {
            var methods = ownerType.GetMethods(binding);
            for (int i = 0; i < methods.Length; i++)
            {
                var candidate = methods[i];
                if (candidate == null)
                {
                    continue;
                }

                if (!string.Equals(candidate.Name, methodName, StringComparison.Ordinal))
                {
                    continue;
                }

                method = candidate;
                break;
            }
        }

        RuntimeAssertions.Require(method != null, "Method not found: " + ownerType.FullName + "." + methodName);
        return method;
    }

    public static object InvokeStatic(MethodInfo method, params object[] args)
    {
        RuntimeAssertions.Require(method != null, "InvokeStatic method is null.");
        return method.Invoke(null, args);
    }
}
