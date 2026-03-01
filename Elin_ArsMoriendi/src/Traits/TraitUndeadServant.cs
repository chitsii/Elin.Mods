namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Trait for undead servants raised through necromantic rituals.
    /// Inherits TraitChara to access CanJoinParty override, which hides the
    /// "invite to party" dialogue option (DramaCustomSequence checks trait.CanJoinParty).
    /// Applied via c_idTrait = "TraitUndeadServant" + ApplyTrait() in PerformRitual.
    /// </summary>
    public class TraitUndeadServant : TraitChara
    {
        public bool IsUndead => true;

        public override bool CanJoinParty => false;
        public override bool IsCountAsResident => false;
    }
}
