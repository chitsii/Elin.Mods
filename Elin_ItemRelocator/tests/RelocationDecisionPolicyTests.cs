using Xunit;

namespace Elin_ItemRelocator.Tests;

public sealed class RelocationDecisionPolicyTests {
    public static TheoryData<RelocationScope, bool, bool> OwnerScopes => new() {
        { RelocationScope.Inventory, true, false },
        { RelocationScope.Both, true, false },
        { RelocationScope.PetsOnly, false, true },
        { RelocationScope.ZoneOnly, false, false }
    };

    private static RelocationDecisionInput ValidInput() => new() {
        Scope = RelocationScope.Both,
        DestinationAvailable = true,
        SourceRuleMatches = true,
        DestinationHasCapacityForThing = true,
        ItemCanBeDropped = true
    };

    [Theory]
    [MemberData(nameof(OwnerScopes))]
    public void EquippedItem_IsExcludedRegardlessOfOwner(RelocationScope scope, bool pcOwned, bool petOwned) {
        var input = ValidInput();
        input.Scope = scope;
        input.ItemIsPcOwned = pcOwned;
        input.ItemIsPetOwned = petOwned;
        input.ItemIsEquipped = true;

        Assert.False(RelocationDecisionPolicy.CanPreview(input));
        Assert.False(RelocationDecisionPolicy.CanMove(input));
    }

    [Theory]
    [MemberData(nameof(OwnerScopes))]
    public void UnequippedItem_RemainsEligibleInItsOwnerScope(RelocationScope scope, bool pcOwned, bool petOwned) {
        var input = ValidInput();
        input.Scope = scope;
        input.ItemIsPcOwned = pcOwned;
        input.ItemIsPetOwned = petOwned;

        Assert.True(RelocationDecisionPolicy.CanPreview(input));
        Assert.True(RelocationDecisionPolicy.CanMove(input));
    }

    [Fact]
    public void CanMove_RejectsItemEquippedAfterPreview() {
        var input = ValidInput();
        Assert.True(RelocationDecisionPolicy.CanPreview(input));

        input.ItemIsEquipped = true;

        Assert.False(RelocationDecisionPolicy.CanMove(input));
    }

    [Fact]
    public void CanPreview_RechecksItemProtectionsAfterCacheWasBuilt() {
        var input = ValidInput();
        input.ItemIsImportant = true;

        Assert.False(RelocationDecisionPolicy.CanPreview(input));
    }

    [Fact]
    public void CanPreview_RechecksHardLockAfterCacheWasBuilt() {
        var input = ValidInput();
        input.ItemIsLockedHard = true;

        Assert.False(RelocationDecisionPolicy.CanPreview(input));
    }

    [Fact]
    public void CanPreview_RechecksHotbarAfterCacheWasBuilt() {
        var input = ValidInput();
        input.ItemIsOnHotbar = true;

        Assert.False(RelocationDecisionPolicy.CanPreview(input));
    }

    [Fact]
    public void CanMove_RejectsItemAlreadyInDestinationContainer() {
        var input = ValidInput();
        input.ItemAlreadyInDestination = true;

        Assert.False(RelocationDecisionPolicy.CanMove(input));
    }

    [Fact]
    public void CanPreview_UsesCurrentScopeOwnership() {
        var input = ValidInput();
        input.Scope = RelocationScope.ZoneOnly;
        input.ItemIsPcOwned = true;

        Assert.False(RelocationDecisionPolicy.CanPreview(input));
    }

    [Fact]
    public void CanPreview_UsesCurrentRuleResult() {
        var input = ValidInput();
        input.SourceRuleMatches = false;

        Assert.False(RelocationDecisionPolicy.CanPreview(input));
    }

    [Fact]
    public void CanMove_AllowsFullDestinationWhenThingCanMerge() {
        var input = ValidInput();
        input.DestinationHasCapacityForThing = true;

        Assert.True(RelocationDecisionPolicy.CanMove(input));
    }

    [Fact]
    public void CanMove_RejectsDestinationThatBecameLocked() {
        var input = ValidInput();
        input.DestinationAvailable = false;

        Assert.False(RelocationDecisionPolicy.CanMove(input));
    }

    [Fact]
    public void CanMove_RejectsDestinationWithoutCapacityForNonMergeableThing() {
        var input = ValidInput();
        input.DestinationHasCapacityForThing = false;

        Assert.False(RelocationDecisionPolicy.CanMove(input));
    }

    [Fact]
    public void CanMove_AllowsItemAfterItIsManuallyUnequipped() {
        var input = ValidInput();
        input.ItemIsEquipped = true;

        Assert.False(RelocationDecisionPolicy.CanMove(input));

        input.ItemIsEquipped = false;

        Assert.True(RelocationDecisionPolicy.CanPreview(input));
        Assert.True(RelocationDecisionPolicy.CanMove(input));
    }
}
