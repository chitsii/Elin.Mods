using Elin_SukutsuArena.Attributes;

namespace Elin_SukutsuArena.Conditions
{
    /// <summary>
    /// アスタロトの権能「時の独裁」
    /// 速度に永続的なダメージを与える
    /// </summary>
    [GameDependency("Inheritance", "Condition", "Medium", "Condition base class may change")]
    public class ConAstarothTyranny : Condition
    {
        private const int ELE_SPD = 79;

        public override ConditionType Type => ConditionType.Debuff;

        public override int GetPhase() => 0;

        public override void SetOwner(Chara _owner, bool onDeserialize = false)
        {
            base.SetOwner(_owner);

            if (!onDeserialize)
            {
                ApplyDamage();
            }
        }

        private void ApplyDamage()
        {
            // tempElementsを直接操作（ModTempElementはsustain_SPDを参照してエラーになる）
            if (owner.tempElements == null)
            {
                owner.tempElements = new ElementContainer();
                owner.tempElements.SetParent(owner);
            }

            int before = owner.tempElements.Base(ELE_SPD);
            owner.tempElements.ModBase(ELE_SPD, -1000);
            int after = owner.tempElements.Base(ELE_SPD);
            int actualDamage = before - after;

            if (actualDamage > 0)
            {
                string statName = EClass.sources.elements.map[ELE_SPD].GetName();
                owner.Say($"{statName}が{actualDamage}低下した！");
            }
        }

        public override void PlayEffect()
        {
            if (!Condition.ignoreEffect)
            {
                owner.PlaySound("debuff");
                owner.PlayEffect("debuff");
            }
        }
    }
}
