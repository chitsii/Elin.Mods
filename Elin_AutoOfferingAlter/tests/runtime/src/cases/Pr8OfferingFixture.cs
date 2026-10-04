#if RUNTIME_TEST
using System;
using System.Collections.Generic;
using System.Reflection;

public sealed class Pr8OfferingFixture
{
    private readonly RuntimeTestContext ctx;
    private readonly Player originalPlayer;
    private readonly Chara originalActor;
    private readonly Game game;
    private readonly Pr8PlayerScope<Player> playerScope;
    private readonly Pr8NativeStateScope nativeState;
    private readonly Pr8CardOwnership ownership;
    private readonly Pr8NativeInputs inputs;
    private readonly string originalPlayerState;
    private readonly string originalActorState;
    private readonly List<Pr8CardState> originalItems = new List<Pr8CardState>();
    private readonly List<Thing> created = new List<Thing>();
    private readonly List<Chara> actors = new List<Chara>();
    private readonly UnityEngine.Random.State randomState;
    private bool cleaned;
    private int itemSerial;
    private bool valueBonusFailureLogged;
    private readonly List<object> diagnosticEvents = new List<object>();
    public readonly string Token;
    public Player TestPlayer;
    public Religion LuckFaith, EythFaith;
    public Chara Actor;
    public Chara OtherActor;
    public Thing Box;
    public Pr8OfferingObserver Observer;
    public Pr8OfferingPositionState PositionState;
    private ICardParent expectedBoxParent;
    private string originalName;
    private string originalDeity;

    private Pr8OfferingFixture(RuntimeTestContext context)
    {
        ctx = context;
        Guard();
        nativeState = new Pr8NativeStateScope();
        game = EClass.game;
        originalPlayer = EClass.player;
        originalActor = originalPlayer.chara;
        playerScope = new Pr8PlayerScope<Player>(() => game.player, value => game.player = value);
        originalPlayerState = Newtonsoft.Json.JsonConvert.SerializeObject(originalPlayer, IO.dpFormat, IO.dpSetting);
        originalActorState = Newtonsoft.Json.JsonConvert.SerializeObject(originalActor, IO.dpFormat, IO.dpSetting);
        foreach (Thing t in originalActor.things) CaptureTree(t);
        randomState = UnityEngine.Random.state;
        Token = "RUNTIME_TEST.PR8." + Guid.NewGuid().ToString("N");
        ownership = new Pr8CardOwnership(Token, ObserveAllocation);
        inputs = new Pr8NativeInputs(ctx, Token, Track);
    }

    public static Pr8OfferingFixture Create(RuntimeTestContext ctx)
    {
        Guard();
        Pr8NativePrerequisites.AuditAll(ctx);
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
        // Player deep copy traverses the save's Zone/Region graph and invokes deserialize callbacks.
        // Use independently initialized native Player state, with the active zone and a temporary creation PC reference.
        // No original Player field is changed; only game.player is swapped and restored by rollback.
        // All operations stay synchronous; no frame runs while the fixture Player is installed.
        nativeState.Begin();
        ownership.Install();
        TestPlayer = new Player { zone = game.activeZone };
        RuntimeAssertions.Require(TestPlayer != null && TestPlayer != originalPlayer, "Native Player isolation failed.");
        foreach (FieldInfo field in typeof(Player).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (field.FieldType.IsValueType || field.FieldType == typeof(string) || field.Name == "zone") continue;
            object owned = field.GetValue(TestPlayer);
            object original = field.GetValue(originalPlayer);
            RuntimeAssertions.Require(owned == null || !ReferenceEquals(owned, original),
                "Fixture Player shares mutable original field: " + field.Name);
        }
        RuntimeAssertions.Require(TestPlayer.recipes != null && TestPlayer.flags != null && TestPlayer.stats != null
            && TestPlayer.domains != null && TestPlayer.zone == game.activeZone,
            "Fresh Player native sleep/craft state or active zone context unavailable.");
        LuckFaith = Pr8NativePrerequisites.ResolveFaith<ReligionLuck>();
        EythFaith = Pr8NativePrerequisites.ResolveFaith<ReligionEyth>();
        // Select the isolated Player before native callbacks, while keeping a fully initialized PC reference.
        // Player.OnCreateGame assigns its PC only AFTER CharaGen.Create returns. SetAI inside OnCreate needs this order.
        // This temporary original Actor reference is read context only; the original Player is never selected or mutated.
        RuntimeAssertions.Require(NativeBodyReady(originalActor) && originalActor.party != null && originalActor.party.members != null
            && TestPlayer.queues != null, "Native creation PC/body/party/queue context unavailable.");
        Faction nativePcFaction = originalActor.faction;
        Faction registeredFaction;
        RuntimeAssertions.Require(nativePcFaction != null && nativePcFaction.charaElements != null
            && game.factions.dictAll.TryGetValue(nativePcFaction.uid, out registeredFaction)
            && ReferenceEquals(registeredFaction, nativePcFaction), "Original native PC faction/element context unavailable.");
        Actor = new Chara();
        actors.Add(Actor);
        Actor.faction = nativePcFaction; // Assign only the owned actor id; do not add it to faction membership.
        TestPlayer.chara = originalActor;
        playerScope.Select(TestPlayer);
        Observer = new Pr8OfferingObserver(this, ctx);
        Observer.Install();
        Pr8CreationOrder.CompleteBeforeSelection(
            () => CreateNativeActor(Actor, "primary"), () => NativeBodyReady(Actor),
            () => { TestPlayer.chara = Actor; TestPlayer.uidChara = Actor.uid; });
        FinishActor(Actor);
        AssertOriginalState("after_primary_actor");
        OtherActor = CreateActor();
        AssertOriginalState("after_other_actor");
        Actor.SetFaith(LuckFaith);
        Box = inputs.CreateExact(Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX, 1);
        Track(Box);
        RuntimeAssertions.Require(Box != null && Box.id == Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX && Box.IsContainer,
            "ThingGen fallback is not an actual offering box.");
        // Contents are not owned merely because a creation callback inserted them into this box.
        // Remove them before tests count offerings or stage only their explicit controls.
        ownership.GuardTree(Box); // Complete recursive ownership validation BEFORE any deletion.
        var generatedContents = new List<Thing>(Box.things);
        foreach (Thing item in generatedContents) item.Destroy();
        RuntimeAssertions.Require(Box.things.Count == 0, "Native fixture box was not emptied.");
        Box.c_altName = Token + ".old_metadata";
        Box.c_idDeity = Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX;
        Box.pos.Set(Actor.pos);
        Actor.AddThing(Box, tryStack: false);
        originalName = Box.c_altName;
        originalDeity = Box.c_idDeity;
        RuntimeAssertions.Require(Box.parent == Actor, "Fixture box not attached to native PC inventory.");
        expectedBoxParent = Actor;
        PositionState = new Pr8OfferingPositionState(Box);
        AuditOwnedInputs();
        AssertOriginalState("after_native_preflight");
        ctx.Log("fixture=" + Token + "; actor=" + Actor.uid + "; box=" + Box.uid + "; game=" + Game.id);
        ctx.Log("product=" + typeof(Elin_AutoOfferingAlter.OfferLogic).Assembly.FullName + "; observer=" + Observer.Owner);
    }

