using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

// Native PR3 integration fixtures. Test-owned observation does not replace product behavior.
public static class Pr3ArenaObserver
{
    public static int TargetUid;
    public static ZoneInstanceArenaBattle Instance;
    public static long HealArgument;
    public static long DamageArgument;
    public static int HealCalls;
    public static int DamageCalls;
    public static int LeaveCalls;
    private static Pr3ArenaFixture rewardFixture;

    public static void RewardCreated(Thing item, ZoneInstanceArenaBattle producer)
    {
        var f = rewardFixture;
        if (f == null || item == null || !object.ReferenceEquals(producer, Instance) || producer.stageId != f.State.Token) return;
        int quantity = item.id == "plat" ? producer.rewardPlat : item.id == "lovepotion" ? producer.rewardPotion : 0;
        bool recorded = f.Rewards.Record(item.uid, item, item.id, quantity, producer, Instance, f.State.Inventory.ContainsKey(item.uid));
        f.RewardObservations.Add("reward_create_uid=" + item.uid + "; id=" + item.id + "; expected_Num=" + quantity + "; owned=" + recorded);
    }

    public static IEnumerable<HarmonyLib.CodeInstruction> ObserveRewardCreation(IEnumerable<HarmonyLib.CodeInstruction> instructions)
    {
        var result = new List<HarmonyLib.CodeInstruction>();
        var record = HarmonyLib.AccessTools.Method(typeof(Pr3ArenaObserver), nameof(RewardCreated),
            new[] { typeof(Thing), typeof(ZoneInstanceArenaBattle) });
        int matches = 0;
        foreach (var instruction in instructions)
        {
            result.Add(instruction);
            var method = instruction.operand as MethodInfo;
            if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                method != null && method.DeclaringType == typeof(ThingGen) && method.Name == "Create" && method.ReturnType == typeof(Thing))
            {
                // Observe the exact object returned at the product's creation callsite.
                // The original evaluation stack and subsequent SetNum/Pick remain intact.
                result.Add(new HarmonyLib.CodeInstruction(OpCodes.Dup));
                result.Add(new HarmonyLib.CodeInstruction(OpCodes.Ldarg_0));
                result.Add(new HarmonyLib.CodeInstruction(OpCodes.Call, record));
                matches++;
            }
        }
        RuntimeAssertions.Require(matches == 2, "GiveReward creation contract changed; cannot prove reward ownership. Found=" + matches);
        return result;
    }

    public static void HealPostfix(Card __instance, long a)
    {
        if (__instance.uid != TargetUid) return;
        HealArgument = a;
        HealCalls++;
    }

    public static void DamagePrefix(Card __instance, long dmg)
    {
        if (__instance.uid == TargetUid) DamageArgument = dmg;
    }

    public static void DamagePostfix(Card __instance)
    {
        if (__instance.uid == TargetUid) DamageCalls++;
    }

    public static void LeavePostfix(ZoneInstanceArenaBattle __instance)
    {
        if (object.ReferenceEquals(__instance, Instance)) LeaveCalls++;
    }

    public static void Install(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(TargetUid == 0 && Instance == null, "PR3 observer already active.");
        var harmony = new HarmonyLib.Harmony("runtime_test.pr3.arena." + Guid.NewGuid().ToString("N"));
        ctx.RegisterRollback("pr3.observer", () =>
        {
            try
            {
                harmony.UnpatchSelf();
                var healTarget = Elin_SukutsuArena.CardHealHpPatchTarget.Resolve("Pr3ObserverCleanup");
                var damageTarget = Elin_SukutsuArena.CardDamageHpPatchTarget.Resolve("Pr3ObserverCleanup");
                var leaveTarget = HarmonyLib.AccessTools.Method(typeof(ZoneInstanceArenaBattle), nameof(ZoneInstanceArenaBattle.OnLeaveZone), Type.EmptyTypes);
                var rewardTarget = HarmonyLib.AccessTools.Method(typeof(ZoneInstanceArenaBattle), "GiveReward", Type.EmptyTypes);
                RuntimeAssertions.Require(!CriticalCaseHelpers.HasPatchOwner(HarmonyCompatFacade.GetPatchInfo(healTarget), harmony.Id) &&
                    !CriticalCaseHelpers.HasPatchOwner(HarmonyCompatFacade.GetPatchInfo(damageTarget), harmony.Id) &&
                    !CriticalCaseHelpers.HasPatchOwner(HarmonyCompatFacade.GetPatchInfo(leaveTarget), harmony.Id) &&
                    !CriticalCaseHelpers.HasPatchOwner(HarmonyCompatFacade.GetPatchInfo(rewardTarget), harmony.Id), "Test Harmony owner remains.");
            }
            finally
            {
                TargetUid = 0;
                Instance = null;
                rewardFixture = null;
            }
            RuntimeAssertions.Require(TargetUid == 0 && Instance == null, "Observer cleanup failed.");
        });
        var heal = Elin_SukutsuArena.CardHealHpPatchTarget.Resolve("Pr3ArenaObserver");
        var damage = Elin_SukutsuArena.CardDamageHpPatchTarget.Resolve("Pr3ArenaObserver");
        RuntimeAssertions.Require(heal != null && damage != null, "Native long HP methods unavailable.");
        RuntimeAssertions.Require(damage.GetParameters().Length == 9, "This PR3 run requires the surveyed 9-argument DamageHP.");
        CriticalCaseHelpers.RequireOwnedPatch(HarmonyCompatFacade.GetPatchInfo(heal),
            Elin_SukutsuArena.Arena.ArenaConfig.ModGuid, "Arena HealHP product patch not loaded.");
        harmony.Patch(heal, postfix: new HarmonyLib.HarmonyMethod(typeof(Pr3ArenaObserver), nameof(HealPostfix)) { priority = HarmonyLib.Priority.Last });
        harmony.Patch(damage,
            prefix: new HarmonyLib.HarmonyMethod(typeof(Pr3ArenaObserver), nameof(DamagePrefix)) { priority = HarmonyLib.Priority.First },
            postfix: new HarmonyLib.HarmonyMethod(typeof(Pr3ArenaObserver), nameof(DamagePostfix)) { priority = HarmonyLib.Priority.Last });
        var leave = HarmonyLib.AccessTools.Method(typeof(ZoneInstanceArenaBattle), nameof(ZoneInstanceArenaBattle.OnLeaveZone), Type.EmptyTypes);
        RuntimeAssertions.Require(leave != null, "Arena OnLeaveZone() target unavailable.");
        harmony.Patch(leave,
            postfix: new HarmonyLib.HarmonyMethod(typeof(Pr3ArenaObserver), nameof(LeavePostfix)));
        var reward = HarmonyLib.AccessTools.Method(typeof(ZoneInstanceArenaBattle), "GiveReward", Type.EmptyTypes);
        RuntimeAssertions.Require(reward != null && reward.ReturnType == typeof(void), "Arena GiveReward() target unavailable.");
        rewardFixture = ctx.Get<Pr3ArenaFixture>(Pr3ArenaFixture.ContextKey);
        harmony.Patch(reward, transpiler: new HarmonyLib.HarmonyMethod(typeof(Pr3ArenaObserver), nameof(ObserveRewardCreation)));
        HealCalls = DamageCalls = LeaveCalls = 0;
        ctx.Log("observer_owner=" + harmony.Id + "; product=" + typeof(Elin_SukutsuArena.Plugin).Assembly.FullName);
    }
}

