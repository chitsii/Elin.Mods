using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

public sealed class DramaRuntimeRunnerV2 : MonoBehaviour
{
    private static readonly FieldInfo CurrentEventIdField = typeof(DramaSequence).GetField(
        "currentEventID",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo CurrentEventField = typeof(DramaSequence).GetField(
        "currentEvent",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly List<CaseResult> _cases = new List<CaseResult>();
    private readonly List<string> _activeErrors = new List<string>();
    private readonly List<DramaCaseDefinition> _selectedCases = new List<DramaCaseDefinition>();

    private bool _captureErrors;
    private string _activeDramaId = "";
    private string _pcName = "";
    private string _runStatus = "passed";
    private string _runFailureReason = "";
    private float _runStartRealtime;
    private DateTime _runStartedUtc;
    private string _baselineSaveId = "";
    private bool _baselineSaveCloud;
    private bool _suiteCleanupReloadOk = true;

    private const int MaxErrorsPerDrama = 20;
    private const int StuckFrameThreshold = 30;
    private const int MaxForcedAdvancesPerRun = 40;
    private const float DefaultPerDramaTimeoutSeconds = 30f;
    private const int DefaultMaxBranchRunsPerDrama = 16;
    private const int DefaultMaxChoiceStepsPerRun = 120;
    private const int DefaultMaxQueuedPlans = 64;
    private const float DefaultTargetCoverageRatio = 0.8f;
    private const float ReloadTimeoutSeconds = 15f;
    private const string SomewhereZoneId = "somewhere";

    private sealed class CaseResult
    {
        public string dramaId = "";
        public string status = "passed";
        public string reason = "";
        public bool reloaded;
        public bool setupApplied;
        public bool started;
        public bool timeout;
        public bool branchTruncated;
        public int forcedAdvances;
        public int autoChoices;
        public int durationMs;
        public int runCount;
        public int exploredPlans;
        public int loopCuts;
        public int discoveredChoiceEdges;
        public int coveredChoiceEdges;
        public int coveragePermille;
        public bool coverageTargetReached;
        public bool cleanupApplied;
        public readonly List<string> logs = new List<string>();
        public readonly List<string> errors = new List<string>();
    }

    private sealed class PlanRunResult
    {
        public bool started;
        public bool finished;
        public bool timeout;
        public int forcedAdvances;
        public int autoChoices;
        public int durationMs;
        public int loopCuts;
        public readonly List<string> errors = new List<string>();
    }

    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        _runStartedUtc = DateTime.UtcNow;
        _runStartRealtime = Time.realtimeSinceStartup;
        Application.logMessageReceived += OnLogMessage;

        try
        {
            yield return RunInternal();
        }
        finally
        {
            Application.logMessageReceived -= OnLogMessage;
            SafeExitDramaLayer();
            WriteResultFile();
            Destroy(gameObject);
        }
    }

    private IEnumerator RunInternal()
    {
        if (EClass.pc == null || EClass.game == null)
        {
            _runStatus = "failed";
            _runFailureReason = "pc_or_game_unavailable";
            yield break;
        }

        _pcName = EClass.pc.Name ?? "";
        if (!string.IsNullOrEmpty(RuntimeV2Config.RequiredNameContains)
            && _pcName.IndexOf(RuntimeV2Config.RequiredNameContains, StringComparison.Ordinal) < 0)
        {
            _runStatus = "failed";
            _runFailureReason = "save_guard_rejected";
            yield break;
        }

        _baselineSaveId = Game.id ?? "";
        _baselineSaveCloud = EClass.game.isCloud;
        if (string.IsNullOrEmpty(_baselineSaveId))
        {
            _runStatus = "failed";
            _runFailureReason = "baseline_save_unknown";
            yield break;
        }

        BuildSelectedCases();
        if (_selectedCases.Count == 0)
        {
            _runStatus = "failed";
            _runFailureReason = "no_cases_selected";
            yield break;
        }

        for (int i = 0; i < _selectedCases.Count; i++)
        {
            var definition = _selectedCases[i];
            var caseResult = new CaseResult { dramaId = definition.DramaId };

            yield return ReloadBaselineForCase(caseResult);
            if (caseResult.status == "passed")
            {
                ApplyCaseSetup(definition, caseResult);
            }
            if (caseResult.status == "passed")
            {
                yield return RunOneDrama(definition, caseResult);
            }

            CleanupCaseSetup(definition, caseResult);
            _cases.Add(caseResult);
        }

        for (int i = 0; i < _cases.Count; i++)
        {
            if (_cases[i].status != "passed")
            {
                _runStatus = "failed";
                if (string.IsNullOrEmpty(_runFailureReason))
                {
                    _runFailureReason = "case_failed";
                }
                break;
            }
        }

        yield return RestoreBaselineAfterSuite();
        if (!_suiteCleanupReloadOk && _runStatus == "passed")
        {
            _runStatus = "failed";
            if (string.IsNullOrEmpty(_runFailureReason))
            {
                _runFailureReason = "cleanup_reload_failed";
            }
        }
    }

    private void CleanupCaseSetup(DramaCaseDefinition definition, CaseResult result)
    {
        try
        {
            SafeExitDramaLayer();

            if (definition == null || definition.RequiredNpcIds == null || definition.RequiredNpcIds.Count == 0)
            {
                result.cleanupApplied = true;
                return;
            }

            for (int i = 0; i < definition.RequiredNpcIds.Count; i++)
            {
                string npcId = definition.RequiredNpcIds[i];
                if (string.IsNullOrEmpty(npcId))
                {
                    continue;
                }

                HideNpcToSomewhere(npcId);
                if (result.logs.Count < MaxErrorsPerDrama * 2)
                {
                    result.logs.Add("cleanup:npc:" + npcId);
                }
            }

            result.cleanupApplied = true;
        }
        catch (Exception ex)
        {
            result.cleanupApplied = false;
            result.errors.Add("cleanup_failed: " + ex);
            if (result.status == "passed")
            {
                result.status = "failed";
                result.reason = "cleanup_failed";
            }
        }
    }

    private IEnumerator RestoreBaselineAfterSuite()
    {
        yield return EnsureDramaLayersClosed();

        try
        {
            Game.Load(_baselineSaveId, _baselineSaveCloud);
        }
        catch (Exception ex)
        {
            _suiteCleanupReloadOk = false;
            Debug.LogWarning("[RuntimeTestV2Drama] cleanup reload failed: " + ex.Message);
            yield break;
        }

        float start = Time.realtimeSinceStartup;
        while ((Time.realtimeSinceStartup - start) < ReloadTimeoutSeconds)
        {
            if (EClass.pc != null && EClass._zone != null && EClass.game != null)
            {
                _suiteCleanupReloadOk = true;
                yield break;
            }
            yield return null;
        }

        _suiteCleanupReloadOk = false;
        Debug.LogWarning("[RuntimeTestV2Drama] cleanup reload timed out.");
    }

    private void BuildSelectedCases()
    {
        _selectedCases.Clear();
        var all = DiscoverDramaCases();
        if (all == null)
        {
            return;
        }

        bool filterByCaseId = !string.IsNullOrEmpty(RuntimeV2Config.CaseIdFilter);
        bool useTagFilter = !string.IsNullOrEmpty(RuntimeV2Config.TagFilter);
        bool tagMatchesDrama = !useTagFilter
            || string.Equals(RuntimeV2Config.TagFilter, "drama", StringComparison.OrdinalIgnoreCase);
        if (!tagMatchesDrama)
        {
            return;
        }

        for (int i = 0; i < all.Count; i++)
        {
            var def = all[i];
            if (def == null || string.IsNullOrEmpty(def.DramaId))
            {
                continue;
            }

            if (filterByCaseId &&
                !string.Equals(def.DramaId, RuntimeV2Config.CaseIdFilter, StringComparison.Ordinal))
            {
                continue;
            }

            _selectedCases.Add(def);
        }
    }

    private IEnumerator ReloadBaselineForCase(CaseResult result)
    {
        yield return EnsureDramaLayersClosed();

        try
        {
            Game.Load(_baselineSaveId, _baselineSaveCloud);
        }
        catch (Exception ex)
        {
            result.status = "failed";
            result.reason = "reload_exception";
            result.errors.Add("Game.Load failed: " + ex);
            yield break;
        }

        float start = Time.realtimeSinceStartup;
        while ((Time.realtimeSinceStartup - start) < ReloadTimeoutSeconds)
        {
            if (EClass.pc != null && EClass._zone != null && EClass.game != null)
            {
                result.reloaded = true;
                _pcName = EClass.pc.Name ?? "";

                if (!string.IsNullOrEmpty(RuntimeV2Config.RequiredNameContains)
                    && _pcName.IndexOf(RuntimeV2Config.RequiredNameContains, StringComparison.Ordinal) < 0)
                {
                    result.status = "failed";
                    result.reason = "save_guard_rejected_after_reload";
                }

                yield break;
            }
            yield return null;
        }

        result.status = "failed";
        result.reason = "reload_timeout";
        result.errors.Add("Reload timed out waiting for pc/zone.");
    }

    private void ApplyCaseSetup(DramaCaseDefinition definition, CaseResult result)
    {
        if (definition == null)
        {
            result.status = "failed";
            result.reason = "definition_missing";
            return;
        }

        try
        {
            if (definition.SetupFlags != null && definition.SetupFlags.Count > 0)
            {
                if (EClass.player?.dialogFlags == null)
                {
                    throw new InvalidOperationException("dialogFlags unavailable.");
                }

                for (int i = 0; i < definition.SetupFlags.Count; i++)
                {
                    var f = definition.SetupFlags[i];
                    if (f == null || string.IsNullOrEmpty(f.Key))
                    {
                        continue;
                    }

                    EClass.player.dialogFlags[f.Key] = f.Value;
                    if (result.logs.Count < MaxErrorsPerDrama * 2)
                    {
                        result.logs.Add("setup:flag:" + f.Key + "=" + f.Value);
                    }
                }
            }

            if (definition.RequiredNpcIds != null && definition.RequiredNpcIds.Count > 0)
            {
                for (int i = 0; i < definition.RequiredNpcIds.Count; i++)
                {
                    string npcId = definition.RequiredNpcIds[i];
                    if (string.IsNullOrEmpty(npcId))
                    {
                        continue;
                    }

                    var npc = ShowNpcNearPlayer(npcId);
                    if (npc == null)
                    {
                        throw new InvalidOperationException("Failed to prepare NPC: " + npcId);
                    }

                    if (result.logs.Count < MaxErrorsPerDrama * 2)
                    {
                        result.logs.Add("setup:npc:" + npcId + ":" + npc.uid);
                    }
                }
            }

            result.setupApplied = true;
        }
        catch (Exception ex)
        {
            result.status = "failed";
            result.reason = "setup_failed";
            result.errors.Add(ex.ToString());
        }
    }

    private static IReadOnlyList<DramaCaseDefinition> DiscoverDramaCases()
    {
        var cases = new List<DramaCaseDefinition>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var providerTypeNames = new List<string>();
        var providerTypeMap = new Dictionary<string, Type>(StringComparer.Ordinal);

        var asm = typeof(DramaRuntimeRunnerV2).Assembly;
        var types = asm.GetTypes();
        for (int i = 0; i < types.Length; i++)
        {
            var t = types[i];
            if (t == null || t.IsAbstract || t.IsInterface)
            {
                continue;
            }
            if (!typeof(IDramaCaseProvider).IsAssignableFrom(t))
            {
                continue;
            }
            if (t.GetConstructor(Type.EmptyTypes) == null)
            {
                continue;
            }

            string fullName = t.FullName ?? t.Name;
            if (providerTypeMap.ContainsKey(fullName))
            {
                continue;
            }

            providerTypeMap[fullName] = t;
            providerTypeNames.Add(fullName);
        }

        providerTypeNames.Sort(StringComparer.Ordinal);

        for (int i = 0; i < providerTypeNames.Count; i++)
        {
            string name = providerTypeNames[i];
            Type providerType;
            if (!providerTypeMap.TryGetValue(name, out providerType))
            {
                continue;
            }

            IDramaCaseProvider provider = null;
            try
            {
                provider = Activator.CreateInstance(providerType) as IDramaCaseProvider;
            }
            catch
            {
                continue;
            }

            if (provider == null)
            {
                continue;
            }

            IReadOnlyList<DramaCaseDefinition> built = null;
            try
            {
                built = provider.BuildCases();
            }
            catch
            {
                continue;
            }

            if (built == null)
            {
                continue;
            }

            for (int c = 0; c < built.Count; c++)
            {
                var def = built[c];
                if (def == null || string.IsNullOrEmpty(def.DramaId))
                {
                    continue;
                }
                if (seenIds.Contains(def.DramaId))
                {
                    continue;
                }

                seenIds.Add(def.DramaId);
                cases.Add(def);
            }
        }

        cases.Sort(new DramaCaseDefinitionComparer());
        return cases;
    }

    private sealed class DramaCaseDefinitionComparer : IComparer<DramaCaseDefinition>
    {
        public int Compare(DramaCaseDefinition x, DramaCaseDefinition y)
        {
            string a = x != null ? (x.DramaId ?? string.Empty) : string.Empty;
            string b = y != null ? (y.DramaId ?? string.Empty) : string.Empty;
            return string.Compare(a, b, StringComparison.Ordinal);
        }
    }

    private IEnumerator RunOneDrama(DramaCaseDefinition definition, CaseResult result)
    {
        float caseStart = Time.realtimeSinceStartup;
        int maxBranchRuns = definition == null ? DefaultMaxBranchRunsPerDrama : Math.Max(1, definition.MaxBranchRuns);
        int maxQueuedPlans = definition == null ? DefaultMaxQueuedPlans : Math.Max(4, definition.MaxQueuedPlans);
        float targetCoverage = definition == null ? DefaultTargetCoverageRatio : definition.TargetCoverageRatio;
        if (targetCoverage < 0f)
        {
            targetCoverage = 0f;
        }
        if (targetCoverage > 1f)
        {
            targetCoverage = 1f;
        }
        int targetCoveragePermille = (int)(targetCoverage * 1000f);

        var planStack = new Stack<List<int>>();
        var plannedOrVisited = new HashSet<string>(StringComparer.Ordinal);
        var globallyExpandedChoiceSignatures = new HashSet<string>(StringComparer.Ordinal);
        var discoveredChoiceEdges = new HashSet<string>(StringComparer.Ordinal);
        var coveredChoiceEdges = new HashSet<string>(StringComparer.Ordinal);
        EnqueuePlanIfNew(planStack, plannedOrVisited, new List<int>(), maxQueuedPlans);

        while (planStack.Count > 0)
        {
            if (result.runCount >= maxBranchRuns)
            {
                result.branchTruncated = true;
                if (string.IsNullOrEmpty(result.reason))
                {
                    result.reason = "branch_limit_reached";
                }
                break;
            }

            List<int> plan = planStack.Pop();
            result.exploredPlans++;

            var run = new PlanRunResult();
            yield return RunOnePlan(
                definition,
                plan,
                planStack,
                plannedOrVisited,
                globallyExpandedChoiceSignatures,
                discoveredChoiceEdges,
                coveredChoiceEdges,
                maxQueuedPlans,
                run);

            result.runCount++;
            result.started |= run.started;
            result.timeout |= run.timeout;
            result.forcedAdvances += run.forcedAdvances;
            result.autoChoices += run.autoChoices;
            result.loopCuts += run.loopCuts;

            for (int i = 0; i < run.errors.Count; i++)
            {
                if (result.errors.Count >= MaxErrorsPerDrama * 8)
                {
                    break;
                }
                result.errors.Add(run.errors[i]);
            }

            if (!run.started)
            {
                result.status = "failed";
                result.reason = "start_failed";
                break;
            }

            if (!run.finished || run.timeout)
            {
                result.status = "failed";
                result.reason = "timeout";
                break;
            }

            if (run.errors.Count > 0)
            {
                result.status = "failed";
                result.reason = "runtime_error";
                break;
            }

            result.discoveredChoiceEdges = discoveredChoiceEdges.Count;
            result.coveredChoiceEdges = coveredChoiceEdges.Count;
            result.coveragePermille = ToPermille(result.coveredChoiceEdges, result.discoveredChoiceEdges);
            result.coverageTargetReached = result.coveragePermille >= targetCoveragePermille;

            if (result.coverageTargetReached && result.runCount >= 1)
            {
                if (string.IsNullOrEmpty(result.reason))
                {
                    result.reason = "coverage_target_reached";
                }
                break;
            }
        }

        if (!result.started && result.status == "passed")
        {
            result.status = "failed";
            result.reason = "start_failed";
        }

        result.discoveredChoiceEdges = discoveredChoiceEdges.Count;
        result.coveredChoiceEdges = coveredChoiceEdges.Count;
        result.coveragePermille = ToPermille(result.coveredChoiceEdges, result.discoveredChoiceEdges);
        result.coverageTargetReached = result.coveragePermille >= targetCoveragePermille;

        result.durationMs = ToMs(Time.realtimeSinceStartup - caseStart);
    }

    private IEnumerator RunOnePlan(
        DramaCaseDefinition definition,
        List<int> plan,
        Stack<List<int>> planStack,
        HashSet<string> plannedOrVisited,
        HashSet<string> globallyExpandedChoiceSignatures,
        HashSet<string> discoveredChoiceEdges,
        HashSet<string> coveredChoiceEdges,
        int maxQueuedPlans,
        PlanRunResult result)
    {
        string dramaId = definition == null ? string.Empty : definition.DramaId;
        _activeDramaId = dramaId + "#" + EncodePlan(plan);
        _activeErrors.Clear();
        _captureErrors = true;

        yield return EnsureDramaLayersClosed();

        float start = Time.realtimeSinceStartup;
        bool started = false;

        started = TryPlayDrama(dramaId);

        result.started = started;
        if (!started)
        {
            _captureErrors = false;
            result.durationMs = ToMs(Time.realtimeSinceStartup - start);
            result.errors.AddRange(_activeErrors);
            yield return EnsureDramaLayersClosed();
            yield break;
        }

        bool observedSequence = false;
        bool finished = false;
        int stuckFrames = 0;
        int lastEventId = int.MinValue;
        bool choiceHandleArmed = true;
        int forcedAdvances = 0;
        int autoChoices = 0;
        int loopCuts = 0;
        int choiceDepth = 0;
        var resolvedPath = new List<int>(plan.Count + 8);
        var choiceSignatureVisits = new Dictionary<string, int>(StringComparer.Ordinal);
        var selectedChoiceVisits = new Dictionary<string, int>(StringComparer.Ordinal);
        var selectedDescriptorVisits = new Dictionary<string, int>(StringComparer.Ordinal);
        var unplannedChoiceCursor = new Dictionary<string, int>(StringComparer.Ordinal);
        string lastSelectedChoiceDescriptor = "";
        int consecutiveSameChoiceCount = 0;
        int choiceSteps = 0;
        int maxChoiceStepsPerRun = definition == null
            ? DefaultMaxChoiceStepsPerRun
            : Math.Max(10, definition.MaxChoiceStepsPerRun);

        float timeoutSeconds = (definition == null) ? DefaultPerDramaTimeoutSeconds : Math.Max(2f, definition.TimeoutSeconds);
        while ((Time.realtimeSinceStartup - start) < timeoutSeconds)
        {
            LayerDrama layer = LayerDrama.Instance;
            DramaSequence sequence = (layer != null && layer.drama != null) ? layer.drama.sequence : null;

            if (HandleBlockingTopLayer(layer))
            {
                yield return null;
                continue;
            }

            if (sequence == null || sequence.isExited)
            {
                if (observedSequence || (Time.realtimeSinceStartup - start) > 0.2f)
                {
                    finished = true;
                    break;
                }
                yield return null;
                continue;
            }

            observedSequence = true;

            int eventId = GetCurrentEventId(sequence);
            if (eventId == lastEventId)
            {
                stuckFrames++;
            }
            else
            {
                stuckFrames = 0;
                lastEventId = eventId;
                choiceHandleArmed = true;
            }

            try
            {
                sequence.OnUpdate();
            }
            catch (Exception ex)
            {
                AddActiveError("sequence.OnUpdate threw: " + ex);
            }

            bool stopByLoop = false;
            if (TryHandleChoiceEvent(
                sequence,
                eventId,
                plan,
                resolvedPath,
                ref choiceDepth,
                ref choiceHandleArmed,
                planStack,
                plannedOrVisited,
                choiceSignatureVisits,
                selectedChoiceVisits,
                selectedDescriptorVisits,
                ref lastSelectedChoiceDescriptor,
                ref consecutiveSameChoiceCount,
                unplannedChoiceCursor,
                globallyExpandedChoiceSignatures,
                discoveredChoiceEdges,
                coveredChoiceEdges,
                maxQueuedPlans,
                maxChoiceStepsPerRun,
                ref choiceSteps,
                ref stopByLoop))
            {
                autoChoices++;
                stuckFrames = 0;
                yield return null;
                continue;
            }

            if (stopByLoop)
            {
                loopCuts++;
                finished = true;
                break;
            }

            if (stuckFrames >= StuckFrameThreshold)
            {
                try
                {
                    sequence.PlayNext();
                    forcedAdvances++;
                    if (forcedAdvances >= MaxForcedAdvancesPerRun)
                    {
                        loopCuts++;
                        finished = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    AddActiveError("sequence.PlayNext threw: " + ex);
                }
                stuckFrames = 0;
            }

            if (sequence.isExited)
            {
                finished = true;
                break;
            }

            yield return null;
        }

        // Time budget hit after the sequence was observed:
        // treat it as a controlled loop cut so the case can continue exploring other plans.
        if (!finished && observedSequence)
        {
            loopCuts++;
            yield return EnsureDramaLayersClosed();
            finished = true;
        }

        _captureErrors = false;
        result.finished = finished;
        result.timeout = !finished;
        result.forcedAdvances = forcedAdvances;
        result.autoChoices = autoChoices;
        result.loopCuts = loopCuts;
        result.durationMs = ToMs(Time.realtimeSinceStartup - start);
        result.errors.AddRange(_activeErrors);

        yield return EnsureDramaLayersClosed();
    }

    private bool TryHandleChoiceEvent(
        DramaSequence sequence,
        int eventId,
        List<int> plan,
        List<int> resolvedPath,
        ref int choiceDepth,
        ref bool choiceHandleArmed,
        Stack<List<int>> planStack,
        HashSet<string> plannedOrVisited,
        Dictionary<string, int> choiceSignatureVisits,
        Dictionary<string, int> selectedChoiceVisits,
        Dictionary<string, int> selectedDescriptorVisits,
        ref string lastSelectedChoiceDescriptor,
        ref int consecutiveSameChoiceCount,
        Dictionary<string, int> unplannedChoiceCursor,
        HashSet<string> globallyExpandedChoiceSignatures,
        HashSet<string> discoveredChoiceEdges,
        HashSet<string> coveredChoiceEdges,
        int maxQueuedPlans,
        int maxChoiceStepsPerRun,
        ref int choiceSteps,
        ref bool stopByLoop)
    {
        if (eventId == int.MinValue || !choiceHandleArmed)
        {
            return false;
        }

        DramaEvent current = GetCurrentEvent(sequence);
        DramaEventTalk talk = current as DramaEventTalk;
        if (talk == null || talk.choices == null || talk.choices.Count == 0)
        {
            return false;
        }

        var activeIndices = new List<int>(talk.choices.Count);
        for (int i = 0; i < talk.choices.Count; i++)
        {
            DramaChoice choice = talk.choices[i];
            if (choice != null && choice.button != null)
            {
                activeIndices.Add(i);
            }
        }

        if (activeIndices.Count == 0)
        {
            return false;
        }

        if (choiceSteps >= maxChoiceStepsPerRun)
        {
            stopByLoop = true;
            return false;
        }

        string signature = BuildChoiceSignature(talk, activeIndices);
        for (int edgeOrdinal = 0; edgeOrdinal < activeIndices.Count; edgeOrdinal++)
        {
            discoveredChoiceEdges.Add(signature + "#" + edgeOrdinal);
        }
        int visits = 0;
        choiceSignatureVisits.TryGetValue(signature, out visits);
        visits++;
        choiceSignatureVisits[signature] = visits;
        int maxVisitsForNode = Math.Max(4, activeIndices.Count * 3);
        if (visits > maxVisitsForNode)
        {
            stopByLoop = true;
            return false;
        }

        int selectedOrdinal = 0;
        if (choiceDepth < plan.Count)
        {
            int desired = plan[choiceDepth];
            if (desired >= 0 && desired < activeIndices.Count)
            {
                selectedOrdinal = desired;
            }
            else
            {
                AddActiveError("Invalid plan index at depth " + choiceDepth + ": " + desired);
            }
        }
        else
        {
            selectedOrdinal = SelectUnplannedOrdinal(
                signature,
                activeIndices.Count,
                selectedChoiceVisits,
                unplannedChoiceCursor);
            if (selectedOrdinal < 0)
            {
                stopByLoop = true;
                return false;
            }
        }

        if (!globallyExpandedChoiceSignatures.Contains(signature))
        {
            globallyExpandedChoiceSignatures.Add(signature);
            for (int alt = 0; alt < activeIndices.Count; alt++)
            {
                if (alt == selectedOrdinal)
                {
                    continue;
                }

                var altPlan = new List<int>(resolvedPath.Count + 1);
                altPlan.AddRange(resolvedPath);
                altPlan.Add(alt);
                EnqueuePlanIfNew(planStack, plannedOrVisited, altPlan, maxQueuedPlans);
            }
        }

        string selectedKey = signature + "#" + selectedOrdinal;
        int selectedCount = 0;
        selectedChoiceVisits.TryGetValue(selectedKey, out selectedCount);
        if (selectedCount >= 3)
        {
            stopByLoop = true;
            choiceHandleArmed = false;
            return false;
        }
        selectedCount++;
        selectedChoiceVisits[selectedKey] = selectedCount;
        coveredChoiceEdges.Add(selectedKey);
        choiceSteps++;

        resolvedPath.Add(selectedOrdinal);
        choiceDepth++;
        choiceHandleArmed = false;

        DramaChoice selectedChoice = talk.choices[activeIndices[selectedOrdinal]];
        string descriptor = BuildChoiceDescriptor(selectedChoice, selectedOrdinal);
        int descriptorCount = 0;
        selectedDescriptorVisits.TryGetValue(descriptor, out descriptorCount);
        descriptorCount++;
        selectedDescriptorVisits[descriptor] = descriptorCount;
        if (descriptorCount > 8)
        {
            stopByLoop = true;
            return false;
        }

        if (string.Equals(lastSelectedChoiceDescriptor, descriptor, StringComparison.Ordinal))
        {
            consecutiveSameChoiceCount++;
        }
        else
        {
            lastSelectedChoiceDescriptor = descriptor;
            consecutiveSameChoiceCount = 1;
        }

        if (consecutiveSameChoiceCount >= 4)
        {
            stopByLoop = true;
            return false;
        }

        return ExecuteChoice(sequence, selectedChoice);
    }

    private static int SelectUnplannedOrdinal(
        string signature,
        int activeCount,
        Dictionary<string, int> selectedChoiceVisits,
        Dictionary<string, int> unplannedChoiceCursor)
    {
        if (activeCount <= 0)
        {
            return -1;
        }

        int start = 0;
        unplannedChoiceCursor.TryGetValue(signature, out start);
        if (start < 0 || start >= activeCount)
        {
            start = 0;
        }

        for (int offset = 0; offset < activeCount; offset++)
        {
            int candidate = (start + offset) % activeCount;
            string key = signature + "#" + candidate;
            int seen = 0;
            selectedChoiceVisits.TryGetValue(key, out seen);
            if (seen == 0)
            {
                unplannedChoiceCursor[signature] = (candidate + 1) % activeCount;
                return candidate;
            }
        }

        if (activeCount == 1)
        {
            return 0;
        }

        return -1;
    }

    private static string BuildChoiceSignature(
        DramaEventTalk talk,
        List<int> activeIndices)
    {
        string actor = (talk != null ? talk.idActor : "") ?? "";
        var sb = new StringBuilder();
        sb.Append(actor).Append("|");
        if (talk != null && activeIndices != null && talk.choices != null)
        {
            for (int i = 0; i < activeIndices.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append("||");
                }
                int idx = activeIndices[i];
                if (idx < 0 || idx >= talk.choices.Count)
                {
                    continue;
                }
                DramaChoice c = talk.choices[idx];
                if (c == null)
                {
                    continue;
                }
                sb.Append(i).Append(":")
                    .Append(c.idJump ?? "").Append("/")
                    .Append(c.idAction ?? "");
            }
        }
        return sb.ToString();
    }

    private static string BuildChoiceDescriptor(DramaChoice choice, int selectedOrdinal)
    {
        if (choice == null)
        {
            return selectedOrdinal.ToString();
        }
        return selectedOrdinal + ":" + (choice.idJump ?? "") + "/" + (choice.idAction ?? "");
    }

    private bool ExecuteChoice(DramaSequence sequence, DramaChoice choice)
    {
        if (choice == null)
        {
            return false;
        }

        if (choice.button != null)
        {
            try
            {
                choice.button.onClick.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                AddActiveError("choice.button.onClick threw: " + ex);
                return false;
            }
        }

        try
        {
            if (!string.IsNullOrEmpty(choice.idJump))
            {
                sequence.Play(choice.idJump);
            }
            else
            {
                sequence.PlayNext();
            }
            return true;
        }
        catch (Exception ex)
        {
            AddActiveError("choice jump fallback threw: " + ex);
            return false;
        }
    }

    private bool TryPlayDrama(string dramaId)
    {
        try
        {
            string book = dramaId.StartsWith("drama_") ? dramaId : "drama_" + dramaId;
            LayerDrama layer = LayerDrama.Activate(book, dramaId, null, EClass.pc, null, null);
            return layer != null;
        }
        catch (Exception ex)
        {
            AddActiveError("LayerDrama.Activate failed: " + ex);
            return false;
        }
    }

    private bool HandleBlockingTopLayer(LayerDrama dramaLayer)
    {
        try
        {
            if (EClass.ui == null)
            {
                return false;
            }

            Layer top = EClass.ui.TopLayer;
            if (top == null)
            {
                return false;
            }

            if (top is LayerDrama)
            {
                return false;
            }

            if (dramaLayer != null && ReferenceEquals(top, dramaLayer))
            {
                return false;
            }

            // Shop/inventory/list layers can block drama progression.
            top.Close();
            ClearUiCover();
            return true;
        }
        catch (Exception ex)
        {
            AddActiveError("HandleBlockingTopLayer failed: " + ex.Message);
            return false;
        }
    }

    private static DramaEvent GetCurrentEvent(DramaSequence sequence)
    {
        try
        {
            if (CurrentEventField != null)
            {
                return CurrentEventField.GetValue(sequence) as DramaEvent;
            }
        }
        catch
        {
        }
        return null;
    }

    private static int GetCurrentEventId(DramaSequence sequence)
    {
        try
        {
            if (CurrentEventIdField != null)
            {
                return (int)CurrentEventIdField.GetValue(sequence);
            }
        }
        catch
        {
        }
        return int.MinValue;
    }

    private static void EnqueuePlanIfNew(
        Stack<List<int>> stack,
        HashSet<string> plannedOrVisited,
        List<int> plan,
        int maxQueuedPlans)
    {
        if (maxQueuedPlans > 0 && plannedOrVisited.Count >= maxQueuedPlans)
        {
            return;
        }

        string key = EncodePlan(plan);
        if (!plannedOrVisited.Contains(key))
        {
            plannedOrVisited.Add(key);
            stack.Push(plan);
        }
    }

    private static string EncodePlan(List<int> plan)
    {
        if (plan == null || plan.Count == 0)
        {
            return "";
        }

        var sb = new StringBuilder(plan.Count * 2);
        for (int i = 0; i < plan.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('.');
            }
            sb.Append(plan[i]);
        }
        return sb.ToString();
    }

