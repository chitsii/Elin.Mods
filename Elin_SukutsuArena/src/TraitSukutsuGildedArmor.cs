using HarmonyLib;
using UnityEngine;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Localization;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// 虚飾の黄金鎧 - HPダメージの代わりに所持金を消費
    /// </summary>
    [GameDependency("Inheritance", "Trait", "Medium", "Trait base class may change")]
    public class TraitSukutsuGildedArmor : Trait
    {
        /// <summary>
        /// 1HPダメージあたりの消費ゴールド
        /// </summary>
        public const int GoldCostPerHp = 10;

        /// <summary>
        /// キャラクターが虚飾の黄金鎧を装備しているか確認
        /// </summary>
        public static bool IsWearingGildedArmor(Chara c)
        {
            if (c == null || c.body == null) return false;

            foreach (var slot in c.body.slots)
            {
                if (slot.thing != null && slot.thing.id == ArenaConfig.ItemIds.GildedArmor)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 所持金からダメージを吸収し、吸収できた量を返す
        /// </summary>
        public static long AbsorbDamageWithGold(Chara c, long damage)
        {
            if (!c.IsPC) return 0; // PCのみ適用

            // Card.GetCurrency を使用して所持金を取得
            int currentGold = c.GetCurrency("money");
            if (currentGold <= 0) return 0;

            // 吸収可能な最大ダメージ（所持金 ÷ GoldCostPerHp）
            // 例：1000ゴールドで10HPまで吸収可能
            long maxAbsorb = currentGold / GoldCostPerHp;

            // 100ゴールド未満の場合は吸収できない（ゴールドも消費しない）
            if (maxAbsorb == 0)
            {
                ModLog.Log($"[SukutsuArena] Gilded Armor: insufficient gold ({currentGold}), need at least {GoldCostPerHp}");
                return 0;
            }

            long actualAbsorb = System.Math.Min(damage, maxAbsorb);

            if (actualAbsorb > 0)
            {
                // 所持金を消費（Card.ModCurrency を使用）
                int goldCost = (int)(actualAbsorb * GoldCostPerHp);
                c.ModCurrency(-goldCost, "money");

                // 金貨が剥がれ落ちる演出
                PlayGoldEffect(c);

                ModLog.Log($"[SukutsuArena] Gilded Armor absorbed {actualAbsorb} damage, cost {goldCost} gold (remaining: {currentGold - goldCost})");
            }

            return actualAbsorb;
        }

        /// <summary>
        /// 金貨が剥がれ落ちる演出（nullチェック付き）
        /// </summary>
        private static void PlayGoldEffect(Chara c)
        {
            try
            {
                // posがnullの場合はスキップ
                if (c == null || c.pos == null || !c.pos.IsValid) return;

                c.PlaySound("pay");
                c.PlayEffect("identify");  // 金色の光
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SukutsuArena] PlayGoldEffect failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Card.DamageHP のパッチ - 虚飾の黄金鎧の効果を適用
    /// </summary>
    [GameDependency("Patch", "Card.DamageHP", "High", "Method signature may change")]
    [HarmonyPatch(typeof(Card), nameof(Card.DamageHP), new[] { typeof(long), typeof(int), typeof(int), typeof(AttackSource), typeof(Card), typeof(bool), typeof(Thing), typeof(Chara) })]
    public static class Patch_Card_DamageHP_GildedArmor
    {
        /// <summary>
        /// ダメージ計算前に所持金で吸収
        /// </summary>
        [HarmonyPriority(Priority.Low)]
        static void Prefix(Card __instance, ref long dmg, int ele, int eleP, AttackSource attackSource)
        {
            // キャラクターでない場合はスキップ
            if (!__instance.isChara) return;

            Chara c = __instance.Chara;
            if (c == null || !c.IsPC) return;

            // 虚飾の黄金鎧を装備しているか確認
            if (!TraitSukutsuGildedArmor.IsWearingGildedArmor(c)) return;

            ModLog.Log($"[SukutsuArena] Gilded Armor: incoming damage={dmg}, ele={ele}");

            // 全てのダメージをお金で吸収（元素ダメージも含む）
            long absorbed = TraitSukutsuGildedArmor.AbsorbDamageWithGold(c, dmg);
            ModLog.Log($"[SukutsuArena] Gilded Armor: absorbed={absorbed}");

            if (absorbed > 0)
            {
                dmg -= absorbed;
                if (dmg < 0) dmg = 0;

                // 消費したゴールド量
                long goldSpent = absorbed * TraitSukutsuGildedArmor.GoldCostPerHp;
                ModLog.Log($"[SukutsuArena] Gilded Armor: remaining damage={dmg}, gold spent={goldSpent}");

                // メッセージ表示（金貨が剥がれ落ちた）
                Msg.SetColor(Msg.colors.Ding);
                Msg.Say(ArenaLocalization.GildedArmorGoldShattered(goldSpent));
            }
        }
    }
}

