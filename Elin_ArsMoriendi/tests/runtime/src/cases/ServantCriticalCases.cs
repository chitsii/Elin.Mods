using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

// Critical servant operation smoke cases (contract + minimal behavior).
public sealed class ServantRitualCreateAndTrackCase : RuntimeCaseBase
{
    private const string ServantFlagPrefix = "chitsii.ars.sv.";

    public override string Id => "servant.ritual_create_and_track";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "servant" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var mgr = Elin_ArsMoriendi.NecromancyManager.Instance;
        RuntimeAssertions.Require(mgr != null, "NecromancyManager.Instance unavailable.");

        var servant = ctx.SpawnCharaWithRollback("putty", level: 5);
        RuntimeAssertions.Require(servant != null, "Failed to spawn temporary servant candidate.");
        RuntimeAssertions.Require(!mgr.IsServant(servant.uid), "Spawned chara already tracked as servant.");

        ctx.Set("manager", mgr);
        ctx.Set("servant", servant);
        ctx.Set("countBefore", mgr.ServantCount);

        ctx.RegisterRollback("servantTrackCleanup:" + servant.uid, () =>
        {
            if (mgr.IsServant(servant.uid))
                mgr.RemoveServant(servant.uid);
        });
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var mgr = ctx.Get<Elin_ArsMoriendi.NecromancyManager>("manager");
        var servant = ctx.Get<Chara>("servant");
        mgr.AddServant(servant);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var mgr = ctx.Get<Elin_ArsMoriendi.NecromancyManager>("manager");
        var servant = ctx.Get<Chara>("servant");
        int countBefore = ctx.Get<int>("countBefore");

        RuntimeAssertions.Require(mgr.IsServant(servant.uid), "AddServant did not register the servant UID.");
        RuntimeAssertions.Require(
            mgr.ServantCount >= countBefore + 1,
            "ServantCount did not increase after AddServant.");

