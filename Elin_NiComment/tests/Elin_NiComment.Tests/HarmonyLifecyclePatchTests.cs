using System;
using System.Collections.Generic;
using HarmonyLib;
using NUnit.Framework;

namespace Elin_NiComment.Tests
{
    [TestFixture]
    public class HarmonyLifecyclePatchTests
    {
        [SetUp]
        public void SetUp()
        {
            FakeEvents.Clear();
        }

        [Test]
        public void ProductionLifecyclePatchesCanBeAppliedToCurrentElinMethods()
        {
            var harmony = new Harmony("Elin_NiComment.Tests.productionLifecycle");

            try
            {
                Assert.DoesNotThrow(() => harmony.CreateClassProcessor(typeof(CharaDiePatch)).Patch());
                Assert.DoesNotThrow(() => harmony.CreateClassProcessor(typeof(QuestCompletePatch)).Patch());
            }
            finally
            {
                harmony.UnpatchSelf();
            }
        }

        [Test]
        public void BaseDiePatchDoesNotObserveDerivedOverrideThatSkipsBaseMethod()
        {
            var harmony = new Harmony("Elin_NiComment.Tests.baseOverrideMiss");

            try
            {
                harmony.CreateClassProcessor(typeof(FakeBaseDiePatch)).Patch();
                new FakeChara("derived").Die();

                Assert.That(FakeEvents.Items, Is.Empty);
            }
            finally
            {
                harmony.UnpatchSelf();
            }
        }

        [Test]
        public void DerivedDiePatchObservesOverrideLifecycleOnce()
        {
            var harmony = new Harmony("Elin_NiComment.Tests.derivedOverrideHit");

            try
            {
                harmony.CreateClassProcessor(typeof(FakeCharaDiePatch)).Patch();
                new FakeChara("derived").Die();

                Assert.That(FakeEvents.Items, Is.EqualTo(new[] { "death:derived" }));
            }
            finally
            {
                harmony.UnpatchSelf();
            }
        }

        [Test]
        public void DerivedDiePatchSkipsNoOpLifecycle()
        {
            var harmony = new Harmony("Elin_NiComment.Tests.derivedNoOp");

            try
            {
                harmony.CreateClassProcessor(typeof(FakeCharaDiePatch)).Patch();
                new FakeChara("no-op") { SkipDeath = true }.Die();

                Assert.That(FakeEvents.Items, Is.Empty);
            }
            finally
            {
                harmony.UnpatchSelf();
            }
        }

        [Test]
        public void DerivedDiePatchSuppressesSameInstanceReentry()
        {
            var harmony = new Harmony("Elin_NiComment.Tests.derivedSameReentry");
            var chara = new FakeChara("same");
            chara.ReenterSameOnce = true;

            try
            {
                harmony.CreateClassProcessor(typeof(FakeCharaDiePatch)).Patch();
                chara.Die();

                Assert.That(FakeEvents.Items, Is.EqualTo(new[] { "death:same" }));
            }
            finally
            {
                harmony.UnpatchSelf();
            }
        }

        [Test]
        public void QuestCompletePatchAllowsNestedDifferentQuestEvents()
        {
            var harmony = new Harmony("Elin_NiComment.Tests.questNestedDifferent");
            var inner = new FakeQuest("inner");
            var outer = new FakeQuest("outer") { NestedQuest = inner };

            try
            {
                harmony.CreateClassProcessor(typeof(FakeQuestCompletePatch)).Patch();
                outer.Complete();

                Assert.That(FakeEvents.Items, Is.EqualTo(new[] { "quest:inner", "quest:outer" }));
            }
            finally
            {
                harmony.UnpatchSelf();
            }
        }

        [Test]
        public void QuestCompletePatchSuppressesAlreadyCompleteAndNoOpCalls()
        {
            var harmony = new Harmony("Elin_NiComment.Tests.questSuppression");

            try
            {
                harmony.CreateClassProcessor(typeof(FakeQuestCompletePatch)).Patch();
                new FakeQuest("already") { IsComplete = true }.Complete();
                new FakeQuest("no-op") { SkipComplete = true }.Complete();

                Assert.That(FakeEvents.Items, Is.Empty);
            }
            finally
            {
                harmony.UnpatchSelf();
            }
        }

