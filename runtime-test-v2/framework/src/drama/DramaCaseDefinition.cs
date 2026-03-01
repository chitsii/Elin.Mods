using System.Collections.Generic;

// Provider contract implemented by each mod to expose drama case definitions.
public interface IDramaCaseProvider
{
    IReadOnlyList<DramaCaseDefinition> BuildCases();
}

// Drama runtime test case definition (code-first).
public sealed class DramaCaseDefinition
{
    public string DramaId { get; set; } = string.Empty;

    public float TimeoutSeconds { get; set; } = 30f;

    public int MaxBranchRuns { get; set; } = 16;

    // Upper bound for handled choice events per single run.
    public int MaxChoiceStepsPerRun { get; set; } = 120;

    // Upper bound for queued alternative plans to prevent unbounded growth.
    public int MaxQueuedPlans { get; set; } = 64;

    // Best-effort branch coverage target. The runner may stop early once reached.
    public float TargetCoverageRatio { get; set; } = 0.8f;

    public IReadOnlyList<string> RequiredNpcIds { get; set; } = new string[0];

    public IReadOnlyList<DramaFlagOverride> SetupFlags { get; set; } = new DramaFlagOverride[0];
}

public sealed class DramaFlagOverride
{
    public string Key { get; set; } = string.Empty;

    public int Value { get; set; }
}
