using NUnit.Framework;

namespace Elin_ArsMoriendi.Tests
{
    [TestFixture]
    public class ArsDramaResolverTests
    {
        private sealed class FakeContext : IArsDramaRuntimeContext
        {
            public bool QuestComplete;
            public bool ErenosBorrowed = false;
            public bool CanStart = true;
            public bool IsComplete = false;
            public int EnsureCalls;
            public int BorrowCalls;
            public int StopBgmCalls;
            public int PartyShowCalls;
            public int SetTalkPortraitCalls;
            public int TryStartCalls;
            public int TryStartRepeatableCalls;
            public int TryStartUntilCompleteCalls;
            public string? LastTryStartDramaId;
            public string? LastTryStartRepeatableDramaId;
            public string? LastTryStartUntilCompleteDramaId;
            public string? LastIsCompleteDramaId;
            public string? LastEffect;
            public string? LastSound;
            public string? LastPortraitId;

            public bool IsQuestComplete() => QuestComplete;
            public bool IsDramaComplete(string dramaId)
            {
                LastIsCompleteDramaId = dramaId;
                return IsComplete;
            }
            public bool IsErenosBorrowed() => ErenosBorrowed;
            public bool CanStartDrama(string dramaId) => CanStart;
            public bool TryStartDrama(string dramaId)
            {
                TryStartCalls++;
                LastTryStartDramaId = dramaId;
                return CanStart;
            }
            public bool TryStartDramaRepeatable(string dramaId)
            {
                TryStartRepeatableCalls++;
                LastTryStartRepeatableDramaId = dramaId;
                return CanStart;
            }
            public bool TryStartDramaUntilComplete(string dramaId)
            {
                TryStartUntilCompleteCalls++;
                LastTryStartUntilCompleteDramaId = dramaId;
                return CanStart;
            }
            public void EnsureErenosNearPlayerForDrama() => EnsureCalls++;
            public void BorrowErenos() => BorrowCalls++;
            public void StopBgmNow() => StopBgmCalls++;
            public void PlayPcEffect(string effectId, string? soundId = null)
            {
                LastEffect = effectId;
                LastSound = soundId;
            }
            public void PlayHecatiaPartyShow() => PartyShowCalls++;
            public void SetTalkPortrait(string portraitId)
            {
                SetTalkPortraitCalls++;
                LastPortraitId = portraitId;
            }
        }

        [Test]
        public void TryResolveBool_ReturnsKnownQuestState()
        {
            var ctx = new FakeContext { QuestComplete = true };
            var resolver = new ArsDramaResolver(ctx);
            bool ok = resolver.TryResolveBool("quest.is_complete", out bool value);
            Assert.IsTrue(ok);
            Assert.IsTrue(value);
        }

        [Test]
        public void TryResolveBool_ReturnsFalse_ForUnknownKey()
        {
            var resolver = new ArsDramaResolver(new FakeContext());
            bool ok = resolver.TryResolveBool("unknown.key", out bool value);
            Assert.IsFalse(ok);
            Assert.IsFalse(value);
        }

        [Test]
        public void TryExecute_DispatchesKnownCommand()
        {
            var ctx = new FakeContext();
            var resolver = new ArsDramaResolver(ctx);
            Assert.IsTrue(resolver.TryExecute("erenos.ensure_near_player"));
            Assert.AreEqual(1, ctx.EnsureCalls);
        }

        [Test]
        public void TryExecute_DispatchesGenericFxCommand()
        {
            var ctx = new FakeContext();
            var resolver = new ArsDramaResolver(ctx);
            Assert.IsTrue(resolver.TryExecute("fx.pc.darkwomb+sfx.pc.curse3"));
            Assert.AreEqual("darkwomb", ctx.LastEffect);
            Assert.AreEqual("curse3", ctx.LastSound);
        }

        [Test]
        public void TryExecute_DispatchesCueCommand()
        {
            var ctx = new FakeContext();
            var resolver = new ArsDramaResolver(ctx);
            Assert.IsTrue(resolver.TryExecute("cue.apotheosis.silence"));
            Assert.AreEqual(1, ctx.StopBgmCalls);
        }

        [Test]
        public void TryExecute_DispatchesHecatiaPartyCommand()
        {
            var ctx = new FakeContext();
            var resolver = new ArsDramaResolver(ctx);
            Assert.IsTrue(resolver.TryExecute("cmd.hecatia.party_show"));
            Assert.AreEqual(1, ctx.PartyShowCalls);
        }

