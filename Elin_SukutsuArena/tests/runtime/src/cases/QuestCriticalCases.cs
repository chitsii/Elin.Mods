using System.Collections.Generic;

// Critical quest progression chain checks for SukutsuArena.
public sealed class QuestMainAndPostgameChainCase : RuntimeCaseBase
{
    public override string Id => "quest.chain.main_and_postgame";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "quest" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        ctx.Set("opening", CriticalCaseHelpers.RequireQuest("01_opening"));
        ctx.Set("rankUpG", CriticalCaseHelpers.RequireQuest("02_rank_up_G"));
        ctx.Set("vsAstaroth", CriticalCaseHelpers.RequireQuest("17_vs_astaroth"));
        ctx.Set("lastBattle", CriticalCaseHelpers.RequireQuest("18_last_battle"));
        ctx.Set("resurrectionIntro", CriticalCaseHelpers.RequireQuest("pg_02a_resurrection_intro"));
        ctx.Set("resurrectionExec", CriticalCaseHelpers.RequireQuest("pg_02b_resurrection_execution"));
        ctx.Set("scrollShowcase", CriticalCaseHelpers.RequireQuest("pg_03_scroll_showcase"));
    }

    public override void Execute(RuntimeTestContext ctx)
    {
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var opening = ctx.Get<Elin_SukutsuArena.Quests.ArenaQuestDefinition>("opening");
        var rankUpG = ctx.Get<Elin_SukutsuArena.Quests.ArenaQuestDefinition>("rankUpG");
        var vsAstaroth = ctx.Get<Elin_SukutsuArena.Quests.ArenaQuestDefinition>("vsAstaroth");
        var lastBattle = ctx.Get<Elin_SukutsuArena.Quests.ArenaQuestDefinition>("lastBattle");
        var resurrectionIntro = ctx.Get<Elin_SukutsuArena.Quests.ArenaQuestDefinition>("resurrectionIntro");
        var resurrectionExec = ctx.Get<Elin_SukutsuArena.Quests.ArenaQuestDefinition>("resurrectionExec");
        var scrollShowcase = ctx.Get<Elin_SukutsuArena.Quests.ArenaQuestDefinition>("scrollShowcase");

        RuntimeAssertions.Require(opening.AutoTrigger, "01_opening must be auto-trigger.");
        RuntimeAssertions.Require(string.IsNullOrEmpty(opening.QuestGiver), "01_opening must have no quest giver.");

        CriticalCaseHelpers.RequireQuestDependsOn(
            rankUpG,
            "01_opening",
            "02_rank_up_G must depend on 01_opening.");
        RuntimeAssertions.Require(
            rankUpG.AdvancesPhase == Elin_SukutsuArena.Quests.StoryPhase.Initiation,
            "02_rank_up_G must advance to Initiation.");

        CriticalCaseHelpers.RequireQuestDependsOn(
            vsAstaroth,
            "12_rank_up_A",
            "17_vs_astaroth must depend on 12_rank_up_A.");

        CriticalCaseHelpers.RequireQuestDependsOn(
            lastBattle,
            "17_vs_astaroth",
            "18_last_battle must depend on 17_vs_astaroth.");
        RuntimeAssertions.Require(
            lastBattle.AdvancesPhase == Elin_SukutsuArena.Quests.StoryPhase.Postgame,
            "18_last_battle must advance to Postgame.");

        CriticalCaseHelpers.RequireQuestDependsOn(
            resurrectionExec,
            "pg_02a_resurrection_intro",
            "pg_02b_resurrection_execution must depend on pg_02a_resurrection_intro.");

        CriticalCaseHelpers.RequireQuestDependsOn(
            scrollShowcase,
            "pg_02b_resurrection_execution",
            "pg_03_scroll_showcase must depend on pg_02b_resurrection_execution.");
        RuntimeAssertions.Require(
            string.Equals(scrollShowcase.QuestGiver, Elin_SukutsuArena.Quests.ArenaNpcIds.Lily, System.StringComparison.Ordinal),
            "pg_03_scroll_showcase must be given by Lily.");

        var autoQuests = Elin_SukutsuArena.Quests.ArenaQuestDatabase.AutoTriggerQuests;
        RuntimeAssertions.Require(autoQuests != null, "ArenaQuestDatabase.AutoTriggerQuests is null.");
        RuntimeAssertions.Require(autoQuests.Count > 0, "ArenaQuestDatabase.AutoTriggerQuests is empty.");
        RuntimeAssertions.Require(
            string.Equals(autoQuests[0].QuestId, "01_opening", System.StringComparison.Ordinal),
            "First auto-trigger quest must remain 01_opening.");

        RuntimeAssertions.Require(
            resurrectionIntro.Phase == Elin_SukutsuArena.Quests.StoryPhase.Postgame,
            "pg_02a_resurrection_intro phase must remain Postgame.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}

public sealed class QuestManagerPhaseContractCase : RuntimeCaseBase
{
    public override string Id => "quest.manager.phase_contract";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "quest" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var manager = Elin_SukutsuArena.ArenaQuestManager.Instance;
        RuntimeAssertions.Require(manager != null, "ArenaQuestManager.Instance is null.");
        ctx.Set("manager", manager);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var manager = ctx.Get<Elin_SukutsuArena.ArenaQuestManager>("manager");
        var allQuests = manager.GetAllQuests();
        var availableQuests = manager.GetAvailableQuests();
        var npcsWithQuests = manager.GetNpcsWithQuests();

        ctx.Set("allQuestCount", allQuests != null ? allQuests.Count : 0);
        ctx.Set("availableQuestCount", availableQuests != null ? availableQuests.Count : -1);
        ctx.Set("npcWithQuestCount", npcsWithQuests != null ? npcsWithQuests.Count : -1);
        ctx.Set("currentPhaseValue", (int)manager.GetCurrentPhase());
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        int allQuestCount = ctx.Get<int>("allQuestCount");
        int availableQuestCount = ctx.Get<int>("availableQuestCount");
        int npcWithQuestCount = ctx.Get<int>("npcWithQuestCount");
        int currentPhaseValue = ctx.Get<int>("currentPhaseValue");

        RuntimeAssertions.Require(allQuestCount >= 20, "ArenaQuestManager loaded too few quests: " + allQuestCount);
        RuntimeAssertions.Require(availableQuestCount >= 0, "ArenaQuestManager.GetAvailableQuests returned null.");
        RuntimeAssertions.Require(npcWithQuestCount >= 0, "ArenaQuestManager.GetNpcsWithQuests returned null.");
        RuntimeAssertions.Require(currentPhaseValue >= 0 && currentPhaseValue <= 7, "Current phase out of range: " + currentPhaseValue);

        var flagEnumCount = System.Enum.GetValues(typeof(Elin_SukutsuArena.Flags.Phase)).Length;
        var questEnumCount = System.Enum.GetValues(typeof(Elin_SukutsuArena.Quests.StoryPhase)).Length;
        RuntimeAssertions.Require(flagEnumCount == questEnumCount, "Phase enum length mismatch: Flags=" + flagEnumCount + " Quests=" + questEnumCount);

        var manager = ctx.Get<Elin_SukutsuArena.ArenaQuestManager>("manager");
        RuntimeAssertions.Require(
            manager.GetQuest("01_opening") != null,
            "ArenaQuestManager.GetQuest could not resolve 01_opening.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}
