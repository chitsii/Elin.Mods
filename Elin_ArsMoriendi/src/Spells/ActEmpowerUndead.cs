namespace Elin_ArsMoriendi
{
    public class ActEmpowerUndead : Spell
    {
        public override bool Perform()
        {
            // Backward compatibility: legacy alias now routes to the integrated march.
            return new ActFuneralMarch().Perform();
        }
    }
}
