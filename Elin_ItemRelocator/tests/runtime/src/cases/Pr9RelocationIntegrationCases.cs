using System;
using System.Collections.Generic;

public abstract class Pr9RelocationCase : RuntimeCaseBase
{
    public override IReadOnlyList<string> Tags => new[] { "integration", "destructive", "pr9" };
    protected Pr9RelocationFixtures Fixture(RuntimeTestContext ctx) => ctx.Get<Pr9RelocationFixtures>("fixture");
    public override void Prepare(RuntimeTestContext ctx) => ctx.Set("fixture", new Pr9RelocationFixtures(ctx));
    public override void Verify(RuntimeTestContext ctx) => Pr9RelocationFixtures.Require(ctx.Get<bool>("verified"), "Case not verified.");
    public override void Cleanup(RuntimeTestContext ctx) => ctx.GetOrDefault<Pr9RelocationFixtures>("fixture")?.Cleanup();

    protected Thing Destination(Pr9RelocationFixtures f, bool onPc = false)
    {
        var destination = f.CreateThing("chest6");
        Pr9RelocationFixtures.Require(destination.IsContainer, "Destination is not a native container.");
        if (onPc) f.Pc.AddThing(destination, false);
        else f.Zone.AddCard(destination, f.Pc.pos.GetNearestPoint(allowBlock: false, allowChara: false));
        return destination;
    }

    protected void Blocked(Pr9RelocationFixtures f, Thing destination, Thing target, string label)
    {
        var parent = target.parent;
        var root = target.GetRootCard();
        var quantity = target.Num;
        var slot = target.c_equippedSlot;
        var count = f.MoveCount(target);
        f.Move(destination, target, false);
        f.Move(destination, target, true);
        Pr9RelocationFixtures.Require(f.MoveCount(target) == count && target.parent == parent
            && target.GetRootCard() == root && target.Num == quantity && target.c_equippedSlot == slot,
            label + ": protected fixture was transferred or changed.");
    }
}

public sealed class Pr9EquipmentIntegrationCase : Pr9RelocationCase
{
    public override string Id => "pr9.item.equipped_all_owners";

    public override void Execute(RuntimeTestContext ctx)
    {
        var f = Fixture(ctx);
        var destination = Destination(f);
        var ballast = f.CreateThing("rock");
        f.Pc.AddThing(ballast, false);
        var owners = new[] { f.Pc, f.CreatePartyMember("begger"), f.CreatePartyMember("dog") };
        foreach (var owner in owners) {
            foreach (var cursed in new[] { false, true }) {
                Thing target = null;
                foreach (var id in new[] { "sword", "ring", "amulet", "helmet" }) {
                    var candidate = f.CreateThing(id);
                    var slot = owner.body.GetSlot(candidate, true);
                    if (slot != null && slot.thing == null && owner.body.IsEquippable(candidate, slot, false)) {
                        target = candidate;
                        break;
                    }
                }
                Pr9RelocationFixtures.Require(target != null, "No empty native equipment slot for owner " + owner.uid);
                owner.AddThing(target, false);
                var profile = f.Profile(destination, target);
                profile.Scope = owner == f.Pc ? Elin_ItemRelocator.RelocationProfile.FilterScope.Inventory
                    : Elin_ItemRelocator.RelocationProfile.FilterScope.PetsOnly;
                Pr9RelocationFixtures.Require(f.PreviewHas(destination, target), "Unequipped fixture not initially selected.");
                f.Equip(owner, target, cursed);
                Pr9RelocationFixtures.Require(!f.PreviewHas(destination, target), "Cached preview included equipment.");
                Blocked(f, destination, target, "equipped owner=" + owner.uid + ":cursed=" + cursed);
                Pr9RelocationFixtures.Require(f.CacheHas(ballast) && f.CacheHas(target), "Equipment test lost nonempty cache.");
                owner.body.Unequip(target);
                Pr9RelocationFixtures.Require(!target.isEquipped && f.PreviewHas(destination, target),
                    "Manual native unequip did not reappear without cache clear.");
                var quantity = target.Num;
                f.Move(destination, target, false);
                Pr9RelocationFixtures.Require(target.parent == destination && target.Num == quantity && f.MoveCount(target) == 1,
                    "Unequipped item did not transfer once with quantity preserved.");
                ctx.Log("equipment:owner=" + owner.uid + ":cursed=" + cursed + ":uid=" + target.uid + ":passed");
            }
        }
        ctx.Set("verified", true);
    }
}

