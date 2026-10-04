using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

// Declarations only: the runtime host owns execution in a disposable test save.
public sealed class Pr9RelocationFixtures
{
    private static Pr9RelocationFixtures active;
    private static bool cleanupFailed;
    private readonly RuntimeTestContext context;
    private readonly List<Card> created = new List<Card>();
    private readonly HashSet<int> fixtureUids = new HashSet<int>();
    private readonly Dictionary<int, int> moves = new Dictionary<int, int>();
    private readonly List<Action> restore = new List<Action>();
    private readonly List<Action> originalChecks = new List<Action>();
    private readonly Harmony observer;
    private readonly UnityEngine.Random.State random;
    private readonly int combatCount;
    private bool observing;
    private Thing observedDestination;
    private int observedSourceUid;
    private bool cleaned;
    private string cleanupError;
    public readonly string Marker = "PR9_RUNTIME_" + Guid.NewGuid().ToString("N");
    public readonly Chara Pc;
    public readonly Zone Zone;
    public readonly Elin_ItemRelocator.RelocatorManager Manager;

    public Pr9RelocationFixtures(RuntimeTestContext ctx)
    {
        Require(!cleanupFailed, "Prior PR9 cleanup failed; reload the disposable save.");
        Require(active == null, "PR9 fixture already active.");
        Pc = EClass.pc;
        Zone = EClass._zone;
        Require(Pc != null && Zone != null && EClass.player != null, "Game unavailable.");
        Require(Pc.Name.IndexOf("RUNTIME_TEST", StringComparison.OrdinalIgnoreCase) >= 0,
            "Refusing mutation outside a RUNTIME_TEST character.");
        context = ctx;
        random = UnityEngine.Random.state;
        combatCount = Pc.combatCount;
        Manager = Elin_ItemRelocator.RelocatorManager.Instance;
        Require(Manager != null, "ItemRelocator is not loaded.");
        observer = new Harmony("runtime.pr9." + Marker);
        // Register before cache, native patch, inventory, or party mutations.
        ctx.RegisterRollback(Marker, Cleanup);
        SnapshotManager();
        var ingredients = EClass.player.recipes.knownIngredients;
        var knownIngredients = new HashSet<string>(ingredients);
        restore.Add(() => {
            ingredients.Clear(); ingredients.UnionWith(knownIngredients);
            Require(ingredients.SetEquals(knownIngredients), "Recipe ingredient knowledge restore failed.");
        });
        originalChecks.Add(() => Require(ingredients.SetEquals(knownIngredients), "Recipe ingredient knowledge changed."));
        var originalParty = Pc.party == null ? null : new List<Chara>(Pc.party.members);
        var originalPartyIds = Pc.party == null ? null : new List<int>(Pc.party.uidMembers);
        var originalZoneThings = new List<Thing>(EClass._map.things);
        var originalGlobal = new Dictionary<int, Chara>(EClass.game.cards.globalCharas);
        var originalHp = Pc.hp;
        originalChecks.Add(() => {
            Require(EClass.pc == Pc && EClass._zone == Zone, "PC/zone identity changed.");
            Require(Same(originalZoneThings, EClass._map.things), "Zone fixture residue/order changed.");
            Require(originalGlobal.Count == EClass.game.cards.globalCharas.Count, "Global chara residue changed.");
            foreach (var pair in originalGlobal) {
                Chara current;
                Require(EClass.game.cards.globalCharas.TryGetValue(pair.Key, out current) && current == pair.Value,
                    "Original global chara identity changed.");
            }
            Require(Pc.hp == originalHp && Pc.combatCount == combatCount, "PC HP/combat state changed.");
            if (originalParty != null) {
                Require(Same(originalParty, Pc.party.members), "Party membership/order changed.");
                Require(Same(originalPartyIds, Pc.party.uidMembers), "Party UID order changed.");
            }
        });
        foreach (var thing in Pc.things) SnapshotThing(thing);
        foreach (var thing in EClass._map.things) SnapshotThing(thing);
        if (originalParty != null) foreach (var member in originalParty) {
            if (member != Pc) foreach (var thing in member.things) SnapshotThing(thing);
        }
        foreach (var slot in Pc.body.slots) {
            var original = slot.thing;
            originalChecks.Add(() => Require(slot.thing == original, "Original equipment changed."));
        }
        active = this;
        var addThing = typeof(Card).GetMethod("AddThing", new[] {
            typeof(Thing), typeof(bool), typeof(int), typeof(int) });
        Require(addThing != null, "Native AddThing signature unavailable.");
        observer.Patch(addThing, prefix: new HarmonyMethod(typeof(Pr9RelocationFixtures), "ObserveAddThing"));
        context.Log("fixture:" + Marker + ":pc=" + Pc.uid + ":zone=" + Zone.uid);
    }

