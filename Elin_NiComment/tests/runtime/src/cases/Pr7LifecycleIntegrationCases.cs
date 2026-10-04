// Test-only native lifecycle cases. No production hooks are called directly.
public abstract class NiPr7Case : RuntimeCaseBase
{
    protected NiPr7Scope Scope;
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "pr7", "integration", "destructive", "save_reload" };
    public override void Prepare(RuntimeTestContext ctx) { Scope = new NiPr7Scope(ctx); }
    public override void Verify(RuntimeTestContext ctx) { Scope.RequireObservationValid(); }
}

public sealed class Pr7NiDeathRealCase : NiPr7Case
{
    public override string Id => "pr7.ni.death_real";
    public override void Execute(RuntimeTestContext ctx)
    {
        var target = Scope.Spawn("eligible", EClass.pc.LV);
        Scope.Row("eligible", target, () => target.Die(origin: EClass.pc, attackSource: AttackSource.DeathSentence), true, 1, 1);
        Scope.Row("already_dead", target, () => target.Die(origin: EClass.pc, attackSource: AttackSource.DeathSentence), true, 0, 1);
        var noOrigin = Scope.Spawn("null_origin", EClass.pc.LV);
        Scope.Row("null_origin", noOrigin, () => noOrigin.Die(attackSource: AttackSource.DeathSentence), true, 0, 1);
        var npcOrigin = Scope.Spawn("npc_origin", EClass.pc.LV);
        var other = Scope.Spawn("origin_control", 1);
        Scope.Row("npc_origin", npcOrigin, () => npcOrigin.Die(origin: other, attackSource: AttackSource.DeathSentence), true, 0, 1);
        var weak = Scope.SpawnLowerLevelControl();
        RuntimeAssertions.Require(weak.LV < EClass.pc.LV, "Lower-level control precondition failed.");
        Scope.Row("lower_level", weak, () => weak.Die(origin: EClass.pc, attackSource: AttackSource.DeathSentence), true, 0, 1);
        var mount = Scope.Spawn("mounted_faint", EClass.pc.LV);
        // Only generated NPC links are changed; ActRide.Ride would also enroll the mount in PC's party.
        mount.host = other;
        other.ride = mount;
        Scope.Row("mounted_faint", mount, () => mount.Die(origin: EClass.pc), false, 0, 1);
        RuntimeAssertions.Require(mount.hp < 0 && mount.HasCondition<ConFaint>(), "Native mounted Die did not take the faint branch.");
    }
}

public sealed class Pr7NiQuestRealCase : NiPr7Case
{
    public override string Id => "pr7.ni.quest_real";
    public override void Execute(RuntimeTestContext ctx)
    {
        var quest = Scope.NewQuest("complete");
        int karma = EClass.player.karma;
        Scope.Row("complete", quest, quest.Complete, true, 1, 1);
        RuntimeAssertions.Require(quest.AfterCalls == 1 && quest.RewardCalls == 1, "Native Complete did not dispatch derived overrides.");
        RuntimeAssertions.Require(EClass.game.quests.completedIDs.Contains(quest.id)
            && EClass.game.quests.completedTypes.Contains(quest.GetType().ToString())
            && EClass._zone.completedQuests.Contains(quest.uid), "Native quest completion records absent.");
        RuntimeAssertions.Require(EClass.player.karma == System.Math.Min(100, karma + 1), "Native Complete karma transition absent.");
        Scope.Row("already_complete", quest, quest.Complete, true, 0, 1);
        RuntimeAssertions.Require(quest.AfterCalls == 2 && quest.RewardCalls == 2, "Already-complete native body was unexpectedly suppressed.");
        ctx.Log("already_complete:native_side_effects_repeat:notification_only_is_suppressed");
    }
}

public sealed class Pr7NiDeathStressCase : NiPr7Case
{
    public override string Id => "pr7.ni.death_reentry_exception";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "pr7", "integration", "destructive", "save_reload", "fault_injection" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var target = Scope.Spawn("reentry", EClass.pc.LV);
        Scope.BodyFaults[target] = () => target.Die(origin: EClass.pc, attackSource: AttackSource.DeathSentence);
        Scope.Row("same_instance_reentry", target, () => target.Die(origin: EClass.pc, attackSource: AttackSource.DeathSentence), true, 1, 2);
        var retry = Scope.Spawn("exception", EClass.pc.LV);
        Scope.BodyFaults[retry] = () => { throw new NiPr7InjectedException(); };
        Scope.Row("throw_before_death", retry, () => retry.Die(origin: EClass.pc, attackSource: AttackSource.DeathSentence), false, 0, 1, true);
        Scope.Row("retry_after_finalizer", retry, () => retry.Die(origin: EClass.pc, attackSource: AttackSource.DeathSentence), true, 1, 1);
        var skipped = Scope.Spawn("skip", EClass.pc.LV);
        Scope.SkipOnce = skipped;
        Scope.Row("fixture_prefix_skip", skipped, () => skipped.Die(origin: EClass.pc, attackSource: AttackSource.DeathSentence), false, 0, 0);
        Scope.Row("retry_after_skip", skipped, () => skipped.Die(origin: EClass.pc, attackSource: AttackSource.DeathSentence), true, 1, 1);
    }
}

public sealed class Pr7NiQuestStressCase : NiPr7Case
{
    public override string Id => "pr7.ni.quest_reentry_exception";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "pr7", "integration", "destructive", "save_reload", "fault_injection" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var same = Scope.NewQuest("reentry");
        same.Before = same.Complete;
        Scope.Row("same_instance_reentry", same, same.Complete, true, 1, 2);
        var outer = Scope.NewQuest("outer");
        var inner = Scope.NewQuest("inner");
        outer.Before = inner.Complete;
        Scope.Row("different_instance_nested", outer, outer.Complete, true, 2, 1);
        RuntimeAssertions.Require(inner.isComplete && Scope.NotificationsFor(inner) == 1
            && Scope.NotificationsFor(outer) == 1 && Scope.BodiesFor(inner) == 1, "Nested quest notification identity was mixed or suppressed.");
        var failed = Scope.NewQuest("exception_before");
        failed.Before = () => { throw new NiPr7InjectedException(); };
        Scope.Row("throw_before_complete", failed, failed.Complete, false, 0, 1, true);
        Scope.Row("retry_after_finalizer", failed, failed.Complete, true, 1, 1);
        var late = Scope.NewQuest("exception_oncomplete");
        late.After = () => { throw new NiPr7InjectedException(); };
        Scope.Row("throw_after_records_before_final_state", late, late.Complete, false, 0, 1, true);
        RuntimeAssertions.Require(EClass.game.quests.completedIDs.Contains(late.id), "Late exception did not occur after native record mutation.");
        Scope.Row("retry_after_partial_completion", late, late.Complete, true, 1, 1);
        var skipped = Scope.NewQuest("skip");
        Scope.SkipOnce = skipped;
        Scope.Row("fixture_prefix_skip", skipped, skipped.Complete, false, 0, 0);
        Scope.Row("retry_after_skip", skipped, skipped.Complete, true, 1, 1);
    }
}

