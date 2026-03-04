using System;
using System.Collections.Generic;
using System.Linq;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Manages necromancy state: unlocked spells, servant tracking,
    /// soul unit system, servant enhancement, body augmentation, and rampage risk.
    /// Uses EClass.player.dialogFlags for per-save persistence.
    /// </summary>
    public class NecromancyManager
    {
        /// <summary>Custom int ID stored on corpses to record the creature's actual LV at death.</summary>
        public const int CorpseLvIntId = 92710;
        /// <summary>Custom int ID stored on corpses to preserve the victim's main element ID.</summary>
        public const int CorpseMainElementIntId = 92712;
        internal const string ServantAuraFxId = "FxServantShadowTentacleAura_SmokeDrip"; // V1
        private const string LegacyServantAuraFxId = "FxServantShadowTentacleAura";
        private const string ServantAuraFxV2Id = "FxServantShadowTentacleAura_SmokeDrip_v2";

        public static NecromancyManager Instance { get; } = new();

        /// <summary>Main quest state machine (Unhallowed Path).</summary>
        public UnhallowedPath QuestPath { get; } = new();

        private HashSet<string> _unlocked = new();
        private List<int> _servantUidList = new();
        private Dictionary<int, ServantEnhancement> _enhancements = new();

        /// <summary>Tracks which Game instance we've loaded state for, to avoid redundant loads.</summary>
        private Game? _loadedForGame;

        // ── dialogFlags key prefixes ──
        private const string SpellPrefix = "chitsii.ars.spell.";
        private const string ServantPrefix = "chitsii.ars.sv.";
        private const string EnhPrefix = "chitsii.ars.enh.";
        private const string SoulBindSacrificeUidKey = "chitsii.ars.state.soulbind_sacrifice_uid";

        // ============================================================
        // Soul Unit Constants
        // ============================================================

        /// <summary>100%復元に必要なSU（強い魂30個相当）</summary>
        public const int MaxResurrectionSU = 1200;

        /// <summary>最低復元率（魂1個でも元Lvの5%は保証）</summary>
        public const double BaseRecoveryRate = 0.05;

        public static readonly Dictionary<string, int> SoulUnitTable = new()
        {
            ["ars_soul_weak"] = 5,
            ["ars_soul_normal"] = 15,
            ["ars_soul_strong"] = 40,
            ["ars_soul_legendary"] = 100,
        };

        /// <summary>All soul item IDs in ascending power order.</summary>
        public static readonly string[] SoulIds =
            { "ars_soul_weak", "ars_soul_normal", "ars_soul_strong", "ars_soul_legendary" };

        /// <summary>
        /// Hard cap per attribute injection action.
        /// Forces enhancement to be an iterative process instead of one-shot dumping.
        /// </summary>
        public const int MaxSoulsPerInjection = 10;

        // ============================================================
        // Body Augmentation Constants
        // ============================================================

        /// <summary>Augmentable body parts: slot ID, JP figure name.</summary>
        public static readonly AugmentableSlot[] AugmentableSlots =
        {
            new(34, "腕"), // arm
            new(35, "手"), // hand
            new(30, "頭"), // head
            new(36, "指"), // finger
            new(39, "足"), // foot (game uses "足"→39, not "脚"→38)
            new(31, "首"), // neck
            new(32, "体"), // body
            new(33, "背"), // back
            new(37, "腰"), // waist
        };

        /// <summary>Max total added body parts across all slot types.</summary>
        public const int MaxTotalAddedParts = 20;

        /// <summary>Base success rate for body augmentation (10%).</summary>
        public const double AugmentBaseRate = 0.10;

        // ============================================================
        // Attribute IDs (Elin Element IDs)
        // ============================================================

        public static readonly int[] AttributeIds = { 70, 71, 72, 73, 74, 75, 76, 77, 79 };
        // 70=STR, 71=END, 72=DEX, 73=PER, 74=LER, 75=WIL, 76=MAG, 77=CHA, 79=SPD

        public static readonly List<SpellUnlock> SpellUnlocks = new()
        {
            new SpellUnlock("actSoulTrap", "ars_soul_weak", 0,
                "魂魄保存", "Preserve Soul",
                "対象に魂魄保存を付与する。効果中に倒すと魂をドロップする。",
                "Applies Preserve Soul to the target. If slain while active, it drops a soul.",
                true, "灵魂封存", "对目标施加灵魂封存。效果期间击杀会掉落灵魂。"),

            new SpellUnlock("actPreserveCorpse", "ars_soul_weak", 0,
                "屍体保存", "Preserve Corpse",
                "効果中、PC陣営が倒した敵は必ず死体を残す。",
                "While active, enemies slain by your side always leave a corpse.",
                true, "保存尸体", "效果期间，玩家阵营击杀的敌人必定留下尸体。"),

            new SpellUnlock("actCurseWeakness", "ars_soul_weak", 5,
                "衰弱の呪い", "Curse of Weakness",
                "対象のSTR・DEXを大幅に低下させる呪い。",
                "A curse that greatly reduces the target's STR and DEX.",
                nameCN: "衰弱诅咒", descCN: "大幅降低目标STR与DEX的诅咒。"),

            new SpellUnlock("actSummonUndead", "ars_soul_strong", 4,
                "アンデッド召喚", "Summon Undead",
                "一時的なアンデッド従者を召喚する。召喚体のレベルは呪文威力で上がり、PCが詠唱した場合は到達最深階層を下回らない。",
                "Summons a temporary undead servant. The summon level scales with spell power, and when cast by the player it is floored by your deepest reached depth.",
                nameCN: "召唤亡灵", descCN: "召唤一名临时亡灵仆从。召唤体等级随法术威力提升；由玩家施放时，等级下限不会低于已到达的最深层数。"),

            // Tier 1
            new SpellUnlock("actTerror", "ars_soul_normal", 4,
                "恐怖", "Terror",
                "対象に恐怖状態を強制付与する。",
                "Forcibly inflicts Terror on the target.",
                nameCN: "恐惧", descCN: "对目标强制施加恐惧状态。"),

            // Tier 2
            new SpellUnlock("actCurseFrailty", "ars_soul_normal", 7,
                "衰弱の呪い（重）", "Curse of Frailty",
                "対象のEND・PER・速度を大幅に低下させる。",
                "Greatly reduces the target's END, PER and speed.",
                nameCN: "衰弱诅咒（重）", descCN: "大幅降低目标的END、PER和速度。"),

            new SpellUnlock("actLifeDrain", "ars_soul_normal", 10,
                "生命吸収", "Life Drain",
                "対象のHPを吸い取り、その分自身を回復する。",
                "Drains the target's HP and heals yourself by that amount.",
                nameCN: "生命汲取", descCN: "吸取目标HP，并按吸取量恢复自身。"),

            new SpellUnlock("actStaminaDrain", "ars_soul_normal", 5,
                "精力吸収", "Stamina Drain",
                "対象のスタミナを吸い取り、自身にも一部還元する。",
                "Drains the target's stamina and restores part of it to yourself.",
                nameCN: "精力汲取", descCN: "吸取目标体力，并将其中一部分返还给自身。"),

            new SpellUnlock("actManaDrain", "ars_soul_normal", 5,
                "魔力吸収", "Mana Drain",
                "対象のMPを吸い取り、自身にも一部還元する。",
                "Drains the target's mana and restores part of it to yourself.",
                nameCN: "魔力汲取", descCN: "吸取目标MP，并将其中一部分返还给自身。"),

            // Tier 3
            new SpellUnlock("actPlagueTouch", "ars_soul_strong", 5,
                "疫病の手", "Plague Touch",
                "対象に疫病を付与し、毎ターンダメージ。低確率で隣接へ感染する。",
                "Infects the target with plague, dealing damage each turn and occasionally spreading to adjacent units.",
                nameCN: "瘟疫之触", descCN: "对目标施加瘟疫，每回合造成伤害，并以低概率向相邻单位扩散。"),

            new SpellUnlock("actBoneAegisLegion", "ars_soul_strong", 6,
                "骸骨壁", "Wall of Skeleton",
                "自身と従者全員に短時間の生存保護と被ダメージ軽減を与える。",
                "Grants brief survival protection and damage reduction to you and all servants.",
                nameCN: "骸骨之墙", descCN: "为你与全体仆从赋予短暂生存保护和减伤。"),

            new SpellUnlock("actGraveQuagmire", "ars_soul_strong", 7,
                "黄泉の泥濘", "Grave Quagmire",
                "術者中心の泥濘領域を展開。範囲内の敵を減速し、沈黙失敗時は妨害を与える。",
                "Creates a caster-centered quagmire field. Enemies inside are slowed; if silence fails, suppression is applied.",
                nameCN: "黄泉泥泞", descCN: "展开以施法者为中心的泥泞领域。范围内敌人减速；沉默未命中时改为施加妨害。"),

            new SpellUnlock("actCorpseChainBurst", "ars_soul_strong", 8,
                "屍鎖爆砕", "Corpse Chain Burst",
                "視界内の死体をすべて順次爆砕する。各爆発は周囲の敵に混乱（耐性無視）と大ダメージを与え、爆心地4マスの脆い壁を破壊する。味方は回復し、短時間の生存保護を得る。",
                "Sequentially detonates all visible corpses. Each blast confuses nearby enemies (ignores resistance), deals heavy damage, and breaks fragile walls within 4 tiles of the blast center. Allies are healed and gain brief survival protection.",
                nameCN: "尸锁爆砕", descCN: "按顺序引爆视野内全部尸体。每次爆炸都会使周围敌人混乱（无视抗性）并造成高额伤害，同时破坏爆心4格内的脆弱墙体。友军获得治疗与短暂保命。"),

            new SpellUnlock("actSoulRecall", "ars_soul_strong", 9,
                "死兵還生", "Soul Recall",
                "倒れた従者を複数蘇生し、術者周囲に再配置する。蘇生直後は短時間保護される。",
                "Revives multiple fallen servants, redeploys them near the caster, and grants brief protection on revival.",
                nameCN: "死兵还生", descCN: "复活多名倒下的仆从并重新部署到施法者附近，复活后会获得短暂保护。"),

            new SpellUnlock("actGraveExile", "ars_soul_strong", 8,
                "共連れ転送", "Grave Exile",
                "指定従者と周囲の敵集団を遠方へ転位させ、戦線を分断する。",
                "Teleports a selected servant and nearby enemies far away to split the frontline.",
                nameCN: "共连转送", descCN: "将指定仆从与周围敌群转位到远方，切断战线。"),

            new SpellUnlock("actFuneralMarch", "ars_soul_strong", 7,
                "死軍号令", "Funeral March",
                "術者は鈍化する（低下量は現在速度の2/3が上限）が、従者全員の速度・筋力・耐久を強化する。",
                "Slows the caster (penalty capped at two-thirds of current speed) while empowering all servants' speed, strength, and endurance.",
                nameCN: "死军号令", descCN: "施法者减速（降速上限为当前速度的2/3），并强化全体仆从的速度、力量与耐久。"),

            new SpellUnlock("actSummonSkeletonWarrior", "ars_soul_strong", 6,
                "骸骨兵召喚", "Summon Skeleton Warrior",
                "一時的な骸骨戦士を召喚する。召喚体のレベルは呪文威力で上がり、PCが詠唱した場合は到達最深階層を下回らない。",
                "Summons a temporary skeleton warrior. The summon level scales with spell power, and when cast by the player it is floored by your deepest reached depth.",
                nameCN: "召唤骷髅战士", descCN: "召唤一名临时骷髅战士。召唤体等级随法术威力提升；由玩家施放时，等级下限不会低于已到达的最深层数。"),

            new SpellUnlock("actUnholyVigor", "ars_soul_strong", 3,
                "不浄な活力", "Unholy Vigor",
                "現在MPの1/3を消費し、HPとスタミナを回復する。",
                "Consumes one-third of current MP to restore HP and stamina.",
                nameCN: "不洁之活力", descCN: "消耗当前MP的三分之一，恢复HP与体力。"),

            // Tier 4
            new SpellUnlock("actSoulBind", "ars_soul_legendary", 3,
                "魂の鎖", "Soul Bind",
                "術者と従者1体を鎖で結ぶ。術者が致死を受けると従者が犠牲となり、術者は復活して短時間無敵・沈黙解除。",
                "Links the caster to one servant. On lethal damage, that servant is sacrificed and the caster revives with brief invulnerability and silence removal.",
                nameCN: "灵魂锁链", descCN: "将施法者与1名仆从连接。施法者受到致死伤害时，该仆从会被献祭，施法者复活并获得短暂无敌与沉默解除。"),

            new SpellUnlock("actDeathZone", "ars_soul_legendary", 5,
                "死の領域", "Death Zone",
                "術者追従の領域を展開し、範囲内の敵に継続ダメージ、味方に継続回復を与える。",
                "Creates a caster-following zone that deals periodic damage to enemies and heals allies inside.",
                nameCN: "死亡领域", descCN: "展开跟随施法者的领域，对范围内敌人造成持续伤害并为友军持续恢复。"),

            new SpellUnlock("actSoulTrapMass", "ars_soul_strong", 8,
                "魂縛の檻", "Soul Snare",
                "周囲半径6の「魂を持つ敵」全てに魂魄保存を付与する。",
                "Applies Preserve Soul to all soul-bearing enemies within radius 6.",
                nameCN: "灵魂囚笼", descCN: "对周围半径6内所有“拥有灵魂”的敌人施加灵魂封存。"),
        };

        public void Init()
        {
            // No-op at startup; game state is loaded lazily via EnsureGameStateLoaded()
            // when EClass.player becomes available.
        }

        /// <summary>
        /// Clear all in-memory state. Called by ModUninstaller after all save data is cleaned up.
        /// </summary>
        public void ResetState()
        {
            _unlocked.Clear();
            _servantUidList.Clear();
            _enhancements.Clear();
            _loadedForGame = null;
        }

        /// <summary>
        /// Lazily loads game state from dialogFlags when the current Game instance changes.
        /// Call this at the start of any public method that reads/writes game state.
        /// </summary>
        private void EnsureGameStateLoaded()
        {
            if (EClass.game == null || EClass.player?.dialogFlags == null) return;
            if (_loadedForGame == EClass.game) return;
            _loadedForGame = EClass.game;
            LoadFromDialogFlags();
            PruneOrphanedServants();
            EnsureStashedServantsInHomeZoneOnLoad();
            EnsureServantVisualStateAll();
            MigrateSpellUnlockAliases();
            QuestPath.EnsureQuestExists();

            // Ensure quest NPCs exist in globalCharas (save/load safety)
            var stage = QuestPath.CurrentStage;
            if (stage >= UnhallowedStage.Stage2)
                KnightEncounter.EnsureKarenExists();
            if (stage >= UnhallowedStage.Stage4)
                KnightEncounter.EnsureErenosExists();

            // Keep feat-driven apotheosis stat bonuses consistent with current config.
            if (EClass.pc != null)
                ApotheosisFeatBonus.SyncWithConfigAndFeat(EClass.pc);
        }

        private void LoadFromDialogFlags()
        {
            _unlocked.Clear();
            _servantUidList.Clear();
            _enhancements.Clear();

            var flags = EClass.player.dialogFlags;

            foreach (var kvp in flags)
            {
                if (kvp.Key.StartsWith(SpellPrefix))
                {
                    var alias = kvp.Key.Substring(SpellPrefix.Length);
                    if (alias.Length > 0 && kvp.Value == 1)
                        _unlocked.Add(alias);
                }
                else if (kvp.Key.StartsWith(ServantPrefix))
                {
                    var uidStr = kvp.Key.Substring(ServantPrefix.Length);
                    if (int.TryParse(uidStr, out int uid) && kvp.Value == 1)
                        _servantUidList.Add(uid);
                }
                else if (kvp.Key.StartsWith(EnhPrefix))
                {
                    ParseEnhancementFlag(kvp.Key.Substring(EnhPrefix.Length), kvp.Value);
                }
            }

            ModLog.Log("LoadFromDialogFlags: {0} spells, {1} servants, {2} enhancements",
                _unlocked.Count, _servantUidList.Count, _enhancements.Count);
        }

        private void MigrateSpellUnlockAliases()
        {
            MigrateSpellAlias("actBoneShield", "actBoneAegisLegion");
            MigrateSpellAlias("actEmpowerUndead", "actFuneralMarch");
        }

        private void MigrateSpellAlias(string oldAlias, string newAlias)
        {
            if (_unlocked.Contains(oldAlias) && !_unlocked.Contains(newAlias))
            {
                _unlocked.Add(newAlias);
                SetFlag(SpellPrefix + newAlias, 1);
                ModLog.Log("Migrated unlocked spell alias: {0} -> {1}", oldAlias, newAlias);
            }
        }

        private void ParseEnhancementFlag(string suffix, int value)
        {
            // suffix format: "{uid}.{field}" or "{uid}.{field}.{subId}"
            var dotIndex = suffix.IndexOf('.');
            if (dotIndex < 0) return;

            var uidStr = suffix.Substring(0, dotIndex);
            if (!int.TryParse(uidStr, out int uid)) return;

            var rest = suffix.Substring(dotIndex + 1);
            if (!_enhancements.TryGetValue(uid, out var enh))
            {
                enh = new ServantEnhancement();
                _enhancements[uid] = enh;
            }

            if (rest == "lvl") enh.EnhancementLevel = value;
            else if (rest == "bp") enh.AddedBodyParts = value;
            else if (rest == "rlv") enh.ResurrectionLevel = value;
            else if (rest == "dorm") enh.IsDormant = value == 1;
            else if (rest == "stsh") enh.IsStashed = value == 1;
            else if (rest == "tac") enh.TacticId = TacticIndexToId(value);
            else if (rest.StartsWith("sl."))
            {
                if (int.TryParse(rest.Substring(3), out int slotId))
                    enh.SlotAdditions[slotId] = value;
            }
            else if (rest.StartsWith("ai."))
            {
                if (int.TryParse(rest.Substring(3), out int attrId))
                    enh.AttrInjections[attrId] = value;
            }
            else if (rest.StartsWith("sr."))
            {
                if (int.TryParse(rest.Substring(3), out int slotId))
                    enh.SlotResonance[slotId] = value;
            }
        }

        // ── dialogFlags helpers ──

        private static int GetFlag(string key)
        {
            return DialogFlagStore.GetInt(EClass.player?.dialogFlags, key);
        }

        private static void SetFlag(string key, int value)
        {
            DialogFlagStore.SetInt(EClass.player?.dialogFlags, key, value);
        }

        private static void RemoveFlag(string key)
        {
            EClass.player?.dialogFlags?.Remove(key);
        }

        public int GetSoulBindSacrificeUid()
        {
            EnsureGameStateLoaded();
            return GetFlag(SoulBindSacrificeUidKey);
        }

        public void SetSoulBindSacrificeUid(int uid)
        {
            EnsureGameStateLoaded();
            if (uid <= 0)
            {
                RemoveFlag(SoulBindSacrificeUidKey);
                return;
            }

            SetFlag(SoulBindSacrificeUidKey, uid);
        }

        public void ClearSoulBindSacrificeUid()
        {
            EnsureGameStateLoaded();
            RemoveFlag(SoulBindSacrificeUidKey);
        }

        // ── Tactic ID ↔ int conversion ──

        private static int TacticIdToIndex(string tacticId)
        {
            if (string.IsNullOrEmpty(tacticId)) return 0;
            var rows = EClass.sources?.tactics?.rows;
            if (rows == null) return 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].id == tacticId) return i + 1; // 1-based
            }
            return 0;
        }

        private static string TacticIndexToId(int index)
        {
            if (index <= 0) return "";
            var rows = EClass.sources?.tactics?.rows;
            if (rows == null || index > rows.Count) return "";
            return rows[index - 1].id ?? "";
        }

        // ============================================================
        // Spell Unlock System
        // ============================================================

        public bool IsUnlocked(string spellAlias)
        {
            EnsureGameStateLoaded();
            var unlock = SpellUnlocks.Find(s => s.Alias == spellAlias);
            if (unlock?.InitiallyUnlocked == true) return true;
            return _unlocked.Contains(spellAlias);
        }

        public void Unlock(string spellAlias)
        {
            EnsureGameStateLoaded();
            _unlocked.Add(spellAlias);
            SetFlag(SpellPrefix + spellAlias, 1);

            var spell = SpellUnlocks.Find(s => s.Alias == spellAlias);
            if (spell != null)
                GrantAbilityToPC(spell.ElementId);
        }

        public void GrantAbilityToPC(int elementId)
        {
            var pc = EClass.pc;
            if (pc == null)
            {
                ModLog.Log("GrantAbilityToPC({0}): pc is null, skipping", elementId);
                return;
            }
            if (pc.elements == null)
            {
                ModLog.Log("GrantAbilityToPC({0}): pc.elements is null, skipping", elementId);
                return;
            }

            var existing = pc.elements.GetElement(elementId);
            if (existing != null && existing.ValueWithoutLink > 0)
            {
                ModLog.Log("GrantAbilityToPC({0}): already known (Value={1}), skipping",
                    elementId, existing.ValueWithoutLink);
                return;
            }

            ModLog.Log("GrantAbilityToPC({0}): calling GainAbility (existing={1})",
                elementId, existing == null ? "null" : $"Value={existing.ValueWithoutLink}");
            pc.GainAbility(elementId);
        }

        public void EnsureUnlockedSpellsGranted()
        {
            EnsureGameStateLoaded();
            ModLog.Log("EnsureUnlockedSpellsGranted: checking {0} spell definitions", SpellUnlocks.Count);
            foreach (var spell in SpellUnlocks)
            {
                bool unlocked = IsUnlocked(spell.Alias);
                ModLog.Log("  {0} (id={1}): unlocked={2}", spell.Alias, spell.ElementId, unlocked);
                if (unlocked)
                {
                    GrantAbilityToPC(spell.ElementId);
                }
            }
        }

        /// <summary>
        /// Ensure all unlocked necromancy spells have at least the specified stock.
        /// Intended for debug/testing workflows.
        /// </summary>
        public void EnsureUnlockedSpellStocksAtLeast(int minimumStock)
        {
            EnsureGameStateLoaded();
            if (minimumStock <= 0) return;

            var pc = EClass.pc;
            if (pc?.elements == null) return;

            foreach (var spell in SpellUnlocks)
            {
                if (!IsUnlocked(spell.Alias)) continue;

                // Make sure the spell ability entry exists before touching stock.
                GrantAbilityToPC(spell.ElementId);

                var element = pc.elements.GetElement(spell.ElementId);
                if (element == null) continue;

                int delta = minimumStock - element.vPotential;
                if (delta > 0)
                {
                    pc.elements.ModPotential(spell.ElementId, delta);
                }
            }
        }

        /// <summary>
        /// Force re-evaluation of servant visual state (aura FX, condition marker).
        /// Needed for servants created before the visual feature was introduced.
        /// </summary>
        public void RefreshServantVisuals()
        {
            EnsureGameStateLoaded();
            foreach (var uid in _servantUidList)
            {
                Chara? chara = null;
                EClass.game?.cards?.globalCharas?.TryGetValue(uid, out chara);
                if (chara == null) continue;
                EnsureServantVisualState(chara);
                ModLog.Log("RefreshServantVisuals: refreshed visuals for {0} (uid={1})", chara.Name, uid);
            }
        }

        public bool TryUnlockWithSouls(string spellAlias)
        {
            if (IsUnlocked(spellAlias)) return false;

            var unlock = SpellUnlocks.Find(s => s.Alias == spellAlias);
            if (unlock == null) return false;

            var pc = EClass.pc;
            int available = CountItemsInInventory(pc, unlock.RequiredSoulId);
            if (available < unlock.RequiredSoulCount) return false;

            ConsumeItems(pc, unlock.RequiredSoulId, unlock.RequiredSoulCount);
            Unlock(spellAlias);
            return true;
        }

        // ============================================================
        // Soul Unit & Level Calculation
        // ============================================================

        /// <summary>
        /// Calculate total Soul Unit from a dictionary of soul type → quantity.
        /// </summary>
        public static int CalculateTotalSU(Dictionary<string, int> soulAmounts)
        {
            int totalSU = 0;
            foreach (var kvp in soulAmounts)
            {
                if (SoulUnitTable.TryGetValue(kvp.Key, out int suPerUnit))
                    totalSU += suPerUnit * kvp.Value;
            }
            return totalSU;
        }

        /// <summary>
        /// Calculate resurrection level from total SU using recovery-rate model.
        /// Corpse level is intentionally ignored; recovery is based on deepest-derived cap.
        /// </summary>
        public static int CalculateResurrectionLevel(int totalSU, int corpseLv = 0)
        {
            int cap = GetLevelCap();
            return NecromancyCalculations.CalculateResurrectionLevel(totalSU, cap);
        }

        /// <summary>
        /// Get resurrection level cap from player's deepest dungeon level.
        /// The cap is intentionally corpse-independent to keep ritual results predictable.
        /// </summary>
        public static int GetLevelCap(int sourceLevel = 1)
        {
            int deepest = EClass.player?.stats?.deepest ?? 1;
            return NecromancyCalculations.CalculateResurrectionCapFromDeepest(deepest);
        }

        /// <summary>
        /// Calculate final resurrection level (capped).
        /// Corpse level is intentionally ignored to keep ritual output corpse-independent.
        /// </summary>
        public static int CalculateFinalLevel(int totalSU, int corpseLv = 0)
        {
            int rawLevel = CalculateResurrectionLevel(totalSU);
            int cap = GetLevelCap();
            return Math.Min(rawLevel, cap);
        }

        // ============================================================
        // Servant System
        // ============================================================

        public int ServantCount { get { EnsureGameStateLoaded(); return _servantUidList.Count; } }

        public bool IsServant(int uid) { EnsureGameStateLoaded(); return _servantUidList.Contains(uid); }

        public List<Chara> GetAliveServants()
        {
            EnsureGameStateLoaded();
            PruneOrphanedServants();
            var alive = new List<Chara>();

            foreach (var uid in _servantUidList)
            {
                Chara? chara = null;
                EClass.game?.cards?.globalCharas?.TryGetValue(uid, out chara);
                if (chara == null || chara.isDestroyed) continue;
                if (!chara.isDead)
                    alive.Add(chara);
            }

            return alive;
        }

        /// <summary>
        /// Alive servants currently participating in the active zone battle space.
        /// Excludes stashed servants and display-mode servants.
        /// </summary>
        public List<Chara> GetCombatServantsInCurrentZone()
        {
            var result = new List<Chara>();
            foreach (var servant in GetAliveServants())
            {
                if (servant.currentZone != EClass._zone) continue;
                var enh = GetEnhancement(servant.uid);
                if (enh.IsStashed || enh.IsDormant) continue;
                result.Add(servant);
            }
            return result;
        }

        public List<(Chara chara, bool isAlive)> GetAllServants()
        {
            EnsureGameStateLoaded();
            PruneOrphanedServants();
            var result = new List<(Chara, bool)>();

            foreach (var uid in _servantUidList)
            {
                Chara? chara = null;
                EClass.game?.cards?.globalCharas?.TryGetValue(uid, out chara);
                if (chara == null || chara.isDestroyed) continue;
                result.Add((chara, !chara.isDead));
            }

            return result;
        }

        public bool IsServantStashed(int uid)
        {
            EnsureGameStateLoaded();
            if (!_servantUidList.Contains(uid)) return false;
            return GetEnhancement(uid).IsStashed;
        }

        public int CountStashedServants()
        {
            EnsureGameStateLoaded();
            int count = 0;
            foreach (var uid in _servantUidList)
            {
                var enh = GetEnhancement(uid);
                if (enh.IsStashed) count++;
            }
            return count;
        }

        public bool SetServantStashedState(Chara servant, bool isStashed)
        {
            EnsureGameStateLoaded();
            if (servant == null || servant.isDestroyed || servant.isDead) return false;
            if (!_servantUidList.Contains(servant.uid)) return false;

            var enh = GetEnhancement(servant.uid);
            if (enh.IsStashed == isStashed)
            {
                ApplyServantRuntimeState(servant, enh);
                return true;
            }

            bool moved = isStashed
                ? TryMoveServantToHomeStashZone(servant)
                : TryRecallServantNearPlayer(servant);
            if (!moved) return false;

            enh.IsStashed = isStashed;
            SaveEnhancement(servant.uid, enh);
            ApplyServantRuntimeState(servant, enh);
            return moved;
        }

        public int StashAllActiveServants()
        {
            EnsureGameStateLoaded();
            int count = 0;
            foreach (var servant in GetAliveServants())
            {
                var enh = GetEnhancement(servant.uid);
                if (enh.IsStashed) continue;
                if (SetServantStashedState(servant, true))
                    count++;
            }
            return count;
        }

        public int RecallStashedServants(int maxCount = int.MaxValue)
        {
            EnsureGameStateLoaded();
            if (maxCount <= 0) return 0;

            int recalled = 0;
            var candidates = GetAliveServants()
                .Where(s => IsServantStashed(s.uid))
                .OrderByDescending(s => s.LV)
                .ThenBy(s => s.uid)
                .ToList();

            foreach (var servant in candidates)
            {
                if (recalled >= maxCount) break;
                if (SetServantStashedState(servant, false))
                    recalled++;
            }
            return recalled;
        }

        private void EnsureStashedServantsInHomeZoneOnLoad()
        {
            int migrated = 0;
            int pending = 0;

            foreach (var uid in _servantUidList.ToList())
            {
                var enh = GetEnhancement(uid);
                if (!enh.IsStashed) continue;

                Chara? chara = null;
                EClass.game?.cards?.globalCharas?.TryGetValue(uid, out chara);
                if (chara == null || chara.isDestroyed)
                {
                    pending++;
                    continue;
                }

                if (chara.isDead)
                {
                    enh.IsStashed = false;
                    SaveEnhancement(uid, enh);
                    migrated++;
                    continue;
                }

                if (!TryMoveServantToHomeStashZone(chara))
                {
                    pending++;
                    continue;
                }

                ApplyServantRuntimeState(chara, enh);
                migrated++;
            }

            if (migrated > 0 || pending > 0)
                ModLog.Log("EnsureStashedServantsInHomeZoneOnLoad: migrated={0}, pending={1}", migrated, pending);
        }

        private static bool TryMoveServantToHomeStashZone(Chara servant)
        {
            try
            {
                CustomAssetFx.StopAllAttachedFx(servant);
                var homeZone = servant.homeBranch?.owner ?? EClass.pc?.homeZone;
                if (homeZone == null) return false;

                if (servant.currentZone != homeZone)
                    servant.MoveZone(homeZone, ZoneTransition.EnterState.RandomVisit);

                servant.enemy = null;
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"TryMoveServantToHomeStashZone failed: {servant.Name} ({servant.uid}) / {ex.Message}");
                return false;
            }
        }

        private static bool TryRecallServantNearPlayer(Chara servant)
        {
            var pc = EClass.pc;
            var zone = EClass._zone;
            if (pc == null || zone == null) return false;

            try
            {
                if (servant.currentZone != zone)
                {
                    if (servant.currentZone != null)
                        servant.currentZone.RemoveCard(servant);

                    var spawn = pc.pos.GetNearestPointCompat(allowBlock: false, allowChara: false);
                    zone.AddCard(servant, spawn);
                }

                servant.enemy = null;
                EnsureServantVisualState(servant);
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"TryRecallServantNearPlayer failed: {servant.Name} ({servant.uid}) / {ex.Message}");
                return false;
            }
        }

        public void AddServant(Chara servant)
        {
            EnsureGameStateLoaded();
            if (!_servantUidList.Contains(servant.uid))
            {
                _servantUidList.Add(servant.uid);
                SetFlag(ServantPrefix + servant.uid, 1);
            }
            EnsureServantVisualState(servant);
        }

        private void EnsureServantVisualStateAll()
        {
            foreach (var uid in _servantUidList)
            {
                Chara? chara = null;
                EClass.game?.cards?.globalCharas?.TryGetValue(uid, out chara);
                if (chara == null) continue;
                EnsureServantVisualState(chara);
            }
        }

        public void RefreshServantVisualStateCurrentZone()
        {
            EnsureGameStateLoaded();
            foreach (var servant in GetAliveServants())
            {
                if (servant.currentZone != EClass._zone) continue;
                EnsureServantVisualState(servant);
            }
        }

        /// <summary>
        /// Attach servant aura if the config toggle is ON; no-op when OFF.
        /// Use this at non-zone-transition call sites (Tick, RefreshColor, Revive, GetRevived).
        /// </summary>
        internal static void TryEnsureServantAura(Card card)
        {
            if (ModConfig.ShowServantAura.Value)
                TryEnsureServantAuraFx(card);
        }

        private static void EnsureServantVisualState(Chara servant)
        {
            if (servant == null || servant.isDestroyed) return;
            if (!servant.HasCondition<ConUndeadServantPresence>())
                servant.AddCondition<ConUndeadServantPresence>(1, force: true);

            // Zone-transition visual attach: attach or detach based on config toggle.
            if (ModConfig.ShowServantAura.Value)
                TryEnsureServantAuraFx(servant);
            else
                StopServantAuraFx(servant);
        }

        public void RemoveServant(int uid)
        {
            EnsureGameStateLoaded();
            Chara? chara = null;
            EClass.game?.cards?.globalCharas?.TryGetValue(uid, out chara);
            if (chara != null)
                StopServantAuraFx(chara);

            _servantUidList.Remove(uid);
            _enhancements.Remove(uid);
            RemoveServantFlags(uid);
        }

        /// <summary>Remove all dialogFlags for a servant (sv + enh keys).</summary>
        private void RemoveServantFlags(int uid)
        {
            RemoveFlag(ServantPrefix + uid);
            RemoveEnhancementFlags(uid);
        }

        private void RemoveEnhancementFlags(int uid)
        {
            var prefix = EnhPrefix + uid + ".";
            RemoveFlagsWithPrefix(prefix);
        }

        private static void RemoveFlagsWithPrefix(string prefix)
        {
            var flags = EClass.player?.dialogFlags;
            if (flags == null) return;
            var keysToRemove = new List<string>();
            foreach (var key in flags.Keys)
            {
                if (key.StartsWith(prefix))
                    keysToRemove.Add(key);
            }
            foreach (var key in keysToRemove)
                flags.Remove(key);
        }

        /// <summary>
        /// Removes servant UIDs whose global card is missing or no longer under PC faction/minion ownership.
        /// This prevents stale servant references from breaking stash/recall flows on load/runtime queries.
        /// </summary>
        private int PruneOrphanedServants()
        {
            if (_servantUidList.Count == 0) return 0;

            var removed = new List<int>();
            foreach (var uid in _servantUidList)
            {
                Chara? chara = null;
                EClass.game?.cards?.globalCharas?.TryGetValue(uid, out chara);
                if (chara == null || chara.isDestroyed || !chara.IsPCFactionOrMinion)
                    removed.Add(uid);
            }

            foreach (var uid in removed)
            {
                Chara? chara = null;
                EClass.game?.cards?.globalCharas?.TryGetValue(uid, out chara);
                if (chara != null)
                    StopServantAuraFx(chara);

                RemoveServantFlags(uid);
                _servantUidList.Remove(uid);
                _enhancements.Remove(uid);
                ModLog.Log("Servant UID removed (orphaned/banished): {0}", uid);
            }

            return removed.Count;
        }

        internal static void StopServantAuraFx(Card card)
        {
            if (card == null) return;
            CustomAssetFx.StopAttachedFx(ServantAuraFxId, card);
            if (!string.Equals(ServantAuraFxId, LegacyServantAuraFxId, StringComparison.Ordinal))
                CustomAssetFx.StopAttachedFx(LegacyServantAuraFxId, card);
            if (!string.Equals(ServantAuraFxId, ServantAuraFxV2Id, StringComparison.Ordinal))
                CustomAssetFx.StopAttachedFx(ServantAuraFxV2Id, card);
        }

        private static void TryEnsureServantAuraFx(Card card)
        {
            if (card == null) return;
            // Servant aura is a persistent marker effect; do not lease it by turn.
            // Lease-based loops can be swept/recreated around turn boundaries, which
            // visibly resets long-running particle simulation.
            CustomAssetFx.TryEnsureLoopAttachedToCard(ServantAuraFxId, card);
            // Remove stale variants so switching between V0/V1/V2 is immediate.
            if (!string.Equals(ServantAuraFxId, LegacyServantAuraFxId, StringComparison.Ordinal))
                CustomAssetFx.StopAttachedFx(LegacyServantAuraFxId, card);
            if (!string.Equals(ServantAuraFxId, ServantAuraFxV2Id, StringComparison.Ordinal))
                CustomAssetFx.StopAttachedFx(ServantAuraFxV2Id, card);
        }

        /// <summary>
        /// Perform the ritual: consume a corpse and souls to raise an undead servant.
        /// Soul amounts determine resurrection level via SU system.
        /// </summary>
        public Chara? PerformRitual(Thing corpse, Dictionary<string, int> soulAmounts)
        {
            try
            {
                var sourceId = corpse.c_idRefCard;
                if (string.IsNullOrEmpty(sourceId))
                    return null;

                // Calculate level from soul unit + corpse creature level
                int totalSU = CalculateTotalSU(soulAmounts);
                if (totalSU <= 0) return null;

                int level = CalculateFinalLevel(totalSU);
                int corpseMainElementId = corpse.GetInt(CorpseMainElementIntId);

                // Create the servant at calculated level
                var servant = CharaGen.Create(sourceId, level);
                if (servant == null)
                    return null;
                servant.SetLv(level);
                TryRestoreMainElement(servant, corpseMainElementId);

                // 1. Place in the zone
                EClass._zone.AddCard(servant, NecroSpellUtil.GetSpawnPos());

                // 2. Register as home member
                if (EClass.pc.homeBranch != null)
                    EClass.pc.homeBranch.AddMemeber(servant);
                else
                {
                    servant.SetGlobal();
                    servant.SetFaction(EClass.Home);
                }

                // 3. Set ally attributes
                servant.hostility = Hostility.Ally;
                servant.c_originalHostility = Hostility.Ally;
                servant.orgPos = null;
                servant.c_summonDuration = 0;
                servant.isSummon = false;

                // 4. MakeMinion sets c_uidMaster
                servant.MakeMinion(EClass.pc);

                // 5. Ensure permanent
                servant.isSummon = false;
                servant.c_summonDuration = 0;

                // 6. Apply undead servant trait
                servant.c_idTrait = "Elin_ArsMoriendi.TraitUndeadServant";
                servant.ApplyTrait();

                // 7. Track the servant
                AddServant(servant);

                // 8. Store resurrection level for enhancement system
                var enhData = GetEnhancement(servant.uid);
                enhData.ResurrectionLevel = level;
                SaveEnhancement(servant.uid, enhData);

                // 9. Refresh
                servant.Refresh();
                EClass.pc.Refresh();

                // Consume corpse
                if (corpse.Num > 1)
                    corpse.ModNum(-1);
                else
                    corpse.Destroy();

                // Consume souls
                ConsumeSouls(soulAmounts);

                ModLog.Log("Ritual complete: {0} (uid={1}) raised at Lv{2} (SU={3})",
                    servant.Name, servant.uid, level, totalSU);

                KnightEncounter.TrySpawnScouts();

                return servant;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Ritual failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns the victim's main element ID for corpse metadata, or 0 if none.
        /// </summary>
        public static int GetMainElementId(Chara? chara)
        {
            if (chara == null) return 0;
            try
            {
                var main = chara.MainElement;
                if (main == null || main == Element.Void) return 0;
                return main.id;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Restores main element from corpse metadata. Missing/invalid metadata is ignored.
        /// </summary>
        private static void TryRestoreMainElement(Chara servant, int mainElementId)
        {
            if (servant == null || mainElementId <= 0) return;
            if (EClass.sources?.elements?.map?.TryGetValue(mainElementId) == null) return;
            if (!SafeInvoke.TrySetMainElement(servant, mainElementId))
                ModLog.Warn($"Main element restore skipped for {servant.Name}: compat invoke failed.");
        }

        /// <summary>Consume specified soul amounts from PC inventory.</summary>
        private void ConsumeSouls(Dictionary<string, int> soulAmounts)
        {
            foreach (var kvp in soulAmounts)
            {
                if (kvp.Value > 0)
                    ConsumeItems(EClass.pc, kvp.Key, kvp.Value);
            }
        }

        public void ReleaseServant(Chara servant)
        {
            if (servant == null)
                return;

            // Best-effort cleanup for any loop/attached FX before card teardown.
            CustomAssetFx.StopAllAttachedFx(servant);
            RemoveServant(servant.uid);

            if (!SafeInvoke.TryDetachMinion(servant))
                ModLog.Warn($"ReleaseServant: detach minion failed for {servant.Name} (uid={servant.uid})");

            if (EClass.pc?.party?.members?.Contains(servant) == true)
                EClass.pc.party.RemoveMember(servant);

            if (servant.homeBranch != null)
                servant.homeBranch.RemoveMemeber(servant);

            servant.RemoveGlobal();
            if (!servant.isDestroyed)
                servant.Destroy();
            if (!servant.isDestroyed)
                ModLog.Warn($"ReleaseServant: failed to destroy {servant.Name} (uid={servant.uid})");

            EClass.pc?.Refresh();

            ModLog.Log("Servant released: {0} (uid={1})", servant.Name, servant.uid);
        }

        public static bool SafeDie(Chara chara, Card? origin = null, AttackSource attackSource = AttackSource.None, Chara? originalTarget = null)
        {
            return SafeInvoke.TryDie(chara, origin, attackSource, originalTarget);
        }

        // ============================================================
        // Servant Enhancement System
        // ============================================================

        private void SaveEnhancement(int uid, ServantEnhancement e)
        {
            var p = EnhPrefix + uid + ".";
            SetFlag(p + "lvl", e.EnhancementLevel);
            SetFlag(p + "bp", e.AddedBodyParts);
            SetFlag(p + "rlv", e.ResurrectionLevel);
            SetFlag(p + "dorm", e.IsDormant ? 1 : 0);
            SetFlag(p + "stsh", e.IsStashed ? 1 : 0);
            SetFlag(p + "tac", TacticIdToIndex(e.TacticId));
            foreach (var kvp in e.SlotAdditions) SetFlag(p + "sl." + kvp.Key, kvp.Value);
            foreach (var kvp in e.AttrInjections) SetFlag(p + "ai." + kvp.Key, kvp.Value);
            foreach (var kvp in e.SlotResonance) SetFlag(p + "sr." + kvp.Key, kvp.Value);
        }

        private void SaveAllEnhancements()
        {
            RemoveFlagsWithPrefix(EnhPrefix);
            foreach (var kvp in _enhancements)
                SaveEnhancement(kvp.Key, kvp.Value);
        }

        public void SaveEnhancementsPublic() => SaveAllEnhancements();

        /// <summary>
        /// Returns whether the servant is currently in display mode (dormant).
        /// Non-servants always return false.
        /// </summary>
        public bool IsServantDormant(int uid)
        {
            EnsureGameStateLoaded();
            if (!_servantUidList.Contains(uid)) return false;
            return GetEnhancement(uid).IsDormant;
        }

        /// <summary>
        /// Returns whether servant is excluded from combat behavior (display mode or stashed).
        /// Non-servants always return false.
        /// </summary>
        public bool IsServantCombatInactive(int uid)
        {
            EnsureGameStateLoaded();
            if (!_servantUidList.Contains(uid)) return false;
            var enh = GetEnhancement(uid);
            return enh.IsDormant || enh.IsStashed;
        }

        /// <summary>
        /// Re-apply runtime state for one servant (movement/AI/minion type).
        /// No-op for non-servants.
        /// </summary>
        public void ApplyRuntimeStateForServant(Chara servant)
        {
            EnsureGameStateLoaded();
            if (servant == null || servant.isDestroyed) return;
            if (!_servantUidList.Contains(servant.uid)) return;

            var enh = GetEnhancement(servant.uid);
            ApplyServantRuntimeState(servant, enh);
        }

        /// <summary>
        /// Set display mode state for a servant and immediately apply runtime behavior.
        /// This is the single entry point for dormant on/off transitions.
        /// </summary>
        public void SetServantDormantState(Chara servant, bool isDormant)
        {
            EnsureGameStateLoaded();
            if (servant == null || servant.isDestroyed) return;
            if (!_servantUidList.Contains(servant.uid)) return;

            var enh = GetEnhancement(servant.uid);
            bool changed = enh.IsDormant != isDormant;
            enh.IsDormant = isDormant;
            if (changed)
                SaveEnhancement(servant.uid, enh);

            ApplyServantRuntimeState(servant, enh);
        }

        /// <summary>
        /// Re-apply runtime behavior (movement/AI) for all tracked servants.
        /// Called on zone activation to recover from serialized stale runtime flags.
        /// </summary>
        public void ReconcileServantRuntimeStates()
        {
            EnsureGameStateLoaded();
            PruneOrphanedServants();
            int active = 0;
            int dormant = 0;
            int stashed = 0;

            foreach (var uid in _servantUidList)
            {
                Chara? chara = null;
                EClass.game?.cards?.globalCharas?.TryGetValue(uid, out chara);
                if (chara == null || chara.isDestroyed) continue;

                var enh = GetEnhancement(uid);
                ApplyServantRuntimeState(chara, enh);
                if (enh.IsStashed) stashed++;
                else if (enh.IsDormant) dormant++;
                else active++;
            }

            if (active > 0 || dormant > 0 || stashed > 0)
                ModLog.Log("ReconcileServantRuntimeStates: active={0}, dormant={1}, stashed={2}", active, dormant, stashed);
        }

        private static void ApplyServantRuntimeState(Chara servant, ServantEnhancement enh)
        {
            if (servant == null || servant.isDestroyed || servant.isDead) return;
            bool isPcMaster = EClass.pc != null && servant.c_uidMaster == EClass.pc.uid;
            if (enh.IsStashed)
            {
                if (isPcMaster && servant.c_minionType != MinionType.Friend)
                    servant.c_minionType = MinionType.Friend;

                servant.noMove = true;
                servant.enemy = null;
                if (!servant.HasNoGoal)
                    servant.SetAI(new NoGoal());
                CustomAssetFx.StopAllAttachedFx(servant);
                if (!TryMoveServantToHomeStashZone(servant))
                    ModLog.Warn($"ApplyServantRuntimeState(stashed) move-home failed: {servant.Name} ({servant.uid})");
                return;
            }

            if (enh.IsDormant)
            {
                // Exclude dormant servants from zone-follow transfer list.
                // Vanilla only transfers MinionType.Default on zone move.
                if (isPcMaster && servant.c_minionType != MinionType.Friend)
                    servant.c_minionType = MinionType.Friend;

                servant.noMove = true;
                servant.enemy = null;
                if (!servant.HasNoGoal)
                    servant.SetAI(new NoGoal());
                return;
            }

            // Active servants must travel with the player as normal minions.
            if (isPcMaster && servant.c_minionType != MinionType.Default)
                servant.c_minionType = MinionType.Default;

            bool wasNoMove = servant.noMove;
            servant.noMove = false;

            // If the servant was previously forced into NoGoal/noMove, request a fresh goal.
            // This fixes cases where non-dormant servants remain idle after zone transitions.
            if (servant.currentZone == EClass._zone && (wasNoMove || servant.HasNoGoal))
                servant.ChooseNewGoal();
        }

        public void RestoreDormantStates()
        {
            // Backward-compatible wrapper for older call sites.
            ReconcileServantRuntimeStates();
        }

        /// <summary>Get or create enhancement data for a servant.</summary>
        public ServantEnhancement GetEnhancement(int uid)
        {
            EnsureGameStateLoaded();
            if (!_enhancements.TryGetValue(uid, out var enh))
            {
                enh = new ServantEnhancement();
                _enhancements[uid] = enh;
            }
            return enh;
        }

        // ── Attribute Injection ──

        /// <summary>
        /// Inject a soul into a servant to boost an attribute.
        /// Returns the actual attribute boost applied.
        /// </summary>
        public int InjectAttribute(Chara servant, int attrId, string soulId, int soulCount, double multiplier = 1.0)
        {
            if (!SoulUnitTable.TryGetValue(soulId, out int suPerUnit)) return 0;
            soulCount = Math.Max(0, Math.Min(soulCount, MaxSoulsPerInjection));
            if (soulCount <= 0) return 0;

            var enh = GetEnhancement(servant.uid);
            enh.AttrInjections.TryGetValue(attrId, out int injectionCount);

            int boost = NecromancyCalculations.CalculateInjectionBoost(suPerUnit, soulCount, injectionCount, multiplier);

            // Apply to servant
            servant.elements.ModBase(attrId, boost);

            // Update tracking.
            // Progress cost scales with consumed souls to prevent one-shot front-loading
            // from being strictly optimal.
            enh.AttrInjections[attrId] = injectionCount + soulCount;
            enh.EnhancementLevel += soulCount;
            SaveEnhancement(servant.uid, enh);

            // Consume souls
            ConsumeItems(EClass.pc, soulId, soulCount);

            double efficiency = NecromancyCalculations.CalculateInjectionEfficiency(injectionCount, soulCount);
            ModLog.Log("Attr injection: {0} +{1} to attr {2} (efficiency={3:P0}, totalInject={4})",
                servant.Name, boost, attrId, efficiency, injectionCount + soulCount);
            return boost;
        }

        /// <summary>Get the current injection count for an attribute on a servant.</summary>
        public int GetAttrInjectionCount(int servantUid, int attrId)
        {
            if (!_enhancements.TryGetValue(servantUid, out var enh)) return 0;
            int count;
            enh.AttrInjections.TryGetValue(attrId, out count);
            return count;
        }

        /// <summary>Get efficiency for next injection (0.0 ~ 1.0).</summary>
        public double GetNextInjectionEfficiency(int servantUid, int attrId)
        {
            int count = GetAttrInjectionCount(servantUid, attrId);
            return NecromancyCalculations.CalculateInjectionEfficiency(count, 1);
        }

        // ── Body Augmentation ──

        /// <summary>
        /// Attempt body augmentation: consume corpses to try adding a body part.
        /// Returns true on success.
        /// </summary>
        public bool AugmentBodyPart(Chara servant, int slotId, List<Thing> materialCorpses)
        {
            if (materialCorpses.Count == 0) return false;

            var slot = Array.Find(AugmentableSlots, s => s.SlotId == slotId);
            if (slot == null) return false;

            // Check total augmentation limit
            var enh = GetEnhancement(servant.uid);
            if (enh.AddedBodyParts >= MaxTotalAddedParts)
            {
                ModLog.Log("Augment failed: {0} at total limit ({1}/{2})", servant.Name, enh.AddedBodyParts, MaxTotalAddedParts);
                return false;
            }
            enh.SlotAdditions.TryGetValue(slotId, out int currentAdded);

            // Validate material corpses have the required body part in their race.figure
            int validCount = 0;
            foreach (var corpse in materialCorpses)
            {
                if (CorpseHasBodyPart(corpse, slot.FigureName))
                    validCount++;
            }
            if (validCount == 0) return false;

            // Calculate success rate with pity timer (resonance)
            enh.SlotResonance.TryGetValue(slotId, out int resonance);
            double successRate = AugmentBaseRate * (1.0 + (validCount - 1) * 0.5) + resonance * 0.05;
            successRate = Math.Min(successRate, 0.95);

            // Consume all material corpses
            foreach (var corpse in materialCorpses)
            {
                if (corpse.Num > 1)
                    corpse.ModNum(-1);
                else
                    corpse.Destroy();
            }

            // Roll for success
            bool success = UnityEngine.Random.value < successRate;

            if (success)
            {
                servant.body.AddBodyPart(slotId);
                servant.body.RefreshBodyParts();

                enh.SlotAdditions[slotId] = currentAdded + 1;
                enh.AddedBodyParts++;
                enh.EnhancementLevel += 2;
                enh.SlotResonance[slotId] = 0; // Reset resonance on success
                SaveEnhancement(servant.uid, enh);

                ModLog.Log("Augment success: {0} gained slot {1} (rate={2:P0}, corpses={3}, resonance={4})",
                    servant.Name, slotId, successRate, validCount, resonance);
            }
            else
            {
                enh.SlotResonance[slotId] = resonance + 1; // Increment resonance on failure
                SaveEnhancement(servant.uid, enh);

                ModLog.Log("Augment failed: {0} slot {1} (rate={2:P0}, corpses={3}, resonance={4}->{5})",
                    servant.Name, slotId, successRate, validCount, resonance, resonance + 1);
            }

            return success;
        }

        /// <summary>Check if a corpse's source race has a body part in its figure string.</summary>
        public static bool CorpseHasBodyPart(Thing corpse, string figureName)
        {
            var sourceId = corpse.c_idRefCard;
            if (string.IsNullOrEmpty(sourceId)) return false;

            if (!EClass.sources.charas.map.TryGetValue(sourceId, out var charaRow))
                return false;

            var raceId = charaRow.race;
            if (string.IsNullOrEmpty(raceId)) return false;

            if (!EClass.sources.races.map.TryGetValue(raceId, out var raceRow))
                return false;

            if (string.IsNullOrEmpty(raceRow.figure)) return false;

            var parts = raceRow.figure.Split('|');
            return parts.Contains(figureName);
        }

        /// <summary>Get how many of a specific slot have been added to a servant.</summary>
        public int GetSlotAdditions(int servantUid, int slotId)
        {
            if (!_enhancements.TryGetValue(servantUid, out var enh)) return 0;
            int count;
            enh.SlotAdditions.TryGetValue(slotId, out count);
            return count;
        }

        /// <summary>Count how many of a specific body slot a servant currently has.</summary>
        public static int CountBodySlots(Chara chara, int slotId)
        {
            int count = 0;
            foreach (var slot in chara.body.slots)
            {
                if (slot.elementId == slotId) count++;
            }
            return count;
        }

        /// <summary>Calculate augmentation success rate for given corpse count.</summary>
        public static double CalculateAugmentRate(int corpseCount, int resonance = 0)
            => NecromancyCalculations.CalculateAugmentRate(corpseCount, resonance);

        /// <summary>Get the resonance (pity timer) for a specific slot on a servant.</summary>
        public int GetSlotResonance(int servantUid, int slotId)
        {
            if (!_enhancements.TryGetValue(servantUid, out var enh)) return 0;
            enh.SlotResonance.TryGetValue(slotId, out int resonance);
            return resonance;
        }

        // ============================================================
        // Rampage System
        // ============================================================

        /// <summary>
        /// Calculate rampage threshold from PC's MAG and kill count.
        /// Delegates to NecromancyCalculations for testability.
        /// </summary>
        public static int GetRampageThreshold()
        {
            int mag = EClass.pc?.MAG ?? 0;
            int kills = EClass.player?.stats?.kills ?? 0;
            return NecromancyCalculations.CalculateRampageThreshold(mag, kills);
        }

        /// <summary>
        /// Calculate rampage chance (0-90%) based on enhancement level vs threshold.
        /// Delegates to NecromancyCalculations for testability.
        /// </summary>
        public int GetRampageChance(int servantUid, int pendingEnhancement = 0)
        {
            var enh = GetEnhancement(servantUid);
            int threshold = GetRampageThreshold();
            int projectedLevel = enh.EnhancementLevel + Math.Max(0, pendingEnhancement);
            return NecromancyCalculations.CalculateRampageChance(projectedLevel, threshold);
        }

        /// <summary>
        /// Check for rampage after enhancement. Returns result type or null if safe.
        /// </summary>
        public RampageResult? CheckRampage(Chara servant, int pendingEnhancement = 0)
        {
            int chance = GetRampageChance(servant.uid, pendingEnhancement);
            if (chance <= 0) return null;

            int roll = UnityEngine.Random.Range(0, 100);
            if (roll >= chance) return null;

            // Rampage triggered - determine result (50% positive, 35% berserk, 15% self-destruct)
            int resultRoll = UnityEngine.Random.Range(1, 101);
            if (resultRoll <= 35) return RampageResult.DarkAwakening;      // ★ 35%
            if (resultRoll <= 70) return RampageResult.Berserk;            // △ 35%
            if (resultRoll <= 85) return RampageResult.SelfDestruct;       // × 15%
            return RampageResult.MutationAwakening;                        // ★ 15%
        }

        /// <summary>
        /// Execute a rampage result on a servant.
        /// </summary>
        public void ExecuteRampage(Chara servant, RampageResult result)
        {
            ModLog.Warn($"Rampage on {servant.Name}: {result}");

            switch (result)
            {
                case RampageResult.DarkAwakening:
                    // ★ Enhancement effect doubled (multiplier applied by caller) + "Grace of Death" buff
                    // Grant a small permanent MAG bonus as "屍気の恩寵"
                    servant.elements.ModBase(76, 5); // +5 MAG
                    break;

                case RampageResult.Berserk:
                    // △ Temporary berserk: hostile + ConBerserk, recovers naturally
                    servant.hostility = Hostility.Enemy;
                    servant.AddCondition<ConBerserk>(300, true);
                    break;

                case RampageResult.SelfDestruct:
                    // × AoE explosion + death
                    RemoveServant(servant.uid);
                    try
                    {
                        ActEffect.ProcAt(EffectId.Suicide, servant.LV * 5, BlessedState.Normal,
                            servant, null, servant.pos, false);
                    }
                    catch (Exception ex)
                    {
                        ModLog.Warn($"SelfDestruct effect error: {ex.Message}");
                        SafeDie(servant);
                    }
                    break;

                case RampageResult.MutationAwakening:
                    // ★ Strong random body part + temporary berserk
                    servant.AddRandomBodyPart(true);
                    servant.AddCondition<ConBerserk>(200, true);
                    var enh = GetEnhancement(servant.uid);
                    enh.AddedBodyParts++;
                    enh.EnhancementLevel += 2;
                    SaveEnhancement(servant.uid, enh);
                    break;
            }
        }

        // ============================================================
        // Apotheosis Ritual Materials (6-slot system)
        // ============================================================

        public const int RitualSlotCount = 6;

        /// <summary>Required quantities for each ritual slot.</summary>
        public static readonly int[] RitualRequired = { 99, 99, 3, 10, 3, 3 };

        /// <summary>LangHelper keys for slot names.</summary>
        public static readonly string[] RitualSlotNameKeys =
        {
            "ritualSlotSoul", "ritualSlotHeart", "ritualSlotBlood",
            "ritualSlotPoison", "ritualSlotMercury", "ritualSlotCursedDew",
        };

        /// <summary>LangHelper keys for Erenos comments on each slot.</summary>
        public static readonly string[] RitualSlotCommentKeys =
        {
            "ritualCommentSoul", "ritualCommentHeart", "ritualCommentBlood",
            "ritualCommentPoison", "ritualCommentMercury", "ritualCommentCursedDew",
        };

        /// <summary>Count available materials for a ritual slot index.</summary>
        public int CountRitualMaterial(int slot)
        {
            var pc = EClass.pc;
            if (pc == null) return 0;

            return slot switch
            {
                0 => CountItemsInInventory(pc, "ars_soul_legendary"),
                1 => CountItemsInInventory(pc, "heart"),
                2 => CountItemsInInventory(pc, "blood_angel"),
                3 => CountItemsInInventory(pc, "336"),
                4 => CountItemsInInventory(pc, "mercury"),
                5 => CountCursedById(pc, "1081"),
                _ => 0,
            };
        }

        /// <summary>Check if all 6 ritual materials are satisfied.</summary>
        public bool HasAllRitualMaterials()
        {
            for (int i = 0; i < RitualSlotCount; i++)
            {
                if (CountRitualMaterial(i) < RitualRequired[i])
                    return false;
            }
            return true;
        }

        /// <summary>Consume all 6 ritual material types.</summary>
        public void ConsumeRitualMaterials()
        {
            var pc = EClass.pc;
            if (pc == null) return;

            ConsumeItems(pc, "ars_soul_legendary", 99);
            ConsumeItems(pc, "heart", 99);
            ConsumeItems(pc, "blood_angel", 3);
            ConsumeItems(pc, "336", 10);
            ConsumeItems(pc, "mercury", 3);
            ConsumeCursedById(pc, "1081", 3);
        }

        private static int CountCursedById(Chara pc, string id)
        {
            int count = 0;
            foreach (var thing in pc.things.List(
                t => t.id == id && t.IsCursed && !t.isEquipped, onlyAccessible: true))
                count += thing.Num;
            return count;
        }

        private void ConsumeCursedById(Chara pc, string id, int amount)
        {
            int remaining = amount;
            foreach (var thing in pc.things.List(
                t => t.id == id && t.IsCursed && !t.isEquipped, onlyAccessible: true))
            {
                if (remaining <= 0) break;
                int take = Math.Min(remaining, thing.Num);
                if (take >= thing.Num) thing.Destroy();
                else thing.ModNum(-take);
                remaining -= take;
            }
        }

        // ============================================================
        // Inventory Helpers
        // ============================================================

        public int CountItemsInInventory(Chara chara, string itemId)
        {
            return chara.things.GetThingStack(itemId).count;
        }

        public void ConsumeItems(Chara chara, string itemId, int amount)
        {
            int remaining = amount;
            var stack = chara.things.GetThingStack(itemId);

            foreach (var thing in stack.list)
            {
                if (remaining <= 0) break;

                if (thing.Num <= remaining)
                {
                    remaining -= thing.Num;
                    thing.Destroy();
                }
                else
                {
                    thing.SetNum(thing.Num - remaining);
                    remaining = 0;
                }
            }
        }

        public List<Thing> FindCorpses()
        {
            return new List<Thing>(EClass.pc.things.List(
                t => t.trait is TraitFoodMeat && !string.IsNullOrEmpty(t.c_idRefCard),
                onlyAccessible: true));
        }

        public List<Thing> FindSouls()
        {
            return new List<Thing>(EClass.pc.things.List(
                t => SoulIds.Contains(t.id),
                onlyAccessible: true));
        }

        // ============================================================
        // Erenos Pet (Borrow System)
        // ============================================================

        private const string ErenosPetId = "ars_erenos_pet";

        public bool IsErenosBorrowed()
        {
            if (EClass.pc?.party?.members != null)
                foreach (var c in EClass.pc.party.members)
                    if (c != null && !c.isDead && c.id == ErenosPetId) return true;
            if (EClass.pc?.homeBranch?.members != null)
                foreach (var c in EClass.pc.homeBranch.members)
                    if (c != null && !c.isDead && c.id == ErenosPetId) return true;
            return false;
        }

        public void BorrowErenos()
        {
            var pc = EClass.pc;
            if (pc == null) return;
            if (IsErenosBorrowed()) return;

            var erenos = NpcPresenceUtil.ShowNearPlayer(ErenosPetId);
            if (erenos == null) return;

            if (pc.homeBranch != null)
            {
                if (!pc.homeBranch.members.Contains(erenos))
                    pc.homeBranch.AddMemeber(erenos);
            }

            if (pc.party?.members != null && !pc.party.members.Contains(erenos))
                pc.party.AddMemeber(erenos);
            erenos.homeZone = pc.homeBranch?.owner;

            ModLog.Log("Erenos borrowed as pet");
        }

        /// <summary>
        /// Ensure Erenos pet actor exists in "somewhere" for drama portrait resolution.
        /// Skips when already borrowed (on map / home party context).
        /// </summary>
        public void EnsureErenosPetSomewhere()
        {
            if (IsErenosBorrowed()) return;
            NpcPresenceUtil.EnsureInSomewhere(ErenosPetId);
        }

        /// <summary>
        /// Ensure Erenos pet exists in the current zone near the player for drama actor usage.
        /// If it exists elsewhere, move it; if missing, create it.
        /// </summary>
        public void EnsureErenosPetNearPlayerForDrama()
        {
            NpcPresenceUtil.ShowNearPlayer(ErenosPetId);
        }

        /// <summary>Get soul counts as a dictionary: soulId → total count (including containers).</summary>
        public Dictionary<string, int> FindSoulsByType()
        {
            var dict = new Dictionary<string, int>();
            foreach (var soulId in SoulIds)
            {
                int count = CountItemsInInventory(EClass.pc, soulId);
                if (count > 0)
                    dict[soulId] = count;
            }
            return dict;
        }

    }

    // ============================================================
    // Data Models
    // ============================================================

    public class ServantEnhancement
    {
        public int EnhancementLevel;
        public int AddedBodyParts;
        /// <summary>Resurrection level at time of creation (for enhancement system thresholds).</summary>
        public int ResurrectionLevel;
        /// <summary>slotId → number of additions for that slot type.</summary>
        public Dictionary<int, int> SlotAdditions = new();
        /// <summary>attrId → cumulative injected soul count for that attribute.</summary>
        public Dictionary<int, int> AttrInjections = new();
        /// <summary>slotId → resonance count (pity timer for augmentation).</summary>
        public Dictionary<int, int> SlotResonance = new();
        /// <summary>True = display mode (no movement, no actions).</summary>
        public bool IsDormant;
        /// <summary>True = parked at home zone and excluded from battlefield.</summary>
        public bool IsStashed;
        /// <summary>Tactics override. Empty = default, otherwise SourceTactics.Row.id.</summary>
        public string TacticId = "";
    }

    public class AugmentableSlot
    {
        public int SlotId;
        /// <summary>JP name used in race.figure (e.g. "腕", "手", "頭").</summary>
        public string FigureName;

        public AugmentableSlot(int slotId, string figureName)
        {
            SlotId = slotId;
            FigureName = figureName;
        }

        public string GetSlotName()
        {
            if (Lang.langCode == "CN")
            {
                return SlotId switch
                {
                    34 => "臂", 35 => "手", 30 => "头", 36 => "指",
                    39 => "足", 31 => "颈", 32 => "躯", 33 => "背", 37 => "腰",
                    _ => $"Slot{SlotId}",
                };
            }
            return SlotId switch
            {
                34 => Lang.isJP ? "腕" : "Arm",
                35 => Lang.isJP ? "手" : "Hand",
                30 => Lang.isJP ? "頭" : "Head",
                36 => Lang.isJP ? "指" : "Finger",
                39 => Lang.isJP ? "足" : "Foot",
                31 => Lang.isJP ? "首" : "Neck",
                32 => Lang.isJP ? "体" : "Body",
                33 => Lang.isJP ? "背" : "Back",
                37 => Lang.isJP ? "腰" : "Waist",
                _ => $"Slot{SlotId}",
            };
        }
    }

    public enum RampageResult
    {
        DarkAwakening,     // 1-35%: ★ enhancement doubled + "Grace of Death" buff
        Berserk,           // 36-70%: △ temporary hostile + ConBerserk
        SelfDestruct,      // 71-85%: × AoE explosion + death (recoverable)
        MutationAwakening, // 86-100%: ★ strong random body part + berserk
    }

    public class SpellUnlock
    {
        public string Alias { get; }
        public int ElementId =>
            EClass.sources?.elements?.alias != null
            && EClass.sources.elements.alias.TryGetValue(Alias, out var row)
                ? row.id : -1;
        public string RequiredSoulId { get; }
        public int RequiredSoulCount { get; }
        public string NameJP { get; }
        public string NameEN { get; }
        public string DescJP { get; }
        public string DescEN { get; }
        public string? NameCN { get; }
        public string? DescCN { get; }
        public bool InitiallyUnlocked { get; }

        public SpellUnlock(string alias, string requiredSoulId, int requiredSoulCount,
            string nameJP, string nameEN, string descJP, string descEN,
            bool initiallyUnlocked = false,
            string? nameCN = null, string? descCN = null)
        {
            Alias = alias;
            RequiredSoulId = requiredSoulId;
            RequiredSoulCount = requiredSoulCount;
            NameJP = nameJP;
            NameEN = nameEN;
            DescJP = descJP;
            DescEN = descEN;
            NameCN = nameCN;
            DescCN = descCN;
            InitiallyUnlocked = initiallyUnlocked;
        }

        public string GetName() =>
            Lang.langCode == "CN" ? (NameCN ?? NameEN) : Lang.isJP ? NameJP : NameEN;
        public string GetDesc() =>
            Lang.langCode == "CN" ? (DescCN ?? DescEN) : Lang.isJP ? DescJP : DescEN;

        public string GetSoulName() => LangHelper.GetSoulName(RequiredSoulId);
    }
}
