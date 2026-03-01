namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Permanent marker condition for undead servants.
    /// Primarily event-driven, with a lightweight turn fallback for zone-transition safety.
    /// </summary>
    public class ConUndeadServantPresence : Condition
    {
        public override bool ShouldRefresh => true;
        public override int GainResistFactor => 0;

        public override void Tick()
        {
            // Fallback re-attach for cases where renderer recreation races zone activation.
            if (owner == null || owner.isDestroyed || owner.isDead) return;
            if (!NecromancyManager.Instance.IsServant(owner.uid)) return;

            NecromancyManager.TryEnsureServantAura(owner);
        }
    }
}