public sealed class Pr9LiveCacheIntegrationCase : Pr9RelocationCase
{
    public override string Id => "pr9.item.live_cache_rule_owner";

    public override void Execute(RuntimeTestContext ctx)
    {
        var f = Fixture(ctx);
        var destination = Destination(f);
        var member = f.CreatePartyMember("begger");
        foreach (var change in new[] { "important", "hardlock", "hotbar", "rule", "profile", "scope", "owner",
            "emptyrule", "petsscope", "zoneowner", "destlock", "destnpc", "gift", "npc", "installed", "same", "destroyed", "destdestroyed" }) {
            if (destination.isDestroyed) destination = Destination(f);
            destination.c_lockLv = 0;
            destination.isNPCProperty = false;
            var target = f.CreateThing("rock", 7);
            f.Pc.AddThing(target, false);
            var profile = f.Profile(destination, target);
            Pr9RelocationFixtures.Require(f.PreviewHas(destination, target), change + ": preview precondition failed.");
            switch (change) {
                case "important": target.c_isImportant = true; break;
                case "hardlock": target.c_lockedHard = true; break;
                case "hotbar":
                    var undo = f.PutOnHotbar(target);
                    // Do not refresh preview before either execution path.
                    f.Move(destination, target, false);
                    Pr9RelocationFixtures.Require(f.MoveCount(target) == 0 && target.parent == f.Pc && target.Num == 7,
                        "Single execution ignored new hotbar entry.");
                    undo();
                    Pr9RelocationFixtures.Require(f.PreviewHas(destination, target), "Hotbar control did not return to preview.");
                    f.PutOnHotbar(target);
                    f.Move(destination, target, true);
                    Pr9RelocationFixtures.Require(f.MoveCount(target) == 0 && target.parent == f.Pc && target.Num == 7,
                        "Bulk execution ignored new hotbar entry.");
                    break;
                case "rule": profile.Rules[0].Enabled = false; break;
                case "emptyrule": profile.Rules.Clear(); break;
                case "profile":
                    profile.Enabled = false;
                    Pr9RelocationFixtures.Require(!f.PreviewHas(destination, target), "Disabled profile appeared in preview.");
                    f.Move(destination, target, true);
                    Pr9RelocationFixtures.Require(!profile.Enabled && f.MoveCount(target) == 0 && target.parent == f.Pc
                        && target.GetRootCard() == f.Pc && target.Num == 7 && target.c_equippedSlot == 0,
                        "Disabled profile bulk execution changed its source.");
                    ctx.Log("profile-disabled:preview=0:bulk=0:uid=" + target.uid + ":num=7:passed");

                    // The single API historically ignores profile.Enabled. Use
                    // independent native fixtures so its move cannot invalidate
                    // the bulk preservation check or later protection scenarios.
                    var singleDestination = Destination(f);
                    var singleTarget = f.CreateThing("rock", 7);
                    f.Pc.AddThing(singleTarget, false);
                    var singleProfile = f.Profile(singleDestination, singleTarget);
                    Pr9RelocationFixtures.Require(f.PreviewHas(singleDestination, singleTarget),
                        "Single API independent fixture precondition failed.");
                    singleProfile.Enabled = false;
                    f.Move(singleDestination, singleTarget, false);
                    Pr9RelocationFixtures.Require(!singleProfile.Enabled && f.MoveCount(singleTarget) == 1 && singleTarget.parent == singleDestination
                        && singleTarget.GetRootCard() == singleDestination && singleTarget.Num == 7
                        && singleTarget.c_equippedSlot == 0 && !singleTarget.isDestroyed && !f.Pc.things.Contains(singleTarget),
                        "Disabled profile single API changed its existing transfer contract.");
                    ctx.Log("profile-disabled:single=1:uid=" + singleTarget.uid + ":num=7:passed");
                    continue;
                case "scope": profile.Scope = Elin_ItemRelocator.RelocationProfile.FilterScope.ZoneOnly; break;
                case "petsscope": profile.Scope = Elin_ItemRelocator.RelocationProfile.FilterScope.PetsOnly; break;
                case "zoneowner":
                    profile.Scope = Elin_ItemRelocator.RelocationProfile.FilterScope.Inventory;
                    f.Zone.AddCard(target, f.Pc.pos.GetNearestPoint(allowBlock: false, allowChara: false));
                    Pr9RelocationFixtures.Require(target.GetRootCard() == target, "Native zone ownership did not change.");
                    break;
                case "owner":
                    profile.Scope = Elin_ItemRelocator.RelocationProfile.FilterScope.Inventory;
                    member.AddThing(target, false);
                    Pr9RelocationFixtures.Require(target.GetRootCard() == member, "Native ownership did not change.");
                    break;
                case "destlock": destination.c_lockLv = 1; break;
                case "destnpc": destination.isNPCProperty = true; break;
                case "gift": target.isGifted = true; break;
                case "npc": target.isNPCProperty = true; break;
                case "installed":
                    f.Zone.AddCard(target, f.Pc.pos.GetNearestPoint(allowBlock: false, allowChara: false));
                    target.SetPlaceState(PlaceState.installed); break;
                case "same": destination.AddThing(target, false); break;
                case "destroyed": f.DestroyFixture(target); break;
                case "destdestroyed": f.DestroyFixture(destination); break;
            }
            Pr9RelocationFixtures.Require(!f.PreviewHas(destination, target), change + ": cached preview ignored live protection.");
            Blocked(f, destination, target, change);
            ctx.Log("live-protection:" + change + ":uid=" + target.uid + ":passed");
        }
        var movingDestination = Destination(f, true);
        var gifted = f.CreateThing("rock", 7);
        f.Pc.AddThing(gifted, false);
        gifted.isGifted = true;
        f.Profile(movingDestination, gifted);
        Pr9RelocationFixtures.Require(f.PreviewHas(movingDestination, gifted), "Gift-to-PC precondition failed.");
        f.Zone.AddCard(movingDestination, f.Pc.pos.GetNearestPoint(allowBlock: false, allowChara: false));
        Pr9RelocationFixtures.Require(!f.PreviewHas(movingDestination, gifted), "Destination ownership change ignored.");
        Blocked(f, movingDestination, gifted, "destination-owner");
        var firstDestination = Destination(f);
        var replacementDestination = Destination(f);
        var changedTarget = f.CreateThing("rock", 7);
        f.Pc.AddThing(changedTarget, false);
        f.Profile(firstDestination, changedTarget);
        Pr9RelocationFixtures.Require(f.PreviewHas(firstDestination, changedTarget), "Target-switch preview precondition failed.");
        var replacementProfile = new Elin_ItemRelocator.RelocationProfile { Enabled = false };
        f.Manager.Profiles[f.Manager.GetProfileKey(replacementDestination)] = replacementProfile;
        // Retain the old destination's nonempty cache while selecting a new target.
        Pr9RelocationFixtures.Require(f.CacheHas(changedTarget) && !f.PreviewHas(replacementDestination, changedTarget),
            "Target replacement reused the old profile's eligibility.");
        Blocked(f, replacementDestination, changedTarget, "target-replacement");
        ctx.Set("verified", true);
    }
}

