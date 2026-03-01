using UnityEngine;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Localization;

namespace Elin_SukutsuArena.RandomBattle
{
    /// <summary>
    /// 魔法親和ギミック: 物理ダメージ0、魔法ダメージ2倍
    /// </summary>
    [GameDependency("Inheritance", "ZoneEvent", "Medium", "Zone event base class may change")]
    public class ZoneEventMagicAffinity : ZoneEvent
    {
        private bool announced = false;

        public override void OnTick()
        {
            if (!announced)
            {
                Msg.Say(ArenaLocalization.GimmickMagicAffinityStart);
                announced = true;
            }
            // 実際のダメージ変更処理はHarmonyパッチで実装が必要
        }

        /// <summary>
        /// 魔法親和が有効かチェック
        /// </summary>
        public static bool IsMagicAffinityActive()
        {
            if (EClass._zone?.events == null) return false;
            return EClass._zone.events.GetEvent<ZoneEventMagicAffinity>() != null;
        }
    }
}
