using System.Collections.Generic;

// Quest runtime drama catalog (placeholder; replace in actual mod).
public sealed class QuestDramaCatalog : IDramaCaseProvider
{
    private const string PlaceholderDramaId = "quest_drama_replace_me";
    private const string FeatureShowcaseDramaId = "quest_drama_feature_showcase";
    private const string FeatureShowcaseFollowupDramaId = "quest_drama_feature_followup";
    private const string FeatureBranchADramaId = "quest_drama_feature_branch_a";
    private const string FeatureBranchBDramaId = "quest_drama_feature_branch_b";
    private const string FeatureMergeDramaId = "quest_drama_feature_merge";

    public IReadOnlyList<DramaCaseDefinition> BuildCases()
    {
        var cases = new List<DramaCaseDefinition>
        {
            new DramaCaseDefinition
            {
                DramaId = PlaceholderDramaId,
                TimeoutSeconds = 20f,
                MaxBranchRuns = 4,
                MaxChoiceStepsPerRun = 120,
                MaxQueuedPlans = 64,
                TargetCoverageRatio = 0.80f,
                RequiredNpcIds = new string[0],
                SetupFlags = new DramaFlagOverride[0],
            }
        };

        cases.Add(new DramaCaseDefinition
        {
            DramaId = FeatureShowcaseDramaId,
            TimeoutSeconds = 25f,
            MaxBranchRuns = 2,
            MaxChoiceStepsPerRun = 80,
            MaxQueuedPlans = 16,
            TargetCoverageRatio = 0.60f,
            RequiredNpcIds = new string[0],
            SetupFlags = new DramaFlagOverride[0],
        });

        cases.Add(new DramaCaseDefinition
        {
            DramaId = FeatureShowcaseFollowupDramaId,
            TimeoutSeconds = 25f,
            MaxBranchRuns = 2,
            MaxChoiceStepsPerRun = 80,
            MaxQueuedPlans = 8,
            TargetCoverageRatio = 0.70f,
            RequiredNpcIds = new string[0],
            SetupFlags = new DramaFlagOverride[0],
        });

        cases.Add(new DramaCaseDefinition
        {
            DramaId = FeatureBranchADramaId,
            TimeoutSeconds = 15f,
            MaxBranchRuns = 1,
            MaxChoiceStepsPerRun = 20,
            MaxQueuedPlans = 8,
            TargetCoverageRatio = 1.0f,
            RequiredNpcIds = new string[0],
            SetupFlags = new DramaFlagOverride[0],
        });

        cases.Add(new DramaCaseDefinition
        {
            DramaId = FeatureBranchBDramaId,
            TimeoutSeconds = 15f,
            MaxBranchRuns = 1,
            MaxChoiceStepsPerRun = 20,
            MaxQueuedPlans = 8,
            TargetCoverageRatio = 1.0f,
            RequiredNpcIds = new string[0],
            SetupFlags = new DramaFlagOverride[0],
        });

        cases.Add(new DramaCaseDefinition
        {
            DramaId = FeatureMergeDramaId,
            TimeoutSeconds = 15f,
            MaxBranchRuns = 1,
            MaxChoiceStepsPerRun = 20,
            MaxQueuedPlans = 8,
            TargetCoverageRatio = 1.0f,
            RequiredNpcIds = new string[0],
            SetupFlags = new DramaFlagOverride[0],
        });

        return cases;
    }
}
