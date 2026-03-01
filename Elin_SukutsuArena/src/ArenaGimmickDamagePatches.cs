using HarmonyLib;
using UnityEngine;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.RandomBattle;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// ダメージ系ギミックのHarmonyパッチ
    /// - MagicAffinity: 物理ダメージ0、魔法ダメージ2倍
    /// - AntiMagic: 魔法ダメージ50%軽減
    /// - CriticalDamage: 物理ダメージ2倍
    /// </summary>
    [GameDependency("Patch", "Card.DamageHP", "High", "Method signature may change")]
    [HarmonyPatch(typeof(Card), nameof(Card.DamageHP), new[] { typeof(long), typeof(int), typeof(int), typeof(AttackSource), typeof(Card), typeof(bool), typeof(Thing), typeof(Chara) })]
    public static class ArenaGimmickDamagePatches
    {
        /// <summary>
        /// ダメージ計算前にギミック効果を適用
        /// </summary>
        [HarmonyPriority(Priority.High)]
        static void Prefix(Card __instance, ref long dmg, int ele)
        {
            // アリーナ戦闘中のみ適用
            if (!(EClass._zone?.instance is ZoneInstanceArenaBattle))
            {
                return;
            }

            // ele == 0 または ele == 926 が物理ダメージ
            bool isPhysical = (ele == 0 || ele == 926);
            long originalDmg = dmg;

            // 魔法親和: 物理0、魔法2倍（排他的ギミック）
            if (ZoneEventMagicAffinity.IsMagicAffinityActive())
            {
                if (isPhysical)
                {
                    dmg = 0;
                    ModLog.Log($"[ArenaGimmick] MagicAffinity: Physical damage blocked ({originalDmg} -> 0)");
                }
                else
                {
                    dmg *= 2;
                    ModLog.Log($"[ArenaGimmick] MagicAffinity: Magic damage doubled ({originalDmg} -> {dmg})");
                }
                return; // MagicAffinityは他のギミックと重複しない
            }

            // 無法地帯: 魔法ダメージ50%軽減
            if (ZoneEventAntiMagic.IsAntiMagicActive() && !isPhysical)
            {
                long newDmg = dmg / 2;
                ModLog.Log($"[ArenaGimmick] AntiMagic: Magic damage halved ({dmg} -> {newDmg})");
                dmg = newDmg;
            }

            // 臨死の闘技場: 物理ダメージ2倍
            if (ZoneEventCriticalDamage.IsCriticalDamageActive() && isPhysical)
            {
                long newDmg = dmg * 2;
                ModLog.Log($"[ArenaGimmick] CriticalDamage: Physical damage doubled ({dmg} -> {newDmg})");
                dmg = newDmg;
            }
        }
    }
}

