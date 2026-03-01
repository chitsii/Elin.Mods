using UnityEngine;

using Elin_SukutsuArena.Attributes;

namespace Elin_SukutsuArena.Conditions
{
    /// <summary>
    /// 痛覚遮断薬の毒効果（デメリット）
    /// 自然回復阻害 + 時々吐く
    /// </summary>
    [GameDependency("Inheritance", "Condition", "Medium", "Condition base class may change")]
    public class ConSukutsuPoison : Condition
    {
        public override ConditionType Type => ConditionType.Debuff;

        public override Emo2 EmoIcon => Emo2.poison;

        /// <summary>
        /// 自然回復を阻止
        /// </summary>
        public override bool PreventRegen => true;

        public override int GetPhase() => 0;

        public override void Tick()
        {
            // 時々吐く（20分の1の確率）
            if (EClass.rnd(20) == 0)
            {
                owner.Vomit();
            }

            // 毎ターン持続時間を1減らす
            Mod(-1);
        }

        public override void OnStart()
        {
            ModLog.Log($"[SukutsuArena] ConSukutsuPoison started on {owner.Name}, value={value}");
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

