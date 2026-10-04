public sealed class Pr1AutoEatFixture
{
    private static bool restorationBlocked;
    public readonly string Token = System.Guid.NewGuid().ToString("N");
    public Chara Actor;
    public Thing Food;
    public Thing Important;
    public int HungryValue;
    public Pr1AutoEatObserver Observer;
    private readonly RuntimeTestContext ctx;
    private readonly System.Collections.Generic.List<Thing> owned = new System.Collections.Generic.List<Thing>();
    private readonly System.Collections.Generic.List<string> files = new System.Collections.Generic.List<string>();
    private readonly System.Collections.Generic.List<Pr1StatsBinding> statBindings = new System.Collections.Generic.List<Pr1StatsBinding>();
    private HarmonyLib.Harmony harmony;
    private object plugin, originalConfig, config;
    private object originalConfigFile;
    private System.Reflection.FieldInfo configField, savedAIField, eatingGuard;
    private System.Reflection.MethodInfo checkEat, captureAI;
    private System.Type configType;
    private AIAct originalSavedAI;
    private Chara originalStatsOwner;
    private bool cleaned, active, capturedStatics;
    private string configDirectory;
    private string baselineState, baselineConfigBytes;
    private Pr1FixtureOwnership<Card> ownership;
    private readonly System.Collections.Generic.List<ConSleep> createdSleeps = new System.Collections.Generic.List<ConSleep>();

    private Pr1AutoEatFixture(RuntimeTestContext context) { ctx = context; }
    public static Pr1AutoEatFixture Create(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.pc != null && EClass.player != null && EClass._zone != null && EClass._map != null,
            "PR1 requires an active PC/map.");
        RuntimeAssertions.Require((EClass.pc.Name ?? "").IndexOf("RUNTIME_TEST", System.StringComparison.Ordinal) >= 0,
            "PR1 dedicated-save guard rejected.");
        RuntimeAssertions.Require(Pr1AutoEatObserver.Current == null, "Concurrent PR1 observer active.");
        RuntimeAssertions.Require(!restorationBlocked, "Previous PR1 restore/cleanup failed; reload dedicated save and use a fresh script/session.");
        var f = new Pr1AutoEatFixture(ctx);
        ctx.Set("pr1.fixture", f);
        ctx.RegisterRollback("pr1.fixture", f.Cleanup);
        f.Initialize();
        return f;
    }

    private void Initialize()
    {
        var pluginType = FindType("Elin_AutoEatSleep.Plugin");
        var logic = FindType("Elin_AutoEatSleep.AutoEatLogic");
        configType = FindType("Elin_AutoEatSleep.ModConfig");
        plugin = pluginType.GetProperty("Instance").GetValue(null, null);
        RuntimeAssertions.Require(plugin != null, "AutoEatSleep plugin not loaded.");
        configField = pluginType.GetField("<MyConfig>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        RuntimeAssertions.Require(configField != null, "Exact MyConfig backing field missing.");
        originalConfig = configField.GetValue(plugin);
        originalConfigFile = Property(Entry(originalConfig, "AutoEatEnabled"), "ConfigFile");
        checkEat = RequireMethod(logic, "CheckAutoEat", System.Type.EmptyTypes);
        captureAI = RequireMethod(logic, "CaptureAI", System.Type.EmptyTypes);
        savedAIField = logic.GetField("_savedAI", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        RuntimeAssertions.Require(savedAIField != null, "Saved AI field missing.");
        originalSavedAI = SavedAI;
        eatingGuard = logic.GetField("_isEating", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        RuntimeAssertions.Require(eatingGuard != null && !(bool)eatingGuard.GetValue(null), "AutoEat guard already active.");
        originalStatsOwner = BaseStats.CC;
        foreach (var field in typeof(Stats).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            var stats = field.GetValue(null) as Stats;
            if (stats != null) statBindings.Add(new Pr1StatsBinding(stats));
        }
        capturedStatics = true;
        configDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(ctx.ModRoot, "..", ".codex-build", "Elin_AutoEatSleep", "runtime", Token));
        var cfgFile = NewConfig("fixture.cfg");
        config = System.Activator.CreateInstance(configType, new object[] { cfgFile });
        Set("AutoEatEnabled", true);
        Set("AutoSleepEnabled", false);
        Set("UseContainerFilter", false);
        Set("HungerThreshold", 3);
        Set("ResumeAiOnWake", true);

        baselineState = OriginalState(EClass.player);
        baselineConfigBytes = FileState((string)Property(originalConfigFile, "ConfigFilePath"));
        var originalUids = new System.Collections.Generic.HashSet<int>();
        RememberOriginalTree(EClass.pc, originalUids);
        foreach (var card in EClass._map.charas) RememberOriginalTree(card, originalUids);
        foreach (var card in EClass._map.things) RememberOriginalTree(card, originalUids);
        foreach (var card in EClass.game.cards.globalCharas.Values) RememberOriginalTree(card, originalUids);
        foreach (var card in EClass.player.listCarryoverMap) RememberOriginalTree(card, originalUids);
        ownership = new Pr1FixtureOwnership<Card>(card => card.uid, card => card.parent, ChildrenOf, originalUids);

        Actor = CharaGen.Create("chara", 5);
        RuntimeAssertions.Require(Actor != null && Actor.uid > 0 && !Actor.IsGlobal, "Invalid non-global fixture Chara.");
        ownership.RecordCreatedTree(Actor);
        ownership.RequireTree(Actor);
        Actor.c_altName = "RUNTIME_TEST_PR1_" + Token;
        OwnInventory(Actor);
        foreach (var thing in owned) if (!thing.isDestroyed) ownership.DestroyChecked(thing, card => card.Destroy());
        RuntimeAssertions.Require(Actor.things.Count == 0, "Generated fixture inventory was not cleared.");
        EClass._zone.AddCard(Actor, EClass.pc.pos.GetNearestPoint(allowBlock: false, allowChara: false));
        RuntimeAssertions.Require(EClass._map.charas.Contains(Actor), "Fixture did not enter current map.");
        ownership.RecordMove(Actor, Actor.parent);
        RuntimeAssertions.Require(Actor.renderer != null, "Native fixture renderer unavailable.");
        Actor.party = new Party();
        Actor.party.members.Add(Actor);
        Actor.things.SetSize(7, 5);
        Actor.SetAI(new AI_Idle());
        for (int value = 1; value <= 100; value++)
        {
            Actor.hunger.Set(value);
            if (Actor.hunger.GetPhase() == 3) { HungryValue = value; break; }
        }
        RuntimeAssertions.Require(HungryValue > 1, "No native Hungry boundary found.");
        Actor.hunger.Set(HungryValue - 1);
        RuntimeAssertions.Require(Actor.hunger.GetPhase() < 3, "Hungry boundary control invalid.");

        Food = CreatePreparedFood();
        Food.SetNum(6);
        // Controlled native cooked food: no gene, human meat, leftovers or random trait side effects.
        Food.elements.dict.Clear();
        Food.elements.SetTo(10, 25);
        Food.elements.SetTo(70, 100);
        Food.decay = 0;
        Important = Own(Food.Duplicate(3));
        Important.c_isImportant = true;
        AddFixtureThing(Actor, Important);
        AddFixtureThing(Actor, Food);
        RuntimeAssertions.Require(Actor.CanEat(Food, true) && !FoodEffect.IsLeftoverable(Food),
            "Fixture must pass native CanEat(shouldEat:true) and fully consume one unit.");
        foreach (var thing in EClass._map.things)
            RuntimeAssertions.Require(thing.id != "731" || !thing.pos.Equals(Actor.pos), "Fixture tile already contains vomit.");

        Observer = new Pr1AutoEatObserver(this);
        harmony = new HarmonyLib.Harmony("runtime.pr1.autoeat." + Token);
        Patch(typeof(Chara), "InstantEat", new[] { typeof(Thing), typeof(bool) }, "InstantBefore", null, "InstantFinally");
        Patch(typeof(FoodEffect), "Proc", new[] { typeof(Chara), typeof(Thing), typeof(bool) }, "FoodBefore", null, null);
        Patch(typeof(FoodEffect), "ProcNutrition", new[] { typeof(Chara), typeof(Thing), typeof(float), typeof(float) }, "NutritionBefore", null, null);
        Patch(typeof(Stats), "OnChangePhase", new[] { typeof(int), typeof(int) }, "PhaseBefore", null, null);
        Patch(typeof(Chara), "Vomit", System.Type.EmptyTypes, "VomitBefore", null, "VomitFinally");
        Patch(typeof(Zone), "AddCard", new[] { typeof(Card), typeof(int), typeof(int) }, "AddCardBefore", "AddCardAfter", null);
        Patch(typeof(ConSleep), "OnRemoved", System.Type.EmptyTypes, null, "SleepAfter", null);
        RequireProductPatch(typeof(Stats), "OnChangePhase", new[] { typeof(int), typeof(int) });
        RequireProductPatch(typeof(ConSleep), "OnRemoved", System.Type.EmptyTypes);
        ctx.Log("fixture:actor=" + Actor.uid + ":food=" + Food.uid + ":num=" + Food.Num + ":hungerBoundary=" + HungryValue);
        ctx.Log("loaded:" + logic.Assembly.FullName + ":location=" + logic.Assembly.Location);
    }

    private static System.Type FindType(string name)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name, false);
            if (type != null) return type;
        }
        throw new System.InvalidOperationException("Loaded type missing: " + name);
    }
    private static System.Reflection.MethodInfo RequireMethod(System.Type type, string name, System.Type[] args)
    {
        var method = HarmonyLib.AccessTools.Method(type, name, args);
        RuntimeAssertions.Require(method != null, "Exact native method missing: " + type.FullName + "." + name);
        return method;
    }
    private void RequireProductPatch(System.Type type, string name, System.Type[] args)
    {
        var info = HarmonyLib.Harmony.GetPatchInfo(RequireMethod(type, name, args));
        bool found = false;
        if (info != null) foreach (var patch in info.Postfixes) if (patch.owner == "com.elin.autoeatsleep") found = true;
        RuntimeAssertions.Require(found, "Live AutoEatSleep postfix absent: " + name);
    }
    private void Patch(System.Type type, string name, System.Type[] args, string before, string after, string finalizer)
    {
        harmony.Patch(RequireMethod(type, name, args),
            prefix: before == null ? null : new HarmonyLib.HarmonyMethod(typeof(Pr1AutoEatObserver), before),
            postfix: after == null ? null : new HarmonyLib.HarmonyMethod(typeof(Pr1AutoEatObserver), after),
            finalizer: finalizer == null ? null : new HarmonyLib.HarmonyMethod(typeof(Pr1AutoEatObserver), finalizer));
    }
    private object Entry(object obj, string name)
    {
        var field = configType.GetField(name);
        RuntimeAssertions.Require(field != null, "Config entry missing: " + name);
        return field.GetValue(obj);
    }
    public void Set(string name, object value)
    {
        SetProperty(Entry(config, name), "BoxedValue", value);
    }
    private static object Property(object obj, string name)
    {
        var property = obj.GetType().GetProperty(name);
        RuntimeAssertions.Require(property != null, "Exact config property missing: " + name);
        return property.GetValue(obj, null);
    }
    private static void SetProperty(object obj, string name, object value)
    {
        var property = obj.GetType().GetProperty(name);
        RuntimeAssertions.Require(property != null && property.CanWrite, "Exact writable config property missing: " + name);
        property.SetValue(obj, value, null);
    }
    private static object OpenConfig(string path)
    {
        var file = System.Activator.CreateInstance(FindType("BepInEx.Configuration.ConfigFile"), new object[] { path, false, null });
        SetProperty(file, "SaveOnConfigSet", false);
        return file;
    }
    private object NewConfig(string name)
    {
        string path = System.IO.Path.Combine(configDirectory, name);
        files.Add(path);
        return OpenConfig(path);
    }
    public void RequireConfigBinding()
    {
        var fresh = System.Activator.CreateInstance(configType, new object[] { NewConfig("fresh.cfg") });
        RuntimeAssertions.Require((int)Property(Entry(fresh, "HungerThreshold"), "BoxedValue") == 3,
            "Unconfigured default is not native Hungry=3.");
        var explicitFile = NewConfig("explicit.cfg");
        var explicitConfig = System.Activator.CreateInstance(configType, new object[] { explicitFile });
        SetProperty(Entry(explicitConfig, "HungerThreshold"), "BoxedValue", 1);
        System.IO.Directory.CreateDirectory(configDirectory);
        RequireMethod(explicitFile.GetType(), "Save", System.Type.EmptyTypes).Invoke(explicitFile, null);
        string explicitPath = (string)Property(explicitFile, "ConfigFilePath");
        var bytes = System.IO.File.ReadAllBytes(explicitPath);
        var loadedFile = OpenConfig(explicitPath);
        var existing = System.Activator.CreateInstance(configType, new object[] { loadedFile });
        RuntimeAssertions.Require((int)Property(Entry(existing, "HungerThreshold"), "BoxedValue") == 1,
            "Existing explicit threshold was replaced by default.");
        RuntimeAssertions.Require(System.Convert.ToBase64String(bytes) == System.Convert.ToBase64String(System.IO.File.ReadAllBytes(explicitPath)),
            "Binding changed existing fixture config bytes.");
        ctx.Log("config:new=3:explicit=1:fileBytesPreserved=true");
    }
    private Thing CreatePreparedFood()
    {
        foreach (var row in EClass.sources.things.rows)
        {
            if (row.trait == null || (System.Array.IndexOf(row.trait, "FoodPrepared") < 0
                && System.Array.IndexOf(row.trait, "TraitFoodPrepared") < 0)) continue;
            var thing = Own(ThingGen.Create(row.id));
            if (thing.trait is TraitFoodPrepared && !(thing.trait is TraitLunch)
                && Actor.CanEat(thing, true) && !thing.c_isImportant) return thing;
        }
        throw new System.InvalidOperationException("No native non-leftover prepared food source; fixture blocked.");
    }
    public Thing CreateContainer()
    {
        foreach (string id in new[] { "cooler", "chest" })
        {
            if (!EClass.sources.cards.map.ContainsKey(id)) continue;
            var box = Own(ThingGen.Create(id));
            if (!box.IsContainer || box.things == null) continue;
            AddFixtureThing(Actor, box);
            ctx.Log("fixture:container=" + box.uid + ":id=" + box.id);
            return box;
        }
        throw new System.InvalidOperationException("Native container source unavailable.");
    }
    public Thing Own(Thing thing)
    {
        RuntimeAssertions.Require(thing != null && thing.uid > 0, "Fixture thing invalid.");
        ownership.RecordCreatedTree(thing);
        ownership.RequireTree(thing);
        if (!owned.Exists(item => object.ReferenceEquals(item, thing))) owned.Add(thing);
        OwnInventory(thing);
        return thing;
    }
    private void OwnInventory(Card card)
    {
        if (card.things == null) return;
        foreach (var thing in card.things) Own(thing);
    }
    private static System.Collections.Generic.IEnumerable<Card> ChildrenOf(Card card)
    {
        if (card.things == null) yield break;
        foreach (var thing in card.things) yield return thing;
    }
    private static void RememberOriginalTree(Card card, System.Collections.Generic.HashSet<int> uids)
    {
        if (card == null || !uids.Add(card.uid)) return;
        foreach (var child in ChildrenOf(card)) RememberOriginalTree(child, uids);
    }
    public void AddFixtureThing(Card destination, Thing thing)
    {
        ownership.RequireTree(destination);
        ownership.RequireTree(thing);
        var moved = destination.AddThing(thing, false);
        RuntimeAssertions.Require(object.ReferenceEquals(moved, thing), "Fixture AddThing replaced/merged the owned reference.");
        ownership.RecordMove(thing, destination);
        ownership.RequireTree(destination);
    }
    public void RecordVomitPlacement(Thing thing)
    {
        RuntimeAssertions.Require(thing.GetRootCard() == thing && EClass._map.things.Contains(thing),
            "Generated vomit moved into an unexpected owner; do not destroy it.");
        ownership.RecordMove(thing, thing.parent);
    }
    public ConSleep CreateSleep()
    {
        RuntimeAssertions.Require(!Actor.IsPC, "ConSleep preparation cannot call PC EndTurn.");
        var sleep = Actor.AddCondition<ConSleep>(100, true) as ConSleep;
        RuntimeAssertions.Require(sleep != null && object.ReferenceEquals(sleep.owner, Actor), "Native sleep fixture owner invalid.");
        createdSleeps.Add(sleep);
        return sleep;
    }
    public void RemoveFixtureSleep(ConSleep sleep)
    {
        RuntimeAssertions.Require(createdSleeps.Contains(sleep) && object.ReferenceEquals(sleep.owner, Actor)
            && Actor.conditions.Contains(sleep) && object.ReferenceEquals(Actor.conSleep, sleep),
            "Condition.Kill refused: sleep is not the owned fixture condition.");
        sleep.Kill(true);
    }
    public AIAct SavedAI
    {
        get { return (AIAct)savedAIField.GetValue(null); }
        set { savedAIField.SetValue(null, value); }
    }
    public void CheckEat() { checkEat.Invoke(null, null); }
    public void CaptureAI() { captureAI.Invoke(null, null); }
    public void Run(System.Action action)
    {
        RuntimeAssertions.Require(!active, "Nested fixture activation.");
        var originalPlayer = EClass.player;
        RuntimeAssertions.Require((originalPlayer.chara.Name ?? "").IndexOf("RUNTIME_TEST", System.StringComparison.Ordinal) >= 0,
            "PR1 activation dedicated-save guard rejected.");
        string before = OriginalState(originalPlayer);
        string originalBytes = FileState((string)Property(originalConfigFile, "ConfigFilePath"));
        var oldConfig = configField.GetValue(plugin);
        var oldSavedAI = SavedAI;
        bool oldEatingGuard = (bool)eatingGuard.GetValue(null);
        var oldCC = Act.CC;
        var oldTC = Act.TC;
        var oldTP = Act.TP;
        var bindings = new System.Collections.Generic.List<Pr1StatsBinding>();
        foreach (var binding in statBindings) bindings.Add(new Pr1StatsBinding(binding.Stats));
        var statsOwner = BaseStats.CC;
        int importantNum = Important.Num;
        var importantParent = Important.parent;
        try
        {
            active = true;
            configField.SetValue(plugin, config);
            SavedAI = null;
            EClass.game.player = new Player { chara = Actor, uidChara = Actor.uid };
            Pr1AutoEatObserver.Current = Observer;
            RuntimeAssertions.Require(Actor.IsPC && Actor.party.leader == Actor, "Native PC/party fixture activation failed.");
            RuntimeAssertions.Require(Actor.CanEat(Food, true), "PC native CanEat precondition failed.");
            action();
        }
        finally
        {
            Pr1AutoEatObserver.Current = null;
            EClass.game.player = originalPlayer;
            configField.SetValue(plugin, oldConfig);
            SavedAI = oldSavedAI;
            eatingGuard.SetValue(null, oldEatingGuard);
            Act.CC = oldCC;
            Act.TC = oldTC;
            Act.TP = oldTP;
            foreach (var binding in bindings) binding.Restore();
            BaseStats.CC = statsOwner;
            active = false;
            RuntimeAssertions.Require(Important.Num == importantNum && object.ReferenceEquals(Important.parent, importantParent),
                "Important fixture control consumed or moved.");
            bool restored = before == OriginalState(originalPlayer) && originalBytes == FileState((string)Property(originalConfigFile, "ConfigFilePath"));
            if (!restored) restorationBlocked = true;
            RuntimeAssertions.Require(restored,
                "Original PC/player/world/config changed: stop and reload dedicated save.");
            ctx.Log("restore:player/PC/inventory/elements/conditions/AI/karma/flags/config=unchanged");
        }
    }
    public void ExpectNoMeal(string label)
    {
        Observer.Reset();
        int before = Food.Num;
        int hunger = Actor.hunger.value;
        CheckEat();
        LogMeal(label, before);
        RuntimeAssertions.Require(Observer.InstantCalls == 0 && Observer.FoodCalls == 0 && Food.Num == before
            && Actor.hunger.value == hunger, label + " consumed or changed fixture food/hunger.");
    }
    public void LogMeal(string label, int before)
    {
        ctx.Log(label + ":actor=" + Actor.uid + ":food=" + Food.uid + ":num=" + before + "->" + Food.Num
            + ":parent=" + (Food.parent == null ? "null" : Food.parent.GetType().Name)
            + ":root=" + Food.GetRootCard().uid + ":hunger=" + Actor.hunger.value
            + ":instant=" + Observer.InstantCalls + ":foodEffect=" + Observer.FoodCalls
            + ":nutrition=" + Observer.NutritionCalls + ":nestedPhase=" + Observer.NestedPhases
            + ":maxDepth=" + Observer.MaxDepth + ":throws=" + Observer.InstantExceptions);
        ctx.Log("important-control:uid=" + Important.uid + ":num=" + Important.Num + ":root=" + Important.GetRootCard().uid);
    }
    private static string FileState(string path)
    {
        return System.IO.File.Exists(path) ? System.Convert.ToBase64String(System.IO.File.ReadAllBytes(path)) : "absent";
    }
    public static string ElementState(Chara chara)
    {
        var keys = new System.Collections.Generic.List<int>(chara.elements.dict.Keys);
        keys.Sort();
        var text = new System.Text.StringBuilder();
        foreach (int id in keys)
        {
            var element = chara.elements.dict[id];
            text.Append(id).Append(':').Append(element.vBase).Append(':').Append(element.vExp).Append(':').Append(element.vPotential).Append(';');
        }
        return text.ToString();
    }
    private string OriginalState(Player player)
    {
        var text = new System.Text.StringBuilder();
        var pc = player.chara;
        text.Append(pc.uid).Append(':').Append(pc.hp).Append(':').Append(pc.ai == null ? 0 : pc.ai.GetHashCode()).Append(':').Append(player.karma);
        text.Append(':').Append(string.Join(",", pc._cints)).Append(':').Append(string.Join(",", pc._ints));
        text.Append(':').Append(pc.pos.x).Append(':').Append(pc.pos.z).Append(':').Append(string.Join(",", pc.rawSlots ?? new int[0]));
        text.Append("|worldDate:").Append(EClass.world.date.GetRaw());
        text.Append(':').Append(ElementState(pc));
        foreach (var condition in pc.conditions) text.Append(':').Append(condition.GetHashCode()).Append(':').Append(condition.value);
        AppendInventory(pc, text);
        var flags = new System.Collections.Generic.List<string>(player.dialogFlags.Keys);
        flags.Sort(System.StringComparer.Ordinal);
        foreach (var key in flags) text.Append('|').Append(key).Append('=').Append(player.dialogFlags[key]);
        foreach (var card in EClass._map.charas)
        {
            if (card == pc || object.ReferenceEquals(card, Actor)) continue;
            text.Append('|').Append(card.uid).Append(':').Append(card.hp).Append(':').Append(string.Join(",", card._cints));
            text.Append(':').Append(card.pos.x).Append(':').Append(card.pos.z).Append(':').Append(card.ai == null ? 0 : card.ai.GetHashCode());
            text.Append(':').Append(ElementState(card));
            foreach (var condition in card.conditions) text.Append(':').Append(condition.GetHashCode()).Append(':').Append(condition.value);
            AppendInventory(card, text);
        }
        foreach (var thing in EClass._map.things)
        {
            if (owned.Exists(item => object.ReferenceEquals(item, thing))) continue;
            text.Append("|map:").Append(thing.uid).Append(':').Append(thing.Num).Append(':').Append(thing.pos.x).Append(':').Append(thing.pos.z)
                .Append(':').Append(string.Join(",", thing._ints)).Append(':').Append(thing.isStolen).Append(':').Append(thing.isNPCProperty);
        }
        var globals = new System.Collections.Generic.List<int>(EClass.game.cards.globalCharas.Keys);
        globals.Sort();
        text.Append("|global:").Append(string.Join(",", globals));
        foreach (var member in player.chara.party.members) text.Append("|party:").Append(member.uid);
        foreach (var member in player.listCarryoverMap) text.Append("|carry:").Append(member.uid);
        return text.ToString();
    }
    private static void AppendInventory(Card card, System.Text.StringBuilder text)
    {
        if (card.things == null) return;
        foreach (var thing in card.things)
        {
            text.Append('|').Append(thing.uid).Append(':').Append(thing.Num).Append(':').Append(thing.GetRootCard().uid)
                .Append(':').Append(thing.invX).Append(':').Append(thing.invY).Append(':').Append(thing.isEquipped)
                .Append(':').Append(thing.c_isImportant).Append(':').Append(thing.decay)
                .Append(':').Append(thing.parentCard == null ? 0 : thing.parentCard.uid).Append(':').Append(string.Join(",", thing._ints));
            AppendInventory(thing, text);
        }
    }
    public void Cleanup()
    {
        if (cleaned) return;
        RuntimeAssertions.Require(!active, "Fixture activation still active during cleanup.");
        var errors = new System.Collections.Generic.List<string>();
        Attempt(errors, "observer", () => { if (harmony != null) harmony.UnpatchSelf(); });
        if (Pr1AutoEatObserver.Current == Observer) Pr1AutoEatObserver.Current = null;
        Attempt(errors, "config", () => { if (configField != null && originalConfig != null) configField.SetValue(plugin, originalConfig); });
        Attempt(errors, "savedAI", () => { if (savedAIField != null) SavedAI = originalSavedAI; });
        // A failed graph preflight leaves every card intact, including unexpected existing items.
        bool canDestroy = false;
        Attempt(errors, "ownershipPreflight", () =>
        {
            if (Actor != null && !Actor.isDestroyed) ownership.RequireTree(Actor);
            foreach (var thing in owned) if (!thing.isDestroyed) ownership.RequireTree(thing);
            canDestroy = true;
        });
        if (canDestroy) foreach (var thing in owned)
        {
            Attempt(errors, "thing:" + thing.uid, () =>
            {
                if (!thing.isDestroyed) ownership.DestroyChecked(thing, card => card.Destroy());
                RuntimeAssertions.Require(thing.isDestroyed && !EClass._map.things.Contains(thing), "Fixture thing cleanup failed: " + thing.uid);
            });
        }
        if (canDestroy && Actor != null)
        {
            Attempt(errors, "actor:" + Actor.uid, () =>
            {
                if (!Actor.isDestroyed) ownership.DestroyChecked(Actor, card => card.Destroy());
                RuntimeAssertions.Require(Actor.isDestroyed && !EClass._map.charas.Contains(Actor)
                    && !EClass.game.cards.globalCharas.ContainsKey(Actor.uid)
                    && !EClass.player.listCarryoverMap.Contains(Actor)
                    && !EClass.pc.party.members.Contains(Actor), "Fixture Chara cleanup incomplete.");
            });
        }
        foreach (var path in files) Attempt(errors, "configFile", () => { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); });
        Attempt(errors, "configDirectory", () =>
        {
            if (configDirectory != null && System.IO.Directory.Exists(configDirectory)) System.IO.Directory.Delete(configDirectory, false);
        });
        Attempt(errors, "stats", () =>
        {
            if (!capturedStatics) return;
            foreach (var binding in statBindings) binding.Restore();
            BaseStats.CC = originalStatsOwner;
        });
        Attempt(errors, "originalState", () =>
        {
            if (baselineState == null) return;
            RuntimeAssertions.Require(baselineState == OriginalState(EClass.player)
                && baselineConfigBytes == FileState((string)Property(originalConfigFile, "ConfigFilePath"))
                && object.ReferenceEquals(configField.GetValue(plugin), originalConfig)
                && object.ReferenceEquals(SavedAI, originalSavedAI), "Original state not restored after cleanup.");
            ctx.Log("cleanup:originalStateAssert=true");
        });
        cleaned = errors.Count == 0;
        if (!cleaned) restorationBlocked = true;
        ctx.Log("cleanup:fixtureUIDs=" + owned.Count + ":errors=" + errors.Count + ":observerOwner=runtime.pr1.autoeat." + Token);
        RuntimeAssertions.Require(cleaned, "PR1 cleanup failed; stop/reload dedicated save: " + string.Join("; ", errors));
    }
    private void Attempt(System.Collections.Generic.List<string> errors, string label, System.Action action)
    {
        try { action(); }
        catch (System.Exception ex) { errors.Add(label + ":" + ex.Message); ctx.Log("cleanup:failed:" + label + ":" + ex.Message); }
    }
}

