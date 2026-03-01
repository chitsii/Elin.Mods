namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Preserve Soul condition - marks the target for soul drop on death.
    /// No stat effects; purely a marker condition.
    /// CWL loads this via SourceStat type: "Elin_ArsMoriendi.ConSoulTrapped"
    /// </summary>
    public class ConSoulTrapped : Condition
    {
        public override bool ShouldRefresh => false;
        public override int GainResistFactor => 0;

        public override void Tick()
        {
            Mod(-1);
        }
    }
}
