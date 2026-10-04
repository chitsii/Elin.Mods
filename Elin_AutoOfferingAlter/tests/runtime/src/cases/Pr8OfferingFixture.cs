#if RUNTIME_TEST
using System;
using System.Collections.Generic;
using System.Reflection;

public sealed class Pr8OfferingFixture
{
    private readonly RuntimeTestContext ctx;
    private readonly Player originalPlayer;
    private readonly Game game;
    private readonly string originalPlayerState;
    private readonly string originalActorState;
    private readonly List<Pr8CardState> originalItems = new List<Pr8CardState>();
    private readonly List<Thing> created = new List<Thing>();
    private readonly List<Chara> actors = new List<Chara>();
    private readonly UnityEngine.Random.State randomState;
    private bool cleaned;
    private int itemSerial;
    public readonly string Token;
    public Player TestPlayer;
    public Chara Actor;
    public Chara OtherActor;
    public Thing Box;
    public Pr8OfferingObserver Observer;
    private string originalName;
    private string originalDeity;

    private Pr8OfferingFixture(RuntimeTestContext context)
    {
        ctx = context;
        Guard();
        game = EClass.game;
        originalPlayer = EClass.player;
        originalPlayerState = Newtonsoft.Json.JsonConvert.SerializeObject(originalPlayer, IO.dpFormat, IO.dpSetting);
        originalActorState = Newtonsoft.Json.JsonConvert.SerializeObject(originalPlayer.chara, IO.dpFormat, IO.dpSetting);
        foreach (Thing t in originalPlayer.chara.things) CaptureTree(t);
        randomState = UnityEngine.Random.state;
        Token = "RUNTIME_TEST.PR8." + Guid.NewGuid().ToString("N");
    }

    public static Pr8OfferingFixture Create(RuntimeTestContext ctx)
    {
        var f = new Pr8OfferingFixture(ctx);
        ctx.Set("pr8.fixture", f);
        ctx.RegisterRollback("pr8.restore_player_and_remove_owned_fixtures", f.Cleanup);
        f.Prepare();
        return f;
    }

    public static void Guard()
    {
        RuntimeAssertions.Require(EClass.pc != null && EClass.pc.Name.IndexOf("RUNTIME_TEST", StringComparison.Ordinal) >= 0,
            "PR8 requires a disposable RUNTIME_TEST save even when the runner guard is overridden.");
        RuntimeAssertions.Require(EClass.game != null && EClass._zone != null && EClass.pc.pos != null, "Active game/zone unavailable.");
        RuntimeAssertions.Require(EClass.sources.things.map.ContainsKey(Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX),
            "Offering box source missing. Cold boot with product mod; do not inject from the test.");
        RuntimeAssertions.Require(!EClass.pc.isDead && EClass.pc.conSleep == null, "Original PC must be alive and awake.");
    }

    private void Prepare()
    {
        // Clone native Player state so sleep recipe/dream/karma effects do not touch the original Player.
        // All operations stay synchronous; no frame runs while the fixture Player is installed.
        TestPlayer = IO.DeepCopy(originalPlayer);
        RuntimeAssertions.Require(TestPlayer != null && TestPlayer != originalPlayer, "Native Player isolation failed.");
        Actor = CreateActor();
        OtherActor = CreateActor();
        TestPlayer.chara = Actor;
        game.player = TestPlayer;
        Actor.c_altName = Token + ".actor";
        Actor.SetFaith("ehekatl");
        Observer = new Pr8OfferingObserver(this, ctx);
        Observer.Install();
        Box = ThingGen.Create(Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX);
        Track(Box);
        RuntimeAssertions.Require(Box != null && Box.id == Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX && Box.IsContainer,
            "ThingGen fallback is not an actual offering box.");
        Box.c_altName = Token + ".old_metadata";
        Box.c_idDeity = Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX;
        Box.pos.Set(Actor.pos);
        Actor.AddThing(Box, tryStack: false);
        originalName = Box.c_altName;
        originalDeity = Box.c_idDeity;
        RuntimeAssertions.Require(Box.parent == Actor, "Fixture box not attached to native PC inventory.");
        ctx.Log("fixture=" + Token + "; actor=" + Actor.uid + "; box=" + Box.uid + "; game=" + Game.id);
        ctx.Log("product=" + typeof(Elin_AutoOfferingAlter.OfferLogic).Assembly.FullName + "; observer=" + Observer.Owner);
    }