public sealed class Pr1StatsBinding
{
    public readonly Stats Stats;
    private readonly int[] raw;
    private readonly int rawIndex;
    public Pr1StatsBinding(Stats stats) { Stats = stats; raw = stats.raw; rawIndex = stats.rawIndex; }
    public void Restore() { Stats.raw = raw; Stats.rawIndex = rawIndex; }
}

public sealed class Pr1AutoEatObserver
{
    public static Pr1AutoEatObserver Current;
    private readonly Pr1AutoEatFixture fixture;
    public int InstantCalls, FoodCalls, NutritionCalls, NestedPhases, MaxDepth, InstantExceptions, Depth;
    public int ThrowsInjected, ReentryInjected, VomitCalls, VomitDepth, SleepRemovedCalls;
    public bool InjectThrow, InjectReentry;
    public Pr1AutoEatObserver(Pr1AutoEatFixture f) { fixture = f; }
    public void Reset()
    {
        RuntimeAssertions.Require(Depth == 0 && VomitDepth == 0, "Observer depth leaked after native call.");
        InstantCalls = FoodCalls = NutritionCalls = NestedPhases = MaxDepth = InstantExceptions = 0;
        ThrowsInjected = ReentryInjected = VomitCalls = SleepRemovedCalls = 0;
        InjectThrow = InjectReentry = false;
    }
    public static void InstantBefore(Chara __instance, Thing __0, out bool __state)
    {
        var o = Current;
        __state = o != null && object.ReferenceEquals(__instance, o.fixture.Actor) && object.ReferenceEquals(__0, o.fixture.Food);
        if (!__state) return;
        o.InstantCalls++;
        o.Depth++;
        o.MaxDepth = System.Math.Max(o.MaxDepth, o.Depth);
    }
    public static System.Exception InstantFinally(System.Exception __exception, bool __state)
    {
        if (__state && Current != null)
        {
            Current.Depth--;
            if (__exception != null) Current.InstantExceptions++;
        }
        return __exception;
    }
    public static void FoodBefore(Chara __0, Thing __1)
    {
        var o = Current;
        if (o == null || !object.ReferenceEquals(__0, o.fixture.Actor) || !object.ReferenceEquals(__1, o.fixture.Food)) return;
        o.FoodCalls++;
        if (o.InjectThrow)
        {
            o.InjectThrow = false;
            o.ThrowsInjected++;
            throw new System.InvalidOperationException("PR1 fixture-only FoodEffect fault");
        }
        if (o.InjectReentry)
        {
            o.InjectReentry = false;
            o.ReentryInjected++;
            o.fixture.CheckEat();
        }
    }
    public static void NutritionBefore(Chara __0, Thing __1)
    {
        var o = Current;
        if (o != null && object.ReferenceEquals(__0, o.fixture.Actor) && object.ReferenceEquals(__1, o.fixture.Food)) o.NutritionCalls++;
    }
    public static void PhaseBefore(Stats __instance)
    {
        var o = Current;
        if (o != null && o.Depth > 0 && object.ReferenceEquals(__instance, Stats.Hunger)
            && object.ReferenceEquals(BaseStats.CC, o.fixture.Actor)) o.NestedPhases++;
    }
    public static void VomitBefore(Chara __instance, out bool __state)
    {
        var o = Current;
        __state = o != null && object.ReferenceEquals(__instance, o.fixture.Actor);
        if (__state) { o.VomitCalls++; o.VomitDepth++; }
    }
    public static System.Exception VomitFinally(System.Exception __exception, bool __state)
    {
        if (__state && Current != null) Current.VomitDepth--;
        return __exception;
    }
    public static void AddCardBefore(Card __0, out bool __state)
    {
        var o = Current;
        __state = o != null && o.VomitDepth > 0 && __0 is Thing && __0.id == "731";
        if (!__state) return;
        RuntimeAssertions.Require(__0.parent == null && !__0.ExistsOnMap, "Vomit insertion did not receive a fresh unowned Thing.");
        o.fixture.Own((Thing)__0);
    }
    public static void AddCardAfter(Card __0, bool __state)
    {
        if (__state && Current != null) Current.fixture.RecordVomitPlacement((Thing)__0);
    }
    public static void SleepAfter(ConSleep __instance)
    {
        var o = Current;
        if (o != null && object.ReferenceEquals(__instance.owner, o.fixture.Actor)) o.SleepRemovedCalls++;
    }
}
