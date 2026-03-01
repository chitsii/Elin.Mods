using UnityEngine;

using Elin_SukutsuArena.Attributes;

namespace Elin_SukutsuArena.Conditions
{
    /// <summary>
    /// 禁断の覚醒剤のブースト効果（elementsはExcel側で設定）
    /// </summary>
    [GameDependency("Inheritance", "Condition", "Medium", "Condition base class may change")]
    public class ConSukutsuBoost : Condition
    {
        public override ConditionType Type => ConditionType.Buff;

        public override int GetPhase() => 0;

        public override void OnStart()
        {
            if (owner != null)
            {
                owner.Talk("awaken");
            }
            owner.PlaySound("boost2");
            ModLog.Log($"[SukutsuArena] ConSukutsuBoost started on {owner.Name}, value={value}");
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

