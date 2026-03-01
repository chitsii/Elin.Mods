using System;

namespace Elin_ArsMoriendi
{
    public class ConGraveQuagmireSlow : Condition
    {
        public override bool UseElements => true;
        public override bool ShouldRefresh => true;
        public override bool ShouldOverride(Condition c) => true;
        public override bool IsOverrideConditionMet(Condition c, int turn) => true;
        public override int GainResistFactor => 0;
        public override int EvaluateTurn(int p) => 3;

        public override void SetOwner(Chara _owner, bool onDeserialize = false)
        {
            base.SetOwner(_owner, onDeserialize);
            elements.SetBase(79, -Math.Max(1, power));
        }

        public override void Tick()
        {
            Mod(-1);
        }
    }
}