public sealed class NiPr7InjectedException : System.Exception { }

// A real Quest subtype with no item rewards/affinity. Complete itself remains vanilla.
public sealed class NiPr7Quest : Quest
{
    public SourceQuest.Row NativeSource;
    public System.Action Before, After;
    public int BeforeCalls, AfterCalls, RewardCalls;
    public override SourceQuest.Row source => NativeSource;
    public override Chara DestChara => null;
    public override int AffinityGain => 0;
    public override void ShowCompleteText() { }
    public override void OnDropReward() { RewardCalls++; }
    public override void OnBeforeComplete()
    {
        BeforeCalls++;
        var action = Before;
        Before = null;
        action?.Invoke();
    }
    public override void OnComplete()
    {
        AfterCalls++;
        var action = After;
        After = null;
        action?.Invoke();
    }
}

public sealed class NiPr7Scope
{
    public static bool RequiresReload;
    public readonly RuntimeTestContext Context;
    public readonly System.Collections.Generic.List<Chara> Cards = new System.Collections.Generic.List<Chara>();
    public readonly System.Collections.Generic.List<NiPr7Quest> Quests = new System.Collections.Generic.List<NiPr7Quest>();
    public readonly System.Collections.Generic.Dictionary<object, System.Action> BodyFaults = new System.Collections.Generic.Dictionary<object, System.Action>();
    public readonly System.Collections.Generic.Dictionary<object, int> Bodies = new System.Collections.Generic.Dictionary<object, int>();
    public readonly System.Collections.Generic.List<object> Notifications = new System.Collections.Generic.List<object>();
    public readonly System.Collections.Generic.List<object> Frames = new System.Collections.Generic.List<object>();
    public object SkipOnce;
    public int Sends, FireDepth, Finalizers, ExceptionFinalizers;
    public string ObservationError;
    private readonly string token = System.Guid.NewGuid().ToString("N");
    private readonly NiPr7Baseline baseline;
    private readonly HarmonyLib.Harmony observer;
    private readonly System.Collections.Generic.List<System.Reflection.MethodInfo> targets = new System.Collections.Generic.List<System.Reflection.MethodInfo>();
    private readonly System.Collections.Generic.Dictionary<Chara, int> cardUids = new System.Collections.Generic.Dictionary<Chara, int>();
    private readonly System.Collections.Generic.Dictionary<Chara, System.Collections.Generic.Dictionary<Thing, int>> generatedItems
        = new System.Collections.Generic.Dictionary<Chara, System.Collections.Generic.Dictionary<Thing, int>>();
    private bool deathEnvironmentChecked;

    public NiPr7Scope(RuntimeTestContext ctx)
    {
        Guard();
        Context = ctx;
        // Register first so this runs last, even when fixture/world rollback throws.
        // Capture without calling Chara.mana/stamina (those getters rebind shared Stats).
        var shared = new NiPr7SharedNativeState();
        ctx.RegisterRollback("pr7.shared_native_state", () => Rollback(() =>
        {
            shared.RestoreAndAssert();
            ctx.Log("cleanup_shared_native_state:random_reference_and_state:stats_bindings:CC:restored");
        }));
        baseline = new NiPr7Baseline();
        ctx.RegisterRollback("pr7.original_state", () => Rollback(baseline.RestoreAndAssert));
        ctx.RegisterRollback("pr7.generated_fixtures", () => Rollback(CleanupFixtures));
        observer = new HarmonyLib.Harmony("runtime.pr7.ni." + token);
        ctx.RegisterRollback("pr7.observer_owner", () => Rollback(RemoveObserver));
        RuntimeAssertions.Require(NiPr7Observer.Active == null, "Another PR7 observer is active.");
        NiPr7Observer.Active = this;
        var death = HarmonyLib.AccessTools.DeclaredMethod(typeof(Chara), "Die", new[] { typeof(Element), typeof(Card), typeof(AttackSource), typeof(Chara) });
        var complete = HarmonyLib.AccessTools.DeclaredMethod(typeof(Quest), "Complete", System.Type.EmptyTypes);
        HookLifecycle(death, "Elin_NiComment.CharaDiePatch");
        HookLifecycle(complete, "Elin_NiComment.QuestCompletePatch");
        var fire = HarmonyLib.AccessTools.DeclaredMethod(typeof(Elin_NiComment.CommentTrigger), "FireBarrage", new[] { typeof(string), typeof(UnityEngine.Color) });
        Patch(fire, prefix: Hook("FireEnter"), finalizer: Hook("FireExit"));
        var send = HarmonyLib.AccessTools.DeclaredMethod(typeof(Elin_NiComment.NiCommentAPI), "Send", new[] { typeof(string), typeof(UnityEngine.Color) });
        Patch(send, prefix: Hook("Send"));
        ctx.Log("loaded_ni:mvid=" + typeof(Elin_NiComment.CommentTrigger).Module.ModuleVersionId
            + ":path=" + typeof(Elin_NiComment.CommentTrigger).Assembly.Location);
        ctx.Log("loaded_elin:mvid=" + typeof(Chara).Module.ModuleVersionId);
        ctx.Log("llm:disabled_and_inactive:config_unchanged;reload_dedicated_save_after_run");
    }

