using Xunit;

namespace Elin_ItemRelocator.Tests;

public sealed class ManagerEquipmentTests {
    private readonly RelocatorManager manager = new();
    private readonly Thing destination = new() { uid = 100, IsContainer = true };

    public ManagerEquipmentTests() {
        EClass.pc = new Chara { IsPC = true };
        EClass._map = new TestMap();
        EClass.player = new TestPlayer();
        manager.GetProfile(destination).Rules.Add(new RelocationRule());
    }

    private Thing AddItem(string owner, bool equipped = false, bool cursed = false) {
        Chara holder = EClass.pc;
        if (owner != "pc") {
            holder = new Chara();
            EClass.pc.party.members.Add(holder);
            manager.GetProfile(destination).Scope = RelocationProfile.FilterScope.PetsOnly;
        }
        var item = new Thing { parent = holder, c_equippedSlot = equipped ? 1 : 0, IsCursed = cursed };
        holder.things.Add(item);
        return item;
    }

    [Theory]
    [InlineData("pc", false)]
    [InlineData("pc", true)]
    [InlineData("follower", false)]
    [InlineData("follower", true)]
    [InlineData("pet", false)]
    [InlineData("pet", true)]
    public void EquippedItem_IsExcludedFromPreviewBatchAndSingle(string owner, bool cursed) {
        var item = AddItem(owner, equipped: true, cursed: cursed);
        manager.BuildCache(manager.GetProfile(destination), destination);

        Assert.DoesNotContain(item, manager.GetMatches(destination));
        manager.ExecuteRelocation(destination);
        manager.RelocateSingleThing(item, destination);

        Assert.Empty(destination.AddedThings);
        Assert.Equal(1, item.c_equippedSlot);
    }

    [Fact]
    public void Preview_ShowsManuallyUnequippedItemWithoutRebuildingNonemptyCache() {
        var equipped = AddItem("pc", equipped: true);
        var normal = AddItem("pc");
        manager.BuildCache(manager.GetProfile(destination), destination);
        Assert.Equal(new[] { normal }, manager.GetMatches(destination));

        equipped.c_equippedSlot = 0;

        Assert.Contains(equipped, manager.GetMatches(destination));
        Assert.Contains(normal, manager.GetMatches(destination));
    }

    [Fact]
    public void PreviewAndTransfers_RejectItemEquippedAfterPreview() {
        var item = AddItem("pc");
        Assert.Contains(item, manager.GetMatches(destination));

        item.c_equippedSlot = 1;

        Assert.DoesNotContain(item, manager.GetMatches(destination));
        manager.ExecuteRelocation(destination);
        manager.RelocateSingleThing(item, destination);
        Assert.Empty(destination.AddedThings);
    }

    [Fact]
    public void Batch_RechecksEquipmentAfterMatchesWereCollected() {
        var item = AddItem("pc");
        int matches = 0;
        manager.GetProfile(destination).Rules[0].Match = _ => {
            if (++matches == 2)
                item.c_equippedSlot = 1;
            return true;
        };

        manager.ExecuteRelocation(destination);

        Assert.Equal(2, matches);
        Assert.Empty(destination.AddedThings);
    }

    [Fact]
    public void Single_RechecksEquipmentAfterRuleEvaluation() {
        var item = AddItem("pc");
        manager.GetProfile(destination).Rules[0].Match = _ => {
            item.c_equippedSlot = 1;
            return true;
        };

        manager.RelocateSingleThing(item, destination);

        Assert.Empty(destination.AddedThings);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnequippedItem_RemainsEligibleForBatchOrSingle(bool single) {
        var item = AddItem("pc");
        Assert.Contains(item, manager.GetMatches(destination));

        if (single)
            manager.RelocateSingleThing(item, destination);
        else
            manager.ExecuteRelocation(destination);

        Assert.Equal(new[] { item }, destination.AddedThings);
    }

    [Fact]
    public void DisabledProfile_HidesPreviewAndBlocksBulkButPreservesSingleApiContract() {
        var item = AddItem("pc");
        item.Num = 7;
        var profile = manager.GetProfile(destination);
        Assert.Contains(item, manager.GetMatches(destination));

        profile.Enabled = false;
        Assert.Empty(manager.GetMatches(destination));
        manager.ExecuteRelocation(destination);
        Assert.Empty(destination.AddedThings);
        Assert.Same(EClass.pc, item.parent);
        Assert.Equal(7, item.Num);

        manager.RelocateSingleThing(item, destination);
        Assert.Equal(new[] { item }, destination.AddedThings);
        Assert.Same(destination, item.parent);
        Assert.Equal(7, item.Num);
        Assert.False(profile.Enabled);
    }
}
