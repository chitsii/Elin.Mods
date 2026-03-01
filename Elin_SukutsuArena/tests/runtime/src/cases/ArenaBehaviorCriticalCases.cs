using System;
using System.Collections.Generic;
using System.Reflection;

// Behavior-focused critical runtime cases for arena result routing and quest dispatch.
public sealed class ArenaZoneInstanceResultRoutingFlagsCase : RuntimeCaseBase
{
    private static readonly string[] ManagedFlags = new[]
    {
        Elin_SukutsuArena.Flags.SessionFlagKeys.ArenaResult,
        Elin_SukutsuArena.Flags.SessionFlagKeys.QuestBattle,
        Elin_SukutsuArena.Flags.SessionFlagKeys.IsQuestBattleResult,
        Elin_SukutsuArena.Flags.SessionFlagKeys.IsRankUpResult,
        Elin_SukutsuArena.Flags.SessionFlagKeys.AutoDialog,
        Elin_SukutsuArena.Flags.SessionFlagKeys.DirectDrama
    };

    public override string Id => "arena.zoneinstance.result_routing_flags";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "arena", "quest" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.player?.dialogFlags != null, "player.dialogFlags unavailable.");
        SnapshotDialogFlagsWithRollback(ctx, ManagedFlags);

        bool haltPlaylistBefore = LayerDrama.haltPlaylist;
        ctx.RegisterRollback("layerDrama.haltPlaylist", () => LayerDrama.haltPlaylist = haltPlaylistBefore);

        var instance = new ZoneInstanceArenaBattle
        {
            uidMaster = 987654
        };

        ctx.Set("instance", instance);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var instance = ctx.Get<ZoneInstanceArenaBattle>("instance");

        RunOnLeaveScenario(
            ctx,
            instance,
            "questBattle",
            questBattleValue: 4,
            isRankUp: true);

        RunOnLeaveScenario(
            ctx,
            instance,
            "rankUpBattle",
            questBattleValue: 0,
            isRankUp: true);

        RunOnLeaveScenario(
            ctx,
            instance,
            "normalBattle",
            questBattleValue: 0,
            isRankUp: false);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        int uidMaster = ctx.Get<int>("questBattle.autoDialogExpected");

        RuntimeAssertions.Require(
            ctx.Get<int>("questBattle.arenaResult") == 2,
            "Quest battle route must set ArenaResult=2 on defeat.");
        RuntimeAssertions.Require(
            ctx.Get<int>("questBattle.isQuestBattleResult") == 1,
            "Quest battle route must set IsQuestBattleResult=1.");
        RuntimeAssertions.Require(
            ctx.Get<int>("questBattle.isRankUpResult") == 0,
            "Quest battle route must set IsRankUpResult=0.");
        RuntimeAssertions.Require(
            ctx.Get<int>("questBattle.autoDialog") == uidMaster,
            "Quest battle route must schedule auto-dialog for uidMaster.");

        RuntimeAssertions.Require(
            ctx.Get<int>("rankUpBattle.arenaResult") == 2,
            "Rank-up route must set ArenaResult=2 on defeat.");
        RuntimeAssertions.Require(
            ctx.Get<int>("rankUpBattle.isQuestBattleResult") == 0,
            "Rank-up route must keep IsQuestBattleResult=0.");
        RuntimeAssertions.Require(
            ctx.Get<int>("rankUpBattle.isRankUpResult") == 1,
            "Rank-up route must set IsRankUpResult=1.");
        RuntimeAssertions.Require(
            ctx.Get<int>("rankUpBattle.autoDialog") == uidMaster,
            "Rank-up route must schedule auto-dialog for uidMaster.");

        RuntimeAssertions.Require(
            ctx.Get<int>("normalBattle.arenaResult") == 2,
            "Normal route must set ArenaResult=2 on defeat.");
        RuntimeAssertions.Require(
            ctx.Get<int>("normalBattle.isQuestBattleResult") == 0,
            "Normal route must keep IsQuestBattleResult=0.");
        RuntimeAssertions.Require(
            ctx.Get<int>("normalBattle.isRankUpResult") == 0,
            "Normal route must keep IsRankUpResult=0.");
        RuntimeAssertions.Require(
            ctx.Get<int>("normalBattle.autoDialog") == uidMaster,
            "Normal route must schedule auto-dialog for uidMaster.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }

    private static void RunOnLeaveScenario(
        RuntimeTestContext ctx,
        ZoneInstanceArenaBattle instance,
        string scenarioKey,
        int questBattleValue,
        bool isRankUp)
    {
        instance.isRankUp = isRankUp;
        instance.victoryDramaId = string.Empty;
        instance.defeatDramaId = string.Empty;
        ZoneInstanceArenaBattle.PendingDirectDrama = string.Empty;

        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.ArenaResult, 0);
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.QuestBattle, questBattleValue);
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.IsQuestBattleResult, 0);
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.IsRankUpResult, 0);
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.AutoDialog, 0);
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.DirectDrama, 0);

        instance.OnLeaveZone();

        ctx.Set(scenarioKey + ".arenaResult", GetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.ArenaResult, -1));
        ctx.Set(scenarioKey + ".isQuestBattleResult", GetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.IsQuestBattleResult, -1));
        ctx.Set(scenarioKey + ".isRankUpResult", GetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.IsRankUpResult, -1));
        ctx.Set(scenarioKey + ".autoDialog", GetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.AutoDialog, -1));
        ctx.Set(scenarioKey + ".autoDialogExpected", instance.uidMaster);
    }

    private static void SnapshotDialogFlagsWithRollback(RuntimeTestContext ctx, IReadOnlyList<string> keys)
    {
        var flags = EClass.player.dialogFlags;
        var had = new Dictionary<string, bool>(StringComparer.Ordinal);
        var values = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            if (string.IsNullOrEmpty(key))
                continue;

            if (flags.TryGetValue(key, out int value))
            {
                had[key] = true;
                values[key] = value;
            }
            else
            {
                had[key] = false;
            }
        }

        ctx.RegisterRollback("dialogFlags.restore.arenaRouting", () =>
        {
            var current = EClass.player?.dialogFlags;
            if (current == null)
                return;

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                if (string.IsNullOrEmpty(key))
                    continue;

                if (had.TryGetValue(key, out bool existed) && existed)
                {
                    if (values.TryGetValue(key, out int value))
                        current[key] = value;
                }
                else
                {
                    current.Remove(key);
                }
            }
        });
    }

    private static void SetFlag(string key, int value)
    {
        var flags = EClass.player?.dialogFlags;
        RuntimeAssertions.Require(flags != null, "dialogFlags unavailable while setting flag: " + key);
        flags[key] = value;
    }

    private static int GetFlag(string key, int defaultValue)
    {
        var flags = EClass.player?.dialogFlags;
        if (flags == null)
            return defaultValue;
        return flags.TryGetValue(key, out int value) ? value : defaultValue;
    }
}