    public static void Guard()
    {
        RuntimeAssertions.Require(!RequiresReload, "PR7 cleanup previously failed; reload the dedicated save in a new suite/session.");
        RuntimeAssertions.Require(EClass.pc != null && EClass._zone != null && EClass._map != null && EClass.game != null,
            "PR7 requires an active gameplay save.");
        RuntimeAssertions.Require((EClass.pc.Name ?? "").IndexOf("RUNTIME_TEST", System.StringComparison.Ordinal) >= 0,
            "PR7 dedicated RUNTIME_TEST save guard rejected (independent of runner parameters).");
        RuntimeAssertions.Require(!EClass.pc.isDead && EClass.pc.LV >= 1 && EClass.pc.LV <= 50
            && EClass.pc.host == null && EClass.pc.ride == null && !(EClass.pc.ai is GoalAutoCombat),
            "PR7 needs a live, unmounted, idle PC of level 1..50.");
        RuntimeAssertions.Require(EClass.player.karma >= 0, "PR7 requires noncriminal karma to avoid criminal-status transitions.");
        RuntimeAssertions.Require(Elin_NiComment.ModConfig.EnableMod != null && Elin_NiComment.ModConfig.EnableMod.Value
            && Elin_NiComment.NiCommentAPI.IsReady && Elin_NiComment.CommentTrigger.Instance != null,
            "NiComment enabled/real overlay ready precondition failed; no stub overlay is created.");
        RuntimeAssertions.Require(Elin_NiComment.Llm.LlmConfig.EnableLlm != null && !Elin_NiComment.Llm.LlmConfig.EnableLlm.Value,
            "Runtime operator must disable external LLM reactions before loading this test save.");
        var llm = Elin_NiComment.Llm.LlmReactionService.Instance;
        RuntimeAssertions.Require(llm == null || (!llm.IsActive
            && object.Equals(ReflectionCompat.GetFieldOrPropertyValue(llm, "_requestInFlight"), false)),
            "LLM service/provider active or request in flight; restart with LLM disabled.");
    }

    private static HarmonyLib.HarmonyMethod Hook(string name)
    {
        return new HarmonyLib.HarmonyMethod(typeof(NiPr7Observer), name);
    }

    private void HookLifecycle(System.Reflection.MethodInfo method, string productType)
    {
        RuntimeAssertions.Require(method != null, "Native lifecycle signature changed: " + productType);
        var info = HarmonyLib.Harmony.GetPatchInfo(method);
        RuntimeAssertions.Require(info != null, "NiComment lifecycle patch is not loaded: " + productType);
        var owners = new System.Collections.Generic.List<string>();
        int posts = 0, finals = 0;
        foreach (var patch in info.Prefixes)
            if (patch.PatchMethod.DeclaringType.FullName == productType) owners.Add(patch.owner);
        foreach (var patch in info.Postfixes)
            if (patch.PatchMethod.DeclaringType.FullName == productType) posts++;
        foreach (var patch in info.Finalizers)
            if (patch.PatchMethod.DeclaringType.FullName == productType) finals++;
        RuntimeAssertions.Require(owners.Count == 1 && posts == 1 && finals == 1, "Missing or duplicate product lifecycle hooks: " + productType);
        var enter = Hook("Enter"); enter.priority = HarmonyLib.Priority.First; enter.before = owners.ToArray();
        var skip = Hook("Skip"); skip.priority = HarmonyLib.Priority.Last; skip.after = owners.ToArray();
        var exit = Hook("Exit"); exit.priority = HarmonyLib.Priority.Last; exit.after = owners.ToArray();
        Patch(method, prefix: enter, transpiler: Hook("ObserveBody"), finalizer: exit);
        observer.Patch(method, prefix: skip);
        Context.Log("product_hook:" + productType + ":owner=" + owners[0]);
    }

    private void Patch(System.Reflection.MethodInfo method, HarmonyLib.HarmonyMethod prefix = null,
        HarmonyLib.HarmonyMethod transpiler = null, HarmonyLib.HarmonyMethod finalizer = null)
    {
        RuntimeAssertions.Require(method != null, "Exact native observation target absent.");
        targets.Add(method);
        observer.Patch(method, prefix: prefix, transpiler: transpiler, finalizer: finalizer);
    }

    public Chara Spawn(string role, int level)
    {
        RequireDeathEnvironment();
        RuntimeAssertions.Require(level >= 1 && level <= 50, "Generated death fixture level must be 1..50 before boundary setup.");
        RuntimeAssertions.Require(CardBlueprint.current == null, "Pending native CardBlueprint belongs to another operation; do not consume it.");
        RuntimeAssertions.Require(EClass.sources.charas.map.ContainsKey("bat"), "Native bat source required.");
        var card = CharaGen.Create("bat", level);
        RuntimeAssertions.Require(card != null && card.uid > 0 && !baseline.ProtectedUid(card.uid), "Generated fixture UID is not exclusively new.");
        Cards.Add(card);
        cardUids.Add(card, card.uid);
        var items = new System.Collections.Generic.Dictionary<Thing, int>();
        generatedItems.Add(card, items);
        RecordGeneratedItems(card, items);
        Context.Log("fixture_created:" + role + ":uid=" + card.uid + ":requestedLV=" + level
            + ":nativeLV=" + card.LV + ":sourceLV=" + card.source.LV + ":genLV=" + card.genLv
            + ":hp=" + card.hp + ":dead=" + card.isDead + ":destroyed=" + card.isDestroyed);
        RuntimeAssertions.Require(card.held == null, "Generated NPC unexpectedly holds an external object.");
        card.c_altName = "RUNTIME_TEST_PR7_" + token + "_" + role;
        RuntimeAssertions.Require(!card.IsPC && !card.IsGlobal && !card.IsPCFaction && !card.isSummon
            && !card.IsHuman && !card.IsMultisize && card.OriginalHostility < Hostility.Neutral,
            "Death fixture must be an ordinary single-cell hostile generated NPC.");
        RuntimeAssertions.Require(card.parent == null && card.currentZone == null && !card.isDead && !card.isDestroyed
            && card.renderer != null && card.body != null && card.elements != null && card.mana != null,
            "Native Create did not return a detached live initialized NPC; do not repair dead/external objects.");
        // Create's level argument is genLv, not an exact LV assignment. Native SetLv
        // initializes this exclusively owned NPC's level, attributes, HP, mana and stamina.
        RuntimeAssertions.Require(object.ReferenceEquals(card.SetLv(level), card), "Native SetLv changed fixture identity.");
        RuntimeAssertions.Require(card.LV == level && card.hp == card.MaxHP && card.hp > 0
            && card.mana.value == card.mana.max && !card.isDead && !card.isDestroyed,
            "Native NPC level/stat initialization failed.");
        RuntimeAssertions.Require(!card.HasElement(488) && !card.HasCondition<ConFaint>(),
            "Death fixture has a death-scream/faint effect; reject unsafe or non-fresh source.");
        var point = EClass.pc.pos.GetNearestPoint(allowBlock: false, allowChara: false);
        RuntimeAssertions.Require(point != null && point.IsInBounds && !point.cell.HasFullBlock, "No safe fixture spawn point; do not destroy map blocks.");
        RuntimeAssertions.Require(object.ReferenceEquals(EClass._zone.AddCard(card, point), card), "Native AddCard changed fixture identity.");
        Context.Log("fixture:" + role + ":uid=" + card.uid + ":LV=" + card.LV + ":requestedLV=" + level
            + ":active=" + card.IsInActiveZone + ":dead=" + card.isDead + ":hp=" + card.hp + ":maxHP=" + card.MaxHP
            + ":currentZone=" + (card.currentZone == null ? 0 : card.currentZone.uid) + ":activeZone=" + EClass._zone.uid
            + ":parentZone=" + object.ReferenceEquals(card.parent, EClass._zone)
            + ":mapRefs=" + CountMapReferences(card) + ":inBounds=" + card.pos.IsInBounds);
        RuntimeAssertions.Require(card.IsInActiveZone && card.LV == level && !card.isDead, "Native NPC fixture setup failed.");
        RequireLiveDeathFixture(card);
        baseline.RequirePcLevelUnchanged();
        return card;
    }

