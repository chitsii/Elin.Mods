using System;

namespace Elin_ArsMoriendi
{
    public class ConFuneralMarchServant : Condition
    {
        public int SpeedBonus;
        public int StrBonus;
        public int EndBonus;
        public int DurationTurns = 8;

        public override bool UseElements => true;
        public override bool ShouldRefresh => true;
        public override bool ShouldOverride(Condition c) => true;
        public override bool IsOverrideConditionMet(Condition c, int turn) => true;
        public override int GainResistFactor => 0;
        public override int EvaluateTurn(int p) => Math.Max(1, DurationTurns);

        public override void SetOwner(Chara _owner, bool onDeserialize = false)
        {
            base.SetOwner(_owner, onDeserialize);
            elements.SetBase(79, Math.Max(1, SpeedBonus));
            elements.SetBase(70, Math.Max(1, StrBonus));
            elements.SetBase(71, Math.Max(1, EndBonus));
        }

        public override void Tick()
        {
            Mod(-1);
        }
    }
}
