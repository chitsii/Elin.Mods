using System.Collections;
using System.Collections.Generic;
using System;

// Critical quest progression smoke cases (transition contract checks).
public sealed class QuestStage0To1FirstSoulCase : RuntimeCaseBase
{
    public override string Id => "quest.stage0_to1_first_soul";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "quest" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var rules = CriticalCaseHelpers.GetQuestTransitionRules();
        ctx.Set("rules", rules);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var rules = ctx.Get<IDictionary>("rules");
        CriticalCaseHelpers.RequireQuestRule(
            rules,
            Elin_ArsMoriendi.UnhallowedStage.NotStarted,
            "trigger.first_soul_drop",
            Elin_ArsMoriendi.UnhallowedStage.Stage1);

        CriticalCaseHelpers.RequireMethod(
            typeof(Elin_ArsMoriendi.UnhallowedPath),
            nameof(Elin_ArsMoriendi.UnhallowedPath.TryAdvanceOnFirstSoulDrop),
            null);
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}

public sealed class QuestStage1To2TomeOpenCase : RuntimeCaseBase
{
    public override string Id => "quest.stage1_to2_tome_open";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "quest" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var rules = CriticalCaseHelpers.GetQuestTransitionRules();
        ctx.Set("rules", rules);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var rules = ctx.Get<IDictionary>("rules");
        CriticalCaseHelpers.RequireQuestRule(
            rules,
            Elin_ArsMoriendi.UnhallowedStage.Stage1,
            "trigger.tome_open",
            Elin_ArsMoriendi.UnhallowedStage.Stage2);

