// Getter errors are data: finish all independent reads before any assertion or functional call.
public sealed class Pr1PrerequisiteReport
{
    private readonly System.Action<string> log;
    private readonly System.Collections.Generic.Dictionary<string, string> values = new System.Collections.Generic.Dictionary<string, string>();
    public int Reads, Errors;
    public Pr1PrerequisiteReport(System.Action<string> sink) { log = sink; }
    public void Read(string key, System.Func<object> getter)
    {
        Reads++;
        string value;
        try { value = Describe(getter()); }
        catch (System.Exception ex)
        {
            Errors++;
            value = "<error:" + ex.GetType().FullName + ":" + ex.Message.Replace("\r", " ").Replace("\n", " ") + ">";
        }
        values.Add(key, value);
    }
    private static string Describe(object value)
    {
        if (value == null) return "<null>";
        if (value is bool) return (bool)value ? "true" : "false";
        var type = value.GetType();
        if (value is string || type.IsPrimitive || type.IsEnum || value is decimal)
            return System.Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture).Replace("\r", " ").Replace("\n", " ");
        // Do not call native Card/source/container ToString methods while formatting diagnostics.
        return type.FullName + "@" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
    public void Finish()
    {
        log("native-prereq:data:" + Newtonsoft.Json.JsonConvert.SerializeObject(values));
        log("native-prereq:complete:reads=" + Reads + ":errors=" + Errors + ":functionalCalls=0");
    }
}

