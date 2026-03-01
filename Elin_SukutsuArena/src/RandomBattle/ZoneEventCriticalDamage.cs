using UnityEngine;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Localization;

namespace Elin_SukutsuArena.RandomBattle
{
    /// <summary>
    /// 臨死の闘技場ギミック: 耐性なしで受けるダメージ2倍
    /// </summary>
    [GameDependency("Inheritance", "ZoneEvent", "Medium", "Zone event base class may change")]
    public class ZoneEventCriticalDamage : ZoneEvent
    {
        private bool announced = false;

        public override void OnTick()
        {
            if (!announced)
            {
                Msg.Say(ArenaLocalization.GimmickCriticalDamageStart);
                announced = true;
            }
            // 実際のダメージ2倍処理はHarmonyパッチで実装
        }

        /// <summary>
        /// 臨死の闘技場が有効かチェック
        /// </summary>
        public static bool IsCriticalDamageActive()
        {
            if (EClass._zone?.events == null) return false;
            return EClass._zone.events.GetEvent<ZoneEventCriticalDamage>() != null;
        }
    }
}