    private void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!_captureErrors)
        {
            return;
        }

        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
        {
            return;
        }

        if (_activeErrors.Count >= MaxErrorsPerDrama)
        {
            return;
        }

        string message = condition ?? "(null)";
        if (message.IndexOf("DramaRuntimeRunnerV2", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return;
        }

        if (!string.IsNullOrEmpty(stackTrace))
        {
            message = message + "\n" + stackTrace;
        }

        AddActiveError(message);
    }

    private void AddActiveError(string error)
    {
        if (_activeErrors.Count < MaxErrorsPerDrama)
        {
            _activeErrors.Add(error ?? "(null)");
        }
    }

    private static Chara ShowNpcNearPlayer(string npcId)
    {
        var pc = EClass.pc;
        if (pc == null || EClass._zone == null)
        {
            return null;
        }

        var npc = EnsureNpcInSomewhere(npcId);
        if (npc == null)
        {
            return null;
        }

        if (npc.currentZone != EClass._zone)
        {
            if (npc.currentZone != null)
            {
                npc.currentZone.RemoveCard(npc);
            }

            EClass._zone.AddCard(npc, pc.pos.GetNearestPoint(allowBlock: false, allowChara: false));
        }

        return npc;
    }

    private static void HideNpcToSomewhere(string npcId)
    {
        EnsureNpcInSomewhere(npcId);
    }

    private static Chara EnsureNpcInSomewhere(string npcId)
    {
        var npc = FindGlobalNpc(npcId);
        if (npc == null)
        {
            npc = CharaGen.Create(npcId);
            if (npc == null)
            {
                return null;
            }
        }

        npc.SetGlobal();
        npc.MoveZone(SomewhereZoneId);
        return npc;
    }

    private static Chara FindGlobalNpc(string npcId)
    {
        var globalCharas = EClass.game?.cards?.globalCharas;
        if (globalCharas == null)
        {
            return null;
        }

        foreach (var c in globalCharas.Values)
        {
            if (c != null && !c.isDestroyed && c.id == npcId)
            {
                return c;
            }
        }

        return null;
    }

    private static int ToPermille(int covered, int discovered)
    {
        if (discovered <= 0)
        {
            return 1000;
        }

        if (covered <= 0)
        {
            return 0;
        }

        if (covered >= discovered)
        {
            return 1000;
        }

        return (int)((covered * 1000L) / discovered);
    }

    private static int ToMs(float seconds)
    {
        if (seconds <= 0f)
        {
            return 0;
        }
        return (int)(seconds * 1000f);
    }

    private static void SafeExitDramaLayer()
    {
        try
        {
            LayerDrama layer = LayerDrama.Instance;
            if (layer != null)
            {
                if (layer.drama != null && layer.drama.sequence != null)
                {
                    layer.drama.sequence.Exit();
                }
                layer.Close();
            }
            if (EClass.ui != null)
            {
                EClass.ui.HideCover(0f, null);
                EClass.ui.Show(0f);
            }
        }
        catch
        {
        }
    }

    private IEnumerator EnsureDramaLayersClosed()
    {
        const int maxFrames = 30;
        for (int i = 0; i < maxFrames; i++)
        {
            bool foundDramaLikeLayer = false;

            try
            {
                LayerDrama instance = LayerDrama.Instance;
                if (instance != null)
                {
                    foundDramaLikeLayer = true;
                    try
                    {
                        if (instance.drama != null && instance.drama.sequence != null)
                        {
                            instance.drama.sequence.Exit();
                        }
                    }
                    catch
                    {
                    }

                    try
                    {
                        instance.Close();
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }

            try
            {
                if (EClass.ui != null)
                {
                    Layer top = EClass.ui.TopLayer;
                    if (top != null)
                    {
                        bool isDramaLike = top is LayerDrama;
                        if (!isDramaLike)
                        {
                            string typeName = top.GetType().Name ?? string.Empty;
                            isDramaLike = typeName.IndexOf("Drama", StringComparison.OrdinalIgnoreCase) >= 0;
                        }

                        if (isDramaLike)
                        {
                            foundDramaLikeLayer = true;

                            LayerDrama dramaTop = top as LayerDrama;
                            if (dramaTop != null)
                            {
                                try
                                {
                                    if (dramaTop.drama != null && dramaTop.drama.sequence != null)
                                    {
                                        dramaTop.drama.sequence.Exit();
                                    }
                                }
                                catch
                                {
                                }
                            }

                            try
                            {
                                top.Close();
                            }
                            catch
                            {
                            }
                        }
                    }

                    EClass.ui.HideCover(0f, null);
                    EClass.ui.Show(0f);
                }
            }
            catch
            {
            }

            if (!foundDramaLikeLayer)
            {
                yield break;
            }

            yield return null;
        }

        SafeExitDramaLayer();
    }

    private void ClearUiCover()
    {
        try
        {
            if (EClass.ui == null)
            {
                return;
            }

            EClass.ui.HideCover(0f, null);
            EClass.ui.Show(0f);
        }
        catch (Exception ex)
        {
            AddActiveError("ClearUiCover failed: " + ex.Message);
        }
    }

    private void WriteResultFile()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RuntimeV2Config.ResultPath) ?? ".");
            string json = BuildResultJson();
            File.WriteAllText(RuntimeV2Config.ResultPath, json, new UTF8Encoding(false));
            Debug.Log("[RuntimeTestV2Drama] result written: " + RuntimeV2Config.ResultPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("[RuntimeTestV2Drama] failed to write result: " + ex);
        }
    }

    private string BuildResultJson()
    {
        int passed = 0;
        int failed = 0;
        for (int i = 0; i < _cases.Count; i++)
        {
            if (_cases[i].status == "passed")
            {
                passed++;
            }
            else
            {
                failed++;
            }
        }

        var sb = new StringBuilder();
        sb.Append('{');
        AppendJsonProp(sb, "suite", "runtime_v2_drama");
        sb.Append(',');
        AppendJsonProp(sb, "status", _runStatus);
        sb.Append(',');
        AppendJsonProp(sb, "required_name_contains", RuntimeV2Config.RequiredNameContains);
        sb.Append(',');
        AppendJsonProp(sb, "pc_name", _pcName);
        sb.Append(',');
        AppendJsonProp(sb, "run_failure_reason", _runFailureReason);
        sb.Append(',');
        AppendJsonProp(sb, "started_utc", _runStartedUtc.ToString("o"));
        sb.Append(',');
        AppendJsonProp(sb, "duration_ms", ToMs(Time.realtimeSinceStartup - _runStartRealtime));
        sb.Append(',');
        AppendJsonProp(sb, "reload_per_case", true);
        sb.Append(',');
        sb.Append("\"filters\":{");
        AppendJsonProp(sb, "case_id", RuntimeV2Config.CaseIdFilter);
        sb.Append(',');
        AppendJsonProp(sb, "tag", RuntimeV2Config.TagFilter);
        sb.Append('}');
        sb.Append(',');
        sb.Append("\"summary\":{");
        AppendJsonProp(sb, "total", _cases.Count);
        sb.Append(',');
        AppendJsonProp(sb, "passed", passed);
        sb.Append(',');
        AppendJsonProp(sb, "failed", failed);
        sb.Append('}');
        sb.Append(',');
        sb.Append("\"cases\":[");
        for (int i = 0; i < _cases.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            var c = _cases[i];
            sb.Append('{');
            AppendJsonProp(sb, "drama_id", c.dramaId);
            sb.Append(',');
            AppendJsonProp(sb, "status", c.status);
            sb.Append(',');
            AppendJsonProp(sb, "reason", c.reason);
            sb.Append(',');
            AppendJsonProp(sb, "reloaded", c.reloaded);
            sb.Append(',');
            AppendJsonProp(sb, "setup_applied", c.setupApplied);
            sb.Append(',');
            AppendJsonProp(sb, "cleanup_applied", c.cleanupApplied);
            sb.Append(',');
            AppendJsonProp(sb, "started", c.started);
            sb.Append(',');
            AppendJsonProp(sb, "timeout", c.timeout);
            sb.Append(',');
            AppendJsonProp(sb, "branch_truncated", c.branchTruncated);
            sb.Append(',');
            AppendJsonProp(sb, "run_count", c.runCount);
            sb.Append(',');
            AppendJsonProp(sb, "explored_plans", c.exploredPlans);
            sb.Append(',');
            AppendJsonProp(sb, "loop_cuts", c.loopCuts);
            sb.Append(',');
            AppendJsonProp(sb, "forced_advances", c.forcedAdvances);
            sb.Append(',');
            AppendJsonProp(sb, "auto_choices", c.autoChoices);
            sb.Append(',');
            AppendJsonProp(sb, "discovered_choice_edges", c.discoveredChoiceEdges);
            sb.Append(',');
            AppendJsonProp(sb, "covered_choice_edges", c.coveredChoiceEdges);
            sb.Append(',');
            AppendJsonProp(sb, "coverage_permille", c.coveragePermille);
            sb.Append(',');
            AppendJsonProp(sb, "coverage_target_reached", c.coverageTargetReached);
            sb.Append(',');
            AppendJsonProp(sb, "duration_ms", c.durationMs);
            sb.Append(',');
            sb.Append("\"logs\":[");
            for (int l = 0; l < c.logs.Count; l++)
            {
                if (l > 0)
                {
                    sb.Append(',');
                }
                sb.Append('"').Append(JsonEscape(c.logs[l])).Append('"');
            }
            sb.Append("],");
            sb.Append("\"errors\":[");
            for (int e = 0; e < c.errors.Count; e++)
            {
                if (e > 0)
                {
                    sb.Append(',');
                }
                sb.Append('"').Append(JsonEscape(c.errors[e])).Append('"');
            }
            sb.Append("]}");
        }
        sb.Append("]}");
        return sb.ToString();
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

    private static void AppendJsonProp(StringBuilder sb, string key, bool value)
    {
        sb.Append('"').Append(JsonEscape(key)).Append("\":");
        sb.Append(value ? "true" : "false");
    }

    private static string JsonEscape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

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
