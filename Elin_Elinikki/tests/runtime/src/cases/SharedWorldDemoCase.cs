using System.Collections.Generic;
using UnityEngine;

// Verifies shared-world demo objects are generated and parented under normal/preview roots.
public sealed class SharedWorldDemoCase : RuntimeCaseBase
{
    public override string Id => "elinikki.shared_world.demo_spawned";

    public override IReadOnlyList<string> Tags => new[] { "smoke" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var pluginType = ElinikkiRuntimeReflection.RequireType("Elin_Elinikki.Plugin");
        var settings = ElinikkiRuntimeReflection.RequireStaticField(pluginType, "Settings");
        RuntimeAssertions.Require(settings != null, "Plugin.Settings is null.");

        bool previous = ElinikkiRuntimeReflection.GetConfigBool(settings, "EnableDreamTestSet");
        ctx.RegisterRollback("EnableDreamTestSet", () =>
        {
            ElinikkiRuntimeReflection.SetConfigBool(settings, "EnableDreamTestSet", previous);
        });
        ElinikkiRuntimeReflection.SetConfigBool(settings, "EnableDreamTestSet", true);

        var sharedManager = ElinikkiRuntimeReflection.RequireSceneObject("Elin_Elinikki.SharedWorldObjectManager");
        var fpsManager = ElinikkiRuntimeReflection.RequireSceneObject("Elin_Elinikki.FpsViewManager");

        ctx.Set("sharedManager", sharedManager);
        ctx.Set("fpsManagerType", fpsManager.GetType());
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var sharedManager = ctx.Get<UnityEngine.Object>("sharedManager");
        ElinikkiRuntimeReflection.InvokeInstance(sharedManager, "Update");

        var definitions = ElinikkiRuntimeReflection.RequireInstanceField(sharedManager, "_definitions");
        var handles = ElinikkiRuntimeReflection.RequireInstanceField(sharedManager, "_handles");
        var normalRoot = ElinikkiRuntimeReflection.RequireInstanceField(sharedManager, "_normalRoot") as GameObject;
        var previewRoot = ElinikkiRuntimeReflection.RequireInstanceField(sharedManager, "_previewRoot") as GameObject;

        RuntimeAssertions.Require(normalRoot != null, "_normalRoot is null.");
        RuntimeAssertions.Require(previewRoot != null, "_previewRoot is null.");

        var fpsManagerType = ctx.Get<System.Type>("fpsManagerType");
        var sharedPreviewRootProp = fpsManagerType.GetProperty(
            "SharedWorldPreviewRoot",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        RuntimeAssertions.Require(sharedPreviewRootProp != null, "SharedWorldPreviewRoot property missing.");
        var previewParent = sharedPreviewRootProp.GetValue(null, null) as Transform;

        ctx.Set("definitionCount", ElinikkiRuntimeReflection.GetCount(definitions));
        ctx.Set("handleCount", ElinikkiRuntimeReflection.GetCount(handles));
        ctx.Set("normalRoot", normalRoot);
        ctx.Set("previewRoot", previewRoot);
        ctx.Set("previewParent", previewParent);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var normalRoot = ctx.Get<GameObject>("normalRoot");
        var previewRoot = ctx.Get<GameObject>("previewRoot");
        var previewParent = ctx.Get<Transform>("previewParent");

        RuntimeAssertions.Require(ctx.Get<int>("definitionCount") >= 4, "Expected dream demo definitions to be registered.");
        RuntimeAssertions.Require(ctx.Get<int>("handleCount") >= 4, "Expected dream demo handles to be created.");
        RuntimeAssertions.Require(normalRoot.transform.parent == EClass.scene.transform, "Normal root parent mismatch.");
        RuntimeAssertions.Require(previewParent != null, "SharedWorldPreviewRoot is null.");
        RuntimeAssertions.Require(previewRoot.transform.parent == previewParent, "Preview root parent mismatch.");
        RuntimeAssertions.Require(normalRoot.transform.childCount >= 4, "Normal root has too few children.");
        RuntimeAssertions.Require(previewRoot.transform.childCount >= 4, "Preview root has too few children.");
        ctx.Log("Shared-world demo objects verified.");
    }
}
