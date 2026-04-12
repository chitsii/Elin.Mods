using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Verifies shared-world roots reattach correctly after reloading the current save.
public sealed class SharedWorldReloadCase : RuntimeCaseBase, IRuntimeCoroutineCase
{
    public override string Id => "elinikki.shared_world.reload_reattaches_roots";

    public override IReadOnlyList<string> Tags => new[] { "smoke" };

    public override void Prepare(RuntimeTestContext ctx)
    {
    }

    public IEnumerator PrepareAsync(RuntimeTestContext ctx)
    {
        var pluginType = ElinikkiRuntimeReflection.RequireType("Elin_Elinikki.Plugin");
        var settings = ElinikkiRuntimeReflection.RequireStaticField(pluginType, "Settings");
        RuntimeAssertions.Require(settings != null, "Plugin.Settings is null.");

        bool previousDream = ElinikkiRuntimeReflection.GetConfigBool(settings, "EnableDreamTestSet");
        ctx.RegisterRollback("EnableDreamTestSet", () =>
        {
            ElinikkiRuntimeReflection.SetConfigBool(settings, "EnableDreamTestSet", previousDream);
        });
        ElinikkiRuntimeReflection.SetConfigBool(settings, "EnableDreamTestSet", true);

        var fpsManager = ElinikkiRuntimeReflection.RequireSceneObject("Elin_Elinikki.FpsViewManager");
        var sharedManager = ElinikkiRuntimeReflection.RequireSceneObject("Elin_Elinikki.SharedWorldObjectManager");

        var overlay = ElinikkiRuntimeReflection.RequireInstanceField(fpsManager, "_overlay");
        bool overlayVisibleBefore = (bool)ElinikkiRuntimeReflection.RequireProperty(overlay, "IsVisible");
        ctx.RegisterRollback("overlayVisibility", () =>
        {
            bool current = (bool)ElinikkiRuntimeReflection.RequireProperty(overlay, "IsVisible");
            if (overlayVisibleBefore && !current)
            {
                ElinikkiRuntimeReflection.InvokeInstance(overlay, "Show");
            }
            else if (!overlayVisibleBefore && current)
            {
                ElinikkiRuntimeReflection.InvokeInstance(overlay, "Hide");
            }
        });

        if (!overlayVisibleBefore)
        {
            ElinikkiRuntimeReflection.InvokeInstance(overlay, "Show");
        }

        ElinikkiRuntimeReflection.InvokeInstance(sharedManager, "Update");
        ElinikkiRuntimeReflection.SetInstanceField(fpsManager, "_nextRenderTime", 0f);
        ElinikkiRuntimeReflection.InvokeInstance(fpsManager, "Update");
        yield return ctx.WaitFrames(5);

        ctx.Set("fpsManager", fpsManager);
        ctx.Set("sharedManager", sharedManager);
        ctx.Set("baselineSaveId", Game.id ?? string.Empty);
        ctx.Set("baselineSaveCloud", EClass.game != null && EClass.game.isCloud);
        ctx.Set("baselineMap", EClass._map);
        ctx.Set("baselineScene", EClass.scene);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
    }

    public IEnumerator ExecuteAsync(RuntimeTestContext ctx)
    {
        string baselineSaveId = ctx.Get<string>("baselineSaveId");
        bool baselineSaveCloud = ctx.Get<bool>("baselineSaveCloud");
        RuntimeAssertions.Require(!string.IsNullOrEmpty(baselineSaveId), "Game.id is empty.");

        Game.Load(baselineSaveId, baselineSaveCloud);
        yield return ctx.WaitUntil(
            () => EClass.pc != null && EClass._zone != null && EClass.game != null && EClass.scene != null && EClass._map != null,
            900,
            "Reload timed out waiting for pc/zone/scene.");
        yield return ctx.WaitFrames(20);

        var fpsManager = ElinikkiRuntimeReflection.RequireSceneObject("Elin_Elinikki.FpsViewManager");
        var sharedManager = ElinikkiRuntimeReflection.RequireSceneObject("Elin_Elinikki.SharedWorldObjectManager");
        ElinikkiRuntimeReflection.InvokeInstance(sharedManager, "Update");
        ElinikkiRuntimeReflection.SetInstanceField(fpsManager, "_nextRenderTime", 0f);
        ElinikkiRuntimeReflection.InvokeInstance(fpsManager, "Update");
        yield return ctx.WaitFrames(10);

        ctx.Set("fpsManagerAfter", fpsManager);
        ctx.Set("sharedManagerAfter", sharedManager);
        ctx.Set("sceneAfter", EClass.scene);
        ctx.Set("mapAfter", EClass._map);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
    }

    public IEnumerator VerifyAsync(RuntimeTestContext ctx)
    {
        var sharedManager = ctx.Get<UnityEngine.Object>("sharedManagerAfter");
        var normalRoot = ElinikkiRuntimeReflection.RequireInstanceField(sharedManager, "_normalRoot") as GameObject;
        var previewRoot = ElinikkiRuntimeReflection.RequireInstanceField(sharedManager, "_previewRoot") as GameObject;
        var handles = ElinikkiRuntimeReflection.RequireInstanceField(sharedManager, "_handles");

        RuntimeAssertions.Require(normalRoot != null, "_normalRoot missing after reload.");
        RuntimeAssertions.Require(previewRoot != null, "_previewRoot missing after reload.");
        RuntimeAssertions.Require(normalRoot.transform.parent == EClass.scene.transform, "Normal root did not reattach to current scene.");

        var fpsManager = ctx.Get<UnityEngine.Object>("fpsManagerAfter");
        var gpuRenderer = ElinikkiRuntimeReflection.RequireInstanceField(fpsManager, "_gpuPreviewRenderer");
        var sharedPreviewRoot = ElinikkiRuntimeReflection.RequireProperty(gpuRenderer, "SharedWorldRoot") as Transform;
        RuntimeAssertions.Require(sharedPreviewRoot != null, "SharedWorldRoot missing after reload.");
        RuntimeAssertions.Require(previewRoot.transform.parent == sharedPreviewRoot, "Preview root did not reattach to GPU shared root.");
        RuntimeAssertions.Require(ElinikkiRuntimeReflection.GetCount(handles) >= 4, "Shared handles missing after reload.");
        RuntimeAssertions.Require(normalRoot.transform.childCount >= 4, "Normal root children missing after reload.");
        RuntimeAssertions.Require(previewRoot.transform.childCount >= 4, "Preview root children missing after reload.");

        if (!ReferenceEquals(ctx.Get<object>("baselineMap"), ctx.Get<object>("mapAfter")))
        {
            ctx.Log("Reload produced a new map reference.");
        }

        if (!ReferenceEquals(ctx.Get<object>("baselineScene"), ctx.Get<object>("sceneAfter")))
        {
            ctx.Log("Reload produced a new scene reference.");
        }

        ctx.Log("Shared-world reload reattachment verified.");
        yield break;
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }

    public IEnumerator CleanupAsync(RuntimeTestContext ctx)
    {
        yield break;
    }
}