public sealed class Pr3ArenaManifest
{
    public string Token;
    public string SaveId;
    public string PreparedUtc;
    public int PreparedGameIdentity;
    public int PcUid;
    public int OriginUid;
    public int EntryX;
    public int EntryZ;
    public int PcHp;
    public int BattleUid;
    public bool ConditionFree;
    public bool HaltPlaylist;
    public string PendingDrama;
    public string QuestJson;
    public string CacheJson;
    public int CacheDay;
    public Dictionary<string, int> Flags;
    public Dictionary<int, int> Inventory;
}

public sealed class Pr3ArenaFixture
{
    public const string ContextKey = "pr3.fixture";
    public Pr3ArenaManifest State;
    public Zone Origin;
    public Zone Battle;
    public Chara Target;
    public ZoneInstance OriginalInstance;
    public List<ZoneEvent> OriginalEvents;
    public bool RetainForReload;
    public bool Restored;
    public readonly Pr3RewardOwnership Rewards = new Pr3RewardOwnership();
    public readonly List<string> RewardObservations = new List<string>();
    private bool localOverlay;
    private float healMessageTime;
    private bool ignoreAutoSave;
    private object returnInfo;

    public static void Guard(RuntimeTestContext ctx, bool transition, bool cleanEntry = true)
    {
        RuntimeAssertions.Require(EClass.pc != null && EClass.game != null && EClass.player != null && EClass._zone != null,
            "Loaded game/PC/zone required.");
        RuntimeAssertions.Require((EClass.pc.Name ?? "").IndexOf("RUNTIME_TEST", StringComparison.Ordinal) >= 0,
            "RUNTIME_TEST save guard rejected.");
        RuntimeAssertions.Require(!EClass.pc.isDead && !EClass.pc.isDestroyed && EClass.pc.MaxHP > 3, "Live PC required.");
        RuntimeAssertions.Require(!EClass._zone.IsRegion, "Start in a loaded local map, not the world map.");
        RuntimeAssertions.Require(LayerDrama.Instance == null, "Close any existing drama before PR3.");
        if (transition)
        {
            RuntimeAssertions.Require(string.Equals(RuntimeV2Config.CaseIdFilter, ctx.CaseId, StringComparison.Ordinal),
                "Destructive PR3 cases require an explicit single CaseId; reload baseline after each case.");
            RuntimeAssertions.Require(EClass.pc.party == null || EClass.pc.party.members.Count <= 1, "Use a test PC without companions.");
            if (cleanEntry)
            {
                RuntimeAssertions.Require(!EClass.pc.HasCondition<ConInvulnerable>(), "MoveZone removes ConInvulnerable; use a condition-free PC.");
                RuntimeAssertions.Require(EClass.pc.conditions.Count == 0, "Use a condition-free transition fixture PC.");
            }
        }
    }

