namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Applies/removes apotheosis stat bonuses as a feat-driven effect.
    /// Includes a migration guard for saves that already had bonuses before
    /// this logic moved from ritual-time direct application to feat-based application.
    /// </summary>
    internal static class ApotheosisFeatBonus
    {
        private const string BonusAppliedFlag = "chitsii.ars.quest.state.apotheosis_bonus_applied";
        private const string MigrationCheckedFlag = "chitsii.ars.quest.state.apotheosis_bonus_migrated";
        private const string LegacyApotheosisFlag = "chitsii.ars.quest.state.apotheosis";
        private const string ApotheosisFeatAliasFull = "featNecroDivinity";
        private const string ApotheosisFeatAliasLite = "featNecroDivinityLite";
        private const int ApotheosisFeatIdFull = 101018;
        private const int ApotheosisFeatIdLite = 101019;
        private static bool _isSwitchingFeat;

        public static bool IsApotheosisFeatId(int id)
        {
            GetFeatIds(out int fullId, out int liteId);
            return id == fullId || id == liteId;
        }

        public static bool HasAnyApotheosisFeat(Chara pc)
        {
            if (pc?.elements == null) return false;
            GetFeatIds(out int fullId, out int liteId);
            return pc.elements.Has(fullId) || pc.elements.Has(liteId);
        }

        public static bool TryGetSelectedFeatId(out int featId)
        {
            GetFeatIds(out int fullId, out int liteId);
            featId = ModConfig.EnableApotheosisStatBonuses.Value ? fullId : liteId;
            return featId > 0;
        }

        public static bool HasAppliedBonus()
        {
            return IsBonusApplied();
        }

        public static void OnFeatGranted(Chara pc)
        {
            SyncWithConfigAndFeat(pc);
        }

        public static void OnFeatRemoved(Chara pc)
        {
            SyncWithConfigAndFeat(pc);
        }

        public static void SyncWithConfigAndFeat(Chara pc)
        {
            if (pc?.elements == null) return;
            if (_isSwitchingFeat) return;
            EnsureLegacyMigrationState(pc);

            GetFeatIds(out int fullId, out int liteId);
            bool hasFull = pc.elements.Has(fullId);
            bool hasLite = pc.elements.Has(liteId);
            bool hasFeat = hasFull || hasLite;
            bool enabled = ModConfig.EnableApotheosisStatBonuses.Value;
            bool applied = IsBonusApplied();
            int targetId = enabled ? fullId : liteId;

            if (!hasFeat)
            {
                if (applied) Revert(pc);
                return;
            }

            if (!pc.elements.Has(targetId))
            {
                try
                {
                    _isSwitchingFeat = true;
                    if (hasFull) pc.SetFeat(fullId, 0, msg: false);
                    if (hasLite) pc.SetFeat(liteId, 0, msg: false);
                    pc.SetFeat(targetId, 1, msg: false);
                }
                finally
                {
                    _isSwitchingFeat = false;
                }
                hasFull = targetId == fullId;
            }
            else
            {
                hasFull = pc.elements.Has(fullId);
            }

            if (enabled)
            {
                if (!applied) Apply(pc);
            }
            else
            {
                if (applied) Revert(pc);
            }
        }

        public static void Apply(Chara pc)
        {
            if (pc?.elements == null) return;
            if (IsBonusApplied()) return;

            pc.elements.ModPotential(76, 100);  // MAG potential
            pc.elements.ModPotential(75, 100);  // WIL potential
            pc.elements.ModBase(61, 400);       // mana
            pc.elements.ModBase(65, -15);       // PV
            pc.elements.ModBase(64, -10);       // DV

            SetBonusApplied(true);
            ModLog.Log("Apotheosis feat bonus applied (MAG/WIL potential +100, mana +400, PV -15, DV -10)");
        }

        public static void Revert(Chara pc)
        {
            if (pc?.elements == null) return;
            if (!IsBonusApplied()) return;

            pc.elements.ModPotential(76, -100); // MAG potential
            pc.elements.ModPotential(75, -100); // WIL potential
            pc.elements.ModBase(61, -400);      // mana
            pc.elements.ModBase(65, +15);       // PV
            pc.elements.ModBase(64, +10);       // DV

            SetBonusApplied(false);
            ModLog.Log("Apotheosis feat bonus reverted (MAG/WIL potential -100, mana -400, PV +15, DV +10)");
        }

        public static void ForceRevertLegacy(Chara pc)
        {
            if (pc?.elements == null) return;

            pc.elements.ModPotential(76, -100); // MAG potential
            pc.elements.ModPotential(75, -100); // WIL potential
            pc.elements.ModBase(61, -400);      // mana
            pc.elements.ModBase(65, +15);       // PV
            pc.elements.ModBase(64, +10);       // DV

            SetBonusApplied(false);
            ModLog.Log("Apotheosis legacy bonus force-reverted (migration fallback)");
        }

        private static bool IsBonusApplied()
        {
            return DialogFlagStore.IsTrue(EClass.player?.dialogFlags, BonusAppliedFlag);
        }

        private static bool IsMigrationChecked()
        {
            return DialogFlagStore.IsTrue(EClass.player?.dialogFlags, MigrationCheckedFlag);
        }

        private static bool IsLegacyApotheosisApplied()
        {
            return DialogFlagStore.IsTrue(EClass.player?.dialogFlags, LegacyApotheosisFlag);
        }

        private static bool HasApotheosisFeat(Chara pc)
        {
            return HasAnyApotheosisFeat(pc);
        }

        private static void GetFeatIds(out int fullId, out int liteId)
        {
            if (EClass.sources.elements.alias.TryGetValue(ApotheosisFeatAliasFull, out var fullRow))
                fullId = fullRow.id;
            else
                fullId = ApotheosisFeatIdFull;

            if (EClass.sources.elements.alias.TryGetValue(ApotheosisFeatAliasLite, out var liteRow))
                liteId = liteRow.id;
            else
                liteId = ApotheosisFeatIdLite;
        }

        private static void EnsureLegacyMigrationState(Chara pc)
        {
            if (IsMigrationChecked()) return;
            if (!HasApotheosisFeat(pc)) return;

            // Old saves applied bonuses directly at ritual time and only carried
            // the legacy apotheosis flag. Mark those as already-applied once.
            if (!IsBonusApplied() && IsLegacyApotheosisApplied())
            {
                SetBonusApplied(true);
                ModLog.Log("Apotheosis bonus migration: marked as applied from legacy ritual state.");
            }

            DialogFlagStore.SetBool(EClass.player?.dialogFlags, MigrationCheckedFlag, true);
        }

        private static void SetBonusApplied(bool applied)
        {
            DialogFlagStore.SetBool(EClass.player?.dialogFlags, BonusAppliedFlag, applied);
        }
    }
}