        [Test]
        public void TryExecute_DispatchesHecatiaPartyPortraitCommand()
        {
            var ctx = new FakeContext();
            var resolver = new ArsDramaResolver(ctx);
            Assert.IsTrue(resolver.TryExecute("cmd.hecatia.set_party_portrait"));
            Assert.AreEqual(1, ctx.SetTalkPortraitCalls);
            Assert.AreEqual("UN_ars_hecatia_happy", ctx.LastPortraitId);
        }

        [Test]
        public void TryResolveBool_HandlesQuestCanStartPrefix()
        {
            var ctx = new FakeContext { CanStart = true };
            var resolver = new ArsDramaResolver(ctx);

            bool ok = resolver.TryResolveBool("state.quest.can_start.ars_first_soul", out bool value);

            Assert.IsTrue(ok);
            Assert.IsTrue(value);
        }

        [Test]
        public void TryExecute_HandlesQuestTryStartPrefix()
        {
            var ctx = new FakeContext { CanStart = true };
            var resolver = new ArsDramaResolver(ctx);

            bool ok = resolver.TryExecute("cmd.quest.try_start.ars_first_soul");

            Assert.IsTrue(ok);
            Assert.AreEqual(1, ctx.TryStartCalls);
            Assert.AreEqual("ars_first_soul", ctx.LastTryStartDramaId);
        }

        [Test]
        public void TryExecute_QuestTryStartPrefix_ReturnsTrue_EvenWhenStartIsSkipped()
        {
            var ctx = new FakeContext { CanStart = false };
            var resolver = new ArsDramaResolver(ctx);

            bool ok = resolver.TryExecute("cmd.quest.try_start.ars_first_soul");

            Assert.IsTrue(ok);
            Assert.AreEqual(1, ctx.TryStartCalls);
            Assert.AreEqual("ars_first_soul", ctx.LastTryStartDramaId);
        }

        [Test]
        public void TryExecute_HandlesQuestTryStartRepeatablePrefix()
        {
            var ctx = new FakeContext { CanStart = true };
            var resolver = new ArsDramaResolver(ctx);

            bool ok = resolver.TryExecute("cmd.quest.try_start_repeatable.ars_karen_ambush");

            Assert.IsTrue(ok);
            Assert.AreEqual(1, ctx.TryStartRepeatableCalls);
            Assert.AreEqual("ars_karen_ambush", ctx.LastTryStartRepeatableDramaId);
        }

        [Test]
        public void TryExecute_QuestTryStartRepeatablePrefix_ReturnsTrue_EvenWhenStartIsSkipped()
        {
            var ctx = new FakeContext { CanStart = false };
            var resolver = new ArsDramaResolver(ctx);

            bool ok = resolver.TryExecute("cmd.quest.try_start_repeatable.ars_karen_ambush");

            Assert.IsTrue(ok);
            Assert.AreEqual(1, ctx.TryStartRepeatableCalls);
            Assert.AreEqual("ars_karen_ambush", ctx.LastTryStartRepeatableDramaId);
        }

        [Test]
        public void TryResolveBool_HandlesQuestIsCompletePrefix()
        {
            var ctx = new FakeContext { IsComplete = true };
            var resolver = new ArsDramaResolver(ctx);

            bool ok = resolver.TryResolveBool("state.quest.is_complete.ars_karen_ambush", out bool value);

            Assert.IsTrue(ok);
            Assert.IsTrue(value);
            Assert.AreEqual("ars_karen_ambush", ctx.LastIsCompleteDramaId);
        }

        [Test]
        public void TryExecute_HandlesQuestTryStartUntilCompletePrefix()
        {
            var ctx = new FakeContext { CanStart = true };
            var resolver = new ArsDramaResolver(ctx);

            bool ok = resolver.TryExecute("cmd.quest.try_start_until_complete.ars_karen_ambush");

            Assert.IsTrue(ok);
            Assert.AreEqual(1, ctx.TryStartUntilCompleteCalls);
            Assert.AreEqual("ars_karen_ambush", ctx.LastTryStartUntilCompleteDramaId);
        }

        [Test]
        public void TryExecute_QuestTryStartUntilCompletePrefix_ReturnsTrue_EvenWhenStartIsSkipped()
        {
            var ctx = new FakeContext { CanStart = false };
            var resolver = new ArsDramaResolver(ctx);

            bool ok = resolver.TryExecute("cmd.quest.try_start_until_complete.ars_karen_ambush");

            Assert.IsTrue(ok);
            Assert.AreEqual(1, ctx.TryStartUntilCompleteCalls);
            Assert.AreEqual("ars_karen_ambush", ctx.LastTryStartUntilCompleteDramaId);
        }
    }
}