    public static Pr3ArenaFixture Capture(RuntimeTestContext ctx, bool transition)
    {
        Guard(ctx, transition);
        RuntimeAssertions.Require(EClass._zone.instance == null, "Start outside an existing zone instance.");
        RuntimeAssertions.Require(EClass._zone.events.GetEvent<Elin_SukutsuArena.RandomBattle.ZoneEventNoHealing>() == null,
            "Existing NoHealing event is not a disposable fixture.");
        RuntimeAssertions.Require(EClass._zone.events.GetEvent<ZoneEventArenaBattle>() == null, "Existing battle is not a fixture.");
        var f = new Pr3ArenaFixture();
        f.Origin = EClass._zone;
        f.OriginalInstance = f.Origin.instance;
        f.OriginalEvents = new List<ZoneEvent>(f.Origin.events.list);
        f.State = new Pr3ArenaManifest
        {
            Token = "RUNTIME_TEST_PR3_" + Guid.NewGuid().ToString("N"), SaveId = Game.id,
            PreparedUtc = DateTime.UtcNow.ToString("o"),
            PreparedGameIdentity = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(EClass.game),
            PcUid = EClass.pc.uid, OriginUid = f.Origin.uid, EntryX = EClass.pc.pos.x, EntryZ = EClass.pc.pos.z,
            PcHp = EClass.pc.hp, HaltPlaylist = LayerDrama.haltPlaylist,
            PendingDrama = ZoneInstanceArenaBattle.PendingDirectDrama,
            ConditionFree = transition, QuestJson = QuestJson(), Flags = new Dictionary<string, int>(EClass.player.dialogFlags),
            Inventory = InventorySnapshot()
        };
        RuntimeAssertions.Require(f.State.EntryX > 0 && f.State.EntryZ > 0, "Choose a safe nonzero entry coordinate.");
        f.State.CacheJson = Newtonsoft.Json.JsonConvert.SerializeObject(CacheField("_cachedBattle").GetValue(null));
        f.State.CacheDay = (int)CacheField("_cachedDay").GetValue(null);
        f.healMessageTime = (float)HarmonyLib.AccessTools.Field(typeof(Elin_SukutsuArena.ArenaGimmickHealingPatches), "lastMessageTime").GetValue(null);
        f.ignoreAutoSave = EClass.debug.ignoreAutoSave;
        f.returnInfo = EClass.player.returnInfo;
        ctx.Set(ContextKey, f);
        ctx.RegisterRollback("pr3.restore_and_assert", () => f.Restore(ctx));
        if (transition)
        {
            RuntimeAssertions.Require(EClass.player.returnInfo == null, "Do not overwrite an existing return spell.");
            RuntimeAssertions.Require(string.IsNullOrEmpty(f.State.PendingDrama), "Pending arena drama already exists.");
            RequireNoPendingDialogs();
            foreach (Thing t in EClass.pc.things)
                RuntimeAssertions.Require(t.id != "plat" && t.id != "lovepotion", "Remove plat/lovepotion from fixture PC to avoid reward merging.");
            EClass.debug.ignoreAutoSave = true;
        }
        ctx.Log("fixture=" + f.State.Token + "; PC=" + f.State.PcUid + "; origin=" + f.State.OriginUid + "; entry=" + f.State.EntryX + "," + f.State.EntryZ);
        return f;
    }

    public static Pr3ArenaFixture FromManifest(RuntimeTestContext ctx, bool requireReload = true)
    {
        Guard(ctx, true, false);
        string path = ManifestPath(ctx);
        RuntimeAssertions.Require(File.Exists(path), "Run return_save_prepare first; manifest missing.");
        var state = Newtonsoft.Json.JsonConvert.DeserializeObject<Pr3ArenaManifest>(File.ReadAllText(path));
        RuntimeAssertions.Require(state != null && state.Token != null && state.Token.StartsWith("RUNTIME_TEST_PR3_", StringComparison.Ordinal), "Invalid PR3 manifest.");
        RuntimeAssertions.Require(state.SaveId == Game.id && state.PcUid == EClass.pc.uid, "Manifest belongs to a different save/PC.");
        if (requireReload)
            RuntimeAssertions.Require(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(EClass.game) != state.PreparedGameIdentity,
                "Game object has not been replaced: externally save/reload the disposable save before verify.");
        var f = new Pr3ArenaFixture { State = state };
        f.Origin = EClass.game.spatials.Find(state.OriginUid) as Zone;
        f.Battle = EClass.game.spatials.Find(state.BattleUid) as Zone;
        RuntimeAssertions.Require(f.Origin != null && f.Battle != null && EClass._zone.uid == state.BattleUid, "Reloaded battle/origin unavailable.");
        RuntimeAssertions.Require(f.Battle.instance is ZoneInstanceArenaBattle && ((ZoneInstanceArenaBattle)f.Battle.instance).stageId == state.Token,
            "Refuse cleanup of a zone not owned by this PR3 manifest.");
        f.OriginalInstance = f.Origin.instance;
        f.OriginalEvents = new List<ZoneEvent>(f.Origin.events.list);
        f.healMessageTime = (float)HarmonyLib.AccessTools.Field(typeof(Elin_SukutsuArena.ArenaGimmickHealingPatches), "lastMessageTime").GetValue(null);
        f.ignoreAutoSave = EClass.debug.ignoreAutoSave;
        f.returnInfo = null;
        ctx.Set(ContextKey, f);
        ctx.RegisterRollback("pr3.reload_restore_and_assert", () => f.Restore(ctx));
        EClass.debug.ignoreAutoSave = true;
        return f;
    }

    public static string ManifestPath(RuntimeTestContext ctx)
    {
        string safeId = Game.id;
        RuntimeAssertions.Require(!string.IsNullOrEmpty(safeId) && safeId.IndexOfAny(Path.GetInvalidFileNameChars()) < 0,
            "Game.id cannot be used as a fixture manifest name.");
        return Path.Combine(ctx.ModRoot, "tests", "runtime", "_artifacts", "pr3-return-" + safeId + ".json");
    }

    public void CreateTarget(RuntimeTestContext ctx)
    {
        // Rollback is registered before CharaGen; a detached native card cannot attack the PC.
        Target = CharaGen.Create("putit", 1);
        RuntimeAssertions.Require(Target != null && Target.MaxHP > 3, "Native putit fixture unavailable.");
        Target.c_altName = State.Token;
        Pr3ArenaObserver.TargetUid = Target.uid;
        ctx.Log("native_target_uid=" + Target.uid + "; MaxHP=" + Target.MaxHP);
    }

    public void SetLocalArena(bool arena, bool noHealing)
    {
        localOverlay = true;
        Origin.instance = arena ? new ZoneInstanceArenaBattle { returnX = State.EntryX, returnZ = State.EntryZ, uidZone = Origin.uid } : OriginalInstance;
        foreach (var e in new List<ZoneEvent>(Origin.events.list))
            if (e is Elin_SukutsuArena.RandomBattle.ZoneEventNoHealing) Origin.events.Remove(e);
        if (noHealing) Origin.events.Add(new Elin_SukutsuArena.RandomBattle.ZoneEventNoHealing());
    }