    private Chara CreateActor()
    {
        Chara actor = CharaGen.Create("putty", 5);
        RuntimeAssertions.Require(actor != null, "Native CharaGen fixture creation failed.");
        actors.Add(actor);
        foreach (Thing t in actor.things) Track(t);
        // Generated gear is fixture-owned; remove it before the native sleep scan.
        actor.RemoveThings();
        actor.party = new Party { _members = new List<Chara>() };
        EClass._zone.AddCard(actor, originalPlayer.chara.pos.GetNearestPoint(allowBlock: false, allowChara: false));
        actor.c_altName = Token + ".actor." + actors.Count;
        return actor;
    }

    public TraitAltar Oracle
    {
        get
        {
            Thing altar = ThingGen.Create("altar");
            Track(altar);
            altar.c_altName = Token + ".oracle";
            altar.pos.Set(Actor.pos);
            var trait = altar.trait as TraitAltar;
            RuntimeAssertions.Require(trait != null, "Native altar oracle unavailable.");
            trait.SetDeity(Actor.faith.id);
            return trait;
        }
    }

    public void Track(Thing item)
    {
        if (item == null || created.Contains(item)) return;
        created.Add(item);
    }

    public Thing Item(string id, int num, bool attach = true)
    {
        Thing item = ThingGen.Create(id);
        Track(item);
        RuntimeAssertions.Require(item != null && item.id == id, "Native ThingGen fallback for " + id);
        item.c_altName = Token + ".item." + (++itemSerial);
        item.SetNum(num);
        if (attach) Box.AddThing(item, tryStack: false);
        return item;
    }

    public Thing Offering(string category, int num, bool attach = true)
    {
        foreach (SourceThing.Row row in EClass.sources.things.rows)
        {
            if (row._origin != category && !row.Category.IsChildOf(category)) continue;
            Thing item = Item(row.id, num, attach: false);
            if (Actor.faith.GetOfferingValue(item, 1) <= 0 || !Oracle.CanOffer(Actor, item))
            {
                item.Destroy();
                continue;
            }
            if (attach) Box.AddThing(item, tryStack: false);
            ctx.Log("input=" + category + "; uid=" + item.uid + "; id=" + item.id + "; num=" + item.Num
                + "; unit=" + Actor.faith.GetOfferingValue(item, 1));
            return item;
        }
        throw new InvalidOperationException("No native offerable " + category + " fixture. This is blocked, not passed.");
    }

    public int SplitSize(Thing item)
    {
        int batch = Elin_AutoOfferingAlter.OfferingBatchRunner<Thing>.CalculateBatchSize(Actor.faith.GetOfferingValue(item, 1));
        RuntimeAssertions.Require(batch > 1 && batch < 5000, "Fixture must admit a real bounded split.");
        return batch;
    }

    public Thing ItemForIngredient(Recipe.Ingredient ingredient)
    {
        string id = ingredient.id;
        if (ingredient.useCat)
        {
            id = null;
            foreach (SourceThing.Row row in EClass.sources.things.rows)
            {
                if (row.Category.IsChildOf(ingredient.id)) { id = row.id; break; }
            }
        }
        RuntimeAssertions.Require(!string.IsNullOrEmpty(id), "Native craft ingredient fixture unavailable.");
        Thing item = Item(id, Math.Max(ingredient.req, 1), attach: false);
        Actor.AddThing(item, tryStack: false);
        return item;
    }

    public void RemoveSleep(Chara actor, bool completed, bool dead)
    {
        RuntimeAssertions.Require(actor.conSleep == null, "Fixture already sleeping.");
        ConSleep sleep = actor.AddCondition<ConSleep>(100, force: true) as ConSleep;
        RuntimeAssertions.Require(sleep != null && actor.conditions.Contains(sleep), "Native AddCondition<ConSleep> failed.");
        sleep.slept = completed;
        sleep.pickup = false;
        sleep.uidRide = sleep.uidParasite = 0;
        actor.isDead = dead;
        sleep.Kill(silent: true);
        RuntimeAssertions.Require(!actor.conditions.Contains(sleep) && actor.conSleep == null, "Native sleep removal failed.");
        ctx.Log("sleep_removed: actor=" + actor.uid + "; completed=" + completed + "; dead=" + dead
            + "; offers=" + Observer.OfferCalls.Count);
    }

