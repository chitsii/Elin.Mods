namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Permanent condition granted by the Apotheosis ritual (Stage 7).
    /// Tick() is overridden to never decrement, making it permanent.
    /// Effects: spell power multiplier, servant enhancement, phylactery revival.
    /// </summary>
    public class ConApotheosis : Condition
    {
        public override bool ShouldRefresh => true;
        public override int GainResistFactor => 0;

        public override void Tick()
        {
            // Permanent — never decrement
        }
    }
}