    public Thing CreateThing(string id, int quantity = 1)
    {
        Require(EClass.sources.things.map.ContainsKey(id), "Missing native source: " + id);
        var thing = ThingGen.Create(id, -1, 1);
        Require(thing != null, "ThingGen returned null: " + id);
        Track(thing);
        thing.c_altName = Marker + "_" + thing.uid;
        thing.c_IDTState = 0;
        thing.SetBlessedState(BlessedState.Normal);
        thing.SetNum(quantity);
        context.Log("created:uid=" + thing.uid + ":id=" + id + ":num=" + thing.Num);
        return thing;
    }

    public void Track(Card card)
    {
        Require(card != null && !fixtureUids.Contains(card.uid), "Invalid/duplicate fixture.");
        fixtureUids.Add(card.uid);
        created.Add(card);
        foreach (var child in card.things) Track(child);
    }

    public Chara CreatePartyMember(string id)
    {
        Require(Pc.party != null && EClass.sources.charas.map.ContainsKey(id), "Missing party/chara source.");
        var member = CharaGen.Create(id, 1);
        Require(member != null, "CharaGen returned null.");
        Track(member);
        Require(!member.IsPCFaction, "Fixture faction would prevent native Destroy.");
        Zone.AddCard(member, Pc.pos.GetNearestPoint(allowBlock: false, allowChara: false));
        member.SetGlobal();
        Pc.party.AddMemeber(member, false);
        Require(member.IsAliveInCurrentZone && member.IsPCParty, "Party fixture is not accompanying.");
        return member;
    }

    public BodySlot Equip(Chara owner, Thing item, bool cursed)
    {
        Require(fixtureUids.Contains(item.uid), "Only fixture equipment may be changed.");
        owner.AddThing(item, false);
        var slot = owner.body.GetSlot(item, true);
        Require(slot != null && slot.thing == null, "No empty native equipment slot.");
        Require(owner.body.IsEquippable(item, slot, false), "Native equipment incompatible.");
        Require(owner.body.Equip(item, slot, false), "Native Equip failed.");
        Require(item.isEquipped && slot.thing == item, "Native slot did not equip fixture.");
        if (cursed) item.SetBlessedState(BlessedState.Cursed);
        return slot;
    }

    public Elin_ItemRelocator.RelocationProfile Profile(Thing destination, Thing target)
    {
        var profile = new Elin_ItemRelocator.RelocationProfile();
        var rule = new Elin_ItemRelocator.RelocationRule();
        rule.Conditions.Add(new Elin_ItemRelocator.ConditionText { Text = target.c_altName });
        Require(rule.IsMatch(target), "Production text condition does not select fixture.");
        profile.Rules.Add(rule);
        Manager.Profiles[Manager.GetProfileKey(destination)] = profile;
        Manager.ClearCache();
        return profile;
    }

    public bool PreviewHas(Thing destination, Thing target)
    {
        foreach (var match in Manager.GetMatches(destination)) if (match == target) return true;
        return false;
    }

