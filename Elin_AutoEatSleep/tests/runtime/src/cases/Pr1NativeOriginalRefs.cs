// Identity and collection snapshots of the dedicated baseline, never a new global registration.
public sealed class Pr1NativeOriginalRefs
{
    private readonly Game game;
    private readonly Player player;
    private readonly Chara pc;
    private readonly Party party;
    private readonly Chara leader;
    private readonly int leaderUid;
    private readonly RefChara leaderRef;
    private readonly System.Collections.Generic.List<Chara> memberList, carryList;
    private readonly System.Collections.Generic.List<int> memberUidList;
    private readonly Chara[] members, carry;
    private readonly int[] memberUids;
    private readonly CardManager cards;
    private readonly object globals;
    private readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, Chara>> globalEntries;
    private readonly Map map;
    private readonly object mapCharas, mapThings;
    private readonly FactionManager factions;
    private readonly object factionDictionary;
    private readonly Faction[] factionRoles;
    private readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Faction>> factionEntries;
    private readonly System.Collections.Generic.List<Pr1NativeFactionRefs> factionStates;
    private readonly System.Collections.Generic.Dictionary<Chara, Faction> charaFactions = new System.Collections.Generic.Dictionary<Chara, Faction>();
    private readonly System.Collections.Generic.Dictionary<Chara, string> charaFactionIds = new System.Collections.Generic.Dictionary<Chara, string>();

    public Pr1NativeOriginalRefs()
    {
        game = EClass.game;
        player = game.player;
        pc = player.chara;
        party = pc.party;
        RuntimeAssertions.Require(party != null, "Original PC party unavailable.");
        leader = party.leader;
        leaderUid = party.uidLeader;
        leaderRef = party.refLeader;
        memberList = party.members;
        memberUidList = party.uidMembers;
        members = memberList.ToArray();
        memberUids = memberUidList.ToArray();
        carryList = player.listCarryoverMap;
        carry = carryList.ToArray();
        cards = game.cards;
        globals = cards.globalCharas;
        globalEntries = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, Chara>>(cards.globalCharas);
        map = EClass._map;
        mapCharas = map.charas;
        mapThings = map.things;
        factions = game.factions;
        factionDictionary = factions.dictAll;
        factionRoles = new Faction[] { factions.Home, factions.Wilds, factions.Fighter, factions.Mage, factions.Thief, factions.Merchant };
        factionEntries = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Faction>>(factions.dictAll);
        factionStates = new System.Collections.Generic.List<Pr1NativeFactionRefs>();
        foreach (var entry in factionEntries) factionStates.Add(new Pr1NativeFactionRefs(entry.Value));
        // Resolve native lazy caches under the original manager, before any fixture manager switch.
        RememberFaction(pc);
        foreach (var chara in map.charas) RememberFaction(chara);
        foreach (var chara in cards.globalCharas.Values) RememberFaction(chara);
        foreach (var chara in carry) RememberFaction(chara);
        foreach (var chara in members) RememberFaction(chara);
    }

    public bool Matches()
    {
        if (!object.ReferenceEquals(EClass.game, game) || !object.ReferenceEquals(game.player, player)
            || !object.ReferenceEquals(player.chara, pc) || !object.ReferenceEquals(pc.party, party)
            || !object.ReferenceEquals(party.refLeader, leaderRef) || party.uidLeader != leaderUid
            || !object.ReferenceEquals(party.leader, leader) || !object.ReferenceEquals(party.members, memberList)
            || !object.ReferenceEquals(party.uidMembers, memberUidList)
            || !object.ReferenceEquals(player.listCarryoverMap, carryList)
            || !object.ReferenceEquals(game.cards, cards) || !object.ReferenceEquals(cards.globalCharas, globals)
            || !object.ReferenceEquals(EClass._map, map) || !object.ReferenceEquals(map.charas, mapCharas)
            || !object.ReferenceEquals(map.things, mapThings)) return false;
        if (!object.ReferenceEquals(game.factions, factions) || !object.ReferenceEquals(factions.dictAll, factionDictionary)
            || factions.dictAll.Count != factionEntries.Count) return false;
        var currentRoles = new Faction[] { factions.Home, factions.Wilds, factions.Fighter, factions.Mage, factions.Thief, factions.Merchant };
        for (int i = 0; i < factionRoles.Length; i++) if (!object.ReferenceEquals(currentRoles[i], factionRoles[i])) return false;
        foreach (var entry in factionEntries)
        {
            Faction current;
            if (!factions.dictAll.TryGetValue(entry.Key, out current) || !object.ReferenceEquals(current, entry.Value)) return false;
        }
        foreach (var state in factionStates) if (!state.Matches()) return false;
        foreach (var entry in charaFactions)
            if (entry.Key.idFaction != charaFactionIds[entry.Key] || !object.ReferenceEquals(entry.Key.faction, entry.Value)) return false;
        if (!SameReferences(memberList, members) || !SameReferences(carryList, carry)
            || memberUidList.Count != memberUids.Length || cards.globalCharas.Count != globalEntries.Count) return false;
        for (int i = 0; i < memberUids.Length; i++) if (memberUidList[i] != memberUids[i]) return false;
        foreach (var entry in globalEntries)
        {
            Chara current;
            if (!cards.globalCharas.TryGetValue(entry.Key, out current) || !object.ReferenceEquals(current, entry.Value)) return false;
        }
        return true;
    }

