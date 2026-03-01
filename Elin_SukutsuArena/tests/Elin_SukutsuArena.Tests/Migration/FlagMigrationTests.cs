using System.Collections.Generic;
using NUnit.Framework;
using Elin_SukutsuArena.Migration;

namespace Elin_SukutsuArena.Tests.Migration
{
    [TestFixture]
    public class FlagMigrationTests
    {
        private Dictionary<string, int> _flags;

        [SetUp]
        public void SetUp()
        {
            _flags = new Dictionary<string, int>();
        }

        // === TryCleanup tests ===

        [Test]
        public void TryCleanup_RemovesLegacyKeys()
        {
            // Arrange: 古いキーを設定
            _flags["chitsii.arena.lily_trust_rebuild"] = 1;
            _flags["chitsii.arena.balgas_trust_broken"] = 1;
            _flags["chitsii.arena.balgas_killed"] = 1;

            // Act
            var removedCount = FlagMigration.TryCleanup(_flags);

            // Assert
            Assert.That(removedCount, Is.EqualTo(3));
            Assert.That(_flags.ContainsKey("chitsii.arena.lily_trust_rebuild"), Is.False);
            Assert.That(_flags.ContainsKey("chitsii.arena.balgas_trust_broken"), Is.False);
            Assert.That(_flags.ContainsKey("chitsii.arena.balgas_killed"), Is.False);
        }

        [Test]
        public void TryCleanup_SetsCleanupVersionKey()
        {
            // Act
            FlagMigration.TryCleanup(_flags);

            // Assert
            var versionKey = FlagMigration.GetCleanupVersionKey();
            Assert.That(_flags.ContainsKey(versionKey), Is.True);
            Assert.That(_flags[versionKey], Is.EqualTo(FlagMigration.GetCurrentCleanupVersion()));
        }

        [Test]
        public void TryCleanup_IsIdempotent()
        {
            // Arrange: 古いキーを設定
            _flags["chitsii.arena.lily_trust_rebuild"] = 1;

            // Act: 2回実行
            var firstRun = FlagMigration.TryCleanup(_flags);

            // 再度古いキーを設定（通常起こらないが冪等性テスト）
            _flags["chitsii.arena.lily_trust_rebuild"] = 1;

            var secondRun = FlagMigration.TryCleanup(_flags);

            // Assert: 2回目は何もしない（バージョンチェックでスキップ）
            Assert.That(firstRun, Is.EqualTo(1));
            Assert.That(secondRun, Is.EqualTo(0));

            // キーは残っている（2回目は削除されない）
            Assert.That(_flags.ContainsKey("chitsii.arena.lily_trust_rebuild"), Is.True);
        }

        [Test]
        public void TryCleanup_SkipsWhenAlreadyCleaned()
        {
            // Arrange: クリーンアップ済みとしてマーク
            var versionKey = FlagMigration.GetCleanupVersionKey();
            _flags[versionKey] = FlagMigration.GetCurrentCleanupVersion();

            // 古いキーを設定（本来は既にクリーンされているはず）
            _flags["chitsii.arena.ending"] = 1;

            // Act
            var removedCount = FlagMigration.TryCleanup(_flags);

            // Assert: クリーンアップ済みなので何もしない
            Assert.That(removedCount, Is.EqualTo(0));
            Assert.That(_flags.ContainsKey("chitsii.arena.ending"), Is.True);
        }

        [Test]
        public void TryCleanup_PreservesNewKeys()
        {
            // Arrange: 新しいキーを設定
            _flags["chitsii.arena.player.rank"] = 5;
            _flags["chitsii.arena.player.current_phase"] = 3;
            _flags["chitsii.arena.player.ending"] = 1;  // 新しいキー（player.プレフィックス付き）

            // 古いキーも設定
            _flags["chitsii.arena.ending"] = 2;  // 古いキー

            // Act
            FlagMigration.TryCleanup(_flags);

            // Assert: 新しいキーは保持される
            Assert.That(_flags["chitsii.arena.player.rank"], Is.EqualTo(5));
            Assert.That(_flags["chitsii.arena.player.current_phase"], Is.EqualTo(3));
            Assert.That(_flags["chitsii.arena.player.ending"], Is.EqualTo(1));

            // 古いキーは削除される
            Assert.That(_flags.ContainsKey("chitsii.arena.ending"), Is.False);
        }

        [Test]
        public void TryCleanup_ReturnsZeroWhenNoLegacyKeys()
        {
            // Arrange: 新しいキーのみ
            _flags["chitsii.arena.player.rank"] = 5;

            // Act
            var removedCount = FlagMigration.TryCleanup(_flags);

            // Assert
            Assert.That(removedCount, Is.EqualTo(0));
        }

        [Test]
        public void TryCleanup_HandlesNullFlags()
        {
            // Act & Assert: nullを渡しても例外が発生しない
            var removedCount = FlagMigration.TryCleanup(null);
            Assert.That(removedCount, Is.EqualTo(0));
        }

        // === GetLegacyKeysToRemove tests ===

        [Test]
        public void GetLegacyKeysToRemove_ReturnsExpectedKeys()
        {
            var legacyKeys = FlagMigration.GetLegacyKeysToRemove();

            Assert.That(legacyKeys, Contains.Item("chitsii.arena.lily_trust_rebuild"));
            Assert.That(legacyKeys, Contains.Item("chitsii.arena.balgas_trust_broken"));
            Assert.That(legacyKeys, Contains.Item("chitsii.arena.balgas_killed"));
            Assert.That(legacyKeys, Contains.Item("chitsii.arena.ending"));
            Assert.That(legacyKeys, Contains.Item("chitsii.arena.story.phase"));
        }

        [Test]
        public void GetLegacyKeysToRemove_AllKeysHaveCorrectPrefix()
        {
            var legacyKeys = FlagMigration.GetLegacyKeysToRemove();

            foreach (var key in legacyKeys)
            {
                Assert.That(key.StartsWith("chitsii.arena."), Is.True,
                    $"Legacy key should have chitsii.arena. prefix: {key}");
            }
        }
    }
}