    public bool CacheHas(Thing target)
    {
        var cache = (List<Elin_ItemRelocator.Candidate>)Field("_cachedCandidates").GetValue(Manager);
        foreach (var candidate in cache) if (candidate.Thing == target) return true;
        return false;
    }

    public void Move(Thing destination, Thing target, bool bulk)
    {
        Require(fixtureUids.Contains(destination.uid) && fixtureUids.Contains(target.uid),
            "Only generated fixture moves are permitted.");
        observing = true;
        observedDestination = destination;
        observedSourceUid = target.uid;
        try {
            if (bulk) Manager.ExecuteRelocation(destination);
            else Manager.RelocateSingleThing(target, destination);
        }
        finally { observing = false; }
    }

    public int MoveCount(Thing target)
    {
        int value;
        return moves.TryGetValue(target.uid, out value) ? value : 0;
    }

    public void RegisterRestore(Action action) { restore.Add(action); }

    public Action PutOnHotbar(Thing target)
    {
        Require(fixtureUids.Contains(target.uid), "Only fixtures may enter the hotbar.");
        foreach (var bar in EClass.player.hotbars.bars) {
            if (bar == null || !bar.IsUserHotbar) continue;
            for (int p = 0; p < bar.pages.Count; p++) {
                var page = bar.pages[p];
                for (int i = 0; i < page.items.Count; i++) {
                    if (page.items[i] != null) continue;
                    var index = i;
                    var pageIndex = p;
                    var items = new List<HotItem>(page.items);
                    var selected = page.selected;
                    var dirty = bar.dirty;
                    Action undo = () => {
                        page.items.Clear(); page.items.AddRange(items);
                        page.selected = selected;
                        bar.dirty = dirty;
                        Require(Same(items, page.items) && page.selected == selected && bar.dirty == dirty,
                            "Hotbar restore mismatch.");
                    };
                    RegisterRestore(undo);
                    bar.SetItem(new HotItemThing { thing = target, hotbar = bar }, index, pageIndex, false);
                    Require(page.items[index]?.Thing == target, "Native hotbar registration failed.");
                    return undo;
                }
            }
        }
        throw new InvalidOperationException("No empty user hotbar slot; prepare the disposable save.");
    }

    private static void ObserveAddThing(Card __instance, Thing t)
    {
        var fixture = active;
        if (fixture == null || !fixture.observing) return;
        // Fail before native mutation if a regression selects an original item.
        // This safety assertion is separate from the pass-through call counter.
        if (fixture.fixtureUids.Contains(__instance.uid)) {
            fixture.RequireFixtureTree(t);
        }
        if (__instance != fixture.observedDestination
            || t.uid != fixture.observedSourceUid || !fixture.fixtureUids.Contains(t.uid)) return;
        int value;
        fixture.moves.TryGetValue(t.uid, out value);
        fixture.moves[t.uid] = value + 1;
        fixture.context.Log("native:AddThing:uid=" + t.uid + ":num=" + t.Num + ":destination=" + __instance.uid);
    }

    private void RequireFixtureTree(Card card)
    {
        Pr9FixtureDestructionGuard.Validate<Card>(card, c => fixtureUids.Contains(c.uid), c => c.things);
    }

    public void DestroyFixture(Card card)
    {
        RequireFixtureTree(card);
        var thing = card as Thing;
        if (thing != null && thing.isEquipped) {
            var owner = thing.GetRootCard() as Chara;
            Require(owner != null, "Equipped fixture has no owner.");
            owner.body.Unequip(thing);
        }
        // Recheck after Unequip in case another native hook changed descendants.
        Pr9FixtureDestructionGuard.Destroy<Card>(card, c => fixtureUids.Contains(c.uid), c => c.things, c => {
            if (!c.isDestroyed) c.Destroy();
            Require(c.isDestroyed, "Fixture not destroyed: " + c.uid);
        });
    }