    private Chara CreateActor()
    {
        Chara actor = new Chara();
        actors.Add(actor);
        CreateNativeActor(actor, "other");
        FinishActor(actor);
        return actor;
    }

    private void CreateNativeActor(Chara actor, string role)
    {
        RuntimeAssertions.Require(EClass.player == TestPlayer && EClass.pc != actor && NativeBodyReady(EClass.pc)
            && EClass.pc.party != null && EClass.pc.party.members != null && TestPlayer.queues != null,
            "Native Create requires an isolated Player and a fully initialized, different PC.");
        Diagnostic("create_before_" + role, () => new { actor = ActorSnapshot(actor), pc = ActorSnapshot(EClass.pc) });
        try { actor.Create("putty", -1, 5); }
        catch (Exception ex)
        {
            Diagnostic("create_failed_" + role, () => new { actor = ActorSnapshot(actor), pc = ActorSnapshot(EClass.pc), error = ex.ToString() });
            throw;
        }
        AssertOriginalState("after_native_create_" + role);
        Diagnostic("create_after_" + role, () => new { actor = ActorSnapshot(actor), pc = ActorSnapshot(EClass.pc) });
    }

    private static bool NativeBodyReady(Chara actor)
    {
        return actor != null && actor.isCreated && !actor.isDestroyed && actor.source != null && actor.renderer != null
            && actor.body != null && actor.body.slots != null && actor.elements != null && actor.conditions != null && actor.ai != null;
    }

    public void SetAiDiagnostic(Chara actor, AIAct goal, Exception error)
    {
        if (!actors.Contains(actor)) return;
        Diagnostic(error == null ? "set_ai_before" : "set_ai_failed", () => new {
            actor = ActorSnapshot(actor), pc = ActorSnapshot(EClass.pc), goalNull = goal == null,
            playerQueuesNull = EClass.player == null || EClass.player.queues == null,
            hookOwners = new List<string>(HarmonyLib.Harmony.GetPatchInfo(
                HarmonyLib.AccessTools.DeclaredMethod(typeof(Chara), "SetAI", new[] { typeof(AIAct) })).Owners),
            error = error == null ? null : error.ToString() });
    }

    public void ValueBonusFailure(ElementContainerCard container, Element element, Exception error)
    {
        if (valueBonusFailureLogged) return;
        valueBonusFailureLogged = true;
        Diagnostic("value_bonus_failed", () => new { elementId = element == null ? 0 : element.id,
            elementType = element == null ? null : element.GetType().Name,
            ownerUid = container == null || container.owner == null ? 0 : container.owner.uid,
            ownerIsFixtureActor = container != null && actors.Contains(container.owner as Chara),
            pc = ActorSnapshot(EClass.pc), actor = ActorSnapshot(Actor), error = error.ToString() });
    }

