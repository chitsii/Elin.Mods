using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Safety net: prevent save-load hard failure when trait creation breaks for this mod's cards.
    /// Logs full card context to help diagnose upstream compatibility issues.
    /// </summary>
    [HarmonyPatch(typeof(Card), nameof(Card.ApplyTrait))]
    public static class Patch_Card_ApplyTrait_ArsSafetyNet
    {
        static Exception? Finalizer(Card __instance, Exception __exception)
        {
            if (__exception == null) return null;

            try
            {
                var id = __instance?.id ?? "(null)";
                var traitId = __instance?.c_idTrait ?? "(null)";
                bool isArsCard = id.StartsWith("ars_");
                bool isArsTrait = traitId.IndexOf("ArsMoriendi", StringComparison.Ordinal) >= 0
                    || traitId.IndexOf("NecroMerchant", StringComparison.Ordinal) >= 0
                    || traitId.IndexOf("UndeadServant", StringComparison.Ordinal) >= 0
                    || traitId.IndexOf("NecroSpellbook", StringComparison.Ordinal) >= 0
                    || traitId.IndexOf("ArsMoriendi", StringComparison.Ordinal) >= 0;

                if (isArsCard || isArsTrait)
                {
                    ModLog.Error($"ApplyTrait failed for Ars card. id={id}, uid={__instance?.uid ?? -1}, traitId={traitId}, name={__instance?.Name ?? "(null)"}, ex={__exception}");
                    Plugin.ReportPatchRuntimeFailure(nameof(Patch_Card_ApplyTrait_ArsSafetyNet));
                    // Keep load running for Ars cards; they can be repaired in-game.
                    return null;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ApplyTrait safety finalizer failed: {ex.Message}");
            }

            return __exception;
        }
    }

    /// <summary>
    /// Harmony patch: Drop soul item from Card.SpawnLoot when a character dies while affected by ConSoulTrapped.
    /// Also stamps corpses with the creature's actual LV for resurrection level calculation.
    /// </summary>
    [HarmonyPatch(typeof(Card), nameof(Card.SpawnLoot), typeof(Card))]
    public static class Patch_Chara_Die_SoulDrop
    {
        private readonly struct DeathCorpseStamp
        {
            public readonly int DeathLv;
            public readonly int MainElementId;

            public DeathCorpseStamp(int deathLv, int mainElementId)
            {
                DeathLv = deathLv;
                MainElementId = mainElementId;
            }
        }

        // Cache corpse metadata before Die() runs (in case death logic modifies the chara).
        private static readonly Dictionary<int, DeathCorpseStamp> _deathStampCache = new();

        static void Prefix(Card __instance)
        {
            try
            {
                if (!__instance.isChara)
                    return;

                var deadChara = __instance.Chara;
                if (deadChara == null)
                    return;

                // Visual cleanup for servant aura on death (integrated into existing Die patch).
                var mgr = NecromancyManager.Instance;
                if (mgr.IsServant(deadChara.uid))
                    NecromancyManager.StopServantAuraFx(deadChara);

                // Preserve Corpse caster aura cleanup on death (loop FX is non-autodestroy).
                ConPreserveCorpse.StopAura(deadChara);

                // Cache metadata for corpse stamping in Postfix
                if (!deadChara.IsPC)
                {
                    _deathStampCache[deadChara.uid] = new DeathCorpseStamp(
                        deathLv: Math.Max(1, deadChara.LV),
                        mainElementId: NecromancyManager.GetMainElementId(deadChara));
                }

                // Soul drop logic
                if (deadChara.IsPC) return;
                if (!deadChara.IsAliveInCurrentZone) return;
                if (!deadChara.HasCondition<ConSoulTrapped>()) return;

                var soulId = GetSoulIdByLevel(deadChara.LV);
                var soul = ThingGen.Create(soulId);
                if (soul != null)
                {
                    EClass._zone.AddCard(soul, deadChara.pos);
                    ModLog.Log("SoulDrop: dropped {0} from {1} (Lv{2})",
                        soulId, deadChara.Name, deadChara.LV);

                    // Trigger Stage 1 on first soul drop
                    NecromancyManager.Instance.QuestPath.TryAdvanceOnFirstSoulDrop();

                    // Grimoire prompt: strong/legendary soul → bubble hint
                    if (soulId == "ars_soul_strong" || soulId == "ars_soul_legendary")
                    {
                        var quest = NecromancyManager.Instance.QuestPath;
                        if (quest.IsStarted && !quest.IsComplete)
                            TomePrompt.ShowBubble("tomeChanged");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Warn($"SoulDrop patch error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_Chara_Die_SoulDrop));
            }
        }

        static void Postfix(Card __instance)
        {
            try
            {
                if (!__instance.isChara)
                    return;

                var deadChara = __instance.Chara;
                if (deadChara == null)
                    return;

                if (!_deathStampCache.TryGetValue(deadChara.uid, out var stamp))
                    return;
                _deathStampCache.Remove(deadChara.uid);

                // Find the corpse dropped at death position and stamp it with actual LV
                var pos = deadChara.pos;
                if (pos == null) return;
                var things = pos.Things;
                if (things == null) return;

                // Stamp the most recently added matching corpse first.
                // Reverse loop avoids iterator-dispose IL that CWL may classify as incompatible.
                Thing? targetCorpse = null;
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    var thing = things[i];
                    if (thing.trait is TraitFoodMeat
                        && thing.c_idRefCard == deadChara.id
                        && thing.GetInt(NecromancyManager.CorpseLvIntId) == 0)
                    {
                        targetCorpse = thing;
                        break;
                    }
                }

                if (targetCorpse != null)
                {
                    targetCorpse.SetInt(NecromancyManager.CorpseLvIntId, stamp.DeathLv);
                    if (stamp.MainElementId > 0)
                        targetCorpse.SetInt(NecromancyManager.CorpseMainElementIntId, stamp.MainElementId);
                    ModLog.Log("CorpseMeta: stamped Lv{0}, Ele{1} on corpse of {2}",
                        stamp.DeathLv, stamp.MainElementId, deadChara.Name);
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Warn($"CorpseLv stamp error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_Chara_Die_SoulDrop));
            }
        }

        private static string GetSoulIdByLevel(int level)
        {
            if (level >= 100) return "ars_soul_legendary";
            if (level >= 50) return "ars_soul_strong";
            if (level >= 20) return "ars_soul_normal";
            return "ars_soul_weak";
        }
    }

    /// <summary>
    /// Harmony patch: Apotheosis passive ("Soul Harvest").
    /// If the PC has featNecroDivinity, kills on souled enemies have a chance to
    /// generate one additional soul item based on victim level.
    /// Legacy fallback: ConApotheosis is still accepted for old saves.
    /// </summary>
    [HarmonyPatch(typeof(Chara), nameof(Chara.Die))]
    [HarmonyPriority(Priority.VeryLow)]
    public static class Patch_Chara_Die_ApotheosisSoulHarvest
    {
        private const int HarvestChancePercent = 25;

        static void Postfix(Chara __instance, Card origin)
        {
            try
            {
                if (__instance.IsPC) return;
                if (origin == null || !origin.IsPC) return;
                if (!NecroSpellUtil.HasSoul(__instance)) return;

                var pc = EClass.pc;
                if (pc == null || !HasApotheosisSoulHarvest(pc)) return;
                if (EClass.rnd(100) >= HarvestChancePercent) return;

                string soulId = GetSoulIdByLevel(__instance.LV);
                var soul = ThingGen.Create(soulId);
                if (soul == null) return;

                soul.SetNum(1);
                var dropPos = __instance.pos ?? pc.pos;
                EClass._zone.AddCard(soul, dropPos);

                LangHelper.Say("apotheosisSoulHarvest", pc);
                ModLog.Log("Apotheosis Soul Harvest: dropped {0} from {1} (Lv{2})",
                    soulId, __instance.Name, __instance.LV);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Apotheosis Soul Harvest patch error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_Chara_Die_ApotheosisSoulHarvest));
            }
        }

        private static string GetSoulIdByLevel(int level)
        {
            if (level >= 100) return "ars_soul_legendary";
            if (level >= 50) return "ars_soul_strong";
            if (level >= 20) return "ars_soul_normal";
            return "ars_soul_weak";
        }

        private static bool HasApotheosisSoulHarvest(Chara pc)
        {
            if (pc.HasCondition<ConApotheosis>())
                return true;
            return ApotheosisFeatBonus.HasAnyApotheosisFeat(pc);
        }
    }

    /// <summary>
    /// Remove memory-only Ars temporary servant tracking as soon as the summon actually ends.
    /// Permanent servants remain tracked for corpse/revival flows.
    /// </summary>
    [HarmonyPatch(typeof(Chara), nameof(Chara.Die))]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_Chara_Die_TemporaryServantCleanup
    {
        static void Postfix(Chara __instance)
        {
            try
            {
                NecromancyManager.Instance.UntrackEndedTemporaryServant(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Temporary servant cleanup error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_Chara_Die_TemporaryServantCleanup));
            }
        }
    }

    /// <summary>
    /// Harmony patch: servants are excluded from butcher-knife targeting.
    /// Replaces the target scan so vanilla slaughter never offers servant targets.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_TraitToolButcher_TrySetHeldAct_BlockServant
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(TraitToolButcher), nameof(TraitToolButcher.TrySetHeldAct), new[] { typeof(ActPlan) });
        }

        static bool Prefix(ActPlan p)
        {
            try
            {
                if (p?.pos?.Charas == null)
                    return false;

                var mgr = NecromancyManager.Instance;
                foreach (Chara chara in p.pos.Charas)
                {
                    bool canButcher = mgr.CanOfferButcherAction(chara);
                    if (!canButcher)
                        continue;

                    Chara target = chara;
                    p.TrySetAct("AI_Slaughter", delegate
                    {
                        Dialog.TryWarnSlaughter(delegate
                        {
                            EClass.pc.SetAIImmediate(new AI_Slaughter
                            {
                                target = target
                            });
                        }, target);
                        return false;
                    }, target);
                }

                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"TraitToolButcher servant block patch error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_TraitToolButcher_TrySetHeldAct_BlockServant));
                return true;
            }
        }
    }

    /// <summary>
    /// Harmony patch: direct AI_Slaughter execution is also blocked for servants.
    /// This covers manual AI injection and other non-tool entry points.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_AI_Slaughter_Perform_BlockServant
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(AI_Slaughter), nameof(AI_Slaughter.Perform), Type.EmptyTypes);
        }

        static bool Prefix(AI_Slaughter __instance, ref bool __result)
        {
            try
            {
                var target = __instance.target?.Chara != null ? __instance.target : Act.TC;
                if (target?.Chara == null)
                    return true;

                if (!NecromancyManager.Instance.IsSlaughterProtectedServant(target.Chara))
                    return true;

                LangHelper.Say("servantButcherBlocked");
                __result = false;
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"AI_Slaughter servant block patch error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_AI_Slaughter_Perform_BlockServant));
                return true;
            }
        }
    }

    /// <summary>
    /// Harmony patch: Block permanent ritual servants from joining the party.
    /// Catches all paths: dialogue "invite", GetRevived, nurse revival, hitchingpost, etc.
    /// Servants use listSummon (c_uidMaster) for zone travel instead of party membership.
    /// </summary>
    [HarmonyPatch(typeof(Party), nameof(Party.AddMemeber))]
    public static class Patch_Party_AddMemeber_BlockServant
    {
        static bool Prefix(Chara c)
        {
            try
            {
                var mgr = NecromancyManager.Instance;
                if (mgr.IsServant(c.uid) && !c.isSummon)
                {
                    ModLog.Log("Blocked servant from joining party: {0} (uid={1})",
                        c.Name, c.uid);
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Warn($"AddMemeber block patch error: {ex.Message}");
            }

            return true;
        }
    }

    /// <summary>
    /// Safety net: Restore c_uidMaster if something clears it during revival.
    /// With the correct creation order (AddMemeber → MakeMinion), c_uidMaster should
    /// persist through death/revival. If this patch fires, it indicates an unexpected
    /// code path that needs investigation.
    /// </summary>
    [HarmonyPatch(typeof(Chara), nameof(Chara.Revive))]
    public static class Patch_Chara_Revive_RestoreMaster
    {
        static void Postfix(Chara __instance)
        {
            try
            {
                var mgr = NecromancyManager.Instance;
                if (!mgr.IsServant(__instance.uid) || __instance.isSummon) return;

                if (EClass.pc != null && __instance.c_uidMaster != EClass.pc.uid)
                {
                    __instance.c_uidMaster = EClass.pc.uid;
                    ModLog.Warn($"Safety net fired: restored servant master link after revive: {__instance.Name} (uid={__instance.uid}). This may indicate a design issue.");
                }

                mgr.ApplyRuntimeStateForServant(__instance);
                NecromancyManager.TryEnsureServantAura(__instance);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn($"Revive restore master error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Harmony patch: Revive servants in place instead of sending them home.
    /// GetRevived() calls MoveZone(homeZone) because CanJoinParty is false for servants.
    /// This postfix detects when a servant was sent to a different zone and pulls them
    /// back to the PC's current zone near the PC.
    /// </summary>
    [HarmonyPatch(typeof(Chara), nameof(Chara.GetRevived))]
    public static class Patch_Chara_GetRevived_ServantInPlace
    {
        static void Postfix(Chara __instance)
        {
            try
            {
                var mgr = NecromancyManager.Instance;
                if (mgr == null || !mgr.IsServant(__instance.uid)) return;
                if (__instance.isSummon) return;

                bool isStashed = mgr.IsServantStashed(__instance.uid);
                // GetRevived sent the servant to homeZone — pull active servants back to PC's zone.
                if (!isStashed && __instance.currentZone != null && __instance.currentZone != EClass._zone)
                {
                    __instance.currentZone.RemoveCard(__instance);
                    var point = EClass.pc.pos.GetNearestPointCompat(
                        allowBlock: false, allowChara: false);
                    EClass._zone.AddCard(__instance, point);
                    ModLog.Log("GetRevived: pulled servant {0} back to PC zone", __instance.Name);
                }

                mgr.ApplyRuntimeStateForServant(__instance);
                NecromancyManager.TryEnsureServantAura(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"GetRevived servant in-place patch error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Harmony patch: Exclude permanent ritual servants from the minion count.
    /// Vanilla CountMinions counts all c_uidMaster + MinionType.Default chars.
    /// Without this, ritual servants would consume summon slots (MaxSummon limit).
    /// </summary>
    [HarmonyPatch(typeof(Zone), nameof(Zone.CountMinions))]
    public static class Patch_Zone_CountMinions_ExcludeServants
    {
        static void Postfix(Chara c, ref int __result)
        {
            if (c != EClass.pc) return;

            try
            {
                var mgr = NecromancyManager.Instance;
                var map = EClass._map;
                if (mgr == null || map?.charas == null) return;

                int excluded = 0;
                var charas = map.charas;
                for (int i = 0; i < charas.Count; i++)
                {
                    var chara = charas[i];
                    if (chara == null) continue;
                    if (mgr.IsServant(chara.uid)
                        && !chara.isSummon
                        && chara.c_uidMaster == c.uid
                        && chara.c_minionType == MinionType.Default)
                    {
                        excluded++;
                    }
                }

                if (excluded > 0)
                    __result = Math.Max(0, __result - excluded);
            }
            catch (Exception ex)
            {
                ModLog.Error($"CountMinions servant exclusion patch error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Harmony patch: Preserve Corpse guaranteed corpse drop.
    /// When the attacker (origin) has ConPreserveCorpse, check after SpawnLoot() whether
    /// a corpse was dropped. If not, spawn one manually. This avoids modifying
    /// shared global SourceRace data, which would conflict with other mods.
    /// </summary>
    [HarmonyPatch(typeof(Card), nameof(Card.SpawnLoot), typeof(Card))]
    [HarmonyPriority(Priority.Low)]
    public static class Patch_Chara_Die_PreserveCorpse
    {
        private readonly struct PreserveCorpseState
        {
            public readonly bool Active;
            public readonly int CountBefore;
            public readonly int DeathLv;
            public readonly int MainElementId;

            public PreserveCorpseState(bool active, int countBefore, int deathLv, int mainElementId)
            {
                Active = active;
                CountBefore = countBefore;
                DeathLv = deathLv;
                MainElementId = mainElementId;
            }
        }

        static void Prefix(Card __instance, out PreserveCorpseState __state)
        {
            __state = default;
            try
            {
                if (!__instance.isChara) return;
                var deadChara = __instance.Chara;
                if (deadChara == null) return;
                if (deadChara.IsPC) return;
                if (EClass.pc == null || !EClass.pc.HasCondition<ConPreserveCorpse>()) return;

                int countBefore = CountMatchingCorpses(deadChara.pos, deadChara.id);
                __state = new PreserveCorpseState(
                    active: true,
                    countBefore: countBefore,
                    deathLv: Math.Max(1, deadChara.LV),
                    mainElementId: NecromancyManager.GetMainElementId(deadChara));
            }
            catch (Exception ex)
            {
                ModLog.Warn($"PreserveCorpse patch prefix error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_Chara_Die_PreserveCorpse));
            }
        }

        static void Postfix(Card __instance, Card origin, PreserveCorpseState __state)
        {
            if (!__state.Active) return;

            try
            {
                if (!__instance.isChara) return;
                var deadChara = __instance.Chara;
                if (deadChara == null) return;

                // Only trigger for kills by PC or PC faction minions
                bool isPcKill = origin != null
                    && (origin.IsPC
                        || (origin is Chara originChara && originChara.IsPCFactionOrMinion));
                if (!isPcKill) return;

                // Count corpses after death to see if a new one was dropped
                var pos = deadChara.pos;
                if (pos == null) return;

                int countAfter = CountMatchingCorpses(pos, deadChara.id);

                if (countAfter <= __state.CountBefore)
                {
                    // Use vanilla's race.corpse to determine what to drop
                    var race = deadChara.race;
                    if (race.corpse == null || race.corpse.Length == 0) return;
                    string corpseId = race.corpse[0];

                    // Only force drop for meat-origin items (skip robots, etc.)
                    var corpseSource = EClass.sources.cards.map.TryGetValue(corpseId);
                    if (corpseSource == null || corpseSource._origin != "meat") return;

                    Thing corpse = ThingGen.Create(corpseId);
                    if (corpse == null) return;

                    corpse.MakeFoodFrom(__instance);
                    corpse.SetInt(NecromancyManager.CorpseLvIntId, __state.DeathLv);
                    if (__state.MainElementId > 0)
                        corpse.SetInt(NecromancyManager.CorpseMainElementIntId, __state.MainElementId);
                    EClass._zone.AddCard(corpse, pos);
                    ModLog.Log("PreserveCorpse: spawned {0} for {1} (Lv{2}, Ele{3})",
                        corpseId, deadChara.Name, __state.DeathLv, __state.MainElementId);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"PreserveCorpse patch postfix error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_Chara_Die_PreserveCorpse));
            }
        }

        private static int CountMatchingCorpses(Point pos, string sourceId)
        {
            if (pos?.Things == null) return 0;

            int count = 0;
            var things = pos.Things;
            for (int i = 0; i < things.Count; i++)
            {
                var thing = things[i];
                if (thing.trait is TraitFoodMeat && thing.c_idRefCard == sourceId)
                    count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Harmony patch: Soul Bind death substitution.
    /// Prefix records whether PC has ConSoulBind (Die() may clear conditions).
    /// Die() runs normally so other Mod patches are not skipped.
    /// Postfix revives PC via preventDeathPenalty + Revive() and sacrifices a servant.
    /// </summary>
    [HarmonyPatch(typeof(Chara), nameof(Chara.Die))]
    [HarmonyPriority(Priority.VeryLow)]
    public static class Patch_Chara_Die_SoulBind
    {
        private static bool _isSacrificing;

        static void Prefix(Chara __instance, out bool __state)
        {
            __state = false;
            if (_isSacrificing) return;

            try
            {
                if (!__instance.IsPC) return;
                if (__instance.GetCondition<ConSoulBind>() == null) return;

                var servants = NecromancyManager.Instance.GetCombatServantsInCurrentZone();
                if (servants.Count == 0) return;

                __state = true;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"SoulBind Die prefix error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_Chara_Die_SoulBind));
                __state = false;
            }
        }

        static void Postfix(Chara __instance, bool __state)
        {
            if (!__state) return;

            try
            {
                var mgr = NecromancyManager.Instance;
                var servants = mgr.GetCombatServantsInCurrentZone();
                if (servants.Count == 0) return;

                var sacrifice = SelectSoulBindSacrifice(servants);

                LangHelper.Say("soulBindTrigger", __instance, sacrifice);

                // バニラの遺言状と同じ仕組み: 死亡ペナルティ無効化 + 復活
                EClass.player.preventDeathPenalty = true;
                __instance.Revive(null, false);

                // HP復元 + 2ターン無敵 + 沈黙解除
                __instance.hp = __instance.MaxHP / 3;
                __instance.AddCondition<ConInvulnerable>(2000, force: true);
                __instance.RemoveCondition<ConSilence>();
                __instance.PlayEffect("revive");
                __instance.PlaySound("revive");

                // 魂の鎖を消費 (一回限り)
                // IMPORTANT POLICY:
                // Keep spell-effect state changes here (condition/flags), but do NOT directly stop Soul Bind VFX.
                // Visual cleanup is lease-driven in CustomAssetFx and must follow condition renewal state.
                __instance.RemoveCondition<ConSoulBind>();
                mgr.ClearSoulBindSacrificeUid();

                // 従者を犠牲にする（追跡は維持して死体/蘇生対象に残す）
                _isSacrificing = true;
                try
                {
                    if (!NecromancyManager.SafeDie(sacrifice))
                        Plugin.ReportPatchRuntimeFailure(nameof(Patch_Chara_Die_SoulBind));
                }
                finally { _isSacrificing = false; }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"SoulBind Die postfix error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_Chara_Die_SoulBind));
            }
        }

        private static Chara SelectSoulBindSacrifice(List<Chara> servants)
        {
            var best = servants[0];
            int bestLv = best.LV;
            int bestUid = best.uid;

            for (int i = 1; i < servants.Count; i++)
            {
                var candidate = servants[i];
                int candidateLv = candidate.LV;
                int candidateUid = candidate.uid;

                if (candidateLv < bestLv
                    || (candidateLv == bestLv && candidateUid < bestUid))
                {
                    best = candidate;
                    bestLv = candidateLv;
                    bestUid = candidateUid;
                }
            }

            return best;
        }
    }

    /// <summary>
    /// Harmony patch: run VFX lease maintenance at the end of each player turn.
    /// Hook point is BaseGameScreen.OnEndPlayerTurn(), invoked from Chara.Tick() for PC.
    /// </summary>
    [HarmonyPatch(typeof(BaseGameScreen), nameof(BaseGameScreen.OnEndPlayerTurn))]
    public static class Patch_BaseGameScreen_OnEndPlayerTurn_VfxLeaseMaintenance
    {
        static void Postfix()
        {
            try
            {
                CustomAssetFx.RunLeaseMaintenance();
            }
            catch (Exception ex)
            {
                ModLog.Warn($"VFX lease maintenance error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Harmony patch: Spawn Temple Knights when entering a zone during Stage 2.
    /// </summary>
    [HarmonyPatch(typeof(Zone), nameof(Zone.Activate))]
    public static class Patch_Zone_Activate_KnightEncounter
    {
        static void Postfix()
        {
            try
            {
                CustomAssetFx.EnsureLegacyLoopMigrationOnce();

                // Keep servant runtime reconciliation isolated so quest/spawn errors never skip it.
                NecromancyManager.Instance.ReconcileServantRuntimeStates();
                NecromancyManager.Instance.RefreshServantVisualStateCurrentZone();
            }
            catch (Exception ex)
            {
                ModLog.Error($"Zone.Activate servant runtime reconcile error: {ex.Message}");
            }

            try
            {
                KnightEncounter.TrySpawnKnights();
                KnightEncounter.TryRespawnErenos();
                KnightEncounter.TryRespawnScouts();
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Zone.Activate knight spawn error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Harmony patch: Detect quest-relevant NPC deaths (Karen, Erenos, Scouts).
    /// </summary>
    [HarmonyPatch(typeof(ZoneEventManager), nameof(ZoneEventManager.OnCharaDie), typeof(Chara))]
    [HarmonyPriority(Priority.VeryLow)]
    public static class Patch_Chara_Die_QuestNPC
    {
        static void Postfix(Chara __0)
        {
            try
            {
                var deadChara = __0;
                if (deadChara == null)
                {
                    return;
                }

                KnightEncounter.CheckKarenDefeated(deadChara);
                KnightEncounter.CheckKnightsCleared(deadChara);
                KnightEncounter.CheckErenosDefeated(deadChara);
                KnightEncounter.CheckErenosMinionsCleared(deadChara);
                KnightEncounter.CheckScoutsDefeated(deadChara);

                // Grimoire prompt: bubble on first servant kill (one-time)
                if (!deadChara.IsPC && !deadChara.IsPCFactionOrMinion)
                {
                    var quest = NecromancyManager.Instance.QuestPath;
                    if (quest.IsStarted && !quest.IsComplete
                        && NecromancyManager.Instance.ServantCount > 0)
                    {
                        const string key = "chitsii.ars.prompt.servant_kill";
                        if (EClass.player?.dialogFlags != null
                            && !EClass.player.dialogFlags.ContainsKey(key))
                        {
                            EClass.player.dialogFlags[key] = 1;
                            TomePrompt.ShowBubble("tomeReacting");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"QuestNPC Die patch error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_Chara_Die_QuestNPC));
            }
        }
    }

    // ================================================================
    // Servant Runtime/Tactics System
    // ================================================================

    /// <summary>
    /// Harmony patch: Override Tactics source for servants with custom tactic assignment.
    /// Patches the Tactics(Chara) constructor to replace the source row after
    /// vanilla logic selects a default.
    /// </summary>
    [HarmonyPatch(typeof(Tactics), MethodType.Constructor, typeof(Chara))]
    public static class Patch_Tactics_Ctor_ServantOverride
    {
        static void Postfix(Tactics __instance, Chara c)
        {
            try
            {
                var mgr = NecromancyManager.Instance;
                if (!mgr.IsServant(c.uid)) return;

                var enh = mgr.GetEnhancement(c.uid);
                if (string.IsNullOrEmpty(enh.TacticId)) return;

                if (EClass.sources.tactics.map.TryGetValue(enh.TacticId, out var row))
                    __instance.source = row;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Tactics ctor servant override error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Harmony patch: Block only runtime-frozen servants from entering combat AI.
    /// </summary>
    [HarmonyPatch(typeof(Chara), nameof(Chara.SetAIAggro))]
    public static class Patch_Chara_SetAIAggro_ServantRuntimeFreeze
    {
        static bool Prefix(Chara __instance)
        {
            try
            {
                var mgr = NecromancyManager.Instance;
                if (!mgr.IsServant(__instance.uid)) return true;
                return !mgr.ShouldFreezeServantAI(__instance.uid);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"SetAIAggro servant runtime freeze error: {ex.Message}");
                return true;
            }
        }
    }

    /// <summary>
    /// Harmony patch: Force only runtime-frozen servants into NoGoal when choosing new goals.
    /// </summary>
    [HarmonyPatch(typeof(Chara), nameof(Chara.ChooseNewGoal))]
    public static class Patch_Chara_ChooseNewGoal_ServantRuntimeFreeze
    {
        static bool Prefix(Chara __instance)
        {
            try
            {
                var mgr = NecromancyManager.Instance;
                if (!mgr.IsServant(__instance.uid)) return true;

                if (mgr.ShouldFreezeServantAI(__instance.uid))
                {
                    __instance.SetAI(new NoGoal());
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ChooseNewGoal servant runtime freeze error: {ex.Message}");
                return true;
            }
        }
    }

    /// <summary>
    /// Harmony patch: companion quest progress should count vanilla home members, not Ars servants.
    /// </summary>
    [HarmonyPatch(typeof(QuestCompanion), nameof(QuestCompanion.CanUpdateOnTalk))]
    public static class Patch_QuestCompanion_CanUpdateOnTalk_ExcludeServants
    {
        static bool Prefix(QuestCompanion __instance, ref bool __result)
        {
            try
            {
                int count = NecromancyManager.Instance.CountHomeBranchMembersExcludingServants();
                __result = __instance.phase switch
                {
                    0 => count >= 3,
                    1 => count >= 11,
                    _ => false,
                };
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"QuestCompanion CanUpdateOnTalk patch error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_QuestCompanion_CanUpdateOnTalk_ExcludeServants));
                return true;
            }
        }
    }

    /// <summary>
    /// Harmony patch: companion quest progress text should ignore Ars servants.
    /// </summary>
    [HarmonyPatch(typeof(QuestCompanion), nameof(QuestCompanion.GetTextProgress))]
    public static class Patch_QuestCompanion_GetTextProgress_ExcludeServants
    {
        static bool Prefix(QuestCompanion __instance, ref string __result)
        {
            try
            {
                if (__instance.phase != 1)
                {
                    __result = string.Empty;
                    return false;
                }

                int count = NecromancyManager.Instance.CountHomeBranchMembersExcludingServants();
                __result = "progressRecruit".lang((count - 1).ToString() ?? "", 10.ToString() ?? "");
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"QuestCompanion GetTextProgress patch error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_QuestCompanion_GetTextProgress_ExcludeServants));
                return true;
            }
        }
    }

    /// <summary>
    /// Harmony patch: servants remain visible on the resident board for work inspection,
    /// but cannot be managed from that UI.
    /// </summary>
    [HarmonyPatch(typeof(BaseListPeople), nameof(BaseListPeople.OnClick))]
    public static class Patch_BaseListPeople_OnClick_BlockServantResidentBoardActions
    {
        static bool Prefix(BaseListPeople __instance, Chara c)
        {
            try
            {
                if (!ResidentBoardServantUiHelpers.ShouldTreatAsReadOnlyResidentBoardEntry(__instance, c))
                    return true;

                SE.BeepSmall();
                Msg.SayRaw(
                    Lang.isJP
                        ? "従者は住民掲示板では管理できません。退避・招集などは従者UIを使ってください。"
                        : (Lang.langCode == "CN"
                            ? "仆从不能在居民面板中管理。退避、招集等操作请使用仆从UI。"
                            : "Servants cannot be managed from the resident board. Use the servant UI for stash, recall, and other actions."));
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Resident board servant click block error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_BaseListPeople_OnClick_BlockServantResidentBoardActions));
                return true;
            }
        }
    }

    /// <summary>
    /// Harmony patch: show servant runtime state on resident-board rows.
    /// </summary>
    [HarmonyPatch(typeof(BaseListPeople), nameof(BaseListPeople.OnInstantiate))]
    public static class Patch_BaseListPeople_OnInstantiate_ShowServantResidentBoardState
    {
        static void Postfix(BaseListPeople __instance, Chara a, ItemGeneral b)
        {
            try
            {
                if (__instance.GetType() == typeof(ListPeople))
                {
                    ResidentBoardServantUiHelpers.GetOrCreateResidentBoardStateLabel(b)?.SetActive(enable: false);
                }

                if (!ResidentBoardServantUiHelpers.ShouldTreatAsReadOnlyResidentBoardEntry(__instance, a))
                    return;

                var (text, color) = ResidentBoardServantUiHelpers.GetResidentBoardServantState(a);
                var label = ResidentBoardServantUiHelpers.GetOrCreateResidentBoardStateLabel(b);
                if (label != null && !string.IsNullOrEmpty(text))
                {
                    label.SetActive(enable: true);
                    label.alignment = TextAnchor.MiddleRight;
                    label.SetText(text, color);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Resident board servant state label error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_BaseListPeople_OnInstantiate_ShowServantResidentBoardState));
            }
        }
    }

    /// <summary>
    /// Harmony patch: append servant-specific runtime notes to the work tooltip.
    /// </summary>
    [HarmonyPatch(typeof(BaseListPeople), nameof(BaseListPeople.WriteHobbies))]
    public static class Patch_BaseListPeople_WriteHobbies_ShowServantResidentBoardState
    {
        static void Postfix(BaseListPeople __instance, UITooltip t, Chara a, BaseArea roomWork)
        {
            try
            {
                if (!ResidentBoardServantUiHelpers.ShouldTreatAsReadOnlyResidentBoardEntry(__instance, a) || t?.note == null)
                    return;

                t.note.Space(8);
                var (statusText, statusColor) = ResidentBoardServantUiHelpers.GetResidentBoardServantState(a);
                t.note.AddText(
                    Lang.isJP
                        ? "Ars 従者"
                        : (Lang.langCode == "CN" ? "Ars 仆从" : "Ars servant"),
                    FontColor.Good);
                t.note.AddText(
                    (Lang.isJP
                        ? "状態: "
                        : (Lang.langCode == "CN" ? "状态: " : "State: ")) + statusText,
                    statusColor);

                if (!a.IsInHomeZone())
                {
                    t.note.AddText(
                        Lang.isJP
                            ? "ホーム外にいるため、住民としての仕事は行いません。"
                            : (Lang.langCode == "CN"
                                ? "当前不在家园区域，因此不会执行居民工作。"
                                : "This servant is outside the home zone, so resident work is inactive."),
                        FontColor.Warning);
                }
                else if (NecromancyManager.Instance.IsServantStashed(a.uid) && !ModConfig.EnableStashedServantHomeContribution.Value)
                {
                    t.note.AddText(
                        Lang.isJP
                            ? "退避中の拠点貢献が無効なため、現在は仕事しません。"
                            : (Lang.langCode == "CN"
                                ? "由于“退避中也参与据点贡献”已关闭，当前不会工作。"
                                : "Stashed home contribution is disabled, so this servant is not working right now."),
                        FontColor.Warning);
                }

                t.note.Build();
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Resident board servant tooltip error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_BaseListPeople_WriteHobbies_ShowServantResidentBoardState));
            }
        }
    }

    internal static class ResidentBoardServantUiHelpers
    {
        private const string ResidentBoardStateLabelObjectName = "ArsResidentBoardServantState";

        internal static bool ShouldTreatAsReadOnlyResidentBoardEntry(BaseListPeople? list, Chara? chara)
        {
            if (list == null || list.GetType() != typeof(ListPeople) || chara == null)
                return false;

            if (list.memberType != FactionMemberType.Default && list.memberType != FactionMemberType.Livestock)
                return false;

            var mgr = NecromancyManager.Instance;
            return mgr != null && mgr.IsServant(chara.uid);
        }

        internal static (string Text, FontColor Color) GetResidentBoardServantState(Chara chara)
        {
            if (chara.isDead)
            {
                return (
                    Lang.isJP ? "従者(死亡)" : (Lang.langCode == "CN" ? "仆从(死亡)" : "Servant (Dead)"),
                    FontColor.Bad);
            }

            var mgr = NecromancyManager.Instance;
            if (mgr == null)
                return (Lang.isJP ? "従者(戦闘)" : (Lang.langCode == "CN" ? "仆从(战斗)" : "Servant (Combat)"), FontColor.Good);

            if (mgr.IsServantStashed(chara.uid))
            {
                if (ModConfig.EnableStashedServantHomeContribution.Value)
                {
                    return (
                        Lang.isJP ? "従者(労働)" : (Lang.langCode == "CN" ? "仆从(劳动)" : "Servant (Work)"),
                        FontColor.Good);
                }

                return (
                    Lang.isJP ? "従者(退避)" : (Lang.langCode == "CN" ? "仆从(退避)" : "Servant (Stashed)"),
                    FontColor.Warning);
            }

            return (
                Lang.isJP ? "従者(戦闘)" : (Lang.langCode == "CN" ? "仆从(战斗)" : "Servant (Combat)"),
                FontColor.Good);
        }

        internal static UIText? GetOrCreateResidentBoardStateLabel(ItemGeneral? item)
        {
            if (item?.button1?.subText2 == null)
                return null;

            Transform? existing = item.button1.transform.Find(ResidentBoardStateLabelObjectName);
            if (existing != null)
                return existing.GetComponent<UIText>();

            UIText template = item.button1.subText2;
            UIText label = UnityEngine.Object.Instantiate(template, template.transform.parent);
            label.name = ResidentBoardStateLabelObjectName;
            label.alignment = TextAnchor.MiddleRight;
            label.raycastTarget = false;
            label.SetActive(enable: false);
            return label;
        }
    }

    /// <summary>
    /// Harmony patch: direct reserve writes must never store servants in vanilla hearth reserve.
    /// Use explicit TargetMethod + Prepare so missing runtime methods skip cleanly instead of producing patch errors.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_FACTION_AddReserve_ServantRedirect
    {
        private const string AddReserveMethodName = "AddReserve";

        private static MethodInfo? ResolveTargetMethod()
        {
            return AccessTools.Method(typeof(FACTION), AddReserveMethodName, new[] { typeof(Chara) });
        }

        static bool Prepare()
        {
            return ResolveTargetMethod() != null;
        }

        static MethodBase? TargetMethod()
        {
            return ResolveTargetMethod();
        }

        static bool Prefix(FACTION __instance, Chara c)
        {
            try
            {
                if (!TryHandleServantReserveRedirect(c))
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"FACTION.AddReserve servant redirect error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_FACTION_AddReserve_ServantRedirect));
                return true;
            }
        }

        private static bool TryHandleServantReserveRedirect(Chara? c)
        {
            if (c == null)
                return false;

            var mgr = NecromancyManager.Instance;
            if (mgr == null || !mgr.IsServant(c.uid))
                return false;

            if (!mgr.TryRedirectReserveToStash(c))
            {
                SE.Beep();
                ModLog.Warn($"AddReserve servant redirect rejected: {c.Name} ({c.uid}) dead={c.isDead} zone={(c.currentZone == null ? "null" : c.currentZone.id)}");
                Msg.SayRaw(
                    c.isDead
                        ? (Lang.isJP
                            ? "死亡した従者は盟約の石の予備枠へは移せません。還生後に従者の退避を使ってください。"
                            : (Lang.langCode == "CN"
                                ? "死亡仆从不能移入盟约石预备栏。请先还生，再使用仆从退避。"
                                : "Dead servants cannot be moved into the Hearth Stone reserve. Revive them first, then use servant stash."))
                        : (Lang.isJP
                            ? "この従者は盟約の石の予備枠ではなく、従者の退避で管理されます。今回は退避への移動に失敗しました。"
                            : (Lang.langCode == "CN"
                                ? "该仆从不会进入盟约石的预备栏，而是改用仆从退避来管理。这次转入退避失败了。"
                                : "This servant is managed by servant stashing instead of the Hearth Stone reserve. Redirecting to stash failed this time.")));
            }

            return true;
        }
    }

    /// <summary>
    /// Harmony patch: when a reserved servant is recruited back, restore servant tracking/runtime.
    /// </summary>
    [HarmonyPatch(typeof(FactionBranch), nameof(FactionBranch.Recruit))]
    public static class Patch_FactionBranch_Recruit_ServantReserveRecovery
    {
        static void Postfix(Chara c)
        {
            try
            {
                NecromancyManager.Instance.SyncServantAfterReserveRecruit(c);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Recruit servant reserve recovery error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_FactionBranch_Recruit_ServantReserveRecovery));
            }
        }
    }

    /// <summary>
    /// Harmony patch: servants must never be sent on vanilla expeditions.
    /// This guards direct UI actions and any other backend path that dispatches expeditions.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_ExpeditionManager_Add_BlockServant
    {
        static MethodBase? TargetMethod()
        {
            return AccessTools.Method(typeof(ExpeditionManager), nameof(ExpeditionManager.Add), new[] { typeof(Expedition) });
        }

        static bool Prefix(Expedition ex)
        {
            try
            {
                var servant = ex?.chara;
                var mgr = NecromancyManager.Instance;
                if (servant == null || mgr == null || !mgr.IsServant(servant.uid))
                    return true;

                ModLog.Warn($"Blocked servant expedition dispatch: {servant.Name} ({servant.uid}) type={ex.type}");
                return false;
            }
            catch (Exception ex2)
            {
                ModLog.Warn($"Expedition servant block patch error: {ex2.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_ExpeditionManager_Add_BlockServant));
                return true;
            }
        }
    }

    /// <summary>
    /// Harmony patch: expedition board should not offer servants as dispatch candidates.
    /// Uses Postfix filtering to preserve vanilla board construction.
    /// </summary>
    [HarmonyPatch(typeof(ListPeopleExpedition), nameof(ListPeopleExpedition.OnList))]
    public static class Patch_ListPeopleExpedition_OnList_ExcludeServants
    {
        static void Postfix(ListPeopleExpedition __instance)
        {
            try
            {
                var mgr = NecromancyManager.Instance;
                if (mgr == null || __instance?.list == null)
                    return;

                __instance.list.objects.RemoveAll(o => o is Chara chara && mgr.IsServant(chara.uid));

                if (__instance.expeditions == null || __instance.expeditions.Count == 0)
                    return;

                foreach (var uid in __instance.expeditions.Keys.Where(uid => mgr.IsServant(uid)).ToList())
                    __instance.expeditions.Remove(uid);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Expedition board servant filter patch error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_ListPeopleExpedition_OnList_ExcludeServants));
            }
        }
    }

    internal static class ServantPathingRules
    {
        internal static bool ShouldSuppressHomePathDestruction(Chara chara)
        {
            if (chara == null)
                return false;

            var mgr = NecromancyManager.Instance;
            if (mgr == null || !mgr.IsServant(chara.uid))
                return false;

            return chara.currentZone?.IsPCFaction == true;
        }
    }

    /// <summary>
    /// Harmony patch: servants should not bulldoze home walls/furniture while pathing in PC-owned zones.
    /// Postfix keeps vanilla path-destruction logic intact outside the home zone.
    /// </summary>
    [HarmonyPatch(typeof(Chara), nameof(Chara.CanDestroyPath))]
    public static class Patch_Chara_CanDestroyPath_BlockServantHomePathDestruction
    {
        static void Postfix(Chara __instance, ref bool __result)
        {
            try
            {
                if (__result && ServantPathingRules.ShouldSuppressHomePathDestruction(__instance))
                    __result = false;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Servant CanDestroyPath patch error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_Chara_CanDestroyPath_BlockServantHomePathDestruction));
            }
        }
    }

    // ================================================================
    // Tome Shop (Black Merchant Only)
    // ================================================================

    /// <summary>
    /// Keep Hecatia reroll effectively infinite without mutating global rules.
    /// Refund only the reroll cost after Hecatia reroll execution.
    /// </summary>
    [HarmonyPatch(typeof(Trait), nameof(Trait.OnBarter))]
    public static class Patch_Trait_OnBarter_HecatiaInfiniteReroll
    {
        static void Postfix(Trait __instance, bool reroll)
        {
            try
            {
                if (__instance is not TraitNecroMerchant) return;
                if (!reroll) return;
                if (EClass._zone == null) return;

                int refund = Mathf.Max(1, __instance.CostRerollShop);
                EClass._zone.influence += refund;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Hecatia reroll patch error: {ex.Message}");
                Plugin.ReportPatchRuntimeFailure(nameof(Patch_Trait_OnBarter_HecatiaInfiniteReroll));
            }
        }
    }

    /// <summary>
    /// Harmony patch: Add Ars Moriendi tome to vanilla black merchant shop.
    /// One-time sale: once purchased, never restocked.
    /// Note: Zek/CWL route was removed for stability.
    /// </summary>
    [HarmonyPatch(typeof(Trait), nameof(Trait.OnBarter))]
    public static class Patch_Trait_OnBarter_ZekTome
    {
        private const string TomeId = "ars_moriendi_tome";
        private const string TomePurchasedFlag = "chitsii.ars.shop.ars_moriendi_tome_purchased";
        private const string LegacyZekSoldFlag = "chitsii.ars.zek_tome_sold";
        private const string LegacyBlackSoldFlag = "chitsii.ars.black_tome_sold";

        static void Postfix(Trait __instance)
        {
            try
            {
                if (__instance is TraitMerchantBlack)
                {
                    TryAddTomeToChest(__instance);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Tome shop patch error: {ex.Message}");
            }
        }

        private static void TryAddTomeToChest(Trait merchant)
        {
            Thing chest = merchant.owner.things.Find("chest_merchant");
            if (chest == null) return;
            if (IsPurchased()) return;
            if (chest.things.Find(TomeId) != null) return;
            // Backward-compat: legacy saves used "carried tome == purchased".
            // Keep this once and convert it to a persistent dialog flag.
            if (EClass.pc?.things?.Find(TomeId) != null)
            {
                MarkPurchased();
                return;
            }
            Thing tome = ThingGen.Create(TomeId);
            if (tome != null)
            {
                chest.AddThing(tome);
                ModLog.Log("Added Ars Moriendi tome to merchant");
            }
        }

        internal static bool IsTargetTome(Thing? t)
        {
            return t != null && t.id == TomeId;
        }

        internal static void MarkPurchased()
        {
            DialogFlagStore.SetBool(EClass.player?.dialogFlags, TomePurchasedFlag, true);
        }

        private static bool IsPurchased()
        {
            var flags = EClass.player?.dialogFlags;
            if (DialogFlagStore.IsTrue(flags, TomePurchasedFlag))
                return true;

            // Backward-compat migration: preserve previous versions' sold-state flags.
            if (DialogFlagStore.IsTrue(flags, LegacyZekSoldFlag)
                || DialogFlagStore.IsTrue(flags, LegacyBlackSoldFlag))
            {
                MarkPurchased();
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Keeps apotheosis stat bonuses bound to feat ownership.
    /// </summary>
    [HarmonyPatch(typeof(Chara), nameof(Chara.SetFeat))]
    public static class Patch_Chara_SetFeat_ApotheosisBonus
    {
        static void Postfix(Chara __instance, int id)
        {
            try
            {
                if (__instance == null || !__instance.IsPC) return;
                if (!ApotheosisFeatBonus.IsApotheosisFeatId(id)) return;

                bool hasFeat = __instance.elements.Has(id);
                if (hasFeat)
                    ApotheosisFeatBonus.OnFeatGranted(__instance);
                else
                    ApotheosisFeatBonus.OnFeatRemoved(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Apotheosis feat bonus patch error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Marks tome purchase as persistent state.
    /// This prevents merchant restocks after the first successful purchase.
    /// </summary>
    [HarmonyPatch(typeof(ShopTransaction), nameof(ShopTransaction.Process))]
    public static class Patch_ShopTransaction_Process_TomePurchaseFlag
    {
        static void Postfix(ShopTransaction __instance, Thing t, bool sell)
        {
            try
            {
                if (sell) return;
                if (!Patch_Trait_OnBarter_ZekTome.IsTargetTome(t)) return;
                if (!__instance.HasBought(t)) return;
                Patch_Trait_OnBarter_ZekTome.MarkPurchased();
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Tome purchase flag patch error: {ex.Message}");
            }
        }
    }

}