// Fault injection changes only native fixture state after the real production
// ConditionText matches; it never changes a native method's arguments/results.
public sealed class Pr9BoundaryTextCondition : Elin_ItemRelocator.ConditionText
{
    public Action Mutation;
    private int calls;
    public override bool IsMatch(Thing thing)
    {
        var match = base.IsMatch(thing);
        if (match && ++calls == 2) Mutation();
        return match;
    }
}

public sealed class Pr9ExecutionBoundaryIntegrationCase : Pr9RelocationCase
{
    public override string Id => "pr9.item.execution_boundary";
    public override IReadOnlyList<string> Tags => new[] { "integration", "destructive", "pr9", "fault_injection" };

    public override void Execute(RuntimeTestContext ctx)
    {
        var f = Fixture(ctx);
        foreach (var change in new[] { "important", "hardlock", "equipped", "destlock", "destdestroyed" }) {
            var destination = Destination(f);
            var target = f.CreateThing(change == "equipped" ? "sword" : "rock", 1);
            f.Pc.AddThing(target, false);
            var profile = f.Profile(destination, target);
            Pr9RelocationFixtures.Require(f.PreviewHas(destination, target), "Boundary preview precondition failed.");
            bool mutationRan = false;
            profile.Rules[0].Conditions[0] = new Pr9BoundaryTextCondition {
                Text = target.c_altName,
                Mutation = () => {
                    mutationRan = true;
                    switch (change) {
                        case "important": target.c_isImportant = true; break;
                        case "hardlock": target.c_lockedHard = true; break;
                        case "equipped": f.Equip(f.Pc, target, false); break;
                        case "destlock": destination.c_lockLv = 1; break;
                        case "destdestroyed": f.DestroyFixture(destination); break;
                    }
                }
            };
            f.Move(destination, target, true);
            Pr9RelocationFixtures.Require(mutationRan && f.MoveCount(target) == 0
                && target.parent == f.Pc && target.Num == 1, "Boundary mutation failed or item moved: " + change);
            ctx.Log("execution-boundary:" + change + ":uid=" + target.uid + ":passed");
        }
        ctx.Set("verified", true);
    }
}