    private void RequireDeathEnvironment()
    {
        baseline.RequireSameWorld();
        baseline.RequirePcLevelUnchanged();
        RuntimeAssertions.Require(EClass._zone.IsActiveZone && object.ReferenceEquals(EClass.game.activeZone, EClass._zone)
            && object.ReferenceEquals(EClass._zone.map, EClass._map) && EClass.pc.IsInActiveZone,
            "Native death requires the real active zone/map/PC; do not force currentZone or flags.");
        RuntimeAssertions.Require(EClass.sources.stats.alias.ContainsKey("ConFaint"), "Native ConFaint source required for mounted control.");
        RuntimeAssertions.Require((int)AttackSource.DeathSentence == 17 && (int)AttackSource.None != 17 && (int)AttackSource.None != 18,
            "Native Die special-death/mounted-faint branch values changed; review the DLL.");
        if (!deathEnvironmentChecked)
        {
            Context.Log("death_environment:zone=" + EClass._zone.uid + ":active=true:map_identity=true:ConFaint=true:pcLV=" + EClass.pc.LV);
            deathEnvironmentChecked = true;
        }
    }

    private int CountMapReferences(Chara card)
    {
        int count = 0;
        foreach (var current in EClass._map.charas) if (object.ReferenceEquals(current, card)) count++;
        return count;
    }

    private void RequireLiveDeathFixture(Chara card)
    {
        RuntimeAssertions.Require(Cards.Contains(card) && cardUids[card] == card.uid && !baseline.ProtectedUid(card.uid)
            && !card.IsPC && !card.IsGlobal && !card.IsPCFaction && !card.isSummon && !card.isDestroyed,
            "Live death fixture identity/ownership changed; do not call Die.");
        RuntimeAssertions.Require(card.IsInActiveZone && object.ReferenceEquals(card.currentZone, EClass._zone)
            && object.ReferenceEquals(card.parent, EClass._zone) && CountMapReferences(card) == 1
            && card.pos.IsInBounds && card.renderer != null && card.body != null && card.mana != null
            && card.hp > 0 && !card.isDead && !object.ReferenceEquals(EClass._zone.Boss, card),
            "Live death fixture lacks native placement/stats or became a boss; do not call Die.");
        RuntimeAssertions.Require(!card.HasElement(488), "Fixture acquired an area death-scream effect; do not call Die.");
        var owned = generatedItems[card];
        RuntimeAssertions.Require(card.held == null && NiPr7Ownership.TreeIsOwned(card, owned),
            "Unknown held object/descendant entered the fixture; do not call Die.");
        foreach (var item in owned)
            RuntimeAssertions.Require(item.Key.uid == item.Value && NiPr7Ownership.TreeIsOwned(item.Key, owned)
                && NiPr7Ownership.RootIsOwned(item.Key, card, EClass._zone, owned),
                "Generated descendant identity/owner changed; do not call Die.");
        if (card.host != null)
            RuntimeAssertions.Require(Cards.Contains(card.host) && cardUids[card.host] == card.host.uid
                && !card.host.IsPC && !card.host.isDead && object.ReferenceEquals(card.host.ride, card),
                "Mounted/faint control must link two live owned NPC instances.");
    }

    public Chara SpawnLowerLevelControl()
    {
        baseline.RequirePcLevelUnchanged();
        int comparedLevel = EClass.pc.LV - 1;
        // CharaGen clamps requested levels below 1. Only this generated boundary
        // control temporarily uses LV0; no PC level-up/scaling operation is needed.
        var card = Spawn("lower_level", System.Math.Max(1, comparedLevel));
        if (comparedLevel == 0)
        {
            int originalLevel = card.LV;
            int uid = card.uid;
            Context.RegisterRollback("pr7.fixture_level:" + uid, () => Rollback(() =>
            {
                baseline.RequireSameWorld();
                RuntimeAssertions.Require(Cards.Contains(card) && card.uid == uid && cardUids[card] == uid
                    && !baseline.ProtectedUid(uid) && !card.IsPC && !card.IsGlobal && !card.IsPCFaction
                    && (card.parent == null || object.ReferenceEquals(card.parent, EClass._zone)),
                    "Lower-level fixture identity/owner changed; do not restore an unknown object.");
                card.LV = originalLevel;
                RuntimeAssertions.Require(card.LV == originalLevel, "Generated fixture level restoration failed.");
                baseline.RequirePcLevelUnchanged();
                Context.Log("fixture_level_restored:uid=" + uid + ":LV=" + card.LV + ":pcLV=" + EClass.pc.LV);
            }));
            // Native LV setter writes only Card._ints[25]; SetLv would also scale
            // attributes/skills and consume RNG, unrelated to this numeric guard.
            card.LV = comparedLevel;
        }
        RuntimeAssertions.Require(card.LV == comparedLevel && card.LV < EClass.pc.LV,
            "Lower-level native getter comparison was not established.");
        Context.Log("lower_level_control:uid=" + card.uid + ":LV=" + card.LV + ":pcLV=" + EClass.pc.LV
            + ":synthetic_boundary=" + (comparedLevel == 0));
        return card;
    }

