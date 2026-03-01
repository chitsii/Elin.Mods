using System;
using System.Collections.Generic;
using System.Reflection;

// Verifies the quest patch is attached to Zone.Activate.
public sealed class PatchTargetsCase : RuntimeCaseBase
{
    public override string Id => "questmod.patch.targets.zone_activate_postfix";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "compat" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        Type patchType = ModRuntimeReflection.RequireType(
            "Elin_QuestMod.Patches.Patch_Zone_Activate_QuestPulse");
        var targetMethodResolver = ModRuntimeReflection.RequireStaticMethod(
            patchType,
            "TargetMethod",
            Type.EmptyTypes);

        object rawTarget = ModRuntimeReflection.InvokeStatic(targetMethodResolver);
        var targetMethod = rawTarget as MethodBase;
        RuntimeAssertions.Require(targetMethod != null, "TargetMethod returned null.");

        ctx.Set("patchType", patchType);
        ctx.Set("targetMethod", targetMethod);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var targetMethod = ctx.Get<MethodBase>("targetMethod");
        object patchInfo = HarmonyCompatFacade.GetPatchInfo(targetMethod);
        ctx.Set("patchInfo", patchInfo);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        Type patchType = ctx.Get<Type>("patchType");
        var targetMethod = ctx.Get<MethodBase>("targetMethod");
        object patchInfo = ctx.Get<object>("patchInfo");

        RuntimeAssertions.Require(patchInfo != null, "No patch info found on target method.");
        RuntimeAssertions.Require(
            HarmonyCompatFacade.HasPatch(
                patchInfo,
                "postfix",
                patchType.FullName,
                "Postfix"),
            "Quest postfix patch is missing on Zone.Activate.");

        string targetTypeName = targetMethod.DeclaringType != null
            ? (targetMethod.DeclaringType.FullName ?? targetMethod.DeclaringType.Name)
            : "(null)";
        ctx.Log("Patch target verified: " + targetTypeName + "." + targetMethod.Name);
    }
}
