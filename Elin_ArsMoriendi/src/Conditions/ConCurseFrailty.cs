using System;

namespace Elin_ArsMoriendi
{
    public class ConCurseFrailty : Condition
    {
        private const int ELE_END = 71;
        private const int ELE_PER = 73;
        private const int ELE_SPD = 79;

        public override bool UseElements => true;
        public override bool ShouldRefresh => true;

        public override void SetOwner(Chara _owner, bool onDeserialize = false)
        {
            base.SetOwner(_owner, onDeserialize);

            int attrFlatDebuff = Math.Max(1, power / 12);
            int speedFlatDebuff = Math.Max(1, power / 18);
            double attrRate = Math.Min(0.40, 0.16 + power / 7000.0);
            double speedRate = Math.Min(0.30, 0.10 + power / 9000.0);

            int endRaw = Math.Max(attrFlatDebuff, (int)Math.Floor(_owner.END * attrRate));
            int perRaw = Math.Max(attrFlatDebuff, (int)Math.Floor(_owner.PER * attrRate));
            int speedRaw = Math.Max(speedFlatDebuff, (int)Math.Floor(_owner.Speed * speedRate));

            int endCap = Math.Max(1, (int)Math.Floor(_owner.END * 0.50));
            int perCap = Math.Max(1, (int)Math.Floor(_owner.PER * 0.50));
            int speedCap = Math.Max(1, (int)Math.Floor(_owner.Speed * 0.40));

            int endDebuff = Math.Min(endRaw, endCap);
            int perDebuff = Math.Min(perRaw, perCap);
            int speedDebuff = Math.Min(speedRaw, speedCap);

            elements.SetBase(ELE_END, -endDebuff);
            elements.SetBase(ELE_PER, -perDebuff);
            elements.SetBase(ELE_SPD, -speedDebuff);
        }

        public override void Tick()
        {
            Mod(-1);
        }
    }
}