#if !PR1_PROBE_REPORT_ONLY
public static class Pr1NativePrerequisiteProbe
{
    public static Pr1PrerequisiteReport Capture(System.Action<string> log, Chara actor, Thing food, Thing important, FactionManager expected)
    {
        var p = new Pr1PrerequisiteReport(log);
        p.Read("actor.ref", () => actor);
        p.Read("actor.uid", () => actor?.uid);
        p.Read("actor.source", () => actor?.source);
        p.Read("actor.source.id", () => actor?.source?.id);
        p.Read("actor.race", () => actor?.race);
        p.Read("actor.race.id", () => actor?.race?.id);
        p.Read("actor.job", () => actor?.job);
        p.Read("actor.job.id", () => actor?.job?.id);
        p.Read("actor.bio", () => actor?.bio);
        p.Read("actor.body", () => actor?.body);
        p.Read("actor.body.owner", () => actor?.body?.owner);
        p.Read("actor.body.owner.uid", () => actor?.body?.owner?.uid);
        p.Read("actor.body.ownerMatches", () => object.ReferenceEquals(actor?.body?.owner, actor));
        p.Read("actor.renderer", () => actor?.renderer);
        p.Read("actor.renderer.owner", () => actor?.renderer?.owner);
        p.Read("actor.renderer.owner.uid", () => actor?.renderer?.owner?.uid);
        p.Read("actor.renderer.ownerMatches", () => object.ReferenceEquals(actor?.renderer?.owner, actor));
        p.Read("actor.renderer.orbit", () => actor?.renderer?.orbit);
        p.Read("actor.rawSlots", () => actor?.rawSlots);
        p.Read("actor.rawSlots.length", () => actor?.rawSlots?.Length);
        p.Read("actor.cints.length", () => actor?._cints?.Length);
        p.Read("actor.ints.length", () => actor?._ints?.Length);
        p.Read("actor.conditions", () => actor?.conditions);
        p.Read("actor.conditions.count", () => actor?.conditions?.Count);
        p.Read("actor.faith", () => actor?.faith);
        p.Read("actor.faith.id", () => actor?.idFaith);
        p.Read("actor.faithElements", () => actor?.faithElements);
        p.Read("actor.hp", () => actor?.hp);
        p.Read("actor.isDead", () => actor?.isDead);
        p.Read("actor.isDestroyed", () => actor?.isDestroyed);
        p.Read("actor.parent", () => actor?.parent);
        p.Read("actor.ride", () => actor?.ride);
        p.Read("actor.parasite", () => actor?.parasite);
        p.Read("actor.master", () => actor?.master);
        p.Read("actor.homeBranch", () => actor?.homeBranch);
        p.Read("actor.pos", () => actor?.pos);
        p.Read("actor.pos.x", () => actor?.pos?.x);
        p.Read("actor.pos.z", () => actor?.pos?.z);
        p.Read("actor.IsPC", () => actor?.IsPC);
        p.Read("actor.IsPCParty", () => actor?.IsPCParty);
        p.Read("actor.IsGlobal", () => actor?.IsGlobal);
        p.Read("pc.sameActor", () => object.ReferenceEquals(EClass.pc, actor));
        p.Read("zone.ref", () => EClass._zone);
        p.Read("zone.IsRegion", () => EClass._zone?.IsRegion);
        p.Read("map.ref", () => EClass._map);
        p.Read("map.charas", () => EClass._map?.charas);
        p.Read("map.containsActor", () => EClass._map?.charas?.Contains(actor));
        p.Read("map.things", () => EClass._map?.things);
        p.Read("global.hasActorUid", () => actor == null ? (object)null : EClass.game?.cards?.globalCharas?.ContainsKey(actor.uid));
        p.Read("party.ref", () => actor?.party);
        p.Read("party.uidLeader", () => actor?.party?.uidLeader);
        p.Read("party.leader", () => actor?.party?.leader);
        p.Read("party.leader.uid", () => actor?.party?.leader?.uid);
        p.Read("party.leaderMatches", () => object.ReferenceEquals(actor?.party?.leader, actor));
        p.Read("party.members.count", () => actor?.party?.members?.Count);
        p.Read("party.members.uids", () => actor?.party?.members == null ? null : CardUids(actor.party.members));
        p.Read("party.uidMembers", () => actor?.party?.uidMembers == null ? null : string.Join(",", actor.party.uidMembers));
        p.Read("faction.manager.active", () => EClass.game?.factions);
        p.Read("faction.manager.expected", () => expected);
        p.Read("faction.managerMatches", () => object.ReferenceEquals(EClass.game?.factions, expected));
        p.Read("faction.actor", () => actor?.faction);
        p.Read("faction.actor.id", () => actor?.idFaction);
        p.Read("faction.home", () => expected?.Home);
        p.Read("faction.home.uid", () => expected?.Home?.uid);
        p.Read("faction.home.source", () => expected?.Home?.source);
        p.Read("faction.homeMatches", () => object.ReferenceEquals(actor?.faction, expected?.Home));
        p.Read("faction.lookup", () => actor?.idFaction == null || expected?.dictAll == null ? null : expected.dictAll[actor.idFaction]);
        p.Read("faction.lookupMatches", () => actor?.idFaction != null && expected?.dictAll != null
            && object.ReferenceEquals(expected.dictAll[actor.idFaction], actor.faction));
        p.Read("player.ref", () => EClass.player);
        p.Read("player.chara", () => EClass.player?.chara);
        p.Read("player.uidChara", () => EClass.player?.uidChara);
        p.Read("player.charaMatches", () => object.ReferenceEquals(EClass.player?.chara, actor));
        p.Read("player.stats", () => EClass.player?.stats);
        p.Read("player.flags", () => EClass.player?.flags);
        p.Read("player.nums", () => EClass.player?.nums);
        p.Read("player.notices", () => EClass.player?.notices);
        p.Read("player.queues", () => EClass.player?.queues);
        p.Read("player.recipes", () => EClass.player?.recipes);
        p.Read("player.keyItems", () => EClass.player?.keyItems);
        p.Read("player.domains", () => EClass.player?.domains);
        p.Read("player.dialogFlags", () => EClass.player?.dialogFlags);
        p.Read("sources.ref", () => EClass.sources);
        p.Read("sources.stats", () => EClass.sources?.stats);
        p.Read("sources.elements", () => EClass.sources?.elements);
        Elements(p, "actor.elements", () => actor?.elements, actor);
        Elements(p, "food.elements", () => food?.elements, food);
        Elements(p, "important.elements", () => important?.elements, important);
        Elements(p, "faction.home.elements", () => expected?.Home?.elements, null);
        Elements(p, "faction.home.charaElements", () => expected?.Home?.charaElements, null);
        Food(p, "food", food, actor);
        Food(p, "important", important, actor);
        Stats(p, "hunger", () => actor?.hunger);
        Stats(p, "stamina", () => actor?.stamina);
        Stats(p, "sleepiness", () => actor?.sleepiness);
        Stats(p, "SAN", () => actor?.SAN);
        p.Read("sleep.ref", () => actor?.conSleep);
        p.Read("sleep.owner", () => actor?.conSleep?.owner);
        p.Read("sleep.ownerMatches", () => actor?.conSleep == null ? (object)null : object.ReferenceEquals(actor.conSleep.owner, actor));
        p.Read("sleep.source", () => actor?.conSleep?.source);
        p.Read("sleep.slept", () => actor?.conSleep?.slept);
        p.Read("sleep.pcSleep", () => actor?.conSleep?.pcSleep);
        p.Read("sleep.uidRide", () => actor?.conSleep?.uidRide);
        p.Read("sleep.uidParasite", () => actor?.conSleep?.uidParasite);
        p.Read("sleep.pickup", () => actor?.conSleep?.pickup);
        p.Read("sleep.pcBed", () => actor?.conSleep?.pcBed);
        p.Read("sleep.pcPillow", () => actor?.conSleep?.pcPillow);
        p.Read("conditions.ownerRefs", () => Conditions(actor));
        p.Finish();
        return p;
    }

