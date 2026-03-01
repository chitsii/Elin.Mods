using UnityEngine;

using Elin_SukutsuArena.Attributes;

namespace Elin_SukutsuArena.Conditions
{
    /// <summary>
    /// アイリスの感覚訓練バフ：視認範囲+4
    /// farsee(490)エレメントを付与
    /// </summary>
    [GameDependency("Inheritance", "Condition", "Medium", "Condition base class may change")]
    public class ConIrisSixthSense : Condition
    {
        private const int ELE_FARSEE = 490;
        private const int BUFF_VALUE = 4;

        public override ConditionType Type => ConditionType.Buff;

        public override int GetPhase() => 0;

        public override void OnStart()
        {
            owner.elements.ModBase(ELE_FARSEE, BUFF_VALUE);
            ModLog.Log($"[SukutsuArena] ConIrisSixthSense started on {owner.Name}, farsee +{BUFF_VALUE}");
        }

        public override void OnRemoved()
        {
            owner.elements.ModBase(ELE_FARSEE, -BUFF_VALUE);
            ModLog.Log($"[SukutsuArena] ConIrisSixthSense removed from {owner.Name}");
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

