using System;
using System.Collections.Generic;
using System.Reflection;

// Verifies PointCompat wrapper signature and default argument contract.
public sealed class PointCompatCase : RuntimeCaseBase
{
    public override string Id => "questmod.compat.point_wrapper_default_allow_installed";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "compat" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.pc != null, "EClass.pc is null.");

        Type pointCompatType = ModRuntimeReflection.RequireType("Elin_QuestMod.Compat.PointCompat");
        MethodInfo method = FindGetNearestPointCompat(pointCompatType);
        RuntimeAssertions.Require(method != null, "GetNearestPointCompat method not found.");

        ctx.Set("pointCompatMethod", method);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        MethodInfo method = ctx.Get<MethodInfo>("pointCompatMethod");
        ParameterInfo[] parameters = method.GetParameters();
        RuntimeAssertions.Require(parameters.Length == 5, "Unexpected GetNearestPointCompat parameter count.");

        ParameterInfo allowInstalledParam = parameters[3];
        bool hasDefault = allowInstalledParam.HasDefaultValue;
        bool defaultValue = false;
        if (hasDefault && allowInstalledParam.DefaultValue is bool b)
        {
            defaultValue = b;
        }

        object[] args = new object[] { EClass.pc.pos, false, false, true, false };
        object result = method.Invoke(null, args);

        ctx.Set("hasDefault", hasDefault);
        ctx.Set("defaultValue", defaultValue);
        ctx.Set("resultTypeName", result != null ? result.GetType().FullName : string.Empty);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(ctx.Get<bool>("hasDefault"), "allowInstalled does not expose a default value.");
        RuntimeAssertions.Require(ctx.Get<bool>("defaultValue"), "allowInstalled default value is not true.");

        string resultTypeName = ctx.Get<string>("resultTypeName");
        RuntimeAssertions.Require(
            string.Equals(resultTypeName, typeof(Point).FullName, StringComparison.Ordinal),
            "GetNearestPointCompat returned unexpected type: " + resultTypeName);

        ctx.Log("PointCompat contract verified.");
    }

    private static MethodInfo FindGetNearestPointCompat(Type pointCompatType)
    {
        if (pointCompatType == null)
        {
            return null;
        }

        var methods = pointCompatType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        for (int i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            if (method == null)
            {
                continue;
            }

            if (!string.Equals(method.Name, "GetNearestPointCompat", StringComparison.Ordinal))
            {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 5)
            {
                continue;
            }

            if (parameters[0].ParameterType == typeof(Point))
            {
                return method;
            }
        }

        return null;
    }
}
