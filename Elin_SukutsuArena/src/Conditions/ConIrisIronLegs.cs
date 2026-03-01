using UnityEngine;

using Elin_SukutsuArena.Attributes;

namespace Elin_SukutsuArena.Conditions
{
    /// <summary>
    /// アイリスの足腰訓練バフ：完全回避率+20%
    /// evasionPerfect(57)エレメントを付与
    /// </summary>
    [GameDependency("Inheritance", "Condition", "Medium", "Condition base class may change")]
    public class ConIrisIronLegs : Condition
    {
        private const int ELE_EVASION_PERFECT = 57;
        private const int BUFF_VALUE = 20;

        public override ConditionType Type => ConditionType.Buff;

        public override int GetPhase() => 0;

        public override void OnStart()
        {
            owner.elements.ModBase(ELE_EVASION_PERFECT, BUFF_VALUE);
            ModLog.Log($"[SukutsuArena] ConIrisIronLegs started on {owner.Name}, evasionPerfect +{BUFF_VALUE}");
        }

        public override void OnRemoved()
        {
            owner.elements.ModBase(ELE_EVASION_PERFECT, -BUFF_VALUE);
            ModLog.Log($"[SukutsuArena] ConIrisIronLegs removed from {owner.Name}");
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