    public NiPr7Quest NewQuest(string role)
    {
        RuntimeAssertions.Require(EClass.sources.quests.rows.Count > 0, "Native quest source unavailable.");
        var quest = new NiPr7Quest { id = "RUNTIME_TEST_PR7_" + token + "_" + role,
            uid = -70000000 - Quests.Count, person = new Person(), NativeSource = EClass.sources.quests.rows[0],
            uidClientZone = EClass._zone.uid };
        RuntimeAssertions.Require(EClass.game.quests.Get(quest.uid) == null
            && !EClass._zone.completedQuests.Contains(quest.uid), "Fixture quest UID collision.");
        Quests.Add(quest);
        EClass.game.quests.list.Add(quest);
        RuntimeAssertions.Require(object.ReferenceEquals(quest.ClientZone, EClass._zone) && quest.chara == null,
            "Quest fixture client-zone/no-reward precondition failed.");
        Context.Log("fixture:" + role + ":quest_uid=" + quest.uid + ":id=" + quest.id);
        return quest;
    }

    public bool Owns(object obj)
    {
        return (obj is Chara && Cards.Contains((Chara)obj)) || (obj is NiPr7Quest && Quests.Contains((NiPr7Quest)obj));
    }
    public int BodiesFor(object obj) { int n; return Bodies.TryGetValue(obj, out n) ? n : 0; }
    public int NotificationsFor(object obj)
    {
        int n = 0;
        foreach (var item in Notifications) if (object.ReferenceEquals(item, obj)) n++;
        return n;
    }

    public void Row(string label, object fixture, System.Action operation, bool finalized, int notifications, int bodies, bool throws = false)
    {
        RuntimeAssertions.Require(Owns(fixture), "Operation is not scoped to a generated PR7 fixture.");
        var death = fixture as Chara;
        if (death != null)
        {
            Context.Log("death_before:" + label + ":uid=" + death.uid + ":LV=" + death.LV + ":pcLV=" + EClass.pc.LV
                + ":hp=" + death.hp + ":dead=" + death.isDead + ":active=" + death.IsInActiveZone
                + ":mapRefs=" + CountMapReferences(death) + ":host=" + (death.host == null ? 0 : death.host.uid));
            if (!death.isDead) RequireLiveDeathFixture(death);
            else RuntimeAssertions.Require(death.uid == cardUids[death] && !death.isDestroyed && death.parent == null
                && death.currentZone == null && CountMapReferences(death) == 0,
                "Already-dead control did not retain the native detached dead instance.");
        }
        int before = Notifications.Count, bodyBefore = BodiesFor(fixture), sendsBefore = Sends;
        int exitsBefore = Finalizers, exceptionsBefore = ExceptionFinalizers;
        bool caught = false;
        try { operation(); }
        catch (NiPr7InjectedException) { caught = true; }
        bool actual = fixture is Chara ? ((Chara)fixture).isDead : ((Quest)fixture).isComplete;
        int delta = Notifications.Count - before, bodyDelta = BodiesFor(fixture) - bodyBefore, sendDelta = Sends - sendsBefore;
        Context.Log("row:" + label + ":uid=" + (fixture is Chara ? ((Chara)fixture).uid : ((Quest)fixture).uid)
            + ":finalized=" + actual + ":native_bodies=" + bodyDelta + ":barrage=" + delta
            + ":send_comments=" + sendDelta + ":throw=" + caught + ":finalizers=" + (Finalizers - exitsBefore));
        RuntimeAssertions.Require(caught == throws && actual == finalized && delta == notifications && bodyDelta == bodies,
            "Lifecycle/state/notification mismatch: " + label);
        RuntimeAssertions.Require(sendDelta >= notifications * 3 && sendDelta <= notifications * 5,
            "Real barrage must enqueue 3..5 comments per notification: " + label);
        RuntimeAssertions.Require(Finalizers > exitsBefore && (!throws || ExceptionFinalizers > exceptionsBefore),
            "Native Harmony finalizer path not observed: " + label);
        RequireObservationValid();
    }

    public void RequireObservationValid()
    {
        RuntimeAssertions.Require(ObservationError == null && Frames.Count == 0 && FireDepth == 0,
            "Observer identity/color/order/stack failure: " + ObservationError);
        RuntimeAssertions.Require(!EClass.pc.isDead, "PC was affected; restore dedicated save.");
        baseline.RequirePcLevelUnchanged();
    }

    private void Rollback(System.Action action)
    {
        try { action(); }
        catch { RequiresReload = true; throw; }
    }
    private void RemoveObserver()
    {
        try { observer.UnpatchSelf(); }
        finally { if (object.ReferenceEquals(NiPr7Observer.Active, this)) NiPr7Observer.Active = null; }
        foreach (var target in targets)
        {
            var info = HarmonyLib.Harmony.GetPatchInfo(target);
            RuntimeAssertions.Require(info == null || !info.Owners.Contains(observer.Id), "PR7 observation owner remained patched.");
        }
    }
    private void CleanupFixtures()
    {
        baseline.RequireSameWorld();
        var errors = new System.Collections.Generic.List<string>();
        foreach (var quest in Quests)
        {
            EClass.game.quests.list.Remove(quest);
            EClass.game.quests.completedIDs.Remove(quest.id);
            while (EClass._zone.completedQuests.Remove(quest.uid)) { }
        }
        if (!baseline.CompletedTypes.Contains(typeof(NiPr7Quest).ToString()))
            EClass.game.quests.completedTypes.Remove(typeof(NiPr7Quest).ToString());
        foreach (var card in Cards)
        {
            try
            {
                RuntimeAssertions.Require(card.uid == cardUids[card] && !baseline.ProtectedUid(card.uid), "Cleanup UID changed or became protected.");
                var owned = generatedItems[card];
                RuntimeAssertions.Require(NiPr7Ownership.TreeIsOwned(card, owned), "Unknown descendant/changed item identity; do not Destroy fixture.");
                RuntimeAssertions.Require(card.held == null && (card.parent == null || object.ReferenceEquals(card.parent, EClass._zone)),
                    "Fixture held/root ownership changed; do not Destroy fixture.");
                foreach (var item in owned)
                    RuntimeAssertions.Require(NiPr7Ownership.RootIsOwned(item.Key, card, EClass._zone, owned)
                        && item.Key.uid == item.Value && NiPr7Ownership.TreeIsOwned(item.Key, owned),
                        "Generated child acquired unknown descendants/owner; do not Destroy fixture.");
                // Both endpoints were generated by this scope; no existing mounts are changed.
                if (card.host != null) { RuntimeAssertions.Require(Cards.Contains(card.host), "Unknown fixture host."); card.host.ride = null; card.host = null; }
                if (card.ride != null) { RuntimeAssertions.Require(Cards.Contains(card.ride), "Unknown fixture ride."); card.ride.host = null; card.ride = null; }
                RuntimeAssertions.Require(!card.IsPCFaction && !card.IsGlobal, "Fixture acquired external ownership; reload save.");
                if (!card.isDestroyed) card.Destroy();
                foreach (var item in owned)
                {
                    if (!item.Key.isDestroyed) item.Key.Destroy();
                    RuntimeAssertions.Require(item.Key.isDestroyed, "Generated descendant cleanup failed: " + item.Value);
                }
                EClass._map.deadCharas.Remove(card);
                RuntimeAssertions.Require(card.isDestroyed && !EClass._map.charas.Contains(card)
                    && !EClass._map.deadCharas.Contains(card), "Fixture remains in map/dead registry.");
                Context.Log("cleanup_fixture:uid=" + card.uid + ":destroyed=true");
            }
            catch (System.Exception ex) { errors.Add(card.uid + ":" + ex.Message); }
        }
        RuntimeAssertions.Require(errors.Count == 0, "Fixture cleanup failed: " + string.Join(";", errors));
    }