    public void CreateBattle(RuntimeTestContext ctx, bool legacySave = false)
    {
        var instance = new ZoneInstanceArenaBattle
        {
            uidMaster = 0, uidZone = Origin.uid, returnX = State.EntryX, returnZ = State.EntryZ,
            rewardPlat = 3, rewardPotion = 0, stageId = State.Token
        };
        var before = new HashSet<int>(EClass.game.spatials.map.Keys);
        // Capture a partially registered instance if the native factory throws after Register.
        ctx.RegisterRollback("pr3.partial_factory", () =>
        {
            foreach (var pair in new List<KeyValuePair<int, Spatial>>(EClass.game.spatials.map))
            {
                var z = pair.Value as Zone;
                if (!before.Contains(pair.Key) && z != null && object.ReferenceEquals(z.instance, instance) && z != Battle)
                {
                    RuntimeAssertions.Require(EClass._zone != z, "Partial factory zone is still active; reload baseline.");
                    z.parent?.RemoveChild(z);
                    z.DeleteMapRecursive();
                    EClass.game.spatials.Remove(z);
                }
            }
        });
        Battle = SpatialGen.CreateInstance("field", instance);
        RuntimeAssertions.Require(Battle != null && Battle.instance == instance, "Native CreateInstance did not produce the arena instance.");
        State.BattleUid = Battle.uid;
        Pr3ArenaObserver.Instance = instance;
        var stage = new Elin_SukutsuArena.BattleStageData
        {
            StageId = State.Token, DisplayNameJp = State.Token, DisplayNameEn = State.Token, ZoneType = "field",
            RewardPlat = 3,
            Enemies = new List<Elin_SukutsuArena.EnemyConfig>
            {
                new Elin_SukutsuArena.EnemyConfig { CharaId = "putit", Count = 1, Level = 1, Position = "random" }
            }
        };
        Battle.events.AddPreEnter(new ZonePreEnterArenaBattle { stageId = State.Token, stageData = stage });
        if (legacySave)
        {
            // Deliberately create old-save input. The verify case must not manually resync it.
            instance.x = 0;
            instance.z = 0;
        }
        ctx.Log("battle_uid=" + Battle.uid + "; stage=" + State.Token + "; base=" + instance.x + "," + instance.z + "; return=" + instance.returnX + "," + instance.returnZ);
    }

