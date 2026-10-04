using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public sealed class ArsPr4FixtureOwnershipTests
{
    [TestCase(77, TestName = "ExistingGlobalMovedToMapNeverOwned")]
    [TestCase(88, TestName = "ExistingCarryoverMovedToMapNeverOwned")]
    [TestCase(1, TestName = "ExistingMapCardNeverOwned")]
    public void ExistingUidCannotBecomeOwnedEvenWithFalseBirthEvidence(int uid)
    {
        var spell = new object();
        var original = new object();
        var proof = new ArsPr4SummonOwnership(new[] { 1, 77, 88 }, spell);
        proof.ObserveBirth(original);
        Assert.That(proof.ObserveSpellResult(original, uid, spell), Is.False);
        Assert.That(proof.IsOwned(original, uid), Is.False);
        Assert.That(proof.OwnedObjects.Count, Is.Zero);
        Assert.That(proof.RejectedResult, Is.True);
    }

    [Test]
    public void MapAdditionWithoutGenerationEvidenceNeverOwned()
    {
        var spell = new object();
        var injected = new object();
        var proof = new ArsPr4SummonOwnership(new int[0], spell);
        Assert.That(proof.ObserveSpellResult(injected, 101, spell), Is.False);
        Assert.That(proof.IsOwned(injected, 101), Is.False);
        Assert.That(proof.OwnedObjects.Count, Is.Zero);
        Assert.That(proof.RejectedResult, Is.True);
    }

    [Test]
    public void DifferentPatchCreationWithoutExactSpellReturnNeverOwned()
    {
        var proof = new ArsPr4SummonOwnership(new int[0], new object());
        var unrelated = new object();
        proof.ObserveBirth(unrelated);
        Assert.That(proof.IsOwned(unrelated, 102), Is.False);
        Assert.That(proof.OwnedObjects.Count, Is.Zero);
    }

    [Test]
    public void DifferentSpellInstanceCannotDonateOwnership()
    {
        var proof = new ArsPr4SummonOwnership(new int[0], new object());
        var unrelated = new object();
        proof.ObserveBirth(unrelated);
        Assert.That(proof.ObserveSpellResult(unrelated, 103, new object()), Is.False);
        Assert.That(proof.OwnedObjects.Count, Is.Zero);
    }

    [Test]
    public void NativeBirthAndExactSpellReturnOwnOnlyThatReference()
    {
        var spell = new object();
        var generated = new object();
        var sameUidReplacement = new object();
        var proof = new ArsPr4SummonOwnership(new[] { 1, 77, 88 }, spell);
        proof.ObserveBirth(generated);
        Assert.That(proof.ObserveSpellResult(generated, 104, spell), Is.True);
        Assert.That(proof.IsOwned(generated, 104), Is.True);
        Assert.That(proof.IsOwned(sameUidReplacement, 104), Is.False);
        Assert.That(proof.OwnedObjects, Is.EquivalentTo(new[] { generated }));
        Assert.That(proof.RejectedResult, Is.False);
    }

    [Test]
    public void ProtectedUidSnapshotCannotBeChangedByLaterCollectionMutation()
    {
        var spell = new object();
        var before = new HashSet<int> { 77, 88 };
        var proof = new ArsPr4SummonOwnership(before, spell);
        before.Clear();
        var card = new object();
        proof.ObserveBirth(card);
        Assert.That(proof.ObserveSpellResult(card, 77, spell), Is.False);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void InvalidUidNeverOwned(int uid)
    {
        var spell = new object();
        var card = new object();
        var proof = new ArsPr4SummonOwnership(new int[0], spell);
        proof.ObserveBirth(card);
        Assert.That(proof.ObserveSpellResult(card, uid, spell), Is.False);
        Assert.That(proof.OwnedObjects.Count, Is.Zero);
    }

    [Test]
    public void NullFilterResultDoesNotPreventNativeFallback()
    {
        var spell = new object();
        var card = new object();
        var proof = new ArsPr4SummonOwnership(new int[0], spell);
        Assert.That(proof.ObserveSpellResult(null, 0, spell), Is.False);
        proof.ObserveBirth(card);
        Assert.That(proof.ObserveSpellResult(card, 105, spell), Is.True);
        Assert.That(proof.RejectedResult, Is.False);
    }

    [Test]
    public void DuplicateUidFromDifferentReferenceIsRejected()
    {
        var spell = new object();
        var first = new object();
        var second = new object();
        var proof = new ArsPr4SummonOwnership(new int[0], spell);
        proof.ObserveBirth(first);
        proof.ObserveBirth(second);
        Assert.That(proof.ObserveSpellResult(first, 106, spell), Is.True);
        Assert.That(proof.ObserveSpellResult(second, 106, spell), Is.False);
        Assert.That(proof.IsOwned(first, 106), Is.True);
        Assert.That(proof.IsOwned(second, 106), Is.False);
        Assert.That(proof.RejectedResult, Is.True);
    }
}
