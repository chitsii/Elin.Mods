using Newtonsoft.Json;
using NUnit.Framework;

namespace Elin_SukutsuArena.Tests.Arena
{
    [TestFixture]
    public class ArenaReturnPointStateTests
    {
        [Test]
        public void FromEntryPoint_NormalBattle_MirrorsReturnCoordinatesToBaseCoordinates()
        {
            var state = ArenaReturnPointState.FromEntryPoint(12, 34);

            Assert.That(state.ReturnX, Is.EqualTo(12));
            Assert.That(state.ReturnZ, Is.EqualTo(34));
            Assert.That(state.BaseX, Is.EqualTo(12));
            Assert.That(state.BaseZ, Is.EqualTo(34));
        }

        [Test]
        public void FromEntryPoint_RandomBattle_MirrorsReturnCoordinatesToBaseCoordinates()
        {
            var state = ArenaReturnPointState.FromEntryPoint(56, 78);

            Assert.That(state.ReturnX, Is.EqualTo(56));
            Assert.That(state.ReturnZ, Is.EqualTo(78));
            Assert.That(state.BaseX, Is.EqualTo(56));
            Assert.That(state.BaseZ, Is.EqualTo(78));
        }

        [Test]
        public void Deserialize_OldReturnJson_SynchronizesBaseCoordinates()
        {
            const string json = "{\"returnX\":321,\"returnZ\":654}";

            var state = JsonConvert.DeserializeObject<ArenaReturnPointState>(json);

            Assert.That(state.ReturnX, Is.EqualTo(321));
            Assert.That(state.ReturnZ, Is.EqualTo(654));
            Assert.That(state.BaseX, Is.EqualTo(321));
            Assert.That(state.BaseZ, Is.EqualTo(654));
        }
    }
}