    public void AssertReturn(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.pc.uid == State.PcUid && EClass._zone.uid == State.OriginUid, "Return zone/PC UID mismatch.");
        RuntimeAssertions.Require(EClass.pc.pos.x == State.EntryX && EClass.pc.pos.z == State.EntryZ, "Native return did not reach entry coordinates.");
        RuntimeAssertions.Require(Pr3ArenaObserver.LeaveCalls == 1, "Expected one actual arena OnLeaveZone call.");
        RuntimeAssertions.Require(QuestJson() == State.QuestJson, "Quest state changed during isolated arena transition.");
        ctx.Log("native_return=" + EClass._zone.uid + ":" + EClass.pc.pos.x + "," + EClass.pc.pos.z + "; OnLeaveZone=" + Pr3ArenaObserver.LeaveCalls);
    }

    public void Restore(RuntimeTestContext ctx)
    {
        if (Restored) return;
        if (RetainForReload)
        {
            EClass.debug.ignoreAutoSave = ignoreAutoSave;
            ctx.Log("handoff:retained fixture " + State.Token + "; external save/reload required; not restored to baseline");
            return;
        }
        var errors = new List<string>();
        Action<string, Action> attempt = (name, action) =>
        {
            try { action(); } catch (Exception ex) { errors.Add(name + ": " + ex.Message); }
        };
        attempt("target", () =>
        {
            if (Target != null && !Target.isDestroyed) Target.Destroy();
            RuntimeAssertions.Require(Target == null || Target.isDestroyed, "Target fixture remains.");
        });
        attempt("origin", () =>
        {
            if (localOverlay) Origin.instance = OriginalInstance;
            RuntimeAssertions.Require(Origin.instance == OriginalInstance, "Origin instance changed.");
            if (localOverlay)
            {
                foreach (var e in new List<ZoneEvent>(Origin.events.list))
                    if (!OriginalEvents.Contains(e)) Origin.events.Remove(e);
            }
            RuntimeAssertions.Require(Origin.events.list.Count == OriginalEvents.Count, "Origin event count changed.");
            for (int i = 0; i < OriginalEvents.Count; i++)
                RuntimeAssertions.Require(object.ReferenceEquals(Origin.events.list[i], OriginalEvents[i]), "Origin events not restored.");
        });
        attempt("battle", () =>
        {
            if (Battle == null) return;
            RuntimeAssertions.Require(EClass._zone.uid != Battle.uid, "Battle still active: do not delete; reload baseline.");
            RuntimeAssertions.Require(Battle.instance is ZoneInstanceArenaBattle && ((ZoneInstanceArenaBattle)Battle.instance).stageId == State.Token,
                "Refusing to remove nonfixture zone.");
            Battle.parent?.RemoveChild(Battle);
            Battle.DeleteMapRecursive();
            EClass.game.spatials.Remove(Battle);
            RuntimeAssertions.Require(EClass.game.spatials.Find(State.BattleUid) == null, "Fixture spatial remains.");
        });
        attempt("inventory", () =>
        {
            foreach (string observation in RewardObservations) ctx.Log(observation);
            var retained = new List<string>();
            foreach (Thing t in new List<Thing>(EClass.pc.things))
            {
                if (!State.Inventory.ContainsKey(t.uid))
                {
                    if (Rewards.CanRemove(t.uid, t, t.id, t.Num))
                    {
                        ctx.Log("cleanup_owned_reward_uid=" + t.uid + "; id=" + t.id + "; Num=" + t.Num);
                        t.Destroy();
                    }
                    else
                    {
                        string residue = "uid=" + t.uid + "; id=" + t.id + "; Num=" + t.Num;
                        retained.Add(residue);
                        ctx.Log("cleanup_retained_unproven_item: " + residue + "; reload_baseline_required=true");
                    }
                }
            }
            RuntimeAssertions.Require(retained.Count == 0,
                "Unproven/changed items retained without deletion; reload dedicated baseline: " + string.Join(" | ", retained));
            var after = InventorySnapshot();
            RuntimeAssertions.Require(after.Count == State.Inventory.Count, "Inventory UID count not restored.");
            foreach (var p in State.Inventory)
                RuntimeAssertions.Require(after.ContainsKey(p.Key) && after[p.Key] == p.Value, "Original inventory changed, UID=" + p.Key);
        });
        attempt("flags", () =>
        {
            EClass.player.dialogFlags.Clear();
            foreach (var p in State.Flags) EClass.player.dialogFlags[p.Key] = p.Value;
            RuntimeAssertions.Require(EClass.player.dialogFlags.Count == State.Flags.Count, "Flags not restored.");
            foreach (var p in State.Flags) RuntimeAssertions.Require(EClass.player.dialogFlags[p.Key] == p.Value, "Flag value drift: " + p.Key);
        });
        attempt("static_state", () =>
        {
            LayerDrama.haltPlaylist = State.HaltPlaylist;
            ZoneInstanceArenaBattle.PendingDirectDrama = State.PendingDrama;
            CacheField("_cachedBattle").SetValue(null, Newtonsoft.Json.JsonConvert.DeserializeObject<Elin_SukutsuArena.RandomBattle.TodaysBattleData>(State.CacheJson));
            CacheField("_cachedDay").SetValue(null, State.CacheDay);
            HarmonyLib.AccessTools.Field(typeof(Elin_SukutsuArena.ArenaGimmickHealingPatches), "lastMessageTime").SetValue(null, healMessageTime);
            RuntimeAssertions.Require(LayerDrama.haltPlaylist == State.HaltPlaylist && ZoneInstanceArenaBattle.PendingDirectDrama == State.PendingDrama,
                "Drama state not restored.");
            RuntimeAssertions.Require((int)CacheField("_cachedDay").GetValue(null) == State.CacheDay, "Cache day not restored.");
            RuntimeAssertions.Require(Newtonsoft.Json.JsonConvert.SerializeObject(CacheField("_cachedBattle").GetValue(null)) == State.CacheJson,
                "Battle cache not restored.");
        });
        attempt("pc_and_quests", () =>
        {
            EClass.pc.hp = State.PcHp;
            if (State.ConditionFree)
            {
                foreach (Condition condition in new List<Condition>(EClass.pc.conditions)) condition.Kill(silent: true);
                RuntimeAssertions.Require(EClass.pc.conditions.Count == 0, "Fixture PC conditions not restored.");
            }
            if (EClass._zone.uid == State.OriginUid && (EClass.pc.pos.x != State.EntryX || EClass.pc.pos.z != State.EntryZ))
            {
                // Repair only in cleanup, after the transition assertion has already succeeded or failed.
                EClass.pc.Teleport(new Point(State.EntryX, State.EntryZ), true, true);
            }
            HarmonyLib.AccessTools.Field(typeof(Player), "returnInfo").SetValue(EClass.player, returnInfo);
            RuntimeAssertions.Require(EClass.pc.hp == State.PcHp, "PC HP not restored.");
            RuntimeAssertions.Require(QuestJson() == State.QuestJson, "Quest progression changed: reload baseline.");
            RuntimeAssertions.Require(EClass._zone.uid == State.OriginUid && EClass.pc.pos.x == State.EntryX && EClass.pc.pos.z == State.EntryZ,
                "Origin zone/coordinate not restored.");
        });
        EClass.debug.ignoreAutoSave = ignoreAutoSave;
        RuntimeAssertions.Require(EClass.debug.ignoreAutoSave == ignoreAutoSave, "Autosave switch not restored.");
        Restored = errors.Count == 0;
        ctx.Log("cleanup_scoped_restored=" + Restored + "; reload_baseline_required=true");
        RuntimeAssertions.Require(Restored, "PR3 cleanup failed; STOP and reload baseline: " + string.Join(" | ", errors));
    }

    public static string QuestJson()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(EClass.game.quests, GameIO.jsWriteGame);
    }

    private static Dictionary<int, int> InventorySnapshot()
    {
        var result = new Dictionary<int, int>();
        foreach (Thing t in EClass.pc.things) result.Add(t.uid, t.Num);
        return result;
    }

    private static FieldInfo CacheField(string name)
    {
        var field = HarmonyLib.AccessTools.Field(typeof(Elin_SukutsuArena.RandomBattle.TodaysBattleCache), name);
        RuntimeAssertions.Require(field != null, "Arena cache field missing: " + name);
        return field;
    }

    private static void RequireNoPendingDialogs()
    {
        foreach (string key in new[] { Elin_SukutsuArena.Flags.SessionFlagKeys.AutoDialog, Elin_SukutsuArena.Flags.SessionFlagKeys.DirectDrama })
            RuntimeAssertions.Require(!EClass.player.dialogFlags.ContainsKey(key) || EClass.player.dialogFlags[key] == 0, "Pending dialogue: " + key);
    }
}

public sealed class Pr3ArenaHealLongCase : RuntimeCaseBase
{
    public override string Id => "pr3.arena.heal_long";
    public override IReadOnlyList<string> Tags => new[] { "pr3", "integration", "arena" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        var f = Pr3ArenaFixture.Capture(ctx, false);
        Pr3ArenaObserver.Install(ctx);
        f.CreateTarget(ctx);
    }
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr3ArenaFixture>(Pr3ArenaFixture.ContextKey);
        long huge = (long)int.MaxValue + 17L;
        f.SetLocalArena(true, true);
        Check(ctx, f.Target, 2L, 0L, false);
        Check(ctx, f.Target, huge, 0L, false);
        Check(ctx, f.Target, 0L, 0L, false);
        Check(ctx, f.Target, -1L, -1L, false);
        f.SetLocalArena(true, false);
        Check(ctx, f.Target, 2L, 2L, true);
        Check(ctx, f.Target, huge, huge, true);
        f.SetLocalArena(false, true);
        Check(ctx, f.Target, 2L, 2L, true);
        Check(ctx, f.Target, huge, huge, true);
        ctx.Set("pr3.heal_checks", 8);
    }
    private static void Check(RuntimeTestContext ctx, Chara target, long input, long expectedArgument, bool heals)
    {
        target.hp = Math.Max(2, target.MaxHP / 2);
        int before = target.hp;
        int calls = Pr3ArenaObserver.HealCalls;
        target.HealHP(input, HealSource.None);
        RuntimeAssertions.Require(Pr3ArenaObserver.HealCalls == calls + 1 && Pr3ArenaObserver.HealArgument == expectedArgument,
            "HealHP native argument mismatch: input=" + input + ", observed=" + Pr3ArenaObserver.HealArgument);
        int expectedHp = heals ? (int)Math.Min((long)target.MaxHP, before + input) : before + (input < 0 ? (int)input : 0);
        RuntimeAssertions.Require(target.hp == expectedHp, "Native HealHP HP mismatch.");
        ctx.Log("HealHP uid=" + target.uid + "; input=" + input + "; native_arg=" + Pr3ArenaObserver.HealArgument + "; HP=" + before + "->" + target.hp);
    }
    public override void Verify(RuntimeTestContext ctx) { RuntimeAssertions.Require(ctx.Get<int>("pr3.heal_checks") == 8, "Heal controls incomplete."); }
}

