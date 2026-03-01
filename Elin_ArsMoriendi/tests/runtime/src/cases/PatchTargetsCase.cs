using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

// Validates death-pipeline patch target migration.
public sealed class PatchTargetsCase : RuntimeCaseBase
{
    public override string Id => "patch.targets.spawnloot_and_oncharadie";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "compat" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var spawnLoot = AccessTools.Method(typeof(Card), nameof(Card.SpawnLoot), new[] { typeof(Card) });
        var onCharaDie = AccessTools.Method(typeof(ZoneEventManager), nameof(ZoneEventManager.OnCharaDie), new[] { typeof(Chara) });
        var charaDie = FindCharaDieMethod();

        RuntimeAssertions.Require(spawnLoot != null, "Card.SpawnLoot(Card) not found.");
        RuntimeAssertions.Require(onCharaDie != null, "ZoneEventManager.OnCharaDie(Chara) not found.");
        RuntimeAssertions.Require(charaDie != null, "Chara.Die method not found.");

        ctx.Set("spawnLootMethod", spawnLoot);
        ctx.Set("onCharaDieMethod", onCharaDie);
        ctx.Set("charaDieMethod", charaDie);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var spawnLoot = ctx.Get<MethodInfo>("spawnLootMethod");
        var onCharaDie = ctx.Get<MethodInfo>("onCharaDieMethod");
        var charaDie = ctx.Get<MethodInfo>("charaDieMethod");

        ctx.Set("spawnPatchInfo", HarmonyCompatFacade.GetPatchInfo(spawnLoot));
        ctx.Set("onCharaDiePatchInfo", HarmonyCompatFacade.GetPatchInfo(onCharaDie));
        ctx.Set("charaDiePatchInfo", HarmonyCompatFacade.GetPatchInfo(charaDie));
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var spawnInfo = ctx.GetOrDefault<object>("spawnPatchInfo");
        var onCharaDieInfo = ctx.GetOrDefault<object>("onCharaDiePatchInfo");
        var charaDieInfo = ctx.GetOrDefault<object>("charaDiePatchInfo");

        RuntimeAssertions.Require(spawnInfo != null, "No patch info on Card.SpawnLoot.");
        RuntimeAssertions.Require(onCharaDieInfo != null, "No patch info on ZoneEventManager.OnCharaDie.");

        RuntimeAssertions.Require(
            HarmonyCompatFacade.HasPatch(spawnInfo, "prefix", "Elin_ArsMoriendi.Patch_Chara_Die_SoulDrop", "Prefix"),
            "SoulDrop.Prefix is not patched on Card.SpawnLoot.");
        RuntimeAssertions.Require(
            HarmonyCompatFacade.HasPatch(spawnInfo, "postfix", "Elin_ArsMoriendi.Patch_Chara_Die_SoulDrop", "Postfix"),
            "SoulDrop.Postfix is not patched on Card.SpawnLoot.");
        RuntimeAssertions.Require(
            HarmonyCompatFacade.HasPatch(spawnInfo, "prefix", "Elin_ArsMoriendi.Patch_Chara_Die_PreserveCorpse", "Prefix"),
            "PreserveCorpse.Prefix is not patched on Card.SpawnLoot.");
        RuntimeAssertions.Require(
            HarmonyCompatFacade.HasPatch(spawnInfo, "postfix", "Elin_ArsMoriendi.Patch_Chara_Die_PreserveCorpse", "Postfix"),
            "PreserveCorpse.Postfix is not patched on Card.SpawnLoot.");

        RuntimeAssertions.Require(
            HarmonyCompatFacade.HasPatch(onCharaDieInfo, "postfix", "Elin_ArsMoriendi.Patch_Chara_Die_QuestNPC", "Postfix"),
            "QuestNPC.Postfix is not patched on ZoneEventManager.OnCharaDie.");

        if (charaDieInfo != null)
        {
            RuntimeAssertions.Require(
                !HarmonyCompatFacade.HasPatch(charaDieInfo, "prefix", "Elin_ArsMoriendi.Patch_Chara_Die_PreserveCorpse", "Prefix"),
                "PreserveCorpse.Prefix should not remain on Chara.Die.");
            RuntimeAssertions.Require(
                !HarmonyCompatFacade.HasPatch(charaDieInfo, "postfix", "Elin_ArsMoriendi.Patch_Chara_Die_QuestNPC", "Postfix"),
                "QuestNPC.Postfix should not remain on Chara.Die.");
        }

        ctx.Log("Verified migrated patch targets.");
    }

    private static MethodInfo FindCharaDieMethod()
    {
        var methods = typeof(Chara).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo best = null;
        int bestParams = -1;

        for (int i = 0; i < methods.Length; i++)
        {
            var m = methods[i];
            if (!string.Equals(m.Name, nameof(Chara.Die), StringComparison.Ordinal))
                continue;
            var p = m.GetParameters();
            if (p.Length < 3)
                continue;
            if (p.Length > bestParams)
            {
                best = m;
                bestParams = p.Length;
            }
        }

        return best;
    }
}
