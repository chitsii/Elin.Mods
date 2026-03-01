using System.Collections.Generic;
using NUnit.Framework;

namespace Elin_ArsMoriendi.Tests
{
    [TestFixture]
    public class DialogFlagStoreTests
    {
        [Test]
        public void GetInt_ReturnsZero_WhenMissing()
        {
            var flags = new Dictionary<string, int>();
            Assert.AreEqual(0, DialogFlagStore.GetInt(flags, "missing"));
        }

        [Test]
        public void SetAndGetBool_RoundTrips()
        {
            var flags = new Dictionary<string, int>();
            DialogFlagStore.SetBool(flags, "k", true);
            Assert.IsTrue(DialogFlagStore.IsTrue(flags, "k"));
            DialogFlagStore.SetBool(flags, "k", false);
            Assert.IsFalse(DialogFlagStore.IsTrue(flags, "k"));
        }

        [Test]
        public void NullFlags_AreHandledSafely()
        {
            IDictionary<string, int>? flags = null;
            DialogFlagStore.SetInt(flags, "k", 1);
            Assert.AreEqual(0, DialogFlagStore.GetInt(flags, "k"));
            Assert.IsFalse(DialogFlagStore.IsTrue(flags, "k"));
        }
    }
}