public sealed class Pr3ArenaDamageLongCase : RuntimeCaseBase
{
    public override string Id => "pr3.arena.damage_long";
    public override IReadOnlyList<string> Tags => new[] { "pr3", "integration", "arena" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        var f = Pr3ArenaFixture.Capture(ctx, false);
        Pr3ArenaObserver.Install(ctx);
        f.CreateTarget(ctx);
    }
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr3ArenaFixture>(Pr3ArenaFixture.ContextKey);
        // Native build 24892994 caps damage at 99,999,999; overcap HP keeps this isolated target alive.
        f.Target.hp = 199999998;
        long input = (long)int.MaxValue + 17L;
        bool applied = Elin_SukutsuArena.CardDamageHpPatchTarget.Apply(f.Target, input, 0, 100, AttackSource.Finish, null, false);
        RuntimeAssertions.Require(applied, "Production long DamageHP adapter returned false.");
        RuntimeAssertions.Require(Pr3ArenaObserver.DamageCalls == 1 && Pr3ArenaObserver.DamageArgument == input,
            "Long damage was truncated or native 9-argument method did not complete.");
        RuntimeAssertions.Require(f.Target.hp == 99999999 && !f.Target.isDead, "Native damage cap/HP result mismatch on isolated target.");
        ctx.Log("DamageHP9 uid=" + f.Target.uid + "; input=" + input + "; observed=" + Pr3ArenaObserver.DamageArgument + "; HP=199999998->" + f.Target.hp);
    }
    public override void Verify(RuntimeTestContext ctx) { RuntimeAssertions.Require(Pr3ArenaObserver.DamageCalls == 1, "Damage call coverage missing."); }
}

public sealed class Pr3ArenaFactoryReturnCoordinatesCase : RuntimeCaseBase
{
    public override string Id => "pr3.arena.factory_return_coordinates";
    public override IReadOnlyList<string> Tags => new[] { "pr3", "integration", "arena", "regression" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        Pr3ArenaFixture.Capture(ctx, false);
        Pr3ArenaObserver.Install(ctx);
        ctx.Get<Pr3ArenaFixture>(Pr3ArenaFixture.ContextKey).CreateTarget(ctx);
    }
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr3ArenaFixture>(Pr3ArenaFixture.ContextKey);
        f.Target.pos = new Point(f.State.EntryX + 1, f.State.EntryZ);
        var cache = HarmonyLib.AccessTools.Field(typeof(Elin_SukutsuArena.BattleStageLoader), "_cachedStages");
        var path = HarmonyLib.AccessTools.Field(typeof(Elin_SukutsuArena.BattleStageLoader), "_lastLoadedPath");
        RuntimeAssertions.Require(cache != null && path != null, "Stage loader cache contract changed.");
        object cacheBefore = cache.GetValue(null);
        object pathBefore = path.GetValue(null);
        ctx.RegisterRollback("pr3.stage_loader", () =>
        {
            cache.SetValue(null, cacheBefore);
            path.SetValue(null, pathBefore);
            RuntimeAssertions.Require(object.ReferenceEquals(cache.GetValue(null), cacheBefore) && object.Equals(path.GetValue(null), pathBefore),
                "Stage loader cache not restored.");
        });
        var before = new HashSet<int>(EClass.game.spatials.map.Keys);
        try
        {
            // Real product entry: its post-factory synchronization must survive a master/PC position difference.
            Elin_SukutsuArena.ArenaManager.StartBattleByStage("rank_g_trial", f.Target);
        }
        finally
        {
            foreach (var pair in EClass.game.spatials.map)
            {
                var z = pair.Value as Zone;
                var arena = z != null ? z.instance as ZoneInstanceArenaBattle : null;
                if (!before.Contains(pair.Key) && arena != null && arena.uidMaster == f.Target.uid)
                {
                    f.Battle = z;
                    f.State.BattleUid = z.uid;
                    arena.stageId = f.State.Token;
                }
            }
        }
        RuntimeAssertions.Require(f.Battle != null, "Real StartBattleByStage did not create a zone; check deployed Package/battle_stages.json.");
        var instance = (ZoneInstanceArenaBattle)f.Battle.instance;
        ctx.Log("factory entry=" + f.State.EntryX + "," + f.State.EntryZ + "; return=" + instance.returnX + "," + instance.returnZ + "; base=" + instance.x + "," + instance.z);
        RuntimeAssertions.Require(instance.x == instance.returnX && instance.z == instance.returnZ,
            "Real ArenaManager entry lost returnX/Z synchronization after native CreateInstance. Manager requires post-factory sync.");
    }
    public override void Verify(RuntimeTestContext ctx) { }
}

