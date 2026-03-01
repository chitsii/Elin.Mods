using System;
using System.Collections.Generic;

// Verifies placeholder drama provider output for quest runtime runner.
public sealed class DramaCatalogContractCase : RuntimeCaseBase
{
    private const string PlaceholderDramaId = "quest_drama_replace_me";
    private const string FeatureShowcaseDramaId = "quest_drama_feature_showcase";
    private const string FeatureShowcaseFollowupDramaId = "quest_drama_feature_followup";
    private const string FeatureBranchADramaId = "quest_drama_feature_branch_a";
    private const string FeatureBranchBDramaId = "quest_drama_feature_branch_b";
    private const string FeatureMergeDramaId = "quest_drama_feature_merge";

    public override string Id => "questmod.drama.catalog.placeholder_contract";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "drama" };

    public override void Prepare(RuntimeTestContext ctx)
    {
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var provider = new QuestDramaCatalog();
        RuntimeAssertions.Require(provider != null, "Drama provider is null.");

        var cases = provider.BuildCases();
        RuntimeAssertions.Require(cases != null, "Drama cases are null.");
        RuntimeAssertions.Require(cases.Count > 0, "Drama case list is empty.");
        ctx.Set("cases", cases);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var cases = ctx.Get<IReadOnlyList<DramaCaseDefinition>>("cases");

        DramaCaseDefinition target = null;
        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            if (c != null && string.Equals(c.DramaId, PlaceholderDramaId, StringComparison.Ordinal))
            {
                target = c;
                break;
            }
        }

        RuntimeAssertions.Require(target != null, PlaceholderDramaId + " case is missing.");
        RuntimeAssertions.Require(target.TimeoutSeconds > 0f, "TimeoutSeconds must be > 0.");
        RuntimeAssertions.Require(target.MaxBranchRuns > 0, "MaxBranchRuns must be > 0.");
        RuntimeAssertions.Require(target.MaxChoiceStepsPerRun > 0, "MaxChoiceStepsPerRun must be > 0.");
        RuntimeAssertions.Require(target.MaxQueuedPlans > 0, "MaxQueuedPlans must be > 0.");
        RuntimeAssertions.Require(
            target.TargetCoverageRatio > 0f && target.TargetCoverageRatio <= 1f,
            "TargetCoverageRatio must be in (0, 1].");
        RuntimeAssertions.Require(
            target.RequiredNpcIds == null || target.RequiredNpcIds.Count == 0,
            "Placeholder drama should not require NPC IDs.");

        bool featureShowcasePresent = false;
        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            if (c != null && string.Equals(c.DramaId, FeatureShowcaseDramaId, StringComparison.Ordinal))
            {
                featureShowcasePresent = true;
                break;
            }
        }

        RuntimeAssertions.Require(
            featureShowcasePresent,
            FeatureShowcaseDramaId + " case is missing.");

        bool followupPresent = false;
        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            if (c != null && string.Equals(c.DramaId, FeatureShowcaseFollowupDramaId, StringComparison.Ordinal))
            {
                followupPresent = true;
                break;
            }
        }

        RuntimeAssertions.Require(
            followupPresent,
            FeatureShowcaseFollowupDramaId + " case is missing.");

        bool branchAPresent = false;
        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            if (c != null && string.Equals(c.DramaId, FeatureBranchADramaId, StringComparison.Ordinal))
            {
                branchAPresent = true;
                break;
            }
        }

        RuntimeAssertions.Require(
            branchAPresent,
            FeatureBranchADramaId + " case is missing.");

        bool branchBPresent = false;
        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            if (c != null && string.Equals(c.DramaId, FeatureBranchBDramaId, StringComparison.Ordinal))
            {
                branchBPresent = true;
                break;
            }
        }

        RuntimeAssertions.Require(
            branchBPresent,
            FeatureBranchBDramaId + " case is missing.");

        bool mergePresent = false;
        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            if (c != null && string.Equals(c.DramaId, FeatureMergeDramaId, StringComparison.Ordinal))
            {
                mergePresent = true;
                break;
            }
        }

        RuntimeAssertions.Require(
            mergePresent,
            FeatureMergeDramaId + " case is missing.");

        ctx.Log("Drama catalog contract verified.");
    }
}