    public int SumNamed(string name)
    {
        int sum = 0;
        var seen = new HashSet<int>();
        foreach (Thing item in created)
            if (!item.isDestroyed && item.c_altName == name && seen.Add(item.uid)) sum += item.Num;
        return sum;
    }

    public void AssertAllNamedInBox(string name)
    {
        foreach (Thing item in created)
        {
            if (item.isDestroyed || item.c_altName != name) continue;
            RuntimeAssertions.Require(item.parent == Box && Box.things.Contains(item) && item.GetRootCard() == Actor,
                "Split fixture escaped its box: " + Pr8CardState.Describe(item));
        }
    }

    public void AssertRetained(Thing item, int quantity)
    {
        RuntimeAssertions.Require(!item.isDestroyed && item.Num == quantity && item.parent == Box && Box.things.Contains(item),
            "Retained fixture quantity/parent changed: " + Pr8CardState.Describe(item));
    }

    public void AssertMetadata()
    {
        RuntimeAssertions.Require(Box != null && Box.c_altName == originalName && Box.c_idDeity == originalDeity,
            "Box name/deity metadata was not restored.");
        RuntimeAssertions.Require(Box.parent == Actor, "Box owner changed.");
    }

    public void LogState(string phase)
    {
        ctx.Log(phase + ":box=" + Pr8CardState.Describe(Box));
        foreach (Thing item in Box.things) ctx.Log(phase + ":item=" + Pr8CardState.Describe(item));
    }

    private void CaptureTree(Thing item)
    {
        originalItems.Add(new Pr8CardState(item));
        foreach (Thing child in item.things) CaptureTree(child);
    }

    private void Cleanup()
    {
        if (cleaned) return;
        cleaned = true;
        var failures = new List<string>();
        // Restore PC before destroying fixture actors: Destroy must not enter the PC death lifecycle.
        game.player = originalPlayer;
        if (Observer != null) TryCleanup(() => Observer.Dispose(), failures);
        foreach (Chara actor in actors)
        {
            actor.isDead = false;
            if (actor.conSleep != null) actor.conSleep.slept = false;
        }
        for (int i = created.Count - 1; i >= 0; i--)
        {
            Thing item = created[i];
            TryCleanup(() => { if (item != null && !item.isDestroyed) item.Destroy(); }, failures);
        }
        foreach (Chara actor in actors)
            TryCleanup(() => { if (!actor.isDestroyed) actor.Destroy(); }, failures);
        UnityEngine.Random.state = randomState;
        TryCleanup(() =>
        {
            RuntimeAssertions.Require(EClass.game == game && EClass.player == originalPlayer && EClass.pc == originalPlayer.chara,
                "Original game/Player/PC reference not restored.");
            foreach (Pr8CardState state in originalItems) state.AssertUnchanged();
            RuntimeAssertions.Require(Newtonsoft.Json.JsonConvert.SerializeObject(originalPlayer, IO.dpFormat, IO.dpSetting) == originalPlayerState,
                "Original serialized Player state changed; stop and reload disposable fixture save.");
            RuntimeAssertions.Require(Newtonsoft.Json.JsonConvert.SerializeObject(originalPlayer.chara, IO.dpFormat, IO.dpSetting) == originalActorState,
                "Original PC health/faith/conditions/equipment state changed; stop and reload disposable fixture save.");
            foreach (Thing item in created) RuntimeAssertions.Require(item == null || item.isDestroyed, "Fixture thing leaked.");
            foreach (Chara actor in actors)
                RuntimeAssertions.Require(actor.isDestroyed && !EClass._map.charas.Contains(actor), "Fixture actor leaked into active map.");
            ctx.Log("cleanup: original Player/inventory restored; native fixture UIDs destroyed");
        }, failures);
        if (failures.Count > 0) throw new InvalidOperationException(string.Join(" | ", failures.ToArray()));
    }

    private static void TryCleanup(Action action, List<string> failures)
    {
        try { action(); }
        catch (Exception ex) { failures.Add(ex.GetType().Name + ": " + ex.Message); }
    }
}

