using System;
using System.Collections;
using System.Reflection;

public static class ReflectionCompat
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static object GetFieldOrPropertyValue(object instance, params string[] names)
    {
        if (instance == null || names == null)
            return null;

        var type = instance.GetType();
        for (int i = 0; i < names.Length; i++)
        {
            var name = names[i];
            if (string.IsNullOrEmpty(name))
                continue;

            var prop = type.GetProperty(name, AnyInstance);
            if (prop != null && prop.GetIndexParameters().Length == 0)
                return prop.GetValue(instance, null);

            var field = type.GetField(name, AnyInstance);
            if (field != null)
                return field.GetValue(instance);
        }

        return null;
    }

    public static object GetStaticFieldOrPropertyValue(Type type, params string[] names)
    {
        if (type == null || names == null)
            return null;

        for (int i = 0; i < names.Length; i++)
        {
            var name = names[i];
            if (string.IsNullOrEmpty(name))
                continue;

            var prop = type.GetProperty(name, AnyStatic);
            if (prop != null && prop.GetIndexParameters().Length == 0)
                return prop.GetValue(null, null);

            var field = type.GetField(name, AnyStatic);
            if (field != null)
                return field.GetValue(null);
        }

        return null;
    }

    public static IEnumerable AsEnumerable(object value)
    {
        if (value == null || value is string)
            return null;

        if (value is IEnumerable enumerable)
            return enumerable;

        return null;
    }
}
