using HarmonyLib;
using UnityEngine;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Localization;
using Elin_SukutsuArena.RandomBattle;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// 回復禁止ギミックのHarmonyパッチ
    /// - NoHealing: 回復効果を無効化
    /// </summary>
    [GameDependency("Patch", "Card.HealHP", "High", "Method signature may change")]
    [HarmonyPatch(typeof(Card), nameof(Card.HealHP), new[] { typeof(int), typeof(HealSource) })]
    public static class ArenaGimmickHealingPatches
    {
        private static float lastMessageTime = 0f;
        private const float MESSAGE_COOLDOWN = 3f; // 3秒間は同じメッセージを表示しない

        /// <summary>
        /// 回復前にギミック効果を適用
        /// </summary>
        static void Prefix(Card __instance, ref int a, HealSource origin)
        {
            // アリーナ戦闘中のみ適用
            if (!(EClass._zone?.instance is ZoneInstanceArenaBattle))
            {
                return;
            }

            // 禁忌の癒しが有効でない場合はスキップ
            if (!ZoneEventNoHealing.IsHealingBlocked())
            {
                return;
            }

            // 回復量を0に設定
            if (a > 0)
            {
                ModLog.Log($"[ArenaGimmick] NoHealing: Blocked heal of {a} HP for {__instance.Name}");
                a = 0;

                // クールダウン中でなければメッセージ表示
                if (Time.time - lastMessageTime > MESSAGE_COOLDOWN)
                {
                    Msg.Say(ArenaLocalization.GimmickHealingBlocked);
                    lastMessageTime = Time.time;
                }
            }
        }
    }
}