public sealed class Pr8CardState
{
    private readonly Thing item;
    private readonly string before;
    public Pr8CardState(Thing item) { this.item = item; before = Describe(item); }
    public void AssertUnchanged() { RuntimeAssertions.Require(Describe(item) == before, "Original inventory changed: " + before); }
    public static string Describe(Thing t)
    {
        if (t == null) return "null";
        return "uid=" + t.uid + ";id=" + t.id + ";num=" + t.Num + ";parent=" + (t.parent is Card ? ((Card)t.parent).uid : 0)
            + ";root=" + (t.GetRootCard() == null ? 0 : t.GetRootCard().uid) + ";pos=" + t.pos.x + "," + t.pos.z
            + ";inv=" + t.invX + "," + t.invY + ";equipped=" + t.isEquipped + ";dead=" + t.isDestroyed
            + ";bless=" + t.blessedState + ";name=" + t.c_altName + ";deity=" + t.c_idDeity;
    }
}

public sealed class Pr8OfferCall
{
    public int Uid, Num, ActorUid;
    public string Faith, Deity;
    public bool DestroyedAfter;
}
public sealed class Pr8InjectedOfferingException : Exception { }

public sealed class Pr8OfferingObserver : IDisposable
{
    private static Pr8OfferingObserver active;
    private readonly Pr8OfferingFixture fixture;
    private readonly RuntimeTestContext ctx;
    private readonly HarmonyLib.Harmony harmony;
    public readonly string Owner;
    public readonly List<Pr8OfferCall> OfferCalls = new List<Pr8OfferCall>();
    private readonly Dictionary<int, int> splits = new Dictionary<int, int>();
    public int NormalOfferingCalls;
    public Action AfterFirstOffer;
    public bool ThrowBeforeFirstOffer;
    private bool injected;
    public Pr8OfferingObserver(Pr8OfferingFixture f, RuntimeTestContext context)
    {
        fixture = f; ctx = context;
        Owner = "runtime_test.pr8." + f.Token;
        harmony = new HarmonyLib.Harmony(Owner);
    }
    public void Install()
    {
        RuntimeAssertions.Require(active == null, "Another PR8 observer is active.");
        active = this;
        Patch(typeof(TraitAltar), "OnOffer", new[] { typeof(Chara), typeof(Thing) }, "BeforeOffer", "AfterOffer");
        Patch(typeof(TraitAltar), "_OnOffer", new[] { typeof(Chara), typeof(Thing), typeof(int) }, null, "AfterNormalOffer");
        Patch(typeof(TraitAltar), "CanOffer", new[] { typeof(Chara), typeof(Card) }, null, "AfterCanOffer");
        Patch(typeof(Card), "Split", new[] { typeof(int) }, null, "AfterSplit");
        Patch(typeof(ThingGen), "Create", new[] { typeof(string), typeof(int), typeof(int) }, null, "AfterCreate");
    }
    private void Patch(Type type, string method, Type[] args, string prefix, string postfix)
    {
        MethodInfo target = HarmonyLib.AccessTools.Method(type, method, args);
        RuntimeAssertions.Require(target != null, "Native observer target missing: " + type.Name + "." + method);
        harmony.Patch(target,
            prefix == null ? null : new HarmonyLib.HarmonyMethod(typeof(Pr8OfferingObserver), prefix),
            postfix == null ? null : new HarmonyLib.HarmonyMethod(typeof(Pr8OfferingObserver), postfix));
    }
    private static bool Matches(TraitAltar altar)
    {
        return active != null && active.fixture.Box != null && altar.owner == active.fixture.Box;
    }
    private static void BeforeOffer(TraitAltar __instance, Chara c, Thing t)
    {
        if (!Matches(__instance)) return;
        active.OfferCalls.Add(new Pr8OfferCall { Uid = t.uid, Num = t.Num, ActorUid = c.uid, Faith = c.faith.id, Deity = __instance.Deity.id });
        active.ctx.Log("OnOffer: " + Pr8CardState.Describe(t) + ";actor=" + c.uid + ";faith=" + c.faith.id + ";deity=" + __instance.Deity.id);
        if (active.ThrowBeforeFirstOffer && active.OfferCalls.Count == 1) throw new Pr8InjectedOfferingException();
    }
    private static void AfterOffer(TraitAltar __instance, Thing t)
    {
        if (!Matches(__instance)) return;
        active.OfferCalls[active.OfferCalls.Count - 1].DestroyedAfter = t.isDestroyed;
        if (!active.injected && active.AfterFirstOffer != null)
        {
            active.injected = true;
            active.AfterFirstOffer();
        }
    }
    private static void AfterNormalOffer(TraitAltar __instance)
    {
        if (Matches(__instance)) active.NormalOfferingCalls++;
    }
    private static void AfterCanOffer(TraitAltar __instance, Chara cc, Card c, bool __result)
    {
        if (!Matches(__instance)) return;
        RuntimeAssertions.Require(cc == active.fixture.Actor && __instance.Deity == cc.faith, "CanOffer actor/deity were not set before native predicate.");
        active.ctx.Log("CanOffer: actor=" + cc.uid + ";item=" + c.uid + ";accepted=" + __result);
    }
    private static void AfterSplit(Card __instance, Thing __result)
    {
        if (active == null || __instance.parent != active.fixture.Box) return;
        int count;
        active.splits.TryGetValue(__instance.uid, out count);
        active.splits[__instance.uid] = count + 1;
        active.fixture.Track(__result);
        active.ctx.Log("Split: source=" + __instance.uid + ";detached=" + Pr8CardState.Describe(__result));
    }
    private static void AfterCreate(Thing __result)
    {
        if (active != null && EClass.player == active.fixture.TestPlayer) active.fixture.Track(__result);
    }
    public int CallsFor(int uid)
    {
        int count = 0;
        foreach (Pr8OfferCall call in OfferCalls) if (call.Uid == uid) count++;
        return count;
    }
    public int SplitsFor(int uid) { int count; return splits.TryGetValue(uid, out count) ? count : 0; }
    public void Reset() { OfferCalls.Clear(); splits.Clear(); NormalOfferingCalls = 0; injected = false; }
    public void AssertActorAndDeity(int uid, string faith)
    {
        foreach (Pr8OfferCall call in OfferCalls)
            RuntimeAssertions.Require(call.ActorUid == uid && call.Faith == faith && call.Deity == faith, "OnOffer actor/faith/deity mismatch.");
    }
    public void Dispose()
    {
        harmony.UnpatchSelf();
        if (active == this) active = null;
        foreach (MethodBase method in HarmonyLib.Harmony.GetAllPatchedMethods())
        {
            var info = HarmonyLib.Harmony.GetPatchInfo(method);
            if (info == null) continue;
            foreach (string owner in info.Owners)
                RuntimeAssertions.Require(owner != Owner, "PR8 observer patch leaked: " + method.Name);
        }
    }
}

