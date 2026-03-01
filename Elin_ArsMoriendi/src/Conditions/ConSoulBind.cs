namespace Elin_ArsMoriendi
{
    // IMPORTANT POLICY (Spell effect maintenance):
    // - This condition must manage ONLY gameplay state (condition life-cycle / reserved UID).
    // - Do NOT add direct VFX stop/despawn calls here.
    // - Soul Bind VFX is owned by leased loop entries in CustomAssetFx and expires automatically
    //   when this condition stops renewing it.
    // Always re-check this policy before changing Soul Bind behavior.
    public class ConSoulBind : Condition
    {
        private const string AuraFx = "FxSoulBind";
        private const string OwnerAuraKey = "ConSoulBind.Owner";
        private const string SacrificeAuraKey = "ConSoulBind.Sacrifice";

        public override bool ShouldRefresh => true;
        public override int GainResistFactor => 0;

        public override void Tick()
        {
            if (owner == null || owner.isDestroyed || owner.isDead)
            {
                NecromancyManager.Instance.ClearSoulBindSacrificeUid();
                return;
            }

            CustomAssetFx.TryEnsureOwnedLoopAttachedToCard(
                AuraFx,
                owner,
                OwnerAuraKey,
                casterUid: owner.uid,
                leaseTurns: 2);

            var sacrifice = ResolveSacrifice();
            if (sacrifice == null)
            {
                NecromancyManager.Instance.ClearSoulBindSacrificeUid();
                owner.RemoveCondition<ConSoulBind>();
                return;
            }

            CustomAssetFx.TryEnsureOwnedLoopAttachedToCard(
                AuraFx,
                sacrifice,
                SacrificeAuraKey,
                casterUid: owner.uid,
                leaseTurns: 2);

            Mod(-1);

            if (!owner.HasCondition<ConSoulBind>())
                NecromancyManager.Instance.ClearSoulBindSacrificeUid();
        }

        private Chara? ResolveSacrifice()
        {
            var mgr = NecromancyManager.Instance;
            int uid = mgr.GetSoulBindSacrificeUid();
            if (uid <= 0) return null;

            var servants = mgr.GetCombatServantsInCurrentZone();
            foreach (var s in servants)
            {
                if (s.uid == uid) return s;
            }
            return null;
        }
    }
}
