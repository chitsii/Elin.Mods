using System.Collections.Generic;
using System.Linq;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Removes all Ars Moriendi data from the save for clean mod uninstallation.
    /// Invoked from TraitArsMoriendi context menu.
    /// </summary>
    public static class ModUninstaller
    {
        private const string FlagPrefix = "chitsii.ars.";
        private const string ApotheosisAppliedFlag = "chitsii.ars.quest.state.apotheosis";
        private const string ApotheosisFeatAlias = "featNecroDivinity";
        private const string ApotheosisFeatAliasLite = "featNecroDivinityLite";
        private const int ApotheosisFeatId = 101018;
        private const int ApotheosisFeatLiteId = 101019;
        private const string QuestId = "ars_moriendi";

        private static readonly string[] ModNpcIds =
        {
            "ars_hecatia", "ars_karen", "ars_temple_knight", "ars_temple_scout",
            "ars_erenos_guard", "ars_erenos_shade", "ars_erenos_shadow", "ars_erenos_pet"
        };

        private static readonly string[] ModConditionPrefixes =
        {
            "ConSoulTrapped", "ConCurseWeakness", "ConCurseFrailty", "ConPlagueTouch",
            "ConPreserveCorpse", "ConEmpowerUndead", "ConSoulBind",
            "ConBoneShield", "ConDeathZone", "ConApotheosis"
        };

        private static readonly int[] ModElementIds =
        {
            101001, 101002, 101003, 101004, 101005, 101006, 101007, 101008,
            101009, 101010, 101011, 101012, 101013, 101014, 101015, 101016, 101017,
            101018, 101019
        };

        public struct UninstallResult
        {
            public int ServantsRemoved;
            public int NpcsRemoved;
            public int FlagsRemoved;
            public int QuestsRemoved;
            public int ConditionsRemoved;
            public int ElementsRemoved;
            public bool ApotheosisBonusesReverted;
        }

        public static UninstallResult Execute()
        {
            var result = new UninstallResult();

            // Order matters: servants first (ReleaseServant writes dialogFlags)
            result.ServantsRemoved = RemoveAllServants();
            result.NpcsRemoved = RemoveModNpcs();
            result.QuestsRemoved = RemoveModQuests();
            result.ApotheosisBonusesReverted = RevertApotheosisBonuses();
            result.FlagsRemoved = RemoveDialogFlags();
            result.ConditionsRemoved = RemoveModConditions();
            result.ElementsRemoved = RemoveModElements();
            ResetManagerState();

            ModLog.Log("[Uninstall] Done: servants={0}, npcs={1}, flags={2}, quests={3}, conditions={4}, elements={5}, apotheosisReverted={6}",
                result.ServantsRemoved, result.NpcsRemoved, result.FlagsRemoved,
                result.QuestsRemoved, result.ConditionsRemoved, result.ElementsRemoved,
                result.ApotheosisBonusesReverted);

            return result;
        }

        private static int RemoveAllServants()
        {
            var mgr = NecromancyManager.Instance;
            var all = mgr.GetAllServants();
            int count = 0;

            foreach (var (chara, _) in all)
            {
                mgr.ReleaseServant(chara);
                count++;
            }

            return count;
        }

        private static int RemoveModNpcs()
        {
            int removed = 0;
            var npcIdSet = new HashSet<string>(ModNpcIds);

            // Remove from party first
            if (EClass.pc?.party?.members != null)
            {
                var partyMembers = EClass.pc.party.members.ToList();
                foreach (var member in partyMembers)
                {
                    if (member != null && member != EClass.pc && npcIdSet.Contains(member.id))
                    {
                        EClass.pc.party.RemoveMember(member);
                        if (member.homeBranch != null)
                            member.homeBranch.RemoveMemeber(member);
                        member.Destroy();
                        removed++;
                        ModLog.Log("[Uninstall] Removed party NPC: {0} ({1})", member.Name, member.id);
                    }
                }
            }

            // Remove from current map
            if (EClass._map?.charas != null)
            {
                var mapCharas = EClass._map.charas.ToList();
                foreach (var chara in mapCharas)
                {
                    if (chara != null && npcIdSet.Contains(chara.id))
                    {
                        // Skip if already handled via party removal
                        if (chara.isDestroyed) continue;
                        chara.Destroy();
                        removed++;
                        ModLog.Log("[Uninstall] Removed map NPC: {0} ({1})", chara.Name, chara.id);
                    }
                }
            }

            // Remove from adventurer list
            if (EClass.game?.cards?.listAdv != null)
            {
                int advRemoved = EClass.game.cards.listAdv.RemoveAll(c => c != null && npcIdSet.Contains(c.id));
                if (advRemoved > 0)
                    ModLog.Log("[Uninstall] Removed {0} NPCs from listAdv", advRemoved);
            }

            // Remove from globalCharas
            if (EClass.game?.cards?.globalCharas != null)
            {
                var toRemove = new List<int>();
                foreach (var kvp in EClass.game.cards.globalCharas)
                {
                    if (kvp.Value != null && npcIdSet.Contains(kvp.Value.id))
                        toRemove.Add(kvp.Key);
                }
                foreach (var uid in toRemove)
                {
                    var chara = EClass.game.cards.globalCharas[uid];
                    if (!chara.isDestroyed)
                    {
                        chara.Destroy();
                        removed++;
                        ModLog.Log("[Uninstall] Removed global NPC: {0} ({1})", chara.Name, chara.id);
                    }
                }
            }

            return removed;
        }

        private static int RemoveModQuests()
        {
            var quests = EClass.game?.quests;
            if (quests == null) return 0;

            int removed = 0;

            var toRemoveList = quests.list
                .Where(q => q.id != null && q.id == QuestId)
                .ToList();
            foreach (var q in toRemoveList)
            {
                quests.list.Remove(q);
                removed++;
            }

            var toRemoveGlobal = quests.globalList
                .Where(q => q.id != null && q.id == QuestId)
                .ToList();
            foreach (var q in toRemoveGlobal)
            {
                quests.globalList.Remove(q);
                removed++;
            }

            quests.completedIDs.Remove(QuestId);

            ModLog.Log("[Uninstall] Removed {0} quests", removed);
            return removed;
        }

        private static int RemoveDialogFlags()
        {
            var flags = EClass.player?.dialogFlags;
            if (flags == null) return 0;

            var keysToRemove = flags.Keys
                .Where(k => k.StartsWith(FlagPrefix))
                .ToList();

            foreach (var key in keysToRemove)
                flags.Remove(key);

            ModLog.Log("[Uninstall] Removed {0} dialog flags", keysToRemove.Count);
            return keysToRemove.Count;
        }

        private static bool RevertApotheosisBonuses()
        {
            var pc = EClass.pc;
            var flags = EClass.player?.dialogFlags;
            if (pc?.elements == null) return false;

            bool hasFullFeat = false;
            bool hasLiteFeat = false;
            if (EClass.sources.elements.alias.TryGetValue(ApotheosisFeatAlias, out var featRow))
                hasFullFeat = pc.elements.Has(featRow.id);
            else
                hasFullFeat = pc.elements.Has(ApotheosisFeatId);

            if (EClass.sources.elements.alias.TryGetValue(ApotheosisFeatAliasLite, out var featLiteRow))
                hasLiteFeat = pc.elements.Has(featLiteRow.id);
            else
                hasLiteFeat = pc.elements.Has(ApotheosisFeatLiteId);

            bool hasAnyApotheosisFeat = hasFullFeat || hasLiteFeat;

            // Legacy fallback for pre-feat saves (old condition-based apotheosis).
            bool hasLegacyFlag = flags != null
                && flags.TryGetValue(ApotheosisAppliedFlag, out int applied)
                && applied == 1;
            bool hasAppliedBonus = ApotheosisFeatBonus.HasAppliedBonus();

            if (!hasAnyApotheosisFeat && !hasLegacyFlag && !hasAppliedBonus) return false;

            // Reverse apotheosis bonuses (feat-driven in current versions,
            // ritual-driven in legacy saves).
            if (hasAppliedBonus)
            {
                ApotheosisFeatBonus.Revert(pc);
            }
            else if (hasLegacyFlag && hasFullFeat)
            {
                // Migration fallback: legacy save may have direct ritual bonuses
                // without the new "bonus applied" flag.
                ApotheosisFeatBonus.ForceRevertLegacy(pc);
            }
            else
            {
                return false;
            }

            ModLog.Log("[Uninstall] Reverted apotheosis bonuses (MAG/WIL potential -100, mana -400, PV +15, DV +10)");
            return true;
        }

        private static int RemoveModConditions()
        {
            int removed = 0;

            removed += RemoveConditionsFromChara(EClass.pc);

            if (EClass.pc?.party?.members != null)
            {
                foreach (var member in EClass.pc.party.members)
                {
                    if (member != null && member != EClass.pc)
                        removed += RemoveConditionsFromChara(member);
                }
            }

            ModLog.Log("[Uninstall] Removed {0} conditions", removed);
            return removed;
        }

        private static int RemoveConditionsFromChara(Chara chara)
        {
            if (chara?.conditions == null) return 0;

            int removed = 0;
            var toRemove = chara.conditions
                .Where(c => c != null && IsModCondition(c.GetType().Name))
                .ToList();

            foreach (var condition in toRemove)
            {
                condition.Kill(silent: true);
                removed++;
            }

            return removed;
        }

        private static bool IsModCondition(string typeName)
        {
            foreach (var name in ModConditionPrefixes)
            {
                if (typeName == name) return true;
            }
            return false;
        }

        private static int RemoveModElements()
        {
            if (EClass.pc?.elements == null) return 0;

            int removed = 0;
            foreach (var id in ModElementIds)
            {
                if (EClass.pc.elements.Has(id))
                {
                    EClass.pc.elements.Remove(id);
                    removed++;
                }
            }

            ModLog.Log("[Uninstall] Removed {0} elements from PC", removed);
            return removed;
        }

        private static void ResetManagerState()
        {
            NecromancyManager.Instance.ResetState();
            ModLog.Log("[Uninstall] Manager state reset");
        }
    }
}
