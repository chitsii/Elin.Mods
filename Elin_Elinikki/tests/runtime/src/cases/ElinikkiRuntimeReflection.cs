using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

// Reflection helpers for Elin_Elinikki runtime smoke tests.
public static class ElinikkiRuntimeReflection
{
    public const string ModAssemblyName = "Elin_Elinikki";

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

    public static UnityEngine.Object RequireSceneObject(string fullName)
    {
        var type = RequireType(fullName);
        var obj = UnityEngine.Object.FindObjectOfType(type);
        RuntimeAssertions.Require(obj != null, "Scene object not found: " + fullName);
        return obj;
    }

    public static object RequireStaticField(Type ownerType, string fieldName)
    {
        RuntimeAssertions.Require(ownerType != null, "ownerType is null.");
        var field = ownerType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        RuntimeAssertions.Require(field != null, "Static field not found: " + ownerType.FullName + "." + fieldName);
        return field.GetValue(null);
    }

    public static object RequireInstanceField(object owner, string fieldName)
    {
        RuntimeAssertions.Require(owner != null, "owner is null.");
        var field = owner.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        RuntimeAssertions.Require(field != null, "Instance field not found: " + owner.GetType().FullName + "." + fieldName);
        return field.GetValue(owner);
    }

    public static void SetInstanceField(object owner, string fieldName, object value)
    {
        RuntimeAssertions.Require(owner != null, "owner is null.");
        var field = owner.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        RuntimeAssertions.Require(field != null, "Instance field not found: " + owner.GetType().FullName + "." + fieldName);
        field.SetValue(owner, value);
    }

    public static object RequireProperty(object owner, string propertyName)
    {
        RuntimeAssertions.Require(owner != null, "owner is null.");
        var property = owner.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        RuntimeAssertions.Require(property != null, "Property not found: " + owner.GetType().FullName + "." + propertyName);
        return property.GetValue(owner, null);
    }

    public static object InvokeInstance(object owner, string methodName, params object[] args)
    {
        RuntimeAssertions.Require(owner != null, "owner is null.");
        var method = owner.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        RuntimeAssertions.Require(method != null, "Instance method not found: " + owner.GetType().FullName + "." + methodName);
        return method.Invoke(owner, args);
    }

    public static object InvokeStatic(Type ownerType, string methodName, params object[] args)
    {
        RuntimeAssertions.Require(ownerType != null, "ownerType is null.");
        var method = ownerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        RuntimeAssertions.Require(method != null, "Static method not found: " + ownerType.FullName + "." + methodName);
        return method.Invoke(null, args);
    }

    public static int GetCount(object dictionaryOrCollection)
    {
        RuntimeAssertions.Require(dictionaryOrCollection != null, "collection is null.");

        if (dictionaryOrCollection is ICollection collection)
        {
            return collection.Count;
        }

        var countProperty = dictionaryOrCollection.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        RuntimeAssertions.Require(countProperty != null, "Count property not found on: " + dictionaryOrCollection.GetType().FullName);
        return (int)countProperty.GetValue(dictionaryOrCollection, null);
    }

    public static bool GetConfigBool(object settings, string propertyName)
    {
        RuntimeAssertions.Require(settings != null, "settings is null.");
        var configEntry = RequireProperty(settings, propertyName);
        RuntimeAssertions.Require(configEntry != null, "Config entry is null: " + propertyName);
        return (bool)RequireProperty(configEntry, "Value");
    }

    public static void SetConfigBool(object settings, string propertyName, bool value)
    {
        RuntimeAssertions.Require(settings != null, "settings is null.");
        var configEntry = RequireProperty(settings, propertyName);
        RuntimeAssertions.Require(configEntry != null, "Config entry is null: " + propertyName);
        var prop = configEntry.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        RuntimeAssertions.Require(prop != null, "Config entry Value property missing: " + propertyName);
        prop.SetValue(configEntry, value, null);
    }
}