public static class Pr8SourceChecks
{
    public static void AssertPostBoot(RuntimeTestContext ctx)
    {
        SourceManager source = EClass.sources;
        string id = Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX;
        RuntimeAssertions.Require(source.initialized, "SourceManager not initialized. Do not initialize from the test.");
        int count = 0;
        SourceThing.Row custom = null, chest = null;
        foreach (SourceThing.Row row in source.things.rows)
        {
            if (row.id == id) { count++; custom = row; }
            if (row.id == "chest6") chest = row;
        }
        RuntimeAssertions.Require(count == 1 && custom != null && chest != null, "Custom row missing/duplicated or chest6 missing.");
        RuntimeAssertions.Require(source.things.map[id] == custom && source.cards.map[id] == custom, "Native source maps do not reference registered row.");
        RuntimeAssertions.Require(custom.factory.Length == 1 && custom.factory[0] == "self" && custom.recipeKey[0] == "*", "Custom recipe properties wrong.");
        foreach (FieldInfo field in typeof(SourceThing.Row).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!field.FieldType.IsArray) continue;
            var baseArray = field.GetValue(chest) as Array;
            var customArray = field.GetValue(custom) as Array;
            RuntimeAssertions.Require(baseArray == null || !ReferenceEquals(baseArray, customArray), "Shared source array: " + field.Name);
            if (field.Name != "factory" && field.Name != "recipeKey")
                RuntimeAssertions.Require(Newtonsoft.Json.JsonConvert.SerializeObject(baseArray) == Newtonsoft.Json.JsonConvert.SerializeObject(customArray),
                    "Inherited chest6 array differs: " + field.Name);
        }
        string beforeChest = Newtonsoft.Json.JsonConvert.SerializeObject(chest);
        int beforeRows = source.things.rows.Count;
        source.Init(); // Initialized standard no-op only; never reset initialized or invoke Reload.
        RuntimeAssertions.Require(source.things.rows.Count == beforeRows && Newtonsoft.Json.JsonConvert.SerializeObject(chest) == beforeChest,
            "Initialized Init no-op mutated source/chest6.");
        RuntimeAssertions.Require(RecipeManager.Get(id) != null, "Native RecipeManager did not build custom recipe.");
        ctx.Log("coverage=postboot_contract; rows=" + count + "; map/cards/recipe resolve; initialized Init is no-op");
    }
}
#endif