    private void SnapshotManager()
    {
        var profiles = new Dictionary<string, Elin_ItemRelocator.RelocationProfile>(Manager.Profiles);
        var cache = (List<Elin_ItemRelocator.Candidate>)Field("_cachedCandidates").GetValue(Manager);
        var candidates = new List<Elin_ItemRelocator.Candidate>(cache);
        var seen = (HashSet<Thing>)Field("_seen").GetValue(Manager);
        var originalSeen = new HashSet<Thing>(seen);
        var pool = (HashSet<Thing>)Field("_hotbarPool").GetValue(Manager);
        var originalPool = new HashSet<Thing>(pool);
        restore.Add(() => {
            Manager.Profiles.Clear();
            foreach (var pair in profiles) Manager.Profiles.Add(pair.Key, pair.Value);
            cache.Clear(); cache.AddRange(candidates);
            seen.Clear(); seen.UnionWith(originalSeen);
            pool.Clear(); pool.UnionWith(originalPool);
            Require(Same(candidates, cache) && seen.SetEquals(originalSeen) && pool.SetEquals(originalPool),
                "Manager cache restoration mismatch.");
            Require(Manager.Profiles.Count == profiles.Count, "Profiles restoration mismatch.");
            foreach (var pair in profiles) Require(Manager.Profiles[pair.Key] == pair.Value, "Profile identity changed.");
        });
    }

    private void SnapshotThing(Thing item)
    {
        var parent = item.parent;
        var root = item.GetRootCard();
        var num = item.Num;
        var equipped = item.c_equippedSlot;
        var important = item.c_isImportant;
        var locked = item.c_lockedHard;
        var gifted = item.isGifted;
        var npc = item.isNPCProperty;
        var sale = item.isSale;
        var x = item.invX;
        var y = item.invY;
        var place = item.placeState;
        var blessed = item.blessedState;
        originalChecks.Add(() => Require(!item.isDestroyed && item.parent == parent && item.GetRootCard() == root
            && item.Num == num && item.c_equippedSlot == equipped && item.c_isImportant == important
            && item.c_lockedHard == locked && item.isGifted == gifted && item.isNPCProperty == npc && item.isSale == sale
            && item.invX == x && item.invY == y && item.placeState == place && item.blessedState == blessed,
            "Original item changed: " + item.uid));
        foreach (var child in item.things) SnapshotThing(child);
    }

    public void Cleanup()
    {
        if (cleaned) {
            Require(cleanupError == null, cleanupError);
            return;
        }
        var errors = new List<string>();
        observing = false;
        try { observer.UnpatchSelf(); } catch (Exception ex) { errors.Add("observer:" + ex.Message); }
        for (int i = restore.Count - 1; i >= 0; i--) {
            try { restore[i](); } catch (Exception ex) { errors.Add("restore:" + ex.Message); }
        }
        for (int i = created.Count - 1; i >= 0; i--) {
            var card = created[i];
            try {
                DestroyFixture(card);
            } catch (Exception ex) { errors.Add("fixture:" + card.uid + ":" + ex.Message); }
        }
        Pc.combatCount = combatCount;
        UnityEngine.Random.state = random;
        foreach (var check in originalChecks) {
            try { check(); } catch (Exception ex) { errors.Add("invariant:" + ex.Message); }
        }
        active = null;
        cleaned = true;
        if (errors.Count != 0) {
            cleanupFailed = true;
            cleanupError = "PR9 cleanup failed; reload test save: " + string.Join("; ", errors);
            throw new InvalidOperationException(cleanupError);
        }
        context.Log("cleanup:original-state-preserved:" + Marker);
    }

    private static FieldInfo Field(string name)
    {
        var field = typeof(Elin_ItemRelocator.RelocatorManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Require(field != null, "Manager private contract missing: " + name);
        return field;
    }

    private static bool Same<T>(IList<T> a, IList<T> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++) if (!EqualityComparer<T>.Default.Equals(a[i], b[i])) return false;
        return true;
    }

    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
