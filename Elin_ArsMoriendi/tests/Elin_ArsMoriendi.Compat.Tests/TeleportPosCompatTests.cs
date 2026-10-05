using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Elin_ArsMoriendi.Tests
{
    [TestFixture]
    public class TeleportPosCompatTests
    {
        private static CompatSymbol TeleportSymbol()
        {
            var symbol = CompatSymbol.All.SingleOrDefault(s => s.Id == "ActEffect.GetTeleportPos");
            Assert.That(symbol, Is.Not.Null, "GetTeleportPos must participate in startup warmup.");
            return symbol!;
        }

        private static Func<Point, int, Point> ProductionCall()
        {
            var type = typeof(MethodResolver).Assembly.GetType("Elin_ArsMoriendi.TeleportPosCompat");
            Assert.That(type, Is.Not.Null, "A runtime-compatible call must replace the stable-only call.");
            return (Func<Point, int, Point>)Delegate.CreateDelegate(
                typeof(Func<Point, int, Point>), type!.GetMethod("GetTeleportPos")!);
        }

        [SetUp]
        public void ResetMap() => ContractWorld.Reset();

#if TELEPORT_UNSUPPORTED
        [Test]
        public void UnknownSignature_IsCachedUnresolved_AndNeverReturnsAnUnsafePosition()
        {
            var symbol = TeleportSymbol();
            int before = ModLog.Warnings.Count(w => w.Contains(symbol.Id));
            Assert.That(MethodResolver.Resolve(symbol).IsResolved, Is.False);
            Assert.That(MethodResolver.Resolve(symbol).IsResolved, Is.False);
            Assert.That(ModLog.Warnings.Count(w => w.Contains(symbol.Id)) - before, Is.EqualTo(1));
            var call = ProductionCall();
            var failure = Assert.Throws<MissingMethodException>(() => call(new Point(), 6));
            Assert.That(Assert.Throws<MissingMethodException>(() => call(new Point(), 6)), Is.SameAs(failure));
            Assert.That(ModLog.Warnings.Count(w => w.Contains(symbol.Id)) - before, Is.EqualTo(1));
            Assert.That(ContractWorld.RandomCalls, Is.Zero);
        }
#else
        [Test]
        public void Warmup_ResolvesKnownSignatureStrictly_AndCachesTheSameMethod()
        {
            var symbol = TeleportSymbol();
            var warm = MethodResolver.Warmup().Single(r => r.SymbolId == symbol.Id);
            Assert.That(warm.ResolutionMode, Is.EqualTo(CompatResolutionMode.Strict));
            Assert.That(MethodResolver.Resolve(symbol).Method, Is.SameAs(warm.Method));
#if TELEPORT_NIGHTLY
            Assert.That(warm.Method!.GetParameters().Length, Is.EqualTo(3));
#else
            Assert.That(warm.Method!.GetParameters().Length, Is.EqualTo(2));
#endif
        }

        [TestCase(6, 5)]
        [TestCase(24, 23)]
        public void Call_PreservesOriginAndRadius_WithoutAddingLineOfSightRestrictions(int radius, int offset)
        {
            var origin = new Point { x = 31, z = 17 };
            var call = ProductionCall();
            var result = call(origin, radius);
            Assert.That(result.x, Is.EqualTo(31 + offset));
            Assert.That(result.z, Is.EqualTo(17));
            Assert.That(origin.x, Is.EqualTo(31));
            Assert.That(ContractWorld.RandomCalls, Is.EqualTo(4));
            Assert.That(ContractWorld.LosCalls, Is.Zero, "Nightly must receive target=null.");
            // Reuse the cached binding with fresh arguments.
            Assert.That(call(new Point { x = 2, z = 9 }, radius).x, Is.EqualTo(2 + offset));
        }

        [Test]
        public void ExhaustedSearch_PreservesVanillaNearestPointFallback()
        {
            ContractWorld.Blocked = true;
            Assert.That(ProductionCall()(new Point(), 6), Is.SameAs(ContractWorld.Fallback));
            Assert.That(ContractWorld.RandomCalls, Is.EqualTo(40000));
        }

        [Test]
        public void GameException_PropagatesUnwrapped_ToTheExistingSpellCatch()
        {
            var failure = new InvalidOperationException("controlled map failure");
            ContractWorld.RandomFailure = failure;
            var call = ProductionCall();
            Assert.That(Assert.Throws<InvalidOperationException>(() => call(new Point(), 6)), Is.SameAs(failure));
            ContractWorld.RandomFailure = null;
            Assert.That(call(new Point(), 6).x, Is.EqualTo(5), "A game exception must not poison the binding.");
        }

#if TELEPORT_NIGHTLY
        [Test]
        public void NonNullTargetCounterexample_ActuallyAddsTheNightlyLosRestriction()
        {
            var result = ActEffect.GetTeleportPos(new Point(), 6, new Chara());
            Assert.That(result.x, Is.EqualTo(5));
            Assert.That(ContractWorld.LosCalls, Is.EqualTo(100));
            Assert.That(ContractWorld.RandomCalls, Is.EqualTo(404));
        }
#endif
#endif

        [TestCase(typeof(WrongThirdArgument))]
        [TestCase(typeof(ExtraArgument))]
        [TestCase(typeof(WrongReturn))]
        [TestCase(typeof(InstanceMethod))]
        public void UnsupportedOverload_IsRejectedWithoutLooseFallback(Type owner)
        {
            var known = TeleportSymbol();
            var symbol = new CompatSymbol("counterexample." + owner.Name, owner,
                new[] { "GetTeleportPos" }, true, known.Predicate, known.StrictSignatures.ToArray());
            Assert.That(MethodResolver.Resolve(symbol).IsResolved, Is.False);
        }

        [Test]
        public void BothKnownSignatures_PreferTheNightlySignature()
        {
            var known = TeleportSymbol();
            var symbol = new CompatSymbol("counterexample.both", typeof(BothSignatures),
                new[] { "GetTeleportPos" }, true, known.Predicate, known.StrictSignatures.ToArray());
            Assert.That(MethodResolver.Resolve(symbol).Method!.GetParameters().Length, Is.EqualTo(3));
        }

        public class WrongThirdArgument { public static Point GetTeleportPos(Point p, int r, bool b = false) => p; }
        public class ExtraArgument { public static Point GetTeleportPos(Point p, int r, Chara c, bool b = false) => p; }
        public class WrongReturn { public static object GetTeleportPos(Point p, int r) => p; }
        public class InstanceMethod { public Point GetTeleportPos(Point p, int r) => p; }
        public class BothSignatures
        {
            public static Point GetTeleportPos(Point p, int r) => p;
            public static Point GetTeleportPos(Point p, int r, Chara? c) => p;
        }
    }
}
