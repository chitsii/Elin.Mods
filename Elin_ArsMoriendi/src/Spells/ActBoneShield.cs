namespace Elin_ArsMoriendi
{
    public class ActBoneShield : Spell
    {
        public override bool Perform()
        {
            // Backward compatibility: legacy alias now routes to the legion variant.
            return new ActBoneAegisLegion().Perform();
        }
    }
}