public sealed class Pr9StackCapacityIntegrationCase : Pr9RelocationCase
{
    public override string Id => "pr9.item.native_stack_capacity";

    public override void Execute(RuntimeTestContext ctx)
    {
        var f = Fixture(ctx);
        foreach (var bulk in new[] { false, true }) {
            var destination = Destination(f);
            var target = f.CreateThing("rock", 11);
            f.Pc.AddThing(target, false);
            f.Profile(destination, target);
            Pr9RelocationFixtures.Require(f.PreviewHas(destination, target), "Normal preview failed.");
            f.Move(destination, target, bulk);
            Pr9RelocationFixtures.Require(target.parent == destination && target.GetRootCard() == destination
                && target.Num == 11 && f.MoveCount(target) == 1, "Normal native move did not preserve quantity/parent/root.");
            f.Move(destination, target, bulk);
            Pr9RelocationFixtures.Require(f.MoveCount(target) == 1 && target.Num == 11, "Same move transferred twice.");
            var full = Destination(f);
            full.things.ChangeSize(1, 1);
            var source = f.CreateThing("rock", 13);
            f.Pc.AddThing(source, false);
            var merge = source.Split(5);
            f.Track(merge);
            full.AddThing(merge, false);
            f.Profile(full, source);
            Pr9RelocationFixtures.Require(full.things.IsFull() && !full.things.IsFull(source),
                "Native fixture is not full-but-mergeable.");
            var beforeSlots = full.things.Count;
            f.Move(full, source, bulk);
            Pr9RelocationFixtures.Require(full.things.Count == beforeSlots && merge.Num == 13
                && merge.parent == full && merge.GetRootCard() == full && f.MoveCount(source) == 1
                && source.isDestroyed && !full.things.Contains(source) && !f.Pc.things.Contains(source),
                "Full native merge lost quantity, ownership, or consumed a slot, or retained its source.");
            f.Move(full, source, bulk);
            Pr9RelocationFixtures.Require(merge.Num == 13 && f.MoveCount(source) == 1, "Merged source executed twice.");
            var nonmerge = f.CreateThing("rock", 7);
            nonmerge.SetBlessedState(BlessedState.Cursed);
            f.Pc.AddThing(nonmerge, false);
            f.Profile(full, nonmerge);
            Pr9RelocationFixtures.Require(full.things.IsFull(nonmerge), "Native nonmerge fixture unexpectedly stackable.");
            Blocked(f, full, nonmerge, "full-nonmerge");
            ctx.Log("native-capacity:bulk=" + bulk + ":normal=11:merge=13:blocked=7:passed");
        }
        ctx.Set("verified", true);
    }
}