public sealed class ArenaZoneInstanceDirectDramaScheduleCase : RuntimeCaseBase
{
    private static readonly string[] ManagedFlags = new[]
    {
        Elin_SukutsuArena.Flags.SessionFlagKeys.ArenaResult,
        Elin_SukutsuArena.Flags.SessionFlagKeys.AutoDialog,
        Elin_SukutsuArena.Flags.SessionFlagKeys.DirectDrama
    };

    public override string Id => "arena.zoneinstance.direct_drama_schedule";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "arena", "drama" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.player?.dialogFlags != null, "player.dialogFlags unavailable.");
        SnapshotDialogFlagsWithRollback(ctx, ManagedFlags);

        string pendingBefore = ZoneInstanceArenaBattle.PendingDirectDrama;
        ctx.RegisterRollback("zoneInstance.pendingDirectDrama", () => ZoneInstanceArenaBattle.PendingDirectDrama = pendingBefore);

        var scheduleMethod = CriticalCaseHelpers.RequireMethod(typeof(ZoneInstanceArenaBattle), "ScheduleAutoDialog", null);
        RuntimeAssertions.Require(scheduleMethod != null, "ZoneInstanceArenaBattle.ScheduleAutoDialog not found.");

        var instance = new ZoneInstanceArenaBattle
        {
            uidMaster = 246810,
            victoryDramaId = "runtime_test_victory_drama",
            defeatDramaId = "runtime_test_defeat_drama"
        };

        ctx.Set("scheduleMethod", scheduleMethod);
        ctx.Set("instance", instance);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var scheduleMethod = ctx.Get<MethodInfo>("scheduleMethod");
        var instance = ctx.Get<ZoneInstanceArenaBattle>("instance");

        ZoneInstanceArenaBattle.PendingDirectDrama = string.Empty;
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.AutoDialog, 0);
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.DirectDrama, 0);
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.ArenaResult, 1);
        InvokeSchedule(scheduleMethod, instance);
        ctx.Set("victory.directDramaFlag", GetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.DirectDrama, -1));
        ctx.Set("victory.pendingDrama", ZoneInstanceArenaBattle.PendingDirectDrama ?? string.Empty);

        ZoneInstanceArenaBattle.PendingDirectDrama = string.Empty;
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.AutoDialog, 0);
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.DirectDrama, 0);
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.ArenaResult, 2);
        InvokeSchedule(scheduleMethod, instance);
        ctx.Set("defeat.directDramaFlag", GetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.DirectDrama, -1));
        ctx.Set("defeat.pendingDrama", ZoneInstanceArenaBattle.PendingDirectDrama ?? string.Empty);

        instance.victoryDramaId = string.Empty;
        instance.defeatDramaId = string.Empty;
        ZoneInstanceArenaBattle.PendingDirectDrama = string.Empty;
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.AutoDialog, 0);
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.DirectDrama, 0);
        SetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.ArenaResult, 1);
        InvokeSchedule(scheduleMethod, instance);
        ctx.Set("fallback.autoDialog", GetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.AutoDialog, -1));
        ctx.Set("fallback.directDramaFlag", GetFlag(Elin_SukutsuArena.Flags.SessionFlagKeys.DirectDrama, -1));
        ctx.Set("fallback.pendingDrama", ZoneInstanceArenaBattle.PendingDirectDrama ?? string.Empty);
        ctx.Set("fallback.expectedMasterUid", instance.uidMaster);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(
            ctx.Get<int>("victory.directDramaFlag") == 1,
            "Victory direct drama route must set DirectDrama flag.");
        RuntimeAssertions.Require(
            string.Equals(ctx.Get<string>("victory.pendingDrama"), "runtime_test_victory_drama", StringComparison.Ordinal),
            "Victory direct drama route must set PendingDirectDrama to victoryDramaId.");

        RuntimeAssertions.Require(
            ctx.Get<int>("defeat.directDramaFlag") == 1,
            "Defeat direct drama route must set DirectDrama flag.");
        RuntimeAssertions.Require(
            string.Equals(ctx.Get<string>("defeat.pendingDrama"), "runtime_test_defeat_drama", StringComparison.Ordinal),
            "Defeat direct drama route must set PendingDirectDrama to defeatDramaId.");

        RuntimeAssertions.Require(
            ctx.Get<int>("fallback.autoDialog") == ctx.Get<int>("fallback.expectedMasterUid"),
            "Fallback route must schedule AutoDialog with uidMaster.");
        RuntimeAssertions.Require(
            ctx.Get<int>("fallback.directDramaFlag") == 0,
            "Fallback route must not set DirectDrama flag.");
        RuntimeAssertions.Require(
            string.IsNullOrEmpty(ctx.Get<string>("fallback.pendingDrama")),
            "Fallback route must not keep PendingDirectDrama.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }

    private static void InvokeSchedule(MethodInfo method, ZoneInstanceArenaBattle instance)
    {
        try
        {
            method.Invoke(instance, null);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    private static void SnapshotDialogFlagsWithRollback(RuntimeTestContext ctx, IReadOnlyList<string> keys)
    {
        var flags = EClass.player.dialogFlags;
        var had = new Dictionary<string, bool>(StringComparer.Ordinal);
        var values = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            if (string.IsNullOrEmpty(key))
                continue;

            if (flags.TryGetValue(key, out int value))
            {
                had[key] = true;
                values[key] = value;
            }
            else
            {
                had[key] = false;
            }
        }

        ctx.RegisterRollback("dialogFlags.restore.directDrama", () =>
        {
            var current = EClass.player?.dialogFlags;
            if (current == null)
                return;

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                if (string.IsNullOrEmpty(key))
                    continue;

                if (had.TryGetValue(key, out bool existed) && existed)
                {
                    if (values.TryGetValue(key, out int value))
                        current[key] = value;
                }
                else
                {
                    current.Remove(key);
                }
            }
        });
    }

    private static void SetFlag(string key, int value)
    {
        var flags = EClass.player?.dialogFlags;
        RuntimeAssertions.Require(flags != null, "dialogFlags unavailable while setting flag: " + key);
        flags[key] = value;
    }

    private static int GetFlag(string key, int defaultValue)
    {
        var flags = EClass.player?.dialogFlags;
        if (flags == null)
            return defaultValue;
        return flags.TryGetValue(key, out int value) ? value : defaultValue;
    }
}

