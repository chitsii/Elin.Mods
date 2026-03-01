using System;

namespace Elin_ArsMoriendi
{
    public class ConGraveQuagmireSilence : ConSilence
    {
        public int DurationTurns = 1;

        public override bool ShouldRefresh => true;
        public override bool ShouldOverride(Condition c) => true;
        public override bool IsOverrideConditionMet(Condition c, int turn) => true;
        public override int EvaluateTurn(int p) => Math.Max(1, DurationTurns);
    }
}
