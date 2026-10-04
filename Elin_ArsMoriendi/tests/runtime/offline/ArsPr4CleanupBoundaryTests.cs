using System;
using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public sealed class ArsPr4CleanupBoundaryTests
{
    private sealed class FixtureReference
    {
        public int Uid;
        public bool Mutated;
    }

    [TestCase("global")]
    [TestCase("map")]
    [TestCase("carryover")]
    public void OwnedFirstAndUnownedSameUidElsewhereStopsActualCleanupBeforeMutation(string source)
    {
        var owned = new FixtureReference { Uid = 101 };
        var unowned = new FixtureReference { Uid = 101 };
        var global = new List<FixtureReference>();
        var map = new List<FixtureReference> { owned };
        var carry = new List<FixtureReference>();
        if (source == "global") { global.Add(owned); global.Add(unowned); }
        if (source == "map") map.Add(unowned);
        if (source == "carryover") carry.Add(unowned);
        int mutationCalls = 0;

        Assert.Throws<InvalidOperationException>(() => ArsPr4CleanupBoundary.Run(
            owned, owned.Uid, c => c.Uid, global, map, carry,
            (card, removeCarryover) =>
            {
                mutationCalls++;
                card.Mutated = true;
                removeCarryover();
            }));

        Assert.That(mutationCalls, Is.Zero, "Native cleanup delegate must not run on ambiguous UID.");
        Assert.That(owned.Mutated, Is.False);
        Assert.That(unowned.Mutated, Is.False);
        if (source == "global") Assert.That(global, Does.Contain(unowned));
        if (source == "map") Assert.That(map, Does.Contain(unowned));
        if (source == "carryover") Assert.That(carry, Does.Contain(unowned));
    }

    [Test]
    public void SameOwnedReferenceInSeveralSourcesCanCleanOnlyItsCarryoverEntries()
    {
        var owned = new FixtureReference { Uid = 101 };
        var unrelated = new FixtureReference { Uid = 202 };
        var global = new List<FixtureReference> { owned };
        var map = new List<FixtureReference> { owned };
        var carry = new List<FixtureReference> { owned, unrelated, owned };
        int mutationCalls = 0;
        ArsPr4CleanupBoundary.Run(owned, owned.Uid, c => c.Uid, global, map, carry,
            (card, removeCarryover) => { mutationCalls++; card.Mutated = true; removeCarryover(); });
        Assert.That(mutationCalls, Is.EqualTo(1));
        Assert.That(owned.Mutated, Is.True);
        Assert.That(carry, Is.EqualTo(new[] { unrelated }));
        Assert.That(unrelated.Mutated, Is.False);
    }

    [Test]
    public void CarryoverRemovalNeverDeletesNewSameUidReference()
    {
        var owned = new FixtureReference { Uid = 101 };
        var laterUnowned = new FixtureReference { Uid = 101 };
        var carry = new List<FixtureReference> { owned };
        ArsPr4CleanupBoundary.Run(owned, owned.Uid, c => c.Uid,
            new FixtureReference[0], new[] { owned }, carry,
            (card, removeCarryover) =>
            {
                carry.Add(laterUnowned);
                removeCarryover();
            });
        Assert.That(carry, Is.EqualTo(new[] { laterUnowned }), "Deletion must use reference, never UID.");
        Assert.That(laterUnowned.Mutated, Is.False);
    }
}
