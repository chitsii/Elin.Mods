using System.Collections.Generic;
using System.Reflection;
using Elin_SukutsuArena.Arena;

// Critical patch target contract checks for SukutsuArena.
public sealed class PatchTargetsCoreMethodsCase : RuntimeCaseBase
{
    public override string Id => "patch.targets.core_methods";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "compat" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var zoneActivate = CriticalCaseHelpers.RequireMethod(typeof(Zone), nameof(Zone.Activate), null);
        var shouldAutoRevive = CriticalCaseHelpers.RequireZoneShouldAutoReviveGetter();
        var onKill = CriticalCaseHelpers.RequireMethod(typeof(LayerDrama), nameof(LayerDrama.OnKill), null);
        var tickConditions = CriticalCaseHelpers.RequireMethod(typeof(Chara), nameof(Chara.TickConditions), null);
        var checkRandomSites = CriticalCaseHelpers.RequireMethod(typeof(Region), nameof(Region.CheckRandomSites), null);
        var parseLine = CriticalCaseHelpers.RequireMethod(typeof(DramaManager), "ParseLine", null);
        var damageHp = CriticalCaseHelpers.RequireMethod(
            typeof(Card),
            nameof(Card.DamageHP),
            new[] { typeof(long), typeof(int), typeof(int), typeof(AttackSource), typeof(Card), typeof(bool), typeof(Thing), typeof(Chara) });
        var healHp = CriticalCaseHelpers.RequireMethod(
            typeof(Card),
            nameof(Card.HealHP),
            new[] { typeof(int), typeof(HealSource) });

        ctx.Set("zoneActivate", zoneActivate);
        ctx.Set("zoneShouldAutoRevive", shouldAutoRevive);
        ctx.Set("onKill", onKill);
        ctx.Set("tickConditions", tickConditions);
        ctx.Set("checkRandomSites", checkRandomSites);
        ctx.Set("parseLine", parseLine);
        ctx.Set("damageHp", damageHp);
        ctx.Set("healHp", healHp);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        ctx.Set("zoneActivatePatchInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<MethodInfo>("zoneActivate")));
        ctx.Set("zoneShouldAutoRevivePatchInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<MethodBase>("zoneShouldAutoRevive")));
        ctx.Set("onKillPatchInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<MethodInfo>("onKill")));
        ctx.Set("tickConditionsPatchInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<MethodInfo>("tickConditions")));
        ctx.Set("checkRandomSitesPatchInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<MethodInfo>("checkRandomSites")));
        ctx.Set("parseLinePatchInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<MethodInfo>("parseLine")));
        ctx.Set("damageHpPatchInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<MethodInfo>("damageHp")));
        ctx.Set("healHpPatchInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<MethodInfo>("healHp")));
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var owner = Elin_SukutsuArena.Arena.ArenaConfig.ModGuid;

        var zoneActivatePatchInfo = ctx.GetOrDefault<object>("zoneActivatePatchInfo");
        var zoneShouldAutoRevivePatchInfo = ctx.GetOrDefault<object>("zoneShouldAutoRevivePatchInfo");
        var onKillPatchInfo = ctx.GetOrDefault<object>("onKillPatchInfo");
        var tickConditionsPatchInfo = ctx.GetOrDefault<object>("tickConditionsPatchInfo");
        var checkRandomSitesPatchInfo = ctx.GetOrDefault<object>("checkRandomSitesPatchInfo");
        var parseLinePatchInfo = ctx.GetOrDefault<object>("parseLinePatchInfo");
        var damageHpPatchInfo = ctx.GetOrDefault<object>("damageHpPatchInfo");
        var healHpPatchInfo = ctx.GetOrDefault<object>("healHpPatchInfo");

        RuntimeAssertions.Require(zoneActivatePatchInfo != null, "No patch info on Zone.Activate.");
        RuntimeAssertions.Require(zoneShouldAutoRevivePatchInfo != null, "No patch info on Zone.ShouldAutoRevive getter.");
        RuntimeAssertions.Require(onKillPatchInfo != null, "No patch info on LayerDrama.OnKill.");
        RuntimeAssertions.Require(tickConditionsPatchInfo != null, "No patch info on Chara.TickConditions.");
        RuntimeAssertions.Require(checkRandomSitesPatchInfo != null, "No patch info on Region.CheckRandomSites.");
        RuntimeAssertions.Require(parseLinePatchInfo != null, "No patch info on DramaManager.ParseLine.");
        RuntimeAssertions.Require(damageHpPatchInfo != null, "No patch info on Card.DamageHP.");
        RuntimeAssertions.Require(healHpPatchInfo != null, "No patch info on Card.HealHP.");

        CriticalCaseHelpers.RequireOwnedPatch(zoneActivatePatchInfo, owner, "Sukutsu patch owner missing on Zone.Activate.");
        CriticalCaseHelpers.RequireOwnedPatch(zoneShouldAutoRevivePatchInfo, owner, "Sukutsu patch owner missing on Zone.ShouldAutoRevive getter.");
        CriticalCaseHelpers.RequireOwnedPatch(onKillPatchInfo, owner, "Sukutsu patch owner missing on LayerDrama.OnKill.");
        CriticalCaseHelpers.RequireOwnedPatch(tickConditionsPatchInfo, owner, "Sukutsu patch owner missing on Chara.TickConditions.");
        CriticalCaseHelpers.RequireOwnedPatch(checkRandomSitesPatchInfo, owner, "Sukutsu patch owner missing on Region.CheckRandomSites.");
        CriticalCaseHelpers.RequireOwnedPatch(parseLinePatchInfo, owner, "Sukutsu patch owner missing on DramaManager.ParseLine.");
        CriticalCaseHelpers.RequireOwnedPatch(damageHpPatchInfo, owner, "Sukutsu patch owner missing on Card.DamageHP.");
        CriticalCaseHelpers.RequireOwnedPatch(healHpPatchInfo, owner, "Sukutsu patch owner missing on Card.HealHP.");

        CriticalCaseHelpers.RequirePatched(
            parseLinePatchInfo,
            "postfix",
            "Elin_SukutsuArena.DramaManager_Patch",
            "ParseLine_Postfix",
            "DramaManager.ParseLine postfix route is not patched.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}
