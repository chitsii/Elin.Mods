using HarmonyLib;
using UnityEngine;
using System.Reflection;
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
    [HarmonyPatch]
    public static class ArenaGimmickHealingPatches
    {
        private static float lastMessageTime = 0f;
        private const float MESSAGE_COOLDOWN = 3f; // 3秒間は同じメッセージを表示しない

        static MethodBase TargetMethod()
        {
            return CardHealHpPatchTarget.Resolve(nameof(ArenaGimmickHealingPatches));
        }

        /// <summary>
        /// 回復前にギミック効果を適用
        /// </summary>
        static void Prefix(Card __instance, ref long a, HealSource origin)
        {
            long originalAmount = a;
            if (ArenaGimmickHealingRules.TryBlockHealing(
                EClass._zone?.instance is ZoneInstanceArenaBattle,
                ZoneEventNoHealing.IsHealingBlocked(),
                ref a))
            {
                ModLog.Log($"[ArenaGimmick] NoHealing: Blocked heal of {originalAmount} HP for {__instance.Name}");

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