    private object ActorSnapshot(Chara actor)
    {
        if (actor == null) return null;
        return new { uid = actor.uid, id = actor.id, idFaction = actor.idFaction, position = PointSnapshot(actor.pos),
            factionNull = ReadDiagnostic(() => actor.faction == null),
            factionUid = ReadDiagnostic(() => actor.faction == null ? null : actor.faction.uid),
            factionElementsNull = ReadDiagnostic(() => actor.faction == null || actor.faction.charaElements == null),
            aiNull = actor.ai == null, bodyReady = NativeBodyReady(actor), selectedPc = ReferenceEquals(EClass.pc, actor), created = actor.isCreated, destroyed = actor.isDestroyed,
            tracked = actors.Contains(actor), ownership = ReadDiagnostic(() => { ownership.RequireOwned(actor); return true; }),
            rendererNull = actor.renderer == null, rendererHasActor = actor.renderer != null && actor.renderer.hasActor,
            parentType = actor.parent == null ? null : actor.parent.GetType().Name,
            inMap = ReadDiagnostic(() => EClass._map != null && EClass._map.charas.Contains(actor)),
            globallyRegistered = ReadDiagnostic(() => game.cards.globalCharas.ContainsKey(actor.uid)),
            partyNull = actor.party == null, heldNull = actor.held == null, currentZoneNull = actor.currentZone == null,
            children = actor.things == null ? -1 : actor.things.Count };
    }

    private static object ReadDiagnostic(Func<object> read)
    {
        try { return read(); } catch (Exception ex) { return "probe_error:" + ex.GetType().Name + ":" + ex.Message; }
    }

    private static object PointSnapshot(Point point)
    {
        if (point == null) return null;
        var cells = Point.map == null ? null : Point.map.cells;
        return new { x = point.x, z = point.z, mapWidth = cells == null ? (int?)null : cells.GetLength(0),
            mapHeight = cells == null ? (int?)null : cells.GetLength(1),
            inBounds = cells != null && point.x >= 0 && point.z >= 0 && point.x < cells.GetLength(0) && point.z < cells.GetLength(1) };
    }

    public void OfferDiagnostic(TraitAltar altar, Chara actor, Thing item, Exception error)
    {
        Diagnostic(error == null ? "offer_before" : "offer_failed", () => new {
            owner = altar.owner == null ? null : Pr8CardState.Describe(altar.owner as Thing),
            ownerPosition = altar.owner == null ? null : PointSnapshot(altar.owner.pos),
            ownerRendererNull = altar.owner == null || altar.owner.renderer == null,
            ownerRendererHasActor = altar.owner != null && altar.owner.renderer != null && altar.owner.renderer.hasActor,
            actor = ActorSnapshot(actor), root = ActorSnapshot(altar.owner == null ? null : altar.owner.GetRootCard() as Chara),
            item = Pr8NativeInputs.ItemSnapshot(item),
            zoneUid = EClass._zone == null ? (int?)null : EClass._zone.uid,
            activeZoneUid = game.activeZone == null ? (int?)null : game.activeZone.uid,
            selectedPlayerZoneIsActive = ReferenceEquals(TestPlayer.zone, game.activeZone),
            nativePointMapIsActive = ReferenceEquals(Point.map, EClass._map),
            error = error == null ? null : error.ToString() });
    }

