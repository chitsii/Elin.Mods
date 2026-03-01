using System;
using System.Collections.Generic;
using UnityEngine;

// Runtime Test V2 execution host.
public sealed class RuntimeTestHost
{
    public List<RuntimeCaseResult> Run(
        IReadOnlyList<IRuntimeCase> cases,
        string caseIdFilter,
        string tagFilter,
        string modRoot)
    {
        var results = new List<RuntimeCaseResult>();
        if (cases == null || cases.Count == 0)
            return results;

        for (int i = 0; i < cases.Count; i++)
        {
            var testCase = cases[i];
            if (testCase == null)
                continue;

            if (!ShouldRun(testCase, caseIdFilter, tagFilter))
                continue;

            var result = new RuntimeCaseResult
            {
                id = testCase.Id
            };

            var ctx = new RuntimeTestContext(testCase.Id, modRoot);
            float start = Time.realtimeSinceStartup;
            string step = "prepare";

            try
            {
                ctx.Log("prepare:start");
                testCase.Prepare(ctx);
                ctx.Log("prepare:ok");

                step = "execute";
                ctx.Log("execute:start");
                testCase.Execute(ctx);
                ctx.Log("execute:ok");

                step = "verify";
                ctx.Log("verify:start");
                testCase.Verify(ctx);
                ctx.Log("verify:ok");
            }
            catch (Exception ex)
            {
                result.status = "failed";
                result.failureStep = step;
                result.reason = ex.GetType().Name + ": " + ex.Message;
                AppendLog(result.logs, ex.ToString());
            }
            finally
            {
                try
                {
                    ctx.Log("cleanup:start");
                    testCase.Cleanup(ctx);
                    ctx.Log("cleanup:ok");
                }
                catch (Exception cleanupEx)
                {
                    if (result.status == "passed")
                    {
                        result.status = "failed";
                        result.failureStep = "cleanup";
                        result.reason = cleanupEx.GetType().Name + ": " + cleanupEx.Message;
                    }

                    AppendLog(result.logs, "Cleanup exception: " + cleanupEx);
                }

                try
                {
                    var rollbackErrors = ctx.RunRollback();
                    if (rollbackErrors != null && rollbackErrors.Count > 0)
                    {
                        if (result.status == "passed")
                        {
                            result.status = "failed";
                            result.failureStep = "rollback";
                            result.reason = "Rollback failed.";
                        }

                        for (int e = 0; e < rollbackErrors.Count; e++)
                            AppendLog(result.logs, rollbackErrors[e]);
                    }
                }
                catch (Exception rollbackEx)
                {
                    if (result.status == "passed")
                    {
                        result.status = "failed";
                        result.failureStep = "rollback";
                        result.reason = rollbackEx.GetType().Name + ": " + rollbackEx.Message;
                    }

                    AppendLog(result.logs, "Rollback exception: " + rollbackEx);
                }
            }

            for (int logIndex = 0; logIndex < ctx.Logs.Count; logIndex++)
                AppendLog(result.logs, ctx.Logs[logIndex]);

            result.durationMs = ToMs(Time.realtimeSinceStartup - start);
            results.Add(result);
        }

        return results;
    }

    private static bool ShouldRun(IRuntimeCase testCase, string caseIdFilter, string tagFilter)
    {
        if (testCase == null)
            return false;

        if (!string.IsNullOrEmpty(caseIdFilter) &&
            !string.Equals(testCase.Id, caseIdFilter, StringComparison.Ordinal))
            return false;

        if (string.IsNullOrEmpty(tagFilter))
            return true;

        var tags = testCase.Tags;
        if (tags == null || tags.Count == 0)
            return false;

        for (int i = 0; i < tags.Count; i++)
        {
            if (string.Equals(tags[i], tagFilter, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static int ToMs(float seconds)
    {
        if (seconds <= 0f)
            return 0;
        return (int)(seconds * 1000f);
    }

    private static void AppendLog(List<string> logs, string value)
    {
        if (logs == null)
            return;
        if (logs.Count >= 64)
            return;
        logs.Add(value ?? string.Empty);
    }
}
