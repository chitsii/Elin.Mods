using UnityEngine;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Localization;

namespace Elin_SukutsuArena.RandomBattle
{
    /// <summary>
    /// 禁忌の癒しギミック: 回復アイテム・回復魔法使用不可
    /// </summary>
    [GameDependency("Inheritance", "ZoneEvent", "Medium", "Zone event base class may change")]
    public class ZoneEventNoHealing : ZoneEvent
    {
        private bool announced = false;

        public override void OnTick()
        {
            if (!announced)
            {
                Msg.Say(ArenaLocalization.GimmickNoHealingStart);
                announced = true;
            }
            // 実際の回復禁止処理はHarmonyパッチで実装が必要
            // または、UseItemやCast時にチェックするロジックを追加
        }

        /// <summary>
        /// 回復が禁止されているかチェック
        /// </summary>
        public static bool IsHealingBlocked()
        {
            if (EClass._zone?.events == null) return false;
            return EClass._zone.events.GetEvent<ZoneEventNoHealing>() != null;
        }
    }
}