        [Test]
        public void FinalizerReleasesLifecycleAfterException()
        {
            var harmony = new Harmony("Elin_NiComment.Tests.exceptionCleanup");
            var quest = new FakeQuest("retry") { ThrowBeforeComplete = true };

            try
            {
                harmony.CreateClassProcessor(typeof(FakeQuestCompletePatch)).Patch();

                Assert.Throws<InvalidOperationException>(() => quest.Complete());
                Assert.That(FakeEvents.Items, Is.Empty);

                quest.ThrowBeforeComplete = false;
                quest.Complete();

                Assert.That(FakeEvents.Items, Is.EqualTo(new[] { "quest:retry" }));
            }
            finally
            {
                harmony.UnpatchSelf();
            }
        }
    }

    internal static class FakeEvents
    {
        internal static readonly List<string> Items = new List<string>();

        internal static void Clear()
        {
            Items.Clear();
        }
    }

    internal class FakeCard
    {
        internal FakeCard(string id)
        {
            Id = id;
        }

        internal string Id { get; }
        internal bool IsDead { get; set; }

        public virtual void Die()
        {
            IsDead = true;
        }
    }

    internal class FakeChara : FakeCard
    {
        internal FakeChara(string id) : base(id)
        {
        }

        internal bool ReenterSameOnce { get; set; }
        internal bool SkipDeath { get; set; }

        public override void Die()
        {
            if (ReenterSameOnce)
            {
                ReenterSameOnce = false;
                Die();
            }

            if (!SkipDeath) IsDead = true;
        }
    }

    internal class FakeQuest
    {
        internal FakeQuest(string id)
        {
            Id = id;
        }

        internal string Id { get; }
        internal bool IsComplete { get; set; }
        internal bool SkipComplete { get; set; }
        internal bool ThrowBeforeComplete { get; set; }
        internal FakeQuest NestedQuest { get; set; }

        public void Complete()
        {
            NestedQuest?.Complete();
            if (ThrowBeforeComplete) throw new InvalidOperationException("boom");
            if (!SkipComplete) IsComplete = true;
        }
    }

    [HarmonyPatch(typeof(FakeCard), nameof(FakeCard.Die))]
    internal static class FakeBaseDiePatch
    {
        public static void Postfix(FakeCard __instance)
        {
            if (__instance.IsDead) FakeEvents.Items.Add("base:" + __instance.Id);
        }
    }

    [HarmonyPatch(typeof(FakeChara), nameof(FakeChara.Die))]
    internal static class FakeCharaDiePatch
    {
        private static readonly LifecycleEventGate Gate = new LifecycleEventGate();

        public static void Prefix(FakeChara __instance, out LifecycleEventGate.State __state)
        {
            __state = Gate.Begin(__instance, __instance == null || __instance.IsDead);
        }

        public static void Postfix(FakeChara __instance, LifecycleEventGate.State __state)
        {
            if (Gate.Finish(__state, __instance != null && __instance.IsDead))
            {
                FakeEvents.Items.Add("death:" + __instance.Id);
            }
        }

        public static Exception Finalizer(Exception __exception, LifecycleEventGate.State __state)
        {
            Gate.Abort(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(FakeQuest), nameof(FakeQuest.Complete))]
    internal static class FakeQuestCompletePatch
    {
        private static readonly LifecycleEventGate Gate = new LifecycleEventGate();

        public static void Prefix(FakeQuest __instance, out LifecycleEventGate.State __state)
        {
            __state = Gate.Begin(__instance, __instance == null || __instance.IsComplete);
        }

        public static void Postfix(FakeQuest __instance, LifecycleEventGate.State __state)
        {
            if (Gate.Finish(__state, __instance != null && __instance.IsComplete))
            {
                FakeEvents.Items.Add("quest:" + __instance.Id);
            }
        }

        public static Exception Finalizer(Exception __exception, LifecycleEventGate.State __state)
        {
            Gate.Abort(__state);
            return __exception;
        }
    }
}
