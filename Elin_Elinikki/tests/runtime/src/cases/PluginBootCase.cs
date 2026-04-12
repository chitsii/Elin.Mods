using System.Collections.Generic;
using UnityEngine;

// Verifies Elinikki plugin boot created its manager singletons.
public sealed class PluginBootCase : RuntimeCaseBase
{
    public override string Id => "elinikki.plugin.boot.managers_available";

    public override IReadOnlyList<string> Tags => new[] { "smoke" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var pluginType = ElinikkiRuntimeReflection.RequireType("Elin_Elinikki.Plugin");
        var fpsManager = ElinikkiRuntimeReflection.RequireSceneObject("Elin_Elinikki.FpsViewManager");
        var sharedManager = ElinikkiRuntimeReflection.RequireSceneObject("Elin_Elinikki.SharedWorldObjectManager");

        ctx.Set("pluginType", pluginType);
        ctx.Set("fpsManager", fpsManager);
        ctx.Set("sharedManager", sharedManager);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var pluginType = ctx.Get<System.Type>("pluginType");
        var settings = ElinikkiRuntimeReflection.RequireStaticField(pluginType, "Settings");
        RuntimeAssertions.Require(settings != null, "Plugin.Settings is null.");

        var fpsManager = ctx.Get<UnityEngine.Object>("fpsManager");
        var sharedManager = ctx.Get<UnityEngine.Object>("sharedManager");

        ctx.Set("settings", settings);
        ctx.Set("fpsManagerName", fpsManager.name);
        ctx.Set("sharedManagerName", sharedManager.name);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(
            string.Equals(ctx.Get<string>("fpsManagerName"), "ElinikkiManager", System.StringComparison.Ordinal),
            "Unexpected FpsViewManager object name.");
        RuntimeAssertions.Require(
            string.Equals(ctx.Get<string>("sharedManagerName"), "ElinikkiSharedWorldManager", System.StringComparison.Ordinal),
            "Unexpected SharedWorldObjectManager object name.");
        ctx.Log("Plugin boot managers verified.");
    }
}