public sealed class QuestDispatchSelectionContractCase : RuntimeCaseBase
{
    private const string DispatchFlagKey = "sukutsu_runtime_dispatch_selection_index";

    public override string Id => "quest.dispatch.selection_contract";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "quest", "drama" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.player?.dialogFlags != null, "player.dialogFlags unavailable.");
        SnapshotDialogFlagWithRollback(ctx, DispatchFlagKey);

        var manager = Elin_SukutsuArena.ArenaQuestManager.Instance;
        RuntimeAssertions.Require(manager != null, "ArenaQuestManager.Instance is null.");
        RuntimeAssertions.Require(Elin_SukutsuArena.Core.ArenaContext.I?.Storage != null, "ArenaContext storage unavailable.");

        ctx.Set("manager", manager);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var manager = ctx.Get<Elin_SukutsuArena.ArenaQuestManager>("manager");
        var storage = Elin_SukutsuArena.Core.ArenaContext.I.Storage;

        Elin_SukutsuArena.ArenaManager.CheckQuestsForDispatch(
            DispatchFlagKey,
            "__missing_dispatch_a__,__missing_dispatch_b__");

        int fallbackValue = storage.GetInt(DispatchFlagKey, -1);
        ctx.Set("fallbackValue", fallbackValue);

        var available = manager.GetAvailableQuests();
        int availableCount = available != null ? available.Count : 0;
        ctx.Set("availableCount", availableCount);

        if (availableCount > 0)
        {
            string availableQuestId = available[0].QuestId;
            ctx.Set("availableQuestId", availableQuestId ?? string.Empty);

            Elin_SukutsuArena.ArenaManager.CheckQuestsForDispatch(
                DispatchFlagKey,
                "__missing_dispatch_a__," + availableQuestId + ",__missing_dispatch_b__");

            int selectedValue = storage.GetInt(DispatchFlagKey, -1);
            ctx.Set("selectedValue", selectedValue);
        }
        else
        {
            ctx.Log("No available quests on current save; positive dispatch selection check skipped.");
        }
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(
            ctx.Get<int>("fallbackValue") == 0,
            "Dispatch fallback must set selection index to 0 when no quest matches.");

        int availableCount = ctx.Get<int>("availableCount");
        if (availableCount > 0)
        {
            RuntimeAssertions.Require(
                ctx.Get<int>("selectedValue") == 2,
                "Dispatch must select index 2 for query __missing__,<available>,__missing__.");
        }
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }

    private static void SnapshotDialogFlagWithRollback(RuntimeTestContext ctx, string key)
    {
        var flags = EClass.player.dialogFlags;
        bool had = flags.TryGetValue(key, out int previous);

        ctx.RegisterRollback("dialogFlag.restore:" + key, () =>
        {
            var current = EClass.player?.dialogFlags;
            if (current == null)
                return;

            if (had)
                current[key] = previous;
            else
                current.Remove(key);
        });
    }
}