    private void RecordGeneratedItems(Card root, System.Collections.Generic.Dictionary<Thing, int> items)
    {
        foreach (var item in root.things)
        {
            RuntimeAssertions.Require(item != null && item.uid > 0 && !baseline.ProtectedUid(item.uid), "Generated inventory contains a protected UID.");
            items.Add(item, item.uid);
            RecordGeneratedItems(item, items);
        }
    }
}

public static class NiPr7Ownership
{
    public static bool TreeIsOwned(Card root, System.Collections.Generic.Dictionary<Thing, int> owned)
    {
        foreach (var child in root.things)
        {
            int uid;
            if (!owned.TryGetValue(child, out uid) || uid != child.uid || !TreeIsOwned(child, owned)) return false;
        }
        return true;
    }
    public static bool RootIsOwned(Thing item, Chara card, Zone zone, System.Collections.Generic.Dictionary<Thing, int> owned)
    {
        if (item.isDestroyed) return true;
        object parent = item.parent;
        var seen = new System.Collections.Generic.HashSet<object>();
        while (parent is Thing)
        {
            var container = (Thing)parent;
            int uid;
            if (!seen.Add(container) || !owned.TryGetValue(container, out uid) || uid != container.uid) return false;
            parent = container.parent;
        }
        return object.ReferenceEquals(parent, card) || object.ReferenceEquals(parent, zone) || parent == null;
    }
}

public static class NiPr7Observer
{
    public static NiPr7Scope Active;
    public static void Enter(object __instance, out object __state)
    {
        __state = __instance;
        if (Active != null) Active.Frames.Add(__instance);
    }
    public static bool Skip(object __instance)
    {
        if (Active == null || !Active.Owns(__instance) || !object.ReferenceEquals(Active.SkipOnce, __instance)) return true;
        Active.SkipOnce = null;
        return false;
    }
    public static void Exit(object __state, System.Exception __exception)
    {
        var scope = Active;
        if (scope == null) return;
        if (scope.Owns(__state))
        {
            scope.Finalizers++;
            if (__exception != null) scope.ExceptionFinalizers++;
        }
        int last = scope.Frames.Count - 1;
        if (last < 0 || !object.ReferenceEquals(scope.Frames[last], __state)) scope.ObservationError = "lifecycle_finalizer_stack";
        else scope.Frames.RemoveAt(last);
    }
    public static System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction> ObserveBody(
        System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction> instructions)
    {
        // Add a pass-through body-entry marker. Original native IL and results are retained.
        yield return new HarmonyLib.CodeInstruction(System.Reflection.Emit.OpCodes.Ldarg_0);
        yield return new HarmonyLib.CodeInstruction(System.Reflection.Emit.OpCodes.Call,
            HarmonyLib.AccessTools.DeclaredMethod(typeof(NiPr7Observer), "BodyEnter", new[] { typeof(object) }));
        foreach (var instruction in instructions) yield return instruction;
    }
    public static void BodyEnter(object instance)
    {
        var scope = Active;
        if (scope == null || !scope.Owns(instance)) return;
        scope.Bodies[instance] = scope.BodiesFor(instance) + 1;
        System.Action fault;
        if (scope.BodyFaults.TryGetValue(instance, out fault))
        {
            scope.BodyFaults.Remove(instance);
            fault();
        }
    }
    public static void FireEnter(string eventId, UnityEngine.Color color, out bool __state)
    {
        var scope = Active;
        __state = scope != null && scope.Frames.Count > 0 && scope.Owns(scope.Frames[scope.Frames.Count - 1]);
        if (!__state) return;
        object fixture = scope.Frames[scope.Frames.Count - 1];
        string expected = fixture is Chara ? Elin_NiComment.CommentTexts.StrongKill : Elin_NiComment.CommentTexts.QuestComplete;
        if (eventId != expected || color != new UnityEngine.Color(1f, 0.84f, 0f)) scope.ObservationError = "event_id_or_gold_color";
        scope.Notifications.Add(fixture);
        scope.FireDepth++;
    }
    public static void FireExit(bool __state, System.Exception __exception)
    {
        if (!__state || Active == null) return;
        Active.FireDepth--;
        if (__exception != null) Active.ObservationError = "real_barrage_threw";
    }
    public static void Send()
    {
        if (Active != null && Active.FireDepth > 0) Active.Sends++;
    }
}

