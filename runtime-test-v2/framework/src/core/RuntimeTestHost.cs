using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Runtime Test V2 execution host.
public sealed class RuntimeTestHost
{
    public IEnumerator RunAsync(
        IReadOnlyList<IRuntimeCase> cases,
        string caseIdFilter,
        string tagFilter,
        string modRoot,
        List<RuntimeCaseResult> results)
    {
        if (results == null)
            throw new InvalidOperationException("results is null.");

        if (cases == null || cases.Count == 0)
            yield break;

        for (int i = 0; i < cases.Count; i++)
        {
            var testCase = cases[i];
            if (testCase == null)
                continue;

            if (!ShouldRun(testCase, caseIdFilter, tagFilter))
                continue;

            RuntimeCaseResult result = null;
            yield return RunOneCaseAsync(testCase, modRoot, completed => result = completed);
            if (result != null)
                results.Add(result);
        }
    }

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

    private IEnumerator RunOneCaseAsync(
        IRuntimeCase testCase,
        string modRoot,
        Action<RuntimeCaseResult> onCompleted)
    {
        var result = new RuntimeCaseResult
        {
            id = testCase.Id
        };

        var ctx = new RuntimeTestContext(testCase.Id, modRoot);
        float start = Time.realtimeSinceStartup;
        string step = "prepare";
        Exception stepException = null;
        IRuntimeCoroutineCase coroutineCase = testCase as IRuntimeCoroutineCase;

        ctx.Log("prepare:start");
        if (coroutineCase != null)
        {
            yield return RunEnumeratorSafe(() => coroutineCase.PrepareAsync(ctx), ex => stepException = ex);
        }
        else
        {
            TryRunSync(() => testCase.Prepare(ctx), ex => stepException = ex);
        }
        if (stepException == null)
        {
            ctx.Log("prepare:ok");
        }

        if (stepException == null)
        {
            step = "execute";
            ctx.Log("execute:start");
            if (coroutineCase != null)
            {
                yield return RunEnumeratorSafe(() => coroutineCase.ExecuteAsync(ctx), ex => stepException = ex);
            }
            else
            {
                TryRunSync(() => testCase.Execute(ctx), ex => stepException = ex);
            }

            if (stepException == null)
            {
                ctx.Log("execute:ok");
            }
        }

        if (stepException == null)
        {
            step = "verify";
            ctx.Log("verify:start");
            if (coroutineCase != null)
            {
                yield return RunEnumeratorSafe(() => coroutineCase.VerifyAsync(ctx), ex => stepException = ex);
            }
            else
            {
                TryRunSync(() => testCase.Verify(ctx), ex => stepException = ex);
            }

            if (stepException == null)
            {
                ctx.Log("verify:ok");
            }
        }

        if (stepException != null)
        {
            result.status = "failed";
            result.failureStep = step;
            result.reason = stepException.GetType().Name + ": " + stepException.Message;
            AppendLog(result.logs, stepException.ToString());
        }

        Exception cleanupException = null;
        ctx.Log("cleanup:start");
        if (coroutineCase != null)
        {
            yield return RunEnumeratorSafe(() => coroutineCase.CleanupAsync(ctx), ex => cleanupException = ex);
        }
        else
        {
            TryRunSync(() => testCase.Cleanup(ctx), ex => cleanupException = ex);
        }

        if (cleanupException == null)
        {
            ctx.Log("cleanup:ok");
        }
        else
        {
            if (result.status == "passed")
            {
                result.status = "failed";
                result.failureStep = "cleanup";
                result.reason = cleanupException.GetType().Name + ": " + cleanupException.Message;
            }

            AppendLog(result.logs, "Cleanup exception: " + cleanupException);
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

        for (int logIndex = 0; logIndex < ctx.Logs.Count; logIndex++)
            AppendLog(result.logs, ctx.Logs[logIndex]);

        result.durationMs = ToMs(Time.realtimeSinceStartup - start);
        onCompleted?.Invoke(result);
    }

    private static IEnumerator RunEnumeratorSafe(Func<IEnumerator> routineFactory, Action<Exception> onException)
    {
        IEnumerator routine = null;
        try
        {
            routine = routineFactory != null ? routineFactory() : null;
        }
        catch (Exception ex)
        {
            onException?.Invoke(ex);
            yield break;
        }

        if (routine == null)
            yield break;

        while (true)
        {
            bool moved;
            object current = null;
            try
            {
                moved = routine.MoveNext();
                if (moved)
                    current = routine.Current;
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
                yield break;
            }

            if (!moved)
                yield break;

            yield return current;
        }
    }

    private static void TryRunSync(Action action, Action<Exception> onException)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            onException?.Invoke(ex);
        }
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
