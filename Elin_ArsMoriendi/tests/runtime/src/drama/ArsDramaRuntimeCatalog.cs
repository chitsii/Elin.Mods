using System.Collections.Generic;

// Drama-focused runtime case catalog.
public sealed class ArsDramaRuntimeCatalog : IDramaCaseProvider
{
    // 標準プロファイル: 目標カバレッジ85%で時間を抑える。
    private const float DefaultTimeoutSeconds = 16f;
    private const int DefaultMaxBranchRuns = 10;
    private const int DefaultMaxChoiceStepsPerRun = 100;
    private const int DefaultMaxQueuedPlans = 96;
    private const float DefaultTargetCoverageRatio = 0.85f;

    public IReadOnlyList<DramaCaseDefinition> BuildCases()
    {
        return new DramaCaseDefinition[]
        {
            StandardCase("ars_apotheosis"),
            StandardCase("ars_cinder_records"),
            StandardCase("ars_dormant_flavor"),
            StandardCase("ars_erenos_ambush"),
            StandardCase("ars_erenos_appear"),
            StandardCase("ars_erenos_defeat"),
            StandardCase("ars_first_servant"),
            StandardCase("ars_first_soul"),
            Case(
                "ars_hecatia",
                timeoutSeconds: 20f,
                maxBranchRuns: 12,
                maxChoiceStepsPerRun: 160,
                maxQueuedPlans: 192,
                targetCoverageRatio: 0.85f,
                requiredNpcIds: new[] { "ars_hecatia" }),
            StandardCase("ars_karen_ambush"),
            StandardCase("ars_karen_encounter"),
            StandardCase("ars_karen_retreat"),
            StandardCase("ars_karen_shadow"),
            StandardCase("ars_scout_ambush"),
            StandardCase("ars_scout_encounter"),
            StandardCase("ars_servant_lost"),
            StandardCase("ars_servant_rampage"),
            StandardCase("ars_seventh_sign"),
            StandardCase("ars_stigmata"),
            StandardCase("ars_tome_awakening"),
        };
    }

    // Backward-compatible helper for legacy callers.
    public static IReadOnlyList<DramaCaseDefinition> Build()
    {
        return new ArsDramaRuntimeCatalog().BuildCases();
    }

    private static DramaCaseDefinition StandardCase(string dramaId)
    {
        return Case(
            dramaId,
            timeoutSeconds: DefaultTimeoutSeconds,
            maxBranchRuns: DefaultMaxBranchRuns,
            maxChoiceStepsPerRun: DefaultMaxChoiceStepsPerRun,
            maxQueuedPlans: DefaultMaxQueuedPlans,
            targetCoverageRatio: DefaultTargetCoverageRatio);
    }

    private static DramaCaseDefinition Case(
        string dramaId,
        float timeoutSeconds = DefaultTimeoutSeconds,
        int maxBranchRuns = DefaultMaxBranchRuns,
        int maxChoiceStepsPerRun = DefaultMaxChoiceStepsPerRun,
        int maxQueuedPlans = DefaultMaxQueuedPlans,
        float targetCoverageRatio = DefaultTargetCoverageRatio,
        IReadOnlyList<string> requiredNpcIds = null,
        IReadOnlyList<DramaFlagOverride> setupFlags = null)
    {
        return new DramaCaseDefinition
        {
            DramaId = dramaId ?? string.Empty,
            TimeoutSeconds = timeoutSeconds,
            MaxBranchRuns = maxBranchRuns,
            MaxChoiceStepsPerRun = maxChoiceStepsPerRun,
            MaxQueuedPlans = maxQueuedPlans,
            TargetCoverageRatio = targetCoverageRatio,
            RequiredNpcIds = requiredNpcIds ?? new string[0],
            SetupFlags = setupFlags ?? new DramaFlagOverride[0],
        };
    }
}