        string key = ServantFlagPrefix + servant.uid;
        int stored = Elin_ArsMoriendi.DialogFlagStore.GetInt(EClass.player?.dialogFlags, key);
        RuntimeAssertions.Require(stored == 1, "Servant dialog flag not persisted after AddServant.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}

public sealed class ServantReleaseDetachAndCleanupCase : RuntimeCaseBase
{
    private const string ServantFlagPrefix = "chitsii.ars.sv.";

    public override string Id => "servant.release_detach_and_cleanup";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "servant" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var mgr = Elin_ArsMoriendi.NecromancyManager.Instance;
        RuntimeAssertions.Require(mgr != null, "NecromancyManager.Instance unavailable.");

        var servant = ctx.SpawnCharaWithRollback("putty", level: 6);
        RuntimeAssertions.Require(servant != null, "Failed to spawn release target.");
        RuntimeAssertions.Require(EClass.pc != null, "PC unavailable.");

        servant.MakeMinion(EClass.pc);
        servant.isSummon = false;
        mgr.AddServant(servant);

        ctx.Set("manager", mgr);
        ctx.Set("servant", servant);
        ctx.Set("servantUid", servant.uid);

        ctx.RegisterRollback("servantReleaseCleanup:" + servant.uid, () =>
        {
            if (mgr.IsServant(servant.uid))
                mgr.RemoveServant(servant.uid);
        });
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var mgr = ctx.Get<Elin_ArsMoriendi.NecromancyManager>("manager");
        var servant = ctx.Get<Chara>("servant");
        mgr.ReleaseServant(servant);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var mgr = ctx.Get<Elin_ArsMoriendi.NecromancyManager>("manager");
        var servant = ctx.Get<Chara>("servant");
        int servantUid = ctx.Get<int>("servantUid");

        RuntimeAssertions.Require(
            servant.isDestroyed,
            "ReleaseServant should destroy the servant card.");
        RuntimeAssertions.Require(
            servant.c_uidMaster == 0,
            "ReleaseServant did not clear c_uidMaster.");
        RuntimeAssertions.Require(
            !mgr.IsServant(servant.uid),
            "ReleaseServant did not clear tracked servant UID.");

        if (EClass.game?.cards?.globalCharas != null
            && EClass.game.cards.globalCharas.TryGetValue(servantUid, out var global))
        {
            RuntimeAssertions.Require(
                global == null || global.isDestroyed,
                "ReleaseServant left a non-destroyed global chara entry.");
        }

        RuntimeAssertions.Require(
            EClass.pc?.party?.members?.Contains(servant) != true,
            "ReleaseServant left servant in party members.");

        string key = ServantFlagPrefix + servantUid;
        int stored = Elin_ArsMoriendi.DialogFlagStore.GetInt(EClass.player?.dialogFlags, key);
        RuntimeAssertions.Require(stored == 0, "Servant dialog flag remained after ReleaseServant.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}

public sealed class ServantReviveMasterPersistenceCase : RuntimeCaseBase
{
    public override string Id => "servant.revive_master_persistence";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "servant", "compat" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var partyAddMember = ResolvePartyAddMemberMethod();
        RuntimeAssertions.Require(partyAddMember != null, "Party.AddMemeber target not found.");
        var revive = CriticalCaseHelpers.RequireMethod(typeof(Chara), nameof(Chara.Revive), null);
        var getRevived = CriticalCaseHelpers.RequireMethod(typeof(Chara), nameof(Chara.GetRevived), null);
        var countMinions = CriticalCaseHelpers.RequireMethod(typeof(Zone), nameof(Zone.CountMinions), new[] { typeof(Chara) });
        var tacticsCtor = AccessTools.Constructor(typeof(Tactics), new[] { typeof(Chara) });
        RuntimeAssertions.Require(tacticsCtor != null, "Tactics..ctor(Chara) not found.");

        ctx.Set("partyAddMember", partyAddMember);
        ctx.Set("revive", revive);
        ctx.Set("getRevived", getRevived);
        ctx.Set("countMinions", countMinions);
        ctx.Set("tacticsCtor", tacticsCtor);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        ctx.Set("partyAddMemberInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<MethodInfo>("partyAddMember")));
        ctx.Set("reviveInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<MethodInfo>("revive")));
        ctx.Set("getRevivedInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<MethodInfo>("getRevived")));
        ctx.Set("countMinionsInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<MethodInfo>("countMinions")));
        ctx.Set("tacticsCtorInfo", HarmonyCompatFacade.GetPatchInfo(ctx.Get<ConstructorInfo>("tacticsCtor")));
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var partyAddMemberInfo = ctx.GetOrDefault<object>("partyAddMemberInfo");
        var reviveInfo = ctx.GetOrDefault<object>("reviveInfo");
        var getRevivedInfo = ctx.GetOrDefault<object>("getRevivedInfo");
        var countMinionsInfo = ctx.GetOrDefault<object>("countMinionsInfo");
        var tacticsCtorInfo = ctx.GetOrDefault<object>("tacticsCtorInfo");

        RuntimeAssertions.Require(partyAddMemberInfo != null, "No patch info on Party.AddMemeber.");
        RuntimeAssertions.Require(reviveInfo != null, "No patch info on Chara.Revive.");
        RuntimeAssertions.Require(getRevivedInfo != null, "No patch info on Chara.GetRevived.");
        RuntimeAssertions.Require(countMinionsInfo != null, "No patch info on Zone.CountMinions.");
        RuntimeAssertions.Require(tacticsCtorInfo != null, "No patch info on Tactics..ctor(Chara).");

        CriticalCaseHelpers.RequirePatched(
            partyAddMemberInfo,
            "prefix",
            "Elin_ArsMoriendi.Patch_Party_AddMemeber_BlockServant",
            "Prefix",
            "Servant party block patch missing on Party.AddMemeber.");
        CriticalCaseHelpers.RequirePatched(
            reviveInfo,
            "postfix",
            "Elin_ArsMoriendi.Patch_Chara_Revive_RestoreMaster",
            "Postfix",
            "Servant revive restore patch missing on Chara.Revive.");
        CriticalCaseHelpers.RequirePatched(
            getRevivedInfo,
            "postfix",
            "Elin_ArsMoriendi.Patch_Chara_GetRevived_ServantInPlace",
            "Postfix",
            "Servant revive in-place patch missing on Chara.GetRevived.");
        CriticalCaseHelpers.RequirePatched(
            countMinionsInfo,
            "postfix",
            "Elin_ArsMoriendi.Patch_Zone_CountMinions_ExcludeServants",
            "Postfix",
            "Servant minion exclusion patch missing on Zone.CountMinions.");
        CriticalCaseHelpers.RequirePatched(
            tacticsCtorInfo,
            "postfix",
            "Elin_ArsMoriendi.Patch_Tactics_Ctor_ServantOverride",
            "Postfix",
            "Servant tactics override patch missing on Tactics..ctor.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }

    private static MethodInfo ResolvePartyAddMemberMethod()
    {
        var method = AccessTools.Method(typeof(Party), nameof(Party.AddMemeber), new[] { typeof(Chara), typeof(bool) });
        if (method != null)
            return method;

        method = AccessTools.Method(typeof(Party), nameof(Party.AddMemeber), new[] { typeof(Chara) });
        if (method != null)
            return method;

        var methods = typeof(Party).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < methods.Length; i++)
        {
            var m = methods[i];
            if (m == null || !string.Equals(m.Name, nameof(Party.AddMemeber), StringComparison.Ordinal))
                continue;

            var p = m.GetParameters();
            if (p.Length >= 1 && p[0].ParameterType == typeof(Chara))
                return m;
        }

        return null;
    }
}

public sealed class ServantSoulBindSelectionLowestLevelCase : RuntimeCaseBase
{
    public override string Id => "servant.soulbind_selection_lowest_level";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "servant", "spell" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.pc != null, "PC unavailable.");

        var lowLv = ctx.SpawnCharaWithRollback("putty", level: 5);
        var highLv = ctx.SpawnCharaWithRollback("putty", level: 30);
        RuntimeAssertions.Require(lowLv != null && highLv != null, "Failed to spawn soul bind test servants.");

        lowLv.SetLv(5);
        highLv.SetLv(30);
        lowLv.hp = Math.Max(1, lowLv.MaxHP);
        highLv.hp = Math.Max(1, highLv.MaxHP / 10);

        var patchType = CriticalCaseHelpers.RequireType("Elin_ArsMoriendi.Patch_Chara_Die_SoulBind");
        var selectMethod = AccessTools.Method(
            patchType,
            "SelectSoulBindSacrifice",
            new[] { typeof(List<Chara>) });
        RuntimeAssertions.Require(selectMethod != null, "Patch_Chara_Die_SoulBind.SelectSoulBindSacrifice not found.");

        ctx.Set("lowLvServant", lowLv);
        ctx.Set("highLvServant", highLv);
        ctx.Set("selectMethod", selectMethod);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var lowLv = ctx.Get<Chara>("lowLvServant");
        var highLv = ctx.Get<Chara>("highLvServant");
        var selectMethod = ctx.Get<MethodInfo>("selectMethod");

        var servants = new List<Chara> { highLv, lowLv };
        var selectedDefault = selectMethod.Invoke(null, new object[] { servants }) as Chara;

        RuntimeAssertions.Require(selectedDefault != null, "SoulBind selector returned null for default selection.");

        ctx.Set("selectedDefaultUid", selectedDefault.uid);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var lowLv = ctx.Get<Chara>("lowLvServant");
        var highLv = ctx.Get<Chara>("highLvServant");
        int selectedDefaultUid = ctx.Get<int>("selectedDefaultUid");

        RuntimeAssertions.Require(
            highLv.LV > lowLv.LV,
            "SoulBind selection test setup invalid: highLv must be greater than lowLv.");
        RuntimeAssertions.Require(
            highLv.MaxHP > 0 && lowLv.MaxHP > 0 && highLv.hp * lowLv.MaxHP < lowLv.hp * highLv.MaxHP,
            "SoulBind selection test setup invalid: highLv servant must have lower HP ratio than lowLv servant.");

        RuntimeAssertions.Require(
            selectedDefaultUid == lowLv.uid,
            "SoulBind default selection must pick the lowest-level servant.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}
