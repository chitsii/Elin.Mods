using System;

namespace Elin_ArsMoriendi
{
    public class ConBoneShield : Condition
    {
        public int DurationTurns = 30;

        public override bool UseElements => true;
        public override bool ShouldRefresh => true;
        public override int GainResistFactor => 0;

        // PDR (Physical Damage Reduction) = 55, EDR (Elemental Damage Reduction) = 56
        private const int ELE_PDR = 55;
        private const int ELE_EDR = 56;

        public override int EvaluateTurn(int p) => Math.Max(1, DurationTurns);

        public override void SetOwner(Chara _owner, bool onDeserialize = false)
        {
            base.SetOwner(_owner, onDeserialize);

            double reductionRate = Math.Min(0.45, Math.Max(0.22, 0.22 + power * 0.00020));
            int pdrEdrBonus = (int)Math.Floor(100.0 * reductionRate / (1.0 - reductionRate));
            elements.SetBase(ELE_PDR, pdrEdrBonus);
            elements.SetBase(ELE_EDR, pdrEdrBonus);
        }

        public override void Tick()
        {
            Mod(-1);
        }
    }
}