    private static void Elements(Pr1PrerequisiteReport p, string key, System.Func<ElementContainer> get, Card expectedOwner)
    {
        p.Read(key, () => get());
        p.Read(key + ".dict", () => get()?.dict);
        p.Read(key + ".count", () => get()?.dict?.Count);
        p.Read(key + ".owner", () => (get() as ElementContainerCard)?.owner);
        p.Read(key + ".ownerMatches", () => expectedOwner == null ? (object)null
            : object.ReferenceEquals((get() as ElementContainerCard)?.owner, expectedOwner));
        p.Read(key + ".entries", () =>
        {
            var container = get();
            if (container?.dict == null) return null;
            var keys = new System.Collections.Generic.List<int>(container.dict.Keys);
            keys.Sort();
            var entries = new System.Collections.Generic.List<string>();
            foreach (int id in keys)
            {
                var e = container.dict[id];
                entries.Add(e == null ? id + ":<null>" : id + ":base=" + e.vBase + ":exp=" + e.vExp
                    + ":potential=" + e.vPotential + ":source=" + e.vSource + ":link=" + e.vLink
                    + ":ownerMatches=" + object.ReferenceEquals(e.owner, container));
            }
            return string.Join(";", entries);
        });
    }
    private static void Food(Pr1PrerequisiteReport p, string key, Thing food, Chara actor)
    {
        p.Read(key + ".ref", () => food);
        p.Read(key + ".uid", () => food?.uid);
        p.Read(key + ".id", () => food?.id);
        p.Read(key + ".source", () => food?.source);
        p.Read(key + ".source.id", () => food?.source?.id);
        p.Read(key + ".source.origin", () => food?.source?._origin);
        p.Read(key + ".trait", () => food?.trait);
        p.Read(key + ".trait.exactPrepared", () => food?.trait?.GetType() == typeof(TraitFoodPrepared));
        p.Read(key + ".Num", () => food?.Num);
        p.Read(key + ".important", () => food?.c_isImportant);
        p.Read(key + ".decay", () => food?.decay);
        p.Read(key + ".MaxDecay", () => food?.MaxDecay);
        p.Read(key + ".isDecayed", () => food?.IsDecayed);
        p.Read(key + ".parent", () => food?.parent);
        p.Read(key + ".parentMatchesActor", () => object.ReferenceEquals(food?.parent, actor));
        p.Read(key + ".root", () => food?.GetRootCard());
        p.Read(key + ".rootMatchesActor", () => food != null && object.ReferenceEquals(food.GetRootCard(), actor));
    }
    private static void Stats(Pr1PrerequisiteReport p, string key, System.Func<Stats> get)
    {
        p.Read("stats." + key, () => get());
        p.Read("stats." + key + ".source", () => get()?.source);
        p.Read("stats." + key + ".source.id", () => get()?.source?.id);
        p.Read("stats." + key + ".raw", () => get()?.raw);
        p.Read("stats." + key + ".raw.length", () => get()?.raw?.Length);
        p.Read("stats." + key + ".rawIndex", () => get()?.rawIndex);
        p.Read("stats." + key + ".value", () => get()?.value);
        p.Read("stats." + key + ".phase", () => get()?.GetPhase());
        p.Read("stats." + key + ".TrackPhaseChange", () => get()?.TrackPhaseChange);
    }
    private static string CardUids(System.Collections.Generic.IEnumerable<Card> cards)
    {
        var ids = new System.Collections.Generic.List<string>();
        foreach (var card in cards) ids.Add(card == null ? "<null>" : card.uid.ToString());
        return string.Join(",", ids);
    }
    private static string Conditions(Chara actor)
    {
        if (actor?.conditions == null) return null;
        var values = new System.Collections.Generic.List<string>();
        foreach (var c in actor.conditions) values.Add(c == null ? "<null>" : c.GetType().FullName
            + ":ownerUid=" + (c.owner == null ? "<null>" : c.owner.uid.ToString())
            + ":ownerMatches=" + object.ReferenceEquals(c.owner, actor) + ":value=" + c.value);
        return string.Join(";", values);
    }
}
#endif