public sealed class Pr3ArenaLegacyReturnJsonCase : RuntimeCaseBase
{
    public override string Id => "pr3.arena.legacy_return_json";
    public override IReadOnlyList<string> Tags => new[] { "pr3", "serialization", "arena" };
    public override void Prepare(RuntimeTestContext ctx) { Pr3ArenaFixture.Guard(ctx, false); }
    public override void Execute(RuntimeTestContext ctx)
    {
        var instance = new ZoneInstanceArenaBattle { returnX = 321, returnZ = 654, uidZone = EClass._zone.uid, stageId = "RUNTIME_TEST_PR3_LEGACY" };
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(instance, GameIO.jsWriteGame);
        var old = Newtonsoft.Json.Linq.JObject.Parse(json);
        old.Remove("x"); old.Remove("z");
        var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<ZoneInstanceArenaBattle>(old.ToString(), GameIO.jsReadGame);
        RuntimeAssertions.Require(loaded != null && loaded.returnX == 321 && loaded.returnZ == 654 && loaded.x == 321 && loaded.z == 654,
            "Real ZoneInstanceArenaBattle old-key deserialization did not restore base coordinates.");
        string roundtrip = Newtonsoft.Json.JsonConvert.SerializeObject(loaded, GameIO.jsWriteGame);
        var again = Newtonsoft.Json.JsonConvert.DeserializeObject<ZoneInstanceArenaBattle>(roundtrip, GameIO.jsReadGame);
        RuntimeAssertions.Require(again != null && again.x == 321 && again.z == 654 && again.uidZone == EClass._zone.uid,
            "Native serializer settings lost the return point on roundtrip.");
        ctx.Log("coverage=object_serialization_only; legacy_without_base_xy=" + old.ToString(Newtonsoft.Json.Formatting.None));
    }
    public override void Verify(RuntimeTestContext ctx) { }
}

public abstract class Pr3ArenaTransitionCase : RuntimeCaseBase, IRuntimeCoroutineCase
{
    protected abstract string Route { get; }
    public override IReadOnlyList<string> Tags => new[] { "pr3", "integration", "arena", "destructive", "transition" };
    public override void Prepare(RuntimeTestContext ctx) { throw new InvalidOperationException("Coroutine host required."); }
    public override void Execute(RuntimeTestContext ctx) { throw new InvalidOperationException("Coroutine host required."); }
    public override void Verify(RuntimeTestContext ctx) { throw new InvalidOperationException("Coroutine host required."); }
    public override void Cleanup(RuntimeTestContext ctx) { ctx.GetOrDefault<Pr3ArenaFixture>(Pr3ArenaFixture.ContextKey)?.Restore(ctx); }

    public IEnumerator PrepareAsync(RuntimeTestContext ctx)
    {
        Pr3ArenaFixture f;
        if (Route == "save_verify" || Route == "save_abort") f = Pr3ArenaFixture.FromManifest(ctx, Route == "save_verify");
        else f = Pr3ArenaFixture.Capture(ctx, true);
        Pr3ArenaObserver.Install(ctx);
        if (Route == "save_prepare")
            RuntimeAssertions.Require(!File.Exists(Pr3ArenaFixture.ManifestPath(ctx)), "Existing PR3 handoff manifest: finish/abort it first.");
        if (Route != "save_verify" && Route != "save_abort")
        {
            f.CreateBattle(ctx, Route == "save_prepare");
            EClass.player.dialogFlags[Elin_SukutsuArena.Flags.SessionFlagKeys.ArenaResult] = 0;
            EClass.player.dialogFlags[Elin_SukutsuArena.Flags.SessionFlagKeys.QuestBattle] = 0;
            EClass.pc.MoveZone(f.Battle, ZoneTransition.EnterState.Center);
            float deadline = UnityEngine.Time.realtimeSinceStartup + 20f;
            while (EClass._zone.uid != f.State.BattleUid && UnityEngine.Time.realtimeSinceStartup < deadline) yield return null;
            RuntimeAssertions.Require(EClass._zone.uid == f.State.BattleUid, "Timed out entering real battle zone.");
        }
        Pr3ArenaObserver.Instance = (ZoneInstanceArenaBattle)f.Battle.instance;
        if (Route == "save_abort") yield break;
        RuntimeAssertions.Require(EClass._zone.events.GetEvent<ZoneEventArenaBattle>() != null, "Real pre-enter did not add battle event.");
        RuntimeAssertions.Require(Pr3ArenaFixture.QuestJson() == f.State.QuestJson, "Quest state changed on fixture entry.");
        int enemyCount = 0;
        foreach (Chara c in EClass._map.charas)
        {
            if (!c.IsPC && !c.IsPCFaction && c.IsHostile() && !c.isDead)
            {
                enemyCount++;
                c.c_altName = f.State.Token;
                if (Route == "save_prepare") c.AddCondition<ConSleep>(1000, true);
            }
        }
        RuntimeAssertions.Require(enemyCount > 0, "No native enemy spawned; entry coverage incomplete.");
        ctx.Log("native_entry=" + f.State.BattleUid + "; live_enemy_count=" + enemyCount + "; route=" + Route);
    }

