using System;
using System.Collections.Generic;

// Critical spell smoke cases (contract-focused, low-flake).
public sealed class SpellSoulTrapDropPipelineCase : RuntimeCaseBase
{
    public override string Id => "spell.soultrap_drop_pipeline";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "spell" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var spawnLoot = CriticalCaseHelpers.RequireMethod(typeof(Card), nameof(Card.SpawnLoot), new[] { typeof(Card) });
        ctx.Set("spawnLootMethod", spawnLoot);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var spawnLoot = ctx.Get<System.Reflection.MethodInfo>("spawnLootMethod");
        ctx.Set("spawnPatchInfo", HarmonyCompatFacade.GetPatchInfo(spawnLoot));
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var spawnInfo = ctx.GetOrDefault<object>("spawnPatchInfo");
        RuntimeAssertions.Require(spawnInfo != null, "No patch info on Card.SpawnLoot for soul trap pipeline.");

        CriticalCaseHelpers.RequirePatched(
            spawnInfo,
            "prefix",
            "Elin_ArsMoriendi.Patch_Chara_Die_SoulDrop",
            "Prefix",
            "SoulDrop.Prefix is not patched on Card.SpawnLoot.");
        CriticalCaseHelpers.RequirePatched(
            spawnInfo,
            "postfix",
            "Elin_ArsMoriendi.Patch_Chara_Die_SoulDrop",
            "Postfix",
            "SoulDrop.Postfix is not patched on Card.SpawnLoot.");

        var unlock = CriticalCaseHelpers.RequireSpellUnlock("actSoulTrap");
        RuntimeAssertions.Require(unlock.ElementId > 0, "actSoulTrap element ID is unresolved.");
        RuntimeAssertions.Require(
            string.Equals(unlock.RequiredSoulId, "ars_soul_weak", StringComparison.Ordinal),
            "actSoulTrap required soul mismatch.");

        CriticalCaseHelpers.RequireTypeAssignableTo("Elin_ArsMoriendi.ActSoulTrap", typeof(Spell));
        CriticalCaseHelpers.RequireTypeAssignableTo("Elin_ArsMoriendi.ConSoulTrapped", typeof(Condition));
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}

public sealed class SpellPreserveCorpseGuaranteedDropCase : RuntimeCaseBase
{
    public override string Id => "spell.preservecorpse_guaranteed_drop";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "spell" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var spawnLoot = CriticalCaseHelpers.RequireMethod(typeof(Card), nameof(Card.SpawnLoot), new[] { typeof(Card) });
        ctx.Set("spawnLootMethod", spawnLoot);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var spawnLoot = ctx.Get<System.Reflection.MethodInfo>("spawnLootMethod");
        ctx.Set("spawnPatchInfo", HarmonyCompatFacade.GetPatchInfo(spawnLoot));
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var spawnInfo = ctx.GetOrDefault<object>("spawnPatchInfo");
        RuntimeAssertions.Require(spawnInfo != null, "No patch info on Card.SpawnLoot for preserve corpse.");

        CriticalCaseHelpers.RequirePatched(
            spawnInfo,
            "prefix",
            "Elin_ArsMoriendi.Patch_Chara_Die_PreserveCorpse",
            "Prefix",
            "PreserveCorpse.Prefix is not patched on Card.SpawnLoot.");
        CriticalCaseHelpers.RequirePatched(
            spawnInfo,
            "postfix",
            "Elin_ArsMoriendi.Patch_Chara_Die_PreserveCorpse",
            "Postfix",
            "PreserveCorpse.Postfix is not patched on Card.SpawnLoot.");

        var unlock = CriticalCaseHelpers.RequireSpellUnlock("actPreserveCorpse");
        RuntimeAssertions.Require(unlock.ElementId > 0, "actPreserveCorpse element ID is unresolved.");
        RuntimeAssertions.Require(
            string.Equals(unlock.RequiredSoulId, "ars_soul_weak", StringComparison.Ordinal),
            "actPreserveCorpse required soul mismatch.");

        CriticalCaseHelpers.RequireTypeAssignableTo("Elin_ArsMoriendi.ActPreserveCorpse", typeof(Spell));
        CriticalCaseHelpers.RequireTypeAssignableTo("Elin_ArsMoriendi.ConPreserveCorpse", typeof(Condition));
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}

public sealed class SpellSoulBindSubstitutionCase : RuntimeCaseBase
{
    public override string Id => "spell.soulbind_substitution";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "spell" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var charaDie = CriticalCaseHelpers.RequireBestCharaDieMethod();
        ctx.Set("charaDieMethod", charaDie);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var charaDie = ctx.Get<System.Reflection.MethodInfo>("charaDieMethod");
        ctx.Set("diePatchInfo", HarmonyCompatFacade.GetPatchInfo(charaDie));
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var dieInfo = ctx.GetOrDefault<object>("diePatchInfo");
        RuntimeAssertions.Require(dieInfo != null, "No patch info on Chara.Die for soul bind.");

        CriticalCaseHelpers.RequirePatched(
            dieInfo,
            "prefix",
            "Elin_ArsMoriendi.Patch_Chara_Die_SoulBind",
            "Prefix",
            "SoulBind.Prefix is not patched on Chara.Die.");
        CriticalCaseHelpers.RequirePatched(
            dieInfo,
            "postfix",
            "Elin_ArsMoriendi.Patch_Chara_Die_SoulBind",
            "Postfix",
            "SoulBind.Postfix is not patched on Chara.Die.");

        var unlock = CriticalCaseHelpers.RequireSpellUnlock("actSoulBind");
        RuntimeAssertions.Require(unlock.ElementId > 0, "actSoulBind element ID is unresolved.");
        RuntimeAssertions.Require(
            string.Equals(unlock.RequiredSoulId, "ars_soul_legendary", StringComparison.Ordinal),
            "actSoulBind required soul mismatch.");

        CriticalCaseHelpers.RequireTypeAssignableTo("Elin_ArsMoriendi.ActSoulBind", typeof(Spell));
        CriticalCaseHelpers.RequireTypeAssignableTo("Elin_ArsMoriendi.ConSoulBind", typeof(Condition));

        var mgr = Elin_ArsMoriendi.NecromancyManager.Instance;
        RuntimeAssertions.Require(mgr != null, "NecromancyManager.Instance is null.");
        CriticalCaseHelpers.RequireMethod(
            typeof(Elin_ArsMoriendi.NecromancyManager),
            nameof(Elin_ArsMoriendi.NecromancyManager.GetSoulBindSacrificeUid),
            null);
        CriticalCaseHelpers.RequireMethod(
            typeof(Elin_ArsMoriendi.NecromancyManager),
            nameof(Elin_ArsMoriendi.NecromancyManager.SetSoulBindSacrificeUid),
            new[] { typeof(int) });
        CriticalCaseHelpers.RequireMethod(
            typeof(Elin_ArsMoriendi.NecromancyManager),
            nameof(Elin_ArsMoriendi.NecromancyManager.ClearSoulBindSacrificeUid),
            null);
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}