    private void Diagnostic(string stage, Func<object> readData)
    {
        // Snapshot evaluation, JSON, file and log writes are all optional diagnostics.
        Pr8Diagnostic.BestEffort(() =>
        {
            // A full file persists even if the shared 64-entry log budget is exhausted. Each stage adds only one JSON line.
            var record = new { stage = stage, token = Token, data = readData() };
            diagnosticEvents.Add(record);
            string path = System.IO.Path.Combine(ctx.ModRoot, "tests", "runtime", "_artifacts", "pr8-create", Token + ".json");
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(diagnosticEvents, Newtonsoft.Json.Formatting.Indented));
            }
            catch (Exception ex) { ctx.Log("pr8_diagnostic_write_failed:" + ex.GetType().Name); }
            ctx.Log("pr8_native_json:" + Newtonsoft.Json.JsonConvert.SerializeObject(record));
        });
    }

    private void DestroyActor(Chara actor)
    {
        if (actor.isDestroyed) return;
        ownership.GuardTree(actor);
        if (actor.renderer == null)
        {
            bool mapKnown = EClass._map != null && EClass._map.charas != null;
            bool eligible = Pr8PartialCleanup.CanSupplyEmptyRenderer(true, actor.isCreated, true, actor.parent == null,
                mapKnown, mapKnown && EClass._map.charas.Contains(actor), actor.global != null || game.cards.globalCharas.ContainsKey(actor.uid),
                actor.party == null, actor.held == null && actor.currentZone == null, actor.things.Count);
            Diagnostic("partial_cleanup_gate", () => new { actor = ActorSnapshot(actor), eligible = eligible });
            RuntimeAssertions.Require(eligible, "Renderer-less owned actor is registered/nonempty; refusing speculative cleanup.");
            // CardRenderer hasActor is a plain false field. Card.Destroy only tests it in this empty/unregistered path.
            // Do not call _CreateRenderer/SetOwner: they would create display state or depend on incomplete source/traits.
            actor.renderer = new CardRenderer { owner = actor, isChara = true };
            RuntimeAssertions.Require(!actor.renderer.hasActor && actor.renderer.actor == null, "Cleanup-only renderer acquired a native actor.");
        }
        actor.Destroy();
    }

    private void FinishActor(Chara actor)
    {
        RuntimeAssertions.Require(EClass.player == TestPlayer && EClass.pc == Actor && actor.renderer != null,
            "Native placement must run on the selected fixture Player/PC after Create.");
        foreach (Thing t in actor.things) Track(t);
        // Generated gear is fixture-owned; remove it before the native sleep scan.
        actor.RemoveThings();
        actor.party = new Party { _members = new List<Chara> { actor } };
        EClass._zone.AddCard(actor, originalActor.pos.GetNearestPoint(allowBlock: false, allowChara: false));
        actor.c_altName = Token + ".actor." + actors.Count;
    }

    private void AuditOwnedInputs()
    {
        var errors = new List<string>();
        int firstAuditItem = created.Count;
        try
        {
            CheckOwnedInput(errors, "sleep/actor owner + native body/renderer/party", () =>
                RuntimeAssertions.Require(Actor.IsPC && Actor.renderer != null && Actor.body != null && Actor.body.slots != null
                    && Actor.elements != null && Actor.conditions != null && Actor.party.members.Contains(Actor)
                    && TestPlayer.recipes != null && TestPlayer.stats != null && TestPlayer.domains != null,
                    "Generated native PC/sleep owner not ready."));
            CheckOwnedInput(errors, "water37 native predicate", () =>
                RuntimeAssertions.Require(Oracle.CanOffer(Actor, Item("water", 37, attach: false)), "Native water precondition missing."));
            CheckOwnedInput(errors, "consuming fish/meat values + bounded split", () =>
            {
                SplitSize(Offering("fish", 1, attach: false));
                SplitSize(Offering("meat", 1, attach: false));
            });
            CheckOwnedInput(errors, "element764 rejected control", () =>
            {
                Thing rejected = Offering("meat", 1, attach: false);
                rejected.elements.SetBase(764, 1);
                RuntimeAssertions.Require(!Oracle.CanOffer(Actor, rejected), "Native rejection control missing.");
            });
            CheckOwnedInput(errors, "Eyth native refusal + bounded split", () =>
            {
                try
                {
                    Actor.SetFaith(EythFaith);
                    Actor.elements.SetBase(1228, 0);
                    Thing meat = Offering("meat", 1, attach: false);
                    SplitSize(meat);
                    Oracle.OnOffer(Actor, meat);
                    RuntimeAssertions.Require(!meat.isDestroyed && meat.Num == 1, "Native Eyth refusal precondition missing.");
                }
                finally { Actor.SetFaith(LuckFaith); }
            });
            CheckOwnedInput(errors, "native RecipeCard + ingredients/materials", () =>
            {
                Recipe recipe = Recipe.Create(RecipeManager.Get(Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX));
                RuntimeAssertions.Require(recipe is RecipeCard, "Quick recipe does not select the native RecipeCard override.");
                recipe.BuildIngredientList();
                RuntimeAssertions.Require(recipe.ingredients.Count > 0, "Native craft ingredient list missing.");
                foreach (Recipe.Ingredient ingredient in recipe.ingredients)
                {
                    Thing item = ItemForIngredient(ingredient);
                    RuntimeAssertions.Require(item.Num >= ingredient.req && item.material != null, "Native ingredient quantity/material missing.");
                }
            });
        }
        finally
        {
            for (int i = created.Count - 1; i >= firstAuditItem; i--)
                if (!created[i].isDestroyed) created[i].Destroy();
            Observer.Reset();
        }
        if (errors.Count != 0) throw new InvalidOperationException("PR8 owned native preflight failed: " + string.Join(" | ", errors.ToArray()));
    }

    private void CheckOwnedInput(List<string> errors, string name, Action check)
    {
        try { check(); ctx.Log("native_preflight:pass:" + name); }
        catch (Exception ex) { errors.Add(name + ":" + ex.Message); ctx.Log("native_preflight:failed:" + name + ":" + ex.Message); }
    }

    public void AssertOriginalState(string stage)
    {
        CheckSerializedOriginal("player", stage, originalPlayerState,
            Newtonsoft.Json.JsonConvert.SerializeObject(originalPlayer, IO.dpFormat, IO.dpSetting));
        CheckSerializedOriginal("pc", stage, originalActorState,
            Newtonsoft.Json.JsonConvert.SerializeObject(originalActor, IO.dpFormat, IO.dpSetting));
    }

    private void CheckSerializedOriginal(string kind, string stage, string before, string after)
    {
        if (before == after) return;
        string directory = System.IO.Path.Combine(ctx.ModRoot, "tests", "runtime", "_artifacts", "pr8-preservation", Token);
        System.IO.Directory.CreateDirectory(directory);
        System.IO.File.WriteAllText(System.IO.Path.Combine(directory, stage + "." + kind + ".before.json"), before);
        System.IO.File.WriteAllText(System.IO.Path.Combine(directory, stage + "." + kind + ".after.json"), after);
        List<string> changes = Pr8StateDiff.Describe(before, after);
        System.IO.File.WriteAllLines(System.IO.Path.Combine(directory, stage + "." + kind + ".diff.txt"), changes);
        ctx.Log("preservation_failed: stage=" + stage + ";kind=" + kind + ";diff=" + directory + ";first=" + changes[0]);
        // Retain exact serialized equality. Diagnostics do not filter, normalize or restore any field.
        RuntimeAssertions.Require(after == before, "Original serialized " + kind + " changed at " + stage + "; " + changes[0]);
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

    private void ObserveAllocation(Card card)
    {
        // Called only after the ownership observer accepted a fresh allocation or owned-source Split.
        var thing = card as Thing;
        if (thing != null && !created.Contains(thing)) created.Add(thing);
        var actor = card as Chara;
        if (actor != null && !actors.Contains(actor)) actors.Add(actor);
    }

    public void Track(Thing item)
    {
        if (item == null) return;
        ownership.RequireOwned(item);
        if (!created.Contains(item)) created.Add(item);
    }

    public Thing Item(string id, int num, bool attach = true)
    {
        Thing item = inputs.CreateExact(id, num);
        Track(item);
        RuntimeAssertions.Require(item != null && item.id == id, "Native ThingGen fallback for " + id);
        item.c_altName = Token + ".item." + (++itemSerial);
        item.SetNum(num);
        if (attach) Box.AddThing(item, tryStack: false);
        return item;
    }

    public Thing Offering(string category, int num, bool attach = true)
    {
        inputs.RecordCatalog(category);
        foreach (SourceThing.Row row in Pr8NativeInputs.ConcreteRows())
        {
            if (!Pr8NativeInputs.Matches(row, category)) continue;
            Thing item = Item(row.id, num, attach: false);
            bool matches = Pr8NativeInputs.Matches(item.source, category);
            int unit = Actor.faith.GetOfferingValue(item, 1);
            bool accepted = Oracle.CanOffer(Actor, item);
            int batch = unit > 0 ? Elin_AutoOfferingAlter.OfferingBatchRunner<Thing>.CalculateBatchSize(unit) : 0;
            bool bounded = batch > 1 && batch < 5000;
            bool ordinary = !item.HasTag(CTAG.godArtifact) && !item.HasElement(766);
            inputs.Record("offering_candidate", () => new { requestedCategory = category, requestedId = row.id,
                actual = Pr8NativeInputs.ItemSnapshot(item), faith = Actor.faith.id, semanticMatch = matches,
                unit = unit, canOffer = accepted, batch = batch, boundedSplit = bounded, ordinaryFoodBranch = ordinary });
            if (!matches || unit <= 0 || !accepted || !bounded || !ordinary)
            {
                item.Destroy();
                continue;
            }
            if (attach) Box.AddThing(item, tryStack: false);
            RuntimeAssertions.Require(matches && unit > 0 && accepted && bounded && ordinary && item.Num == num && item.material != null,
                "Selected native offering input does not satisfy semantic/faith/quantity/material/split contract.");
            if (ctx.Logs.Count < 44) ctx.Log("input=" + category + "; uid=" + item.uid + "; id=" + item.id + "; num=" + item.Num
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
        inputs.RecordCatalog(ingredient.id);
        // Native Ingredient.IsValidIngredient accepts categories or id/origin, with material tag constraints.
        foreach (SourceThing.Row row in Pr8NativeInputs.ConcreteRows())
        {
            bool likely = ingredient.useCat ? row.Category.IsChildOf(ingredient.id)
                : row.id == ingredient.id || row._origin == ingredient.id;
            foreach (string alternate in ingredient.idOther)
                likely |= ingredient.useCat ? row.Category.IsChildOf(alternate) : row._origin == alternate;
            if (!likely) continue;
            Thing item = Item(row.id, Math.Max(ingredient.req, 1), attach: false);
            bool valid = ingredient.IsValidIngredient(item);
            inputs.Record("craft_ingredient_candidate", () => new { requestedIngredient = ingredient.id,
                useCat = ingredient.useCat, req = ingredient.req, requestedId = row.id, actual = Pr8NativeInputs.ItemSnapshot(item), nativeValid = valid });
            if (!valid) { item.Destroy(); continue; }
            RuntimeAssertions.Require(item.material != null && item.Num >= ingredient.req,
                "Native ingredient quantity/material contract failed.");
            Actor.AddThing(item, tryStack: false);
            return item;
        }
        throw new InvalidOperationException("No concrete native valid craft ingredient: " + ingredient.id);
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
        RuntimeAssertions.Require(Box.parent == expectedBoxParent && Box.GetRootCard() == Actor, "Box owner changed.");
        PositionState.AssertUnchanged();
        RuntimeAssertions.Require(!Pr8OfferingPositionState.ScopeIsActive(), "Product offering FX scope leaked.");
    }

    public void MoveBoxIntoToolbelt()
    {
        foreach (SourceThing.Row row in Pr8NativeInputs.ConcreteRows())
        {
            if (row.trait == null || row.trait.Length == 0 || row.trait[0].IndexOf("toolbelt", StringComparison.OrdinalIgnoreCase) < 0) continue;
            Thing belt = Item(row.id, 1, attach: false);
            if (!(belt.trait is TraitToolBelt) || !belt.IsContainer) { belt.Destroy(); continue; }
            Actor.AddThing(belt, tryStack: false);
            belt.AddThing(Box, tryStack: false);
            RuntimeAssertions.Require(Box.parent == belt && belt.parent == Actor && Box.GetRootCard() == Actor,
                "Native nested toolbelt placement failed.");
            expectedBoxParent = belt;
            PositionState = new Pr8OfferingPositionState(Box);
            ctx.Log("toolbelt_position: belt=" + belt.uid + ";source=" + belt.id + ";box=" + Pr8CardState.Describe(Box));
            return;
        }
        throw new InvalidOperationException("Loaded concrete native ToolBelt source unavailable; no fabricated belt.");
    }

    public void AssertInvalidWorldStopsBeforeSplit(Thing item)
    {
        int originalNum = item.Num;
        item.SetNum(SplitSize(item) * 2 + 1);
        int quantity = item.Num, calls = Observer.OfferCalls.Count, splits = Observer.SplitsFor(item.uid);
        try
        {
            foreach (string fault in new[] { "point_map", "player_zone", "x_bounds", "z_bounds" })
            {
                Map originalMap = Point.map; Zone originalZone = TestPlayer.zone;
                int x = Actor.pos.x, z = Actor.pos.z;
                try
                {
                    if (fault == "point_map") Point.map = null;
                    if (fault == "player_zone") TestPlayer.zone = null;
                    if (fault == "x_bounds") Actor.pos.x = EClass._map.cells.GetLength(0);
                    if (fault == "z_bounds") Actor.pos.z = -1;
                    Elin_AutoOfferingAlter.OfferLogic.Process(Box);
                }
                finally { Point.map = originalMap; TestPlayer.zone = originalZone; Actor.pos.Set(x, z); }
                RuntimeAssertions.Require(!item.isDestroyed && item.Num == quantity && item.parent == Box
                    && Observer.OfferCalls.Count == calls && Observer.SplitsFor(item.uid) == splits,
                    "Invalid world context reached native split/offer: " + fault);
                AssertMetadata();
                ctx.Log("world_guard:pass:" + fault + "; no split/offer/quantity/metadata/position change");
            }
        }
        finally { if (!item.isDestroyed) item.SetNum(originalNum); }
    }

    public void AssertNestedProductScope()
    {
        Thing inner = inputs.CreateExact(Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX, 1);
        Track(inner); ownership.GuardTree(inner);
        foreach (Thing child in new List<Thing>(inner.things)) child.Destroy();
        Actor.AddThing(inner, tryStack: false);
        Thing food = Offering("meat", 1, attach: false); inner.AddThing(food, tryStack: false);
        var positions = new Pr8OfferingPositionState(inner);
        string name = inner.c_altName, deity = inner.c_idDeity;
        RuntimeAssertions.Require(ReferenceEquals(Pr8OfferingPositionState.ScopedOwnerPoint(), Box.pos), "Outer product scope missing.");
        Elin_AutoOfferingAlter.OfferLogic.Process(inner);
        RuntimeAssertions.Require(food.isDestroyed && ReferenceEquals(Pr8OfferingPositionState.ScopedOwnerPoint(), Box.pos),
            "Nested native product call did not consume or restore outer scope.");
        positions.AssertUnchanged(invokeSaveCallback: true);
        RuntimeAssertions.Require(inner.c_altName == name && inner.c_idDeity == deity, "Nested metadata restoration failed.");
        ctx.Log("nested_product_scope:pass; inner=" + inner.uid + "; native FX/consumption/position/save conservation; outer restored");
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
        Diagnostic("cleanup_before", () => new { actors = actors.ConvertAll(ActorSnapshot), things = created.ConvertAll(item => new { uid = item.uid, destroyed = item.isDestroyed }) });
        try
        {
            // Native removal/held-item callbacks also use EClass.player. Keep them isolated.
            if (TestPlayer != null) playerScope.Select(TestPlayer);
            if (Observer != null) TryCleanup(() => Observer.Dispose(), failures);
            ownership.GuardAll(); // Refuse the entire cleanup if any recursive content is foreign.
            foreach (Chara actor in actors)
            {
                actor.isDead = false;
                // Card.Destroy protects same-faction characters. Only owned actors may bypass it.
                actor.isSummon = true;
                if (actor.conSleep != null) actor.conSleep.slept = false;
            }
            for (int i = created.Count - 1; i >= 0; i--)
            {
                Thing item = created[i];
                TryCleanup(() => { if (item != null && !item.isDestroyed) item.Destroy(); }, failures);
            }
            foreach (Chara actor in actors)
                TryCleanup(() => DestroyActor(actor), failures);
        }
        catch (Exception ex) { failures.Add(ex.ToString()); }
        finally
        {
            TryCleanup(() => ownership.Dispose(), failures);
            TryCleanup(() => playerScope.Dispose(), failures);
            TryCleanup(() => nativeState.Dispose(), failures);
            UnityEngine.Random.state = randomState;
        }
        TryCleanup(() =>
        {
            RuntimeAssertions.Require(EClass.game == game && EClass.player == originalPlayer && EClass.pc == originalActor
                && originalPlayer.chara == originalActor,
                "Original game/Player/PC reference not restored.");
            foreach (Pr8CardState state in originalItems) state.AssertUnchanged();
            AssertOriginalState("after_cleanup");
            nativeState.AssertRestored();
            foreach (Thing item in created) RuntimeAssertions.Require(item == null || item.isDestroyed, "Fixture thing leaked.");
            foreach (Chara actor in actors)
                RuntimeAssertions.Require(actor.isDestroyed && !EClass._map.charas.Contains(actor), "Fixture actor cleanup failed: uid=" + actor.uid + ";destroyed=" + actor.isDestroyed + ";inMap=" + EClass._map.charas.Contains(actor));
            ctx.Log("cleanup: original Player/inventory restored; native fixture UIDs destroyed");
        }, failures);
        Diagnostic("cleanup_after", () => new { actors = actors.ConvertAll(ActorSnapshot),
            things = created.ConvertAll(item => new { uid = item.uid, destroyed = item.isDestroyed }),
            originalPlayerRestored = ReferenceEquals(EClass.player, originalPlayer), originalPcRestored = ReferenceEquals(EClass.pc, originalActor),
            nativeStateRestored = ReadDiagnostic(() => { nativeState.AssertRestored(); return true; }), failures = failures });
        if (failures.Count > 0) throw new InvalidOperationException(string.Join(" | ", failures.ToArray()));
    }

    private static void TryCleanup(Action action, List<string> failures)
    {
        try { action(); }
        catch (Exception ex) { failures.Add(ex.ToString()); }
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
    public int RedirectedFxCalls;
    public Action AfterFirstOffer;
    public Action BeforeFirstOffer;
    private bool beforeInjected;
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
        harmony.Patch(HarmonyLib.AccessTools.DeclaredMethod(typeof(TraitAltar), "OnOffer", new[] { typeof(Chara), typeof(Thing) }),
            finalizer: new HarmonyLib.HarmonyMethod(typeof(Pr8OfferingObserver), "OfferFailed"));
        var effect = HarmonyLib.AccessTools.DeclaredMethod(typeof(Effect), "Play", new[] { typeof(Point), typeof(float), typeof(Point), typeof(UnityEngine.Sprite) });
        RuntimeAssertions.Require(effect != null, "Exact native Effect.Play observer target missing.");
        var beforeFx = new HarmonyLib.HarmonyMethod(typeof(Pr8OfferingObserver), "BeforeFx") { priority = HarmonyLib.Priority.First };
        harmony.Patch(effect, prefix: beforeFx, postfix: new HarmonyLib.HarmonyMethod(typeof(Pr8OfferingObserver), "AfterFx"));
        Patch(typeof(TraitAltar), "_OnOffer", new[] { typeof(Chara), typeof(Thing), typeof(int) }, null, "AfterNormalOffer");
        Patch(typeof(TraitAltar), "CanOffer", new[] { typeof(Chara), typeof(Card) }, null, "AfterCanOffer");
        Patch(typeof(Card), "Split", new[] { typeof(int) }, null, "AfterSplit");
        var setAi = HarmonyLib.AccessTools.DeclaredMethod(typeof(Chara), "SetAI", new[] { typeof(AIAct) });
        RuntimeAssertions.Require(setAi != null, "Native SetAI diagnostic target missing.");
        harmony.Patch(setAi, prefix: new HarmonyLib.HarmonyMethod(typeof(Pr8OfferingObserver), "BeforeSetAi"),
            finalizer: new HarmonyLib.HarmonyMethod(typeof(Pr8OfferingObserver), "SetAiFailed"));
        var valueBonus = HarmonyLib.AccessTools.DeclaredMethod(typeof(ElementContainerCard), "ValueBonus", new[] { typeof(Element) });
        RuntimeAssertions.Require(valueBonus != null, "Native ValueBonus diagnostic target missing.");
        harmony.Patch(valueBonus, finalizer: new HarmonyLib.HarmonyMethod(typeof(Pr8OfferingObserver), "ValueBonusFailed"));
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
        active.fixture.PositionState.AssertUnchanged(invokeSaveCallback: true);
        active.fixture.OfferDiagnostic(__instance, c, t, null);
        active.OfferCalls.Add(new Pr8OfferCall { Uid = t.uid, Num = t.Num, ActorUid = c.uid, Faith = c.faith.id, Deity = __instance.Deity.id });
        if (!active.beforeInjected && active.BeforeFirstOffer != null)
        {
            active.beforeInjected = true;
            active.BeforeFirstOffer();
        }
        active.LogObservation("OnOffer: " + Pr8CardState.Describe(t) + ";actor=" + c.uid + ";faith=" + c.faith.id + ";deity=" + __instance.Deity.id);
        if (active.ThrowBeforeFirstOffer && active.OfferCalls.Count == 1) throw new Pr8InjectedOfferingException();
    }
    private static Exception OfferFailed(TraitAltar __instance, Chara c, Thing t, Exception __exception)
    {
        if (__exception != null && Matches(__instance)) active.fixture.OfferDiagnostic(__instance, c, t, __exception);
        return __exception;
    }
    private static void BeforeFx(Point from, out Pr8OfferingFxState __state)
    {
        __state = null;
        if (active == null || active.fixture.Box == null || !Pr8OfferingPositionState.ScopeIsActive()) return;
        active.fixture.PositionState.AssertUnchanged(invokeSaveCallback: true);
        __state = new Pr8OfferingFxState { Original = from, OwnerPoint = ReferenceEquals(from, Pr8OfferingPositionState.ScopedOwnerPoint()) };
    }
    private static void AfterFx(Point from, Pr8OfferingFxState __state)
    {
        if (__state == null || active == null) return;
        active.fixture.PositionState.AssertUnchanged(invokeSaveCallback: true);
        if (!__state.OwnerPoint)
        {
            RuntimeAssertions.Require(ReferenceEquals(from, __state.Original), "Unrelated native FX Point changed.");
            return;
        }
        Chara actor = active.fixture.Actor;
        RuntimeAssertions.Require(!ReferenceEquals(from, __state.Original) && !ReferenceEquals(from, actor.pos)
            && from.x == actor.pos.x && from.z == actor.pos.z && Point.map != null
            && from.x >= 0 && from.z >= 0 && from.x < Point.map.cells.GetLength(0) && from.z < Point.map.cells.GetLength(1),
            "Native owner FX did not use an independent valid actor Point copy.");
        active.RedirectedFxCalls++;
    }
    private static void AfterOffer(TraitAltar __instance, Thing t)
    {
        if (!Matches(__instance)) return;
        active.fixture.PositionState.AssertUnchanged(invokeSaveCallback: true);
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
        active.LogObservation("CanOffer: actor=" + cc.uid + ";item=" + c.uid + ";accepted=" + __result);
    }
    private static void AfterSplit(Card __instance, Thing __result)
    {
        if (active == null || __instance.parent != active.fixture.Box) return;
        int count;
        active.splits.TryGetValue(__instance.uid, out count);
        active.splits[__instance.uid] = count + 1;
        active.fixture.Track(__result);
        active.LogObservation("Split: source=" + __instance.uid + ";detached=" + Pr8CardState.Describe(__result));
    }
    private static void BeforeSetAi(Chara __instance, AIAct g)
    {
        if (active != null) Pr8Diagnostic.BestEffort(() => active.fixture.SetAiDiagnostic(__instance, g, null));
    }
    private static Exception SetAiFailed(Exception __exception, Chara __instance, AIAct g)
    {
        if (__exception != null && active != null)
            Pr8Diagnostic.BestEffort(() => active.fixture.SetAiDiagnostic(__instance, g, __exception));
        return __exception;
    }
    private static Exception ValueBonusFailed(Exception __exception, ElementContainerCard __instance, Element e)
    {
        if (__exception != null && active != null)
        {
            Pr8Diagnostic.BestEffort(() => active.fixture.ValueBonusFailure(__instance, e, __exception));
        }
        return __exception; // Preserve native exception and result semantics.
    }
    private void LogObservation(string message) { if (ctx.Logs.Count < 44) ctx.Log(message); }
    public int CallsFor(int uid)
    {
        int count = 0;
        foreach (Pr8OfferCall call in OfferCalls) if (call.Uid == uid) count++;
        return count;
    }
    public int SplitsFor(int uid) { int count; return splits.TryGetValue(uid, out count) ? count : 0; }
    public void Reset() { OfferCalls.Clear(); splits.Clear(); NormalOfferingCalls = 0; RedirectedFxCalls = 0; injected = false; beforeInjected = false; }
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
        var fieldSnapshot = new Pr8SourceSnapshot();
        Pr8SourceSpriteCache.AssertIndependent(chest, custom);
        foreach (FieldInfo field in typeof(SourceThing.Row).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!field.FieldType.IsArray) continue;
            var baseArray = field.GetValue(chest) as Array;
            var customArray = field.GetValue(custom) as Array;
            RuntimeAssertions.Require(baseArray == null || !ReferenceEquals(baseArray, customArray), "Shared source array: " + field.Name);
            if (field.Name != "factory" && field.Name != "recipeKey" && !Pr8SourceSpriteCache.IsField(field))
                RuntimeAssertions.Require(fieldSnapshot.Values(baseArray) == fieldSnapshot.Values(customArray),
                    "Inherited chest6 array differs: " + field.Name);
        }
        string beforeChest = fieldSnapshot.State(chest);
        int beforeRows = source.things.rows.Count;
        source.Init(); // Initialized standard no-op only; never reset initialized or invoke Reload.
        RuntimeAssertions.Require(source.things.rows.Count == beforeRows && fieldSnapshot.State(chest) == beforeChest,
            "Initialized Init no-op mutated source/chest6.");
        RuntimeAssertions.Require(RecipeManager.Get(id) != null, "Native RecipeManager did not build custom recipe.");
        ctx.Log("coverage=postboot_contract; rows=" + count + "; map/cards/recipe resolve; initialized Init is no-op");
    }
}
#endif
