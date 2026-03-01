namespace Elin_ArsMoriendi
{
    public class ConPreserveCorpse : Condition
    {
        private const string PreferredAuraFx = "FxPreserveCorpseAura";
        private const string FallbackAuraFx = "FxSoulBind";
        private const string PreferredAuraOwnerKey = "ConPreserveCorpse.Preferred";
        private const string FallbackAuraOwnerKey = "ConPreserveCorpse.Fallback";
        private static bool _useFallbackAura;

        public override bool ShouldRefresh => true;
        public override int GainResistFactor => 0;

        public static void EnsureAura(Card card)
        {
            if (card == null || card.isDestroyed) return;
            if (card is Chara chara && chara.isDead) return;

            if (!_useFallbackAura && CustomAssetFx.TryEnsureOwnedLoopAttachedToCard(
                    PreferredAuraFx,
                    card,
                    PreferredAuraOwnerKey,
                    casterUid: card.uid,
                    leaseTurns: 2))
                return;

            if (!_useFallbackAura)
            {
                _useFallbackAura = true;
                ModLog.Warn($"ConPreserveCorpse: {PreferredAuraFx} not found. Falling back to {FallbackAuraFx}.");
            }

            CustomAssetFx.TryEnsureOwnedLoopAttachedToCard(
                FallbackAuraFx,
                card,
                FallbackAuraOwnerKey,
                casterUid: card.uid,
                leaseTurns: 2);
        }

        public static void StopAura(Card? card)
        {
            if (card == null) return;
            CustomAssetFx.StopAttachedFx(PreferredAuraFx, card);
            CustomAssetFx.StopAttachedFx(FallbackAuraFx, card);
            CustomAssetFx.StopOwnedLoopFx(PreferredAuraFx, card, PreferredAuraOwnerKey, card.uid);
            CustomAssetFx.StopOwnedLoopFx(FallbackAuraFx, card, FallbackAuraOwnerKey, card.uid);
        }

        public override void Tick()
        {
            if (owner == null || owner.isDestroyed || owner.isDead)
            {
                StopAura(owner);
                return;
            }

            EnsureAura(owner);
            Mod(-1);

            // Loop FX is non-autodestroy. Explicitly clean it up when this buff expires.
            if (!owner.HasCondition<ConPreserveCorpse>())
                StopAura(owner);
        }
    }
}

