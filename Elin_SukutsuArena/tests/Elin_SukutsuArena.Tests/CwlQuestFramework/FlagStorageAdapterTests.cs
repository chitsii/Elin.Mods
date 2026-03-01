using System;
using System.Collections.Generic;
using NUnit.Framework;
using CwlQuestFramework;
using Elin_SukutsuArena.Core;

namespace Elin_SukutsuArena.Tests.CwlQuestFramework
{
    [TestFixture]
    public class FlagStorageAdapterTests
    {
        private InMemoryFlagStorage _storage;
        private TestEnumMappingProvider _enumMappingProvider;
        private FlagStorageAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemoryFlagStorage();
            _enumMappingProvider = new TestEnumMappingProvider();
            _adapter = new FlagStorageAdapter(_storage, _enumMappingProvider);
        }

        // === IFlagValueProvider tests ===

        [Test]
        public void GetInt_DelegatesToStorage()
        {
            _storage.SetInt("key", 42);
            Assert.That(_adapter.GetInt("key"), Is.EqualTo(42));
        }

        [Test]
        public void GetInt_ReturnsDefaultValue_WhenKeyNotFound()
        {
            Assert.That(_adapter.GetInt("nonexistent", 99), Is.EqualTo(99));
        }

        [Test]
        public void HasKey_DelegatesToStorage()
        {
            _storage.SetInt("existing", 1);
            Assert.That(_adapter.HasKey("existing"), Is.True);
            Assert.That(_adapter.HasKey("nonexistent"), Is.False);
        }

        // === IFlagSetter tests ===

        [Test]
        public void SetInt_DelegatesToStorage()
        {
            _adapter.SetInt("key", 42);
            Assert.That(_storage.GetInt("key"), Is.EqualTo(42));
        }

        [Test]
        public void SetFromJsonValue_Int_SetsCorrectly()
        {
            _adapter.SetFromJsonValue("key", 42);
            Assert.That(_storage.GetInt("key"), Is.EqualTo(42));
        }

        [Test]
        public void SetFromJsonValue_Long_SetsAsInt()
        {
            _adapter.SetFromJsonValue("key", 123L);
            Assert.That(_storage.GetInt("key"), Is.EqualTo(123));
        }

        [Test]
        public void SetFromJsonValue_BoolTrue_SetsTo1()
        {
            _adapter.SetFromJsonValue("key", true);
            Assert.That(_storage.GetInt("key"), Is.EqualTo(1));
        }

        [Test]
        public void SetFromJsonValue_BoolFalse_SetsTo0()
        {
            _adapter.SetFromJsonValue("key", false);
            Assert.That(_storage.GetInt("key"), Is.EqualTo(0));
        }

        [Test]
        public void SetFromJsonValue_EnumString_UsesMapping()
        {
            // TestEnumMappingProvider returns {"phase": {"Start": 0, "Middle": 1, "End": 2}}
            _adapter.SetFromJsonValue("phase", "Middle");
            Assert.That(_storage.GetInt("phase"), Is.EqualTo(1));
        }

        [Test]
        public void SetFromJsonValue_EnumString_CaseMatters()
        {
            // "middle" (lowercase) should throw because mapping is case-sensitive
            Assert.Throws<InvalidOperationException>(() =>
                _adapter.SetFromJsonValue("phase", "middle"));
        }

        [Test]
        public void SetFromJsonValue_UnknownString_ThrowsException()
        {
            // "Unknown" is not in the phase mapping
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _adapter.SetFromJsonValue("phase", "Unknown"));
            Assert.That(ex.Message, Does.Contain("phase"));
            Assert.That(ex.Message, Does.Contain("Unknown"));
        }

        [Test]
        public void SetFromJsonValue_NumericString_ParsesAsInt()
        {
            // String "42" should be parsed as int when no enum mapping exists
            _adapter.SetFromJsonValue("numeric_key", "42");
            Assert.That(_storage.GetInt("numeric_key"), Is.EqualTo(42));
        }

        [Test]
        public void SetFromJsonValue_NonNumericString_ThrowsException_WhenNoMapping()
        {
            // "invalid" is not a number and has no mapping for "unknown_key"
            Assert.Throws<InvalidOperationException>(() =>
                _adapter.SetFromJsonValue("unknown_key", "invalid"));
        }

        [Test]
        public void SetFromJsonValue_NullEnumMappingProvider_FallsBackToIntParsing()
        {
            // Adapter with null enum mapping provider
            var adapterWithoutMapping = new FlagStorageAdapter(_storage, null);

            // Numeric string should still work
            adapterWithoutMapping.SetFromJsonValue("key", "123");
            Assert.That(_storage.GetInt("key"), Is.EqualTo(123));
        }

        // Test helper: EnumMappingProvider for testing
        private class TestEnumMappingProvider : IEnumMappingProvider
        {
            private readonly Dictionary<string, IDictionary<string, int>> _mappings = new()
            {
                ["phase"] = new Dictionary<string, int>
                {
                    ["Start"] = 0,
                    ["Middle"] = 1,
                    ["End"] = 2
                }
            };

            public bool TryGetMapping(string flagKey, out IDictionary<string, int> mapping)
            {
                return _mappings.TryGetValue(flagKey, out mapping);
            }
        }
    }
}
