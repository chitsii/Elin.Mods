using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

// MonoBehaviour entry point for runtime suite v2.
public sealed class RuntimeTestRunnerV2 : MonoBehaviour
{
    private readonly List<RuntimeCaseResult> _caseResults = new List<RuntimeCaseResult>();
    private string _runStatus = "passed";
    private string _runFailureReason = string.Empty;
    private string _pcName = string.Empty;
    private DateTime _startedUtc;
    private float _startedRealtime;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        _startedUtc = DateTime.UtcNow;
        _startedRealtime = Time.realtimeSinceStartup;

        try
        {
            Run();
        }
        catch (Exception ex)
        {
            _runStatus = "failed";
            _runFailureReason = "runner_exception";
            _caseResults.Add(new RuntimeCaseResult
            {
                id = "runner",
                status = "failed",
                failureStep = "run",
                reason = ex.GetType().Name + ": " + ex.Message,
                durationMs = 0
            });
        }
        finally
        {
            WriteResultFile();
            Destroy(gameObject);
        }
    }

    private void Run()
    {
        if (EClass.pc == null)
        {
            _runStatus = "failed";
            _runFailureReason = "pc_unavailable";
            return;
        }

        _pcName = EClass.pc.Name ?? string.Empty;
        if (!string.IsNullOrEmpty(RuntimeV2Config.RequiredNameContains) &&
            _pcName.IndexOf(RuntimeV2Config.RequiredNameContains, StringComparison.Ordinal) < 0)
        {
            _runStatus = "failed";
            _runFailureReason = "save_guard_rejected";
            return;
        }

        var cases = DiscoverRuntimeCases();
        var host = new RuntimeTestHost();
        var results = host.Run(
            cases,
            RuntimeV2Config.CaseIdFilter,
            RuntimeV2Config.TagFilter,
            RuntimeV2Config.ModRoot);
        _caseResults.AddRange(results);

        if (_caseResults.Count == 0)
        {
            _runStatus = "failed";
            _runFailureReason = "no_cases_selected";
            return;
        }

        for (int i = 0; i < _caseResults.Count; i++)
        {
            if (_caseResults[i].status != "passed")
            {
                _runStatus = "failed";
                if (string.IsNullOrEmpty(_runFailureReason))
                    _runFailureReason = "case_failed";
                break;
            }
        }
    }

    private void WriteResultFile()
    {
        try
        {
            var path = RuntimeV2Config.ResultPath;
            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir))
                dir = ".";
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, BuildResultJson(), new UTF8Encoding(false));
            Debug.Log("[RuntimeTestV2] result written: " + path);
        }
        catch (Exception ex)
        {
            Debug.LogError("[RuntimeTestV2] failed to write result: " + ex);
        }
    }

    private static IReadOnlyList<IRuntimeCase> DiscoverRuntimeCases()
    {
        var discovered = new List<IRuntimeCase>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var typeNames = new List<string>();
        var nameToType = new Dictionary<string, Type>(StringComparer.Ordinal);

        var asm = typeof(RuntimeTestRunnerV2).Assembly;
        var types = asm.GetTypes();
        for (int i = 0; i < types.Length; i++)
        {
            var t = types[i];
            if (t == null || t.IsAbstract || t.IsInterface)
                continue;
            if (!typeof(IRuntimeCase).IsAssignableFrom(t))
                continue;
            if (t.GetConstructor(Type.EmptyTypes) == null)
                continue;

            var typeName = t.FullName ?? t.Name;
            if (nameToType.ContainsKey(typeName))
                continue;

            nameToType[typeName] = t;
            typeNames.Add(typeName);
        }

        typeNames.Sort(StringComparer.Ordinal);

        for (int i = 0; i < typeNames.Count; i++)
        {
            var typeName = typeNames[i];
            Type t;
            if (!nameToType.TryGetValue(typeName, out t))
                continue;

            IRuntimeCase instance = null;
            try
            {
                instance = Activator.CreateInstance(t) as IRuntimeCase;
            }
            catch
            {
                continue;
            }

            if (instance == null || string.IsNullOrEmpty(instance.Id))
                continue;

            if (seenIds.Contains(instance.Id))
                continue;

            seenIds.Add(instance.Id);
            discovered.Add(instance);
        }

        return discovered;
    }

    private string BuildResultJson()
    {
        int passed = 0;
        int failed = 0;
        for (int i = 0; i < _caseResults.Count; i++)
        {
            if (_caseResults[i].status == "passed")
                passed++;
            else
                failed++;
        }

        var sb = new StringBuilder();
        sb.Append('{');
        AppendJsonProp(sb, "suite", "runtime_v2");
        sb.Append(',');
        AppendJsonProp(sb, "status", _runStatus);
        sb.Append(',');
        AppendJsonProp(sb, "required_name_contains", RuntimeV2Config.RequiredNameContains);
        sb.Append(',');
        AppendJsonProp(sb, "pc_name", _pcName);
        sb.Append(',');
        AppendJsonProp(sb, "run_failure_reason", _runFailureReason);
        sb.Append(',');
        AppendJsonProp(sb, "started_utc", _startedUtc.ToString("o"));
        sb.Append(',');
        AppendJsonProp(sb, "duration_ms", ToMs(Time.realtimeSinceStartup - _startedRealtime));
        sb.Append(',');
        sb.Append("\"filters\":{");
        AppendJsonProp(sb, "case_id", RuntimeV2Config.CaseIdFilter);
        sb.Append(',');
        AppendJsonProp(sb, "tag", RuntimeV2Config.TagFilter);
        sb.Append('}');
        sb.Append(',');
        sb.Append("\"summary\":{");
        AppendJsonProp(sb, "total", _caseResults.Count);
        sb.Append(',');
        AppendJsonProp(sb, "passed", passed);
        sb.Append(',');
        AppendJsonProp(sb, "failed", failed);
        sb.Append('}');
        sb.Append(',');
        sb.Append("\"cases\":[");

        for (int i = 0; i < _caseResults.Count; i++)
        {
            if (i > 0)
                sb.Append(',');

            var c = _caseResults[i];
            sb.Append('{');
            AppendJsonProp(sb, "id", c.id);
            sb.Append(',');
            AppendJsonProp(sb, "status", c.status);
            sb.Append(',');
            AppendJsonProp(sb, "failure_step", c.failureStep);
            sb.Append(',');
            AppendJsonProp(sb, "reason", c.reason);
            sb.Append(',');
            AppendJsonProp(sb, "duration_ms", c.durationMs);
            sb.Append(',');
            sb.Append("\"logs\":[");
            for (int l = 0; l < c.logs.Count; l++)
            {
                if (l > 0)
                    sb.Append(',');
                sb.Append('"').Append(JsonEscape(c.logs[l])).Append('"');
            }
            sb.Append("]}");
        }

        sb.Append("]}");
        return sb.ToString();
    }

    private static int ToMs(float seconds)
    {
        if (seconds <= 0f)
            return 0;
        return (int)(seconds * 1000f);
    }

    private static void AppendJsonProp(StringBuilder sb, string key, string value)
    {
        sb.Append('"').Append(JsonEscape(key)).Append("\":\"");
        sb.Append(JsonEscape(value ?? string.Empty));
        sb.Append('"');
    }

    private static void AppendJsonProp(StringBuilder sb, string key, int value)
    {
        sb.Append('"').Append(JsonEscape(key)).Append("\":");
        sb.Append(value);
    }

    private static string JsonEscape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length + 16);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < 32)
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }
}
