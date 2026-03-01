using System.Collections.Generic;

public sealed class RuntimeCaseResult
{
    public string id = string.Empty;
    public string status = "passed";
    public string failureStep = string.Empty;
    public string reason = string.Empty;
    public int durationMs;
    public readonly List<string> logs = new List<string>();
}
