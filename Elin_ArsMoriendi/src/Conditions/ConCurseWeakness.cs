using System;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Curse of Weakness - debuffs STR and DEX via elements defined in SourceStat.
    /// CWL loads this via SourceStat type: "Elin_ArsMoriendi.ConCurseWeakness"
    /// </summary>
    public class ConCurseWeakness : Condition
    {
        private const int ELE_STR = 70;
        private const int ELE_DEX = 72;

        public override bool UseElements => true;
        public override bool ShouldRefresh => true;

        public override void SetOwner(Chara _owner, bool onDeserialize = false)
        {
            base.SetOwner(_owner, onDeserialize);

            int flatDebuff = Math.Max(1, power / 10);
            double rate = Math.Min(0.45, 0.20 + power / 6000.0);

            int strRaw = Math.Max(flatDebuff, (int)Math.Floor(_owner.STR * rate));
            int dexRaw = Math.Max(flatDebuff, (int)Math.Floor(_owner.DEX * rate));

            int strCap = Math.Max(1, (int)Math.Floor(_owner.STR * 0.55));
            int dexCap = Math.Max(1, (int)Math.Floor(_owner.DEX * 0.55));

            int strDebuff = Math.Min(strRaw, strCap);
            int dexDebuff = Math.Min(dexRaw, dexCap);

            elements.SetBase(ELE_STR, -strDebuff);
            elements.SetBase(ELE_DEX, -dexDebuff);
        }

        public override void Tick()
        {
            Mod(-1);
        }
    }
}
