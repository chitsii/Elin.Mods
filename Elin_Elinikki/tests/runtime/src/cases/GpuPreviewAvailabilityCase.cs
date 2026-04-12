using System.Collections.Generic;
using UnityEngine;

// Verifies GPU preview renderer and fullscreen overlay remain usable after boot.
public sealed class GpuPreviewAvailabilityCase : RuntimeCaseBase
{
    public override string Id => "elinikki.gpu_preview.render_texture_available";

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

        var fpsManager = ElinikkiRuntimeReflection.RequireSceneObject("Elin_Elinikki.FpsViewManager");
        var sharedManager = ElinikkiRuntimeReflection.RequireSceneObject("Elin_Elinikki.SharedWorldObjectManager");

        ctx.Set("fpsManager", fpsManager);
        ctx.Set("sharedManager", sharedManager);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var fpsManager = ctx.Get<UnityEngine.Object>("fpsManager");
        var sharedManager = ctx.Get<UnityEngine.Object>("sharedManager");

        ElinikkiRuntimeReflection.InvokeInstance(sharedManager, "Update");

        var overlay = ElinikkiRuntimeReflection.RequireInstanceField(fpsManager, "_overlay");
        var overlayVisibleBefore = (bool)ElinikkiRuntimeReflection.RequireProperty(overlay, "IsVisible");
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

        ElinikkiRuntimeReflection.SetInstanceField(fpsManager, "_nextRenderTime", 0f);
        ElinikkiRuntimeReflection.InvokeInstance(fpsManager, "Update");

        var gpuRenderer = ElinikkiRuntimeReflection.RequireInstanceField(fpsManager, "_gpuPreviewRenderer");
        RuntimeAssertions.Require(gpuRenderer != null, "_gpuPreviewRenderer is null.");

        var outputTexture = ElinikkiRuntimeReflection.RequireProperty(gpuRenderer, "OutputTexture") as RenderTexture;
        RuntimeAssertions.Require(outputTexture != null, "OutputTexture is null.");

        var sharedPreviewRoot = ElinikkiRuntimeReflection.RequireProperty(gpuRenderer, "SharedWorldRoot") as Transform;
        RuntimeAssertions.Require(sharedPreviewRoot != null, "SharedWorldRoot is null.");

        ctx.Set("outputTexture", outputTexture);
        ctx.Set("sharedPreviewRoot", sharedPreviewRoot);
        ctx.Set("overlayVisible", (bool)ElinikkiRuntimeReflection.RequireProperty(overlay, "IsVisible"));
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var outputTexture = ctx.Get<RenderTexture>("outputTexture");
        var sharedPreviewRoot = ctx.Get<Transform>("sharedPreviewRoot");

        RuntimeAssertions.Require(ctx.Get<bool>("overlayVisible"), "Overlay did not become visible.");
        RuntimeAssertions.Require(outputTexture.width > 0, "RenderTexture width is invalid.");
        RuntimeAssertions.Require(outputTexture.height > 0, "RenderTexture height is invalid.");
        RuntimeAssertions.Require(outputTexture.IsCreated(), "RenderTexture is not created.");
        RuntimeAssertions.Require(sharedPreviewRoot.childCount > 0, "Shared preview root has no children.");
        ctx.Log("GPU preview availability verified.");
    }
}