        var method = CriticalCaseHelpers.RequireMethod(
            typeof(Elin_ArsMoriendi.UnhallowedPath),
            nameof(Elin_ArsMoriendi.UnhallowedPath.TryAdvanceOnTomeOpen),
            null);
        RuntimeAssertions.Require(
            method.ReturnType == typeof(bool),
            "UnhallowedPath.TryAdvanceOnTomeOpen must return bool.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}

public sealed class QuestStage2KnightSpawnOnceCase : RuntimeCaseBase
{
    public override string Id => "quest.stage2_knight_spawn_once";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "quest" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var rules = CriticalCaseHelpers.GetQuestTransitionRules();
        var zoneActivate = CriticalCaseHelpers.RequireMethod(typeof(Zone), nameof(Zone.Activate), null);

        ctx.Set("rules", rules);
        ctx.Set("zoneActivateMethod", zoneActivate);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var zoneActivate = ctx.Get<System.Reflection.MethodInfo>("zoneActivateMethod");
        ctx.Set("zoneActivateInfo", HarmonyCompatFacade.GetPatchInfo(zoneActivate));
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var rules = ctx.Get<IDictionary>("rules");
        CriticalCaseHelpers.RequireQuestRule(
            rules,
            Elin_ArsMoriendi.UnhallowedStage.Stage2,
            "trigger.karen_encounter.hostile",
            Elin_ArsMoriendi.UnhallowedStage.Stage3);

        CriticalCaseHelpers.RequireMethod(
            typeof(Elin_ArsMoriendi.KnightEncounter),
            nameof(Elin_ArsMoriendi.KnightEncounter.TrySpawnKnights),
            null);
        CriticalCaseHelpers.RequireMethod(
            typeof(Elin_ArsMoriendi.UnhallowedPath),
            nameof(Elin_ArsMoriendi.UnhallowedPath.MarkKnightsSpawned),
            null);
        CriticalCaseHelpers.RequireMethod(
            typeof(Elin_ArsMoriendi.UnhallowedPath),
            nameof(Elin_ArsMoriendi.UnhallowedPath.ResetKnightsSpawned),
            null);

        var zoneActivateInfo = ctx.GetOrDefault<object>("zoneActivateInfo");
        RuntimeAssertions.Require(zoneActivateInfo != null, "No patch info on Zone.Activate.");
        CriticalCaseHelpers.RequirePatched(
            zoneActivateInfo,
            "postfix",
            "Elin_ArsMoriendi.Patch_Zone_Activate_KnightEncounter",
            "Postfix",
            "Knight encounter patch missing on Zone.Activate.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}

public sealed class QuestStage7ErenosDefeatAdvanceCase : RuntimeCaseBase
{
    public override string Id => "quest.stage7_erenos_defeat_advance";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "quest" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var rules = CriticalCaseHelpers.GetQuestTransitionRules();
        ctx.Set("rules", rules);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var rules = ctx.Get<IDictionary>("rules");
        CriticalCaseHelpers.RequireQuestRule(
            rules,
            Elin_ArsMoriendi.UnhallowedStage.Stage7,
            "trigger.enemy_defeated.erenos",
            Elin_ArsMoriendi.UnhallowedStage.Stage8);
        CriticalCaseHelpers.RequireQuestRule(
            rules,
            Elin_ArsMoriendi.UnhallowedStage.Stage8,
            "trigger.apotheosis_apply",
            Elin_ArsMoriendi.UnhallowedStage.Stage9);

        CriticalCaseHelpers.RequireMethod(
            typeof(Elin_ArsMoriendi.UnhallowedPath),
            nameof(Elin_ArsMoriendi.UnhallowedPath.TryHandleErenosBattleComplete),
            null);
        CriticalCaseHelpers.RequireMethod(
            typeof(Elin_ArsMoriendi.UnhallowedPath),
            nameof(Elin_ArsMoriendi.UnhallowedPath.TryHandleApotheosisApply),
            null);
        CriticalCaseHelpers.RequireMethod(
            typeof(Elin_ArsMoriendi.UnhallowedPath),
            nameof(Elin_ArsMoriendi.UnhallowedPath.MarkErenosDefeated),
            null);
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}

public sealed class QuestBranchContractRulePresenceCase : RuntimeCaseBase
{
    public override string Id => "quest.branch_contract_rule_presence";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "quest" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var fixture = QuestBranchSmokeFixture.Create();
        var rules = CriticalCaseHelpers.GetTriggeredTransitionRules(fixture.StateMachine);
        ctx.Set("fixture", fixture);
        ctx.Set("rules", rules);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var rules = ctx.Get<IDictionary>("rules");
        CriticalCaseHelpers.RequireBranchTargets(
            rules,
            (int)QuestBranchSmokeFixture.BranchState.Start,
            QuestBranchSmokeFixture.TriggerBranchSelect,
            (int)QuestBranchSmokeFixture.BranchState.BranchA,
            (int)QuestBranchSmokeFixture.BranchState.BranchB);

        CriticalCaseHelpers.RequireTriggeredRule(
            rules,
            (int)QuestBranchSmokeFixture.BranchState.BranchA,
            QuestBranchSmokeFixture.TriggerConverge,
            (int)QuestBranchSmokeFixture.BranchState.Converged);
        CriticalCaseHelpers.RequireTriggeredRule(
            rules,
            (int)QuestBranchSmokeFixture.BranchState.BranchB,
            QuestBranchSmokeFixture.TriggerConverge,
            (int)QuestBranchSmokeFixture.BranchState.Converged);
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}

public sealed class QuestBranchRuntimeFlagSelectCase : RuntimeCaseBase
{
    public override string Id => "quest.branch_runtime_flag_select";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "quest" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.player != null, "EClass.player is null.");
        RuntimeAssertions.Require(EClass.player.dialogFlags != null, "dialogFlags is null.");
        CriticalCaseHelpers.CaptureDialogFlagWithRollback(ctx, QuestBranchSmokeFixture.RouteFlagKey);

        var fixture = QuestBranchSmokeFixture.Create();
        ctx.Set("fixture", fixture);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var fixture = ctx.Get<QuestBranchSmokeFixture>("fixture");

        fixture.SetCurrent(QuestBranchSmokeFixture.BranchState.Start);
        CriticalCaseHelpers.SetDialogFlag(QuestBranchSmokeFixture.RouteFlagKey, 1);
        bool advancedBranchA = fixture.StateMachine.TryHandle(QuestBranchSmokeFixture.TriggerBranchSelect);
        var stateAfterBranchA = fixture.GetCurrent();

        fixture.SetCurrent(QuestBranchSmokeFixture.BranchState.Start);
        CriticalCaseHelpers.SetDialogFlag(QuestBranchSmokeFixture.RouteFlagKey, 2);
        bool advancedBranchB = fixture.StateMachine.TryHandle(QuestBranchSmokeFixture.TriggerBranchSelect);
        var stateAfterBranchB = fixture.GetCurrent();

        fixture.ResetInvalidBranchObserved();
        fixture.SetCurrent(QuestBranchSmokeFixture.BranchState.Start);
        CriticalCaseHelpers.SetDialogFlag(QuestBranchSmokeFixture.RouteFlagKey, 0);
        bool advancedInvalid = fixture.StateMachine.TryHandle(QuestBranchSmokeFixture.TriggerBranchSelect);
        var stateAfterInvalid = fixture.GetCurrent();
        bool invalidBranchObserved = fixture.InvalidBranchObserved;

        ctx.Set("advancedBranchA", advancedBranchA);
        ctx.Set("stateAfterBranchA", stateAfterBranchA);
        ctx.Set("advancedBranchB", advancedBranchB);
        ctx.Set("stateAfterBranchB", stateAfterBranchB);
        ctx.Set("advancedInvalid", advancedInvalid);
        ctx.Set("stateAfterInvalid", stateAfterInvalid);
        ctx.Set("invalidBranchObserved", invalidBranchObserved);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(ctx.Get<bool>("advancedBranchA"), "Branch A did not advance.");
        RuntimeAssertions.Require(
            ctx.Get<QuestBranchSmokeFixture.BranchState>("stateAfterBranchA") == QuestBranchSmokeFixture.BranchState.BranchA,
            "Route flag 1 did not select BranchA.");

        RuntimeAssertions.Require(ctx.Get<bool>("advancedBranchB"), "Branch B did not advance.");
        RuntimeAssertions.Require(
            ctx.Get<QuestBranchSmokeFixture.BranchState>("stateAfterBranchB") == QuestBranchSmokeFixture.BranchState.BranchB,
            "Route flag 2 did not select BranchB.");

        RuntimeAssertions.Require(!ctx.Get<bool>("advancedInvalid"), "Invalid route should not advance.");
        RuntimeAssertions.Require(
            ctx.Get<QuestBranchSmokeFixture.BranchState>("stateAfterInvalid") == QuestBranchSmokeFixture.BranchState.Start,
            "Invalid route changed state unexpectedly.");
        RuntimeAssertions.Require(ctx.Get<bool>("invalidBranchObserved"), "Invalid route did not trigger onInvalidBranch.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}

public sealed class QuestBranchConvergeCommonSuccessorCase : RuntimeCaseBase
{
    public override string Id => "quest.branch_converge_common_successor";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "quest" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var fixture = QuestBranchSmokeFixture.Create();
        ctx.Set("fixture", fixture);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var fixture = ctx.Get<QuestBranchSmokeFixture>("fixture");

        fixture.SetCurrent(QuestBranchSmokeFixture.BranchState.BranchA);
        bool advancedFromA = fixture.StateMachine.TryHandle(QuestBranchSmokeFixture.TriggerConverge);
        var stateAfterA = fixture.GetCurrent();

        fixture.SetCurrent(QuestBranchSmokeFixture.BranchState.BranchB);
        bool advancedFromB = fixture.StateMachine.TryHandle(QuestBranchSmokeFixture.TriggerConverge);
        var stateAfterB = fixture.GetCurrent();

        ctx.Set("advancedFromA", advancedFromA);
        ctx.Set("stateAfterA", stateAfterA);
        ctx.Set("advancedFromB", advancedFromB);
        ctx.Set("stateAfterB", stateAfterB);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(ctx.Get<bool>("advancedFromA"), "Converge from BranchA did not advance.");
        RuntimeAssertions.Require(
            ctx.Get<QuestBranchSmokeFixture.BranchState>("stateAfterA") == QuestBranchSmokeFixture.BranchState.Converged,
            "BranchA did not converge to common successor.");

        RuntimeAssertions.Require(ctx.Get<bool>("advancedFromB"), "Converge from BranchB did not advance.");
        RuntimeAssertions.Require(
            ctx.Get<QuestBranchSmokeFixture.BranchState>("stateAfterB") == QuestBranchSmokeFixture.BranchState.Converged,
            "BranchB did not converge to common successor.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}

internal sealed class QuestBranchSmokeFixture
{
    internal enum BranchState
    {
        Start = 0,
        BranchA = 1,
        BranchB = 2,
        Converged = 3,
        Invalid = 99,
    }

    internal const string TriggerBranchSelect = "trigger.branch.select";
    internal const string TriggerConverge = "trigger.branch.converge";
    internal const string RouteFlagKey = "chitsii.ars.runtime.branch.route";

    private BranchState _current = BranchState.Start;

    private QuestBranchSmokeFixture()
    {
        StateMachine = new Elin_ArsMoriendi.QuestTriggeredStateMachine<BranchState, string>(
            () => _current,
            next => _current = next,
            new[]
            {
                new Elin_ArsMoriendi.QuestTriggeredTransitionRule<BranchState, string>(
                    BranchState.Start,
                    TriggerBranchSelect,
                    new[] { BranchState.BranchA, BranchState.BranchB },
                    resolveTo: ResolveBranchTargetFromFlag,
                    canAdvance: () => true,
                    onInvalidBranch: () => InvalidBranchObserved = true),
                new Elin_ArsMoriendi.QuestTriggeredTransitionRule<BranchState, string>(
                    BranchState.BranchA,
                    BranchState.Converged,
                    TriggerConverge,
                    () => true),
                new Elin_ArsMoriendi.QuestTriggeredTransitionRule<BranchState, string>(
                    BranchState.BranchB,
                    BranchState.Converged,
                    TriggerConverge,
                    () => true),
            });
    }

    internal Elin_ArsMoriendi.QuestTriggeredStateMachine<BranchState, string> StateMachine { get; }
    internal bool InvalidBranchObserved { get; private set; }

    internal static QuestBranchSmokeFixture Create()
    {
        return new QuestBranchSmokeFixture();
    }

    internal BranchState GetCurrent()
    {
        return _current;
    }

    internal void SetCurrent(BranchState state)
    {
        _current = state;
    }

    internal void ResetInvalidBranchObserved()
    {
        InvalidBranchObserved = false;
    }

    private static BranchState ResolveBranchTargetFromFlag()
    {
        int route = CriticalCaseHelpers.GetDialogFlag(RouteFlagKey);
        if (route == 1)
            return BranchState.BranchA;
        if (route == 2)
            return BranchState.BranchB;
        return BranchState.Invalid;
    }
}
