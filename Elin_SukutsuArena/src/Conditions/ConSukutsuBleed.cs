using UnityEngine;

using Elin_SukutsuArena.Attributes;

namespace Elin_SukutsuArena.Conditions
{
    /// <summary>
    /// 禁断の覚醒剤の出血効果（デメリット）
    /// バニラのConBleedと同様だが、持続時間とダメージをカスタマイズ可能
    /// </summary>
    [GameDependency("Inheritance", "Condition", "Medium", "Condition base class may change")]
    public class ConSukutsuBleed : Condition
    {
        public override ConditionType Type => ConditionType.Debuff;

        public override Emo2 EmoIcon => Emo2.bleeding;

        // source.elementsを無視
        public override bool UseElements => false;

        public override int GetPhase() => 0;

        public override void Tick()
        {
            // 毎ターン固定ダメージ（バニラより軽めに調整）
            int damage = 3 + EClass.rnd(5);
            owner.DamageHP(damage, AttackSource.Condition);
            owner.AddBlood();

            // 毎ターン持続時間を1減らす
            Mod(-1);
        }

        public override void OnStart()
        {
            ModLog.Log($"[SukutsuArena] ConSukutsuBleed started on {owner.Name}, value={value}");
        }

        public override void PlayEffect()
        {
            if (!Condition.ignoreEffect)
            {
                owner.PlaySound("curse");
                owner.PlayEffect("curse");
            }
        }
    }
}

