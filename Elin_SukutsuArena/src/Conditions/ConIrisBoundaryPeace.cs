using UnityEngine;

using Elin_SukutsuArena.Attributes;

namespace Elin_SukutsuArena.Conditions
{
    /// <summary>
    /// アイリスの足湯リカバリーバフ：スタミナ自然回復+1/ターン
    /// OnTickでスタミナを回復
    /// </summary>
    [GameDependency("Inheritance", "Condition", "Medium", "Condition base class may change")]
    public class ConIrisBoundaryPeace : Condition
    {
        private const int REGEN_VALUE = 1;

        public override ConditionType Type => ConditionType.Buff;

        public override int GetPhase() => 0;

        public override void OnStart()
        {
            ModLog.Log($"[SukutsuArena] ConIrisBoundaryPeace started on {owner.Name}, stamina regen +{REGEN_VALUE}/turn");
        }

        public override void Tick()
        {
            // 基底クラスのTick（残りターン減少処理）を呼び出す
            base.Tick();

            // 毎ターンスタミナを回復
            if (owner.stamina.value < owner.stamina.max)
            {
                owner.stamina.Mod(REGEN_VALUE);
            }
        }

        public override void PlayEffect()
        {
            if (!Condition.ignoreEffect)
            {
                owner.PlaySound("buff");
                owner.PlayEffect("buff");
            }
        }
    }
}