    public IEnumerator ExecuteAsync(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr3ArenaFixture>(Pr3ArenaFixture.ContextKey);
        var instance = (ZoneInstanceArenaBattle)f.Battle.instance;
        if (Route == "save_abort") yield break;
        if (Route == "save_prepare")
        {
            RuntimeAssertions.Require(instance.x == 0 && instance.z == 0 && instance.returnX == f.State.EntryX && instance.returnZ == f.State.EntryZ,
                "Legacy save input was overwritten during entry; inspect native pipeline.");
            string path = Pr3ArenaFixture.ManifestPath(ctx);
            RuntimeAssertions.Require(!File.Exists(path), "Existing PR3 handoff manifest: finish/abort it before preparing another.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(f.State, Newtonsoft.Json.Formatting.Indented));
            ctx.Log("handoff_manifest=" + path + "; action=externally_save_reload; legacy_base_xy=0,0");
            yield break;
        }
        RuntimeAssertions.Require(instance.returnX == f.State.EntryX && instance.returnZ == f.State.EntryZ && instance.x == f.State.EntryX && instance.z == f.State.EntryZ,
            "Arena base/return coordinates differ before exit; verify must not repair them.");
        if (Route == "victory")
        {
            foreach (Chara c in new List<Chara>(EClass._map.charas))
            {
                if (!c.IsPC && !c.IsPCFaction && c.IsHostile() && !c.isDead)
                {
                    Pr3ArenaObserver.TargetUid = c.uid;
                    RuntimeAssertions.Require(Elin_SukutsuArena.CardDamageHpPatchTarget.Apply(c, (long)c.hp + 1L, 0, 100, AttackSource.Finish, null, false),
                        "Real victory damage failed.");
                    RuntimeAssertions.Require(c.isDead || c.isDestroyed, "Native fixture enemy survived; victory not reached.");
                }
            }
        }
        else if (Route == "retreat") instance.LeaveZone();
        else EClass.pc.MoveZone(f.Origin, ZoneTransition.EnterState.Return);
        float deadline = UnityEngine.Time.realtimeSinceStartup + 20f;
        while (EClass._zone.uid != f.State.OriginUid && UnityEngine.Time.realtimeSinceStartup < deadline)
        {
            // Run the actual product event against the live native map; no child IEnumerator can escape cleanup.
            if (Route == "victory" && EClass._zone.uid == f.State.BattleUid)
                EClass._zone.events.GetEvent<ZoneEventArenaBattle>().OnTick();
            yield return null;
        }
        RuntimeAssertions.Require(EClass._zone.uid == f.State.OriginUid, "Timed out exiting battle; reload baseline.");
        f.AssertReturn(ctx);
        int result = EClass.player.dialogFlags[Elin_SukutsuArena.Flags.SessionFlagKeys.ArenaResult];
        RuntimeAssertions.Require(result == (Route == "victory" ? 1 : 2), "Unexpected actual arena result.");
        if (Route == "victory")
        {
            int rewards = 0;
            foreach (Thing t in EClass.pc.things)
                if (t.id == "plat" && f.Rewards.CanRemove(t.uid, t, t.id, t.Num))
                { rewards += t.Num; ctx.Log("owned_reward_uid=" + t.uid + "; Num=" + t.Num); }
            RuntimeAssertions.Require(rewards == 3, "Real victory reward missing or duplicated.");
        }
        ctx.Log("ArenaResult=" + result + "; real_damage_calls=" + Pr3ArenaObserver.DamageCalls);
    }

    public IEnumerator VerifyAsync(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr3ArenaFixture>(Pr3ArenaFixture.ContextKey);
        if (Route == "save_prepare")
        {
            RuntimeAssertions.Require(File.Exists(Pr3ArenaFixture.ManifestPath(ctx)) && EClass._zone.uid == f.State.BattleUid,
                "Save handoff fixture missing.");
            f.RetainForReload = true;
            ctx.Log("coverage=prepare_only; no_save_reload_or_return_claim");
        }
        else if (Route == "save_abort") ctx.Log("coverage=cleanup_only; no integration pass claim");
        else f.AssertReturn(ctx);
        yield break;
    }

    public IEnumerator CleanupAsync(RuntimeTestContext ctx)
    {
        var f = ctx.GetOrDefault<Pr3ArenaFixture>(Pr3ArenaFixture.ContextKey);
        if (f == null) yield break;
        if (!f.RetainForReload && EClass._zone.uid == f.State.BattleUid)
        {
            EClass.pc.MoveZone(f.Origin, ZoneTransition.EnterState.Return);
            float deadline = UnityEngine.Time.realtimeSinceStartup + 20f;
            while (EClass._zone.uid != f.State.OriginUid && UnityEngine.Time.realtimeSinceStartup < deadline) yield return null;
            RuntimeAssertions.Require(EClass._zone.uid == f.State.OriginUid, "Cleanup return timed out; STOP and reload baseline.");
        }
        f.Restore(ctx);
        if (!f.RetainForReload && f.Restored && (Route == "save_verify" || Route == "save_prepare" || Route == "save_abort"))
        {
            string path = Pr3ArenaFixture.ManifestPath(ctx);
            if (File.Exists(path))
            {
                var manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<Pr3ArenaManifest>(File.ReadAllText(path));
                if (manifest != null && manifest.Token == f.State.Token) File.Delete(path);
            }
        }
    }
}

public sealed class Pr3ArenaReturnVictoryCase : Pr3ArenaTransitionCase
{
    public override string Id => "pr3.arena.return_victory";
    protected override string Route => "victory";
}
public sealed class Pr3ArenaReturnRetreatCase : Pr3ArenaTransitionCase
{
    public override string Id => "pr3.arena.return_retreat";
    protected override string Route => "retreat";
}
public sealed class Pr3ArenaReturnVanillaCase : Pr3ArenaTransitionCase
{
    public override string Id => "pr3.arena.return_vanilla";
    protected override string Route => "vanilla";
}
public sealed class Pr3ArenaReturnSavePrepareCase : Pr3ArenaTransitionCase
{
    public override string Id => "pr3.arena.return_save_prepare";
    protected override string Route => "save_prepare";
    public override IReadOnlyList<string> Tags => new[] { "pr3", "save_reload", "destructive", "prepare_only" };
}
public sealed class Pr3ArenaReturnSaveVerifyCase : Pr3ArenaTransitionCase
{
    public override string Id => "pr3.arena.return_save_verify";
    protected override string Route => "save_verify";
    public override IReadOnlyList<string> Tags => new[] { "pr3", "save_reload", "destructive", "integration" };
}
public sealed class Pr3ArenaReturnSaveAbortCase : Pr3ArenaTransitionCase
{
    public override string Id => "pr3.arena.return_save_abort";
    protected override string Route => "save_abort";
    public override IReadOnlyList<string> Tags => new[] { "pr3", "save_reload", "destructive", "cleanup_only" };
}
