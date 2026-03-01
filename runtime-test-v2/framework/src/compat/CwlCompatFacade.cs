using System;
using System.Reflection;
using HarmonyLib;

public static class CwlCompatFacade
{
    public static bool TryTestIncompatibleIl(MethodBase method, out bool incompatible, out string reason)
    {
        incompatible = false;
        reason = string.Empty;

        if (method == null)
        {
            reason = "method_null";
            return false;
        }

        try
        {
            var detailType = AccessTools.TypeByName("Cwl.Helper.String.MethodInfoDetail");
            if (detailType == null)
            {
                reason = "method_info_detail_not_found";
                return false;
            }

            var testMethod = detailType.GetMethod(
                "TestIncompatibleIl",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(MethodBase) },
                null);
            if (testMethod == null)
            {
                reason = "test_method_not_found";
                return false;
            }

            var raw = testMethod.Invoke(null, new object[] { method });
            if (!(raw is bool result))
            {
                reason = "unexpected_return_type";
                return false;
            }

            incompatible = result;
            return true;
        }
        catch (Exception ex)
        {
            reason = ex.GetType().Name + ": " + ex.Message;
            return false;
        }
    }

    public static void ClearIncompatibleCalls()
    {
        var detailType = AccessTools.TypeByName("Cwl.Helper.String.MethodInfoDetail");
        if (detailType == null)
            return;

        var value = ReflectionCompat.GetStaticFieldOrPropertyValue(detailType, "IncompatibleCalls", "incompatibleCalls");
        var enumerable = ReflectionCompat.AsEnumerable(value);
        if (enumerable == null)
            return;

        var clearMethod = enumerable.GetType().GetMethod(
            "Clear",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        if (clearMethod == null)
            return;

        clearMethod.Invoke(enumerable, null);
    }
}