// Test-only snapshot of native shared scratch bindings and the actual RNG state.
// UnityEngine.Random.state covers a different generator. Restoring only Rand's
// reference loses draws made on the original Random before native SetSeed replaces it.
public sealed class NiPr7SharedNativeState
{
    private readonly System.Reflection.FieldInfo randomField = typeof(Rand).GetField("_random",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
    private readonly System.Reflection.FieldInfo baseSeedField = typeof(Rand).GetField("baseSeed",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
    private readonly System.Random random;
    private readonly int baseSeed;
    private readonly Chara cc = BaseStats.CC;
    private readonly System.Collections.Generic.Dictionary<System.Reflection.FieldInfo, int> cursors
        = new System.Collections.Generic.Dictionary<System.Reflection.FieldInfo, int>();
    private readonly System.Reflection.FieldInfo seedField;
    private readonly int[] seedArray, seedValues;
    private readonly System.Collections.Generic.List<Binding> bindings = new System.Collections.Generic.List<Binding>();
    private sealed class Binding
    {
        public System.Reflection.FieldInfo Field;
        public Stats Stats;
        public int[] Raw;
        public int Index;
    }

    public NiPr7SharedNativeState()
    {
        RuntimeAssertions.Require(randomField != null && randomField.FieldType == typeof(System.Random)
            && baseSeedField != null && baseSeedField.FieldType == typeof(int), "Native Rand layout changed; do not run fixtures.");
        random = (System.Random)randomField.GetValue(null);
        baseSeed = (int)baseSeedField.GetValue(null);
        RuntimeAssertions.Require(random != null && random.GetType() == typeof(System.Random),
            "Custom/null native RNG cannot be safely restored; do not run fixtures.");
        // Mono/.NET Framework Random uses two int cursors and one 56-int array.
        // Resolve by complete field shape so Mono field-name differences are allowed;
        // reject new/unsupported layouts before any fixture mutation.
        foreach (var field in typeof(System.Random).GetFields(System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
        {
            RuntimeAssertions.Require(!field.IsInitOnly, "Readonly RNG state is unsupported; do not run fixtures.");
            if (field.FieldType == typeof(int)) cursors.Add(field, (int)field.GetValue(random));
            else
            {
                RuntimeAssertions.Require(field.FieldType == typeof(int[]) && seedField == null,
                    "Unknown RNG field layout; do not run fixtures.");
                seedField = field;
                seedArray = (int[])field.GetValue(random);
                RuntimeAssertions.Require(seedArray != null && seedArray.Length == 56, "Unknown RNG seed array; do not run fixtures.");
                seedValues = (int[])seedArray.Clone();
            }
        }
        RuntimeAssertions.Require(cursors.Count == 2 && seedField != null, "Incomplete RNG snapshot; do not run fixtures.");
        foreach (var field in typeof(Stats).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (!typeof(Stats).IsAssignableFrom(field.FieldType)) continue;
            var stats = (Stats)field.GetValue(null);
            RuntimeAssertions.Require(stats != null, "Missing native Stats singleton; do not run fixtures.");
            bindings.Add(new Binding { Field = field, Stats = stats, Raw = stats.raw, Index = stats.rawIndex });
        }
        RuntimeAssertions.Require(bindings.Count == 9, "Native Stats singleton layout changed; do not run fixtures.");
    }

    public void RestoreAndAssert()
    {
        // Cleanup may itself call getters/RNG; this snapshot must be the last rollback.
        // Restore bindings/CC even if a reflection write to RNG state fails.
        try
        {
            foreach (var cursor in cursors) cursor.Key.SetValue(random, cursor.Value);
            System.Array.Copy(seedValues, seedArray, seedValues.Length);
            seedField.SetValue(random, seedArray);
            baseSeedField.SetValue(null, baseSeed);
            randomField.SetValue(null, random);
        }
        finally
        {
            foreach (var binding in bindings)
            {
                binding.Stats.raw = binding.Raw;
                binding.Stats.rawIndex = binding.Index;
            }
            BaseStats.CC = cc;
        }
        RequireUnchanged();
    }

    public void RequireUnchanged()
    {
        RuntimeAssertions.Require(object.ReferenceEquals(randomField.GetValue(null), random)
            && (int)baseSeedField.GetValue(null) == baseSeed, "Native RNG reference/baseSeed differs after rollback.");
        foreach (var cursor in cursors)
            RuntimeAssertions.Require((int)cursor.Key.GetValue(random) == cursor.Value, "Native RNG cursor differs after rollback.");
        RuntimeAssertions.Require(object.ReferenceEquals(seedField.GetValue(random), seedArray), "Native RNG array identity differs after rollback.");
        for (int i = 0; i < seedValues.Length; i++)
            RuntimeAssertions.Require(seedArray[i] == seedValues[i], "Native RNG array contents differ after rollback.");
        foreach (var binding in bindings)
            RuntimeAssertions.Require(object.ReferenceEquals(binding.Field.GetValue(null), binding.Stats)
                && object.ReferenceEquals(binding.Stats.raw, binding.Raw) && binding.Stats.rawIndex == binding.Index,
                "Native Stats binding differs after rollback: " + binding.Field.Name);
        RuntimeAssertions.Require(object.ReferenceEquals(BaseStats.CC, cc), "Native BaseStats.CC differs after rollback.");
    }
}

public sealed class NiPr7Baseline
{
    private readonly Chara pc = EClass.pc;
    private readonly object game = EClass.game, player = EClass.player, map = EClass._map, zone = EClass._zone;
    private readonly int karma = EClass.player.karma;
    private readonly int pcLevel = EClass.pc.LV, pcExperience = EClass.pc.exp;
    private readonly UnityEngine.Random.State random = UnityEngine.Random.state;
    private readonly System.Collections.Generic.List<Chara> mapCards = new System.Collections.Generic.List<Chara>(EClass._map.charas);
    private readonly System.Collections.Generic.List<Chara> dead = new System.Collections.Generic.List<Chara>(EClass._map.deadCharas);
    private readonly System.Collections.Generic.List<Thing> mapItems = new System.Collections.Generic.List<Thing>(EClass._map.things);
    private readonly System.Collections.Generic.Dictionary<int, Chara> globalCards = new System.Collections.Generic.Dictionary<int, Chara>(EClass.game.cards.globalCharas);
    private readonly System.Collections.Generic.List<Chara> carryover = new System.Collections.Generic.List<Chara>(EClass.player.listCarryoverMap);
    private readonly System.Collections.Generic.List<Quest> quests = new System.Collections.Generic.List<Quest>(EClass.game.quests.list);
    private readonly System.Collections.Generic.List<Quest> globals = new System.Collections.Generic.List<Quest>(EClass.game.quests.globalList);
    private readonly System.Collections.Generic.HashSet<string> ids = new System.Collections.Generic.HashSet<string>(EClass.game.quests.completedIDs);
    public readonly System.Collections.Generic.HashSet<string> CompletedTypes = new System.Collections.Generic.HashSet<string>(EClass.game.quests.completedTypes);
    private readonly System.Collections.Generic.HashSet<int> zoneCompleted = new System.Collections.Generic.HashSet<int>(EClass._zone.completedQuests);
    private readonly System.Collections.Generic.HashSet<int> protectedUids = new System.Collections.Generic.HashSet<int>();
    private readonly System.Collections.Generic.Dictionary<Chara, string> charaStates = new System.Collections.Generic.Dictionary<Chara, string>();
    private readonly System.Collections.Generic.Dictionary<Quest, string> questStates = new System.Collections.Generic.Dictionary<Quest, string>();
    private readonly System.Collections.Generic.Dictionary<Thing, string> thingStates = new System.Collections.Generic.Dictionary<Thing, string>();
    private readonly System.Collections.Generic.Dictionary<Thing, object> itemParents = new System.Collections.Generic.Dictionary<Thing, object>();
    private readonly string inventory;
    private readonly string party;

    public NiPr7Baseline()
    {
        foreach (var c in mapCards) Protect(c);
        foreach (var c in dead) Protect(c);
        foreach (var c in EClass.game.cards.globalCharas.Values) Protect(c);
        foreach (var c in EClass.player.listCarryoverMap) Protect(c);
        Protect(pc);
        foreach (var item in mapItems) ProtectItem(item);
        foreach (var q in quests) questStates[q] = QuestState(q);
        foreach (var q in globals) questStates[q] = QuestState(q);
        inventory = Things(pc);
        party = Party();
    }
    private void Protect(Chara c)
    {
        if (c == null) return;
        protectedUids.Add(c.uid);
        charaStates[c] = CharaState(c);
        foreach (var item in c.things) ProtectItem(item);
    }
    private void ProtectItem(Thing item)
    {
        protectedUids.Add(item.uid);
        thingStates[item] = item.Num + ":" + item.isDestroyed + ":" + Things(item);
        itemParents[item] = item.parent;
        foreach (var child in item.things) ProtectItem(child);
    }
    public bool ProtectedUid(int uid) { return protectedUids.Contains(uid); }
    private static string CharaState(Chara c)
    {
        return c.hp + ":" + c.isDead + ":" + c.isDestroyed + ":" + c.LV + ":" + (c.host == null ? 0 : c.host.uid)
            + ":" + (c.ride == null ? 0 : c.ride.uid) + ":" + Things(c) + ":" + Conditions(c);
    }
    private static string Conditions(Chara c)
    {
        var text = new System.Text.StringBuilder();
        foreach (var condition in c.conditions) text.Append(condition.GetType().FullName).Append(':')
            .Append(condition.id).Append(':').Append(condition.value).Append(':').Append(condition.power).Append(';');
        return text.ToString();
    }
    private static string QuestState(Quest q)
    {
        return q.phase + ":" + q.isComplete + ":" + q.uid + ":" + q.id + ":"
            + Newtonsoft.Json.JsonConvert.SerializeObject(q.task);
    }
    private static string Things(Card card)
    {
        var text = new System.Text.StringBuilder();
        foreach (var t in card.things) text.Append(t.uid).Append(':').Append(t.Num).Append(':').Append(t.isDestroyed)
            .Append(':').Append(t.isEquipped).Append('[').Append(Things(t)).Append(']');
        return text.ToString();
    }
    private static string Party()
    {
        var text = new System.Text.StringBuilder();
        foreach (var c in EClass.pc.party.members) text.Append(c.uid).Append(',');
        return text.ToString();
    }
    private static bool Same<T>(System.Collections.Generic.IList<T> before, System.Collections.Generic.IList<T> after)
    {
        if (before.Count != after.Count) return false;
        for (int i = 0; i < before.Count; i++) if (!object.ReferenceEquals(before[i], after[i])) return false;
        return true;
    }
    public void RequireSameWorld()
    {
        RuntimeAssertions.Require(object.ReferenceEquals(pc, EClass.pc) && object.ReferenceEquals(game, EClass.game)
            && object.ReferenceEquals(player, EClass.player) && object.ReferenceEquals(map, EClass._map)
            && object.ReferenceEquals(zone, EClass._zone), "World/PC changed; discard stale refs and reload dedicated save.");
    }
    public void RestoreAndAssert()
    {
        RequireSameWorld();
        RequirePcLevelUnchanged();
        EClass.player.karma = karma;
        UnityEngine.Random.state = random;
        RuntimeAssertions.Require(Same(mapCards, EClass._map.charas) && Same(dead, EClass._map.deadCharas) && Same(mapItems, EClass._map.things)
            && Same(quests, EClass.game.quests.list) && Same(globals, EClass.game.quests.globalList)
            && ids.SetEquals(EClass.game.quests.completedIDs) && CompletedTypes.SetEquals(EClass.game.quests.completedTypes)
            && zoneCompleted.SetEquals(EClass._zone.completedQuests), "Native world/quest collections differ after fixture cleanup; reload save.");
        foreach (var entry in charaStates) RuntimeAssertions.Require(entry.Value == CharaState(entry.Key), "Existing Chara changed: " + entry.Key.uid);
        foreach (var entry in questStates) RuntimeAssertions.Require(entry.Value == QuestState(entry.Key), "Existing quest changed: " + entry.Key.uid);
        foreach (var entry in thingStates) RuntimeAssertions.Require(entry.Value == entry.Key.Num + ":" + entry.Key.isDestroyed + ":" + Things(entry.Key), "Existing item changed: " + entry.Key.uid);
        foreach (var entry in itemParents) RuntimeAssertions.Require(object.ReferenceEquals(entry.Value, entry.Key.parent), "Existing item parent changed: " + entry.Key.uid);
        RuntimeAssertions.Require(globalCards.Count == EClass.game.cards.globalCharas.Count && Same(carryover, EClass.player.listCarryoverMap),
            "Global/carryover membership changed; reload save.");
        foreach (var entry in globalCards)
        {
            Chara current;
            RuntimeAssertions.Require(EClass.game.cards.globalCharas.TryGetValue(entry.Key, out current)
                && object.ReferenceEquals(current, entry.Value), "Existing global UID/identity changed: " + entry.Key);
        }
        RuntimeAssertions.Require(inventory == Things(pc) && party == Party() && !pc.isDead
            && Elin_NiComment.NiCommentAPI.IsReady && Elin_NiComment.ModConfig.EnableMod.Value
            && !Elin_NiComment.Llm.LlmConfig.EnableLlm.Value, "PC inventory/party/config/overlay invariant failed; reload save.");
    }

    public void RequirePcLevelUnchanged()
    {
        RequireSameWorld();
        RuntimeAssertions.Require(pc.LV == pcLevel && pc.exp == pcExperience,
            "PC level/experience changed; this fixture must not level or restore the PC. Reload dedicated save.");
    }
}
