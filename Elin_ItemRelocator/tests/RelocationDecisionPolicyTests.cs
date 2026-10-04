using Xunit;

namespace Elin_ItemRelocator.Tests;

public sealed class RelocationDecisionPolicyTests {
    private static RelocationDecisionInput ValidInput() => new() {
        Scope = RelocationScope.Both,
        DestinationAvailable = true,
        SourceRuleMatches = true,
        DestinationHasCapacityForThing = true,
        ItemCanBeDropped = true
    };

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
    public void CanMove_PreservesExistingCursedEquippedProtection() {
        var input = ValidInput();
        input.ItemIsEquippedAndCursed = true;

        Assert.False(RelocationDecisionPolicy.CanMove(input));
    }
}
