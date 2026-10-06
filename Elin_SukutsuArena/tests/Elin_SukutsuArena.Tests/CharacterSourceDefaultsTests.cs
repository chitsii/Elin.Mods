#nullable disable
using NUnit.Framework;

namespace Elin_SukutsuArena.Tests
{
    [TestFixture]
    public class CharacterSourceDefaultsTests
    {
        [TearDown]
        public void ResetOwner() => ModUtil.Package = null;

        [Test]
        public void OwnedCharacterWithMissingColorTypeUsesNativeEmptyDefault()
        {
            var row = new SourceChara.Row();
            ModUtil.Package = new ModPackage { id = CharacterSourceDefaultsPatch.PackageId };
            CharacterSourceDefaultsPatch.Prefix(row);
            Assert.That(row.colorType, Is.EqualTo(string.Empty));
            CharacterSourceDefaultsPatch.Prefix(row);
            Assert.That(row.colorType, Is.EqualTo(string.Empty));
        }

        [TestCase("")]
        [TestCase("alt")]
        [TestCase("random/2")]
        [TestCase("dye")]
        public void ExistingColorPolicyIsPreserved(string colorType)
        {
            var row = new SourceChara.Row { colorType = colorType };
            ModUtil.Package = new ModPackage { id = CharacterSourceDefaultsPatch.PackageId };
            CharacterSourceDefaultsPatch.Prefix(row);
            Assert.That(row.colorType, Is.EqualTo(colorType));
        }

        [TestCase(null)]
        [TestCase("other.mod")]
        [TestCase("chitsii.elin.sukutsu_arena.other")]
        public void UnownedCharacterIsUntouched(string packageId)
        {
            var row = new SourceChara.Row();
            ModUtil.Package = packageId == null ? null : new ModPackage { id = packageId };
            CharacterSourceDefaultsPatch.Prefix(row);
            Assert.That(row.colorType, Is.Null);
        }

        [Test]
        public void OwnedThingIsUntouched()
        {
            var row = new CardRow();
            ModUtil.Package = new ModPackage { id = CharacterSourceDefaultsPatch.PackageId };
            CharacterSourceDefaultsPatch.Prefix(row);
            Assert.That(row.colorType, Is.Null);
        }

        [Test]
        public void NullRowIsUntouched() => CharacterSourceDefaultsPatch.Prefix(null);
    }
}

// Unit contracts only. Runtime tests check the actual game's registration,
// materials, and applied Harmony owner.
public class CardRow { public string colorType; }
public class SourceCard { public void AddRow(CardRow row, bool isChara = false) { } }
public class SourceChara { public class Row : CardRow { } }
public class ModPackage { public string id; }
public static class ModUtil
{
    public static ModPackage Package;
    public static ModPackage FindSourceRowPackage(CardRow row) => Package;
}
namespace HarmonyLib
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class HarmonyPatch : System.Attribute
    {
        public HarmonyPatch(System.Type type, string method, System.Type[] parameters) { }
    }
}
