using System;
using System.Collections.Generic;
using System.Reflection;

// Verifies DEBUG-only showcase command and cs.eval shortcut path.
public sealed class DebugShowcaseConsoleCase : RuntimeCaseBase
{
    public override string Id => "questmod.debug.console.showcase_command";

    public override IReadOnlyList<string> Tags => new[] { "debug", "drama" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.pc != null, "EClass.pc is null.");
        RuntimeAssertions.Require(EClass.ui != null, "EClass.ui is null.");
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        Type consoleType = ModRuntimeReflection.RequireType("Elin_QuestMod.DebugTools.QuestModDebugConsole");
        InvokeStatic(consoleType, "Register", Type.EmptyTypes, new object[0]);

        string startResult = InvokeString(consoleType, "StartShowcase");
        bool startActivated = LayerDrama.Instance != null;
        SafeCloseDramaLayers();

        string evalResult = InvokeString(consoleType, "EvalShowcase");
        bool evalActivated = LayerDrama.Instance != null;

        ctx.Set("startResult", startResult);
        ctx.Set("startActivated", startActivated);
        ctx.Set("evalResult", evalResult);
        ctx.Set("evalActivated", evalActivated);

        ctx.RegisterRollback("close_drama_layer", SafeCloseDramaLayers);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        string startResult = ctx.Get<string>("startResult") ?? string.Empty;
        string evalResult = ctx.Get<string>("evalResult") ?? string.Empty;

        RuntimeAssertions.Require(
            ctx.Get<bool>("startActivated"),
            "StartShowcase() did not activate LayerDrama. result=" + startResult);

        RuntimeAssertions.Require(
            ctx.Get<bool>("evalActivated"),
            "EvalShowcase() did not activate LayerDrama. result=" + evalResult);

        RuntimeAssertions.Require(
            startResult.IndexOf("started", StringComparison.OrdinalIgnoreCase) >= 0,
            "StartShowcase() result is unexpected: " + startResult);

        RuntimeAssertions.Require(
            evalResult.IndexOf("started", StringComparison.OrdinalIgnoreCase) >= 0,
            "EvalShowcase() result is unexpected: " + evalResult);

        ctx.Log("Debug showcase console command verified.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
        SafeCloseDramaLayers();
    }

    private static string InvokeString(Type type, string methodName)
    {
        object raw = InvokeStatic(type, methodName, Type.EmptyTypes, new object[0]);
        RuntimeAssertions.Require(raw is string, methodName + " returned non-string.");
        return (string)raw;
    }

    private static object InvokeStatic(Type type, string methodName, Type[] argTypes, object[] args)
    {
        MethodInfo method = ModRuntimeReflection.RequireStaticMethod(type, methodName, argTypes);
        return ModRuntimeReflection.InvokeStatic(method, args);
    }

    private static void SafeCloseDramaLayers()
    {
        try
        {
            LayerDrama layer = LayerDrama.Instance;
            if (layer != null)
            {
                if (layer.drama != null && layer.drama.sequence != null)
                {
                    layer.drama.sequence.Exit();
                }

                layer.Close();
            }

            if (EClass.ui != null)
            {
                Layer top = EClass.ui.TopLayer;
                if (top != null)
                {
                    LayerDrama dramaTop = top as LayerDrama;
                    if (dramaTop != null && dramaTop.drama != null && dramaTop.drama.sequence != null)
                    {
                        dramaTop.drama.sequence.Exit();
                    }

                    top.Close();
                }

                EClass.ui.HideCover(0f, null);
                EClass.ui.Show(0f);
            }
        }
        catch
        {
        }
    }
}