    private static bool SameReferences(System.Collections.Generic.List<Chara> actual, Chara[] before)
    {
        if (actual.Count != before.Length) return false;
        for (int i = 0; i < before.Length; i++) if (!object.ReferenceEquals(actual[i], before[i])) return false;
        return true;
    }

    private void RememberFaction(Chara chara)
    {
        if (chara == null || charaFactions.ContainsKey(chara)) return;
        charaFactions.Add(chara, chara.faction);
        charaFactionIds.Add(chara, chara.idFaction);
    }
}

public sealed class Pr1NativeFactionRefs
{
    private readonly Faction faction;
    private readonly string id, uid, name;
    private readonly FactionRelation relation;
    private readonly int affinity;
    private readonly object relationType;
    private readonly Faction relationOwner;
    private readonly Pr1NativeElementRefs elements, charaElements;

    public Pr1NativeFactionRefs(Faction value)
    {
        faction = value;
        id = value.id;
        uid = value.uid;
        name = value.name;
        relation = value.relation;
        affinity = relation.affinity;
        relationType = relation.type;
        relationOwner = relation.faction;
        elements = new Pr1NativeElementRefs(value.elements);
        charaElements = new Pr1NativeElementRefs(value.charaElements);
    }

    public bool Matches()
    {
        return faction.id == id && faction.uid == uid && faction.name == name
            && object.ReferenceEquals(faction.relation, relation) && relation.affinity == affinity
            && relationType.Equals(relation.type) && object.ReferenceEquals(relation.faction, relationOwner)
            && elements.Matches(faction.elements) && charaElements.Matches(faction.charaElements);
    }
}

public sealed class Pr1NativeElementRefs
{
    private readonly ElementContainer container;
    private readonly object dictionary;
    private readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, Element>> entries;
    private readonly System.Collections.Generic.List<string> values = new System.Collections.Generic.List<string>();
    private readonly System.Collections.Generic.List<ElementContainer> owners = new System.Collections.Generic.List<ElementContainer>();

    public Pr1NativeElementRefs(ElementContainer value)
    {
        container = value;
        dictionary = value.dict;
        entries = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, Element>>(value.dict);
        foreach (var entry in entries) { values.Add(Raw(entry.Value)); owners.Add(entry.Value.owner); }
    }

    public bool Matches(ElementContainer current)
    {
        if (!object.ReferenceEquals(current, container) || !object.ReferenceEquals(current.dict, dictionary)
            || current.dict.Count != entries.Count) return false;
        for (int i = 0; i < entries.Count; i++)
        {
            Element element;
            if (!current.dict.TryGetValue(entries[i].Key, out element) || !object.ReferenceEquals(element, entries[i].Value)
                || Raw(element) != values[i] || !object.ReferenceEquals(element.owner, owners[i])) return false;
        }
        return true;
    }

    private static string Raw(Element element)
    {
        return string.Join(",", element.id, element.vBase, element.vSource, element.vExp, element.vPotential,
            element.vTempPotential, element.vLink, element.vSourcePotential);
    }
}
