namespace Elin_ItemRelocator {
    public enum RelocationScope {
        Inventory,
        Both,
        ZoneOnly,
        PetsOnly
    }

    public struct RelocationDecisionInput {
        public RelocationScope Scope;
        public bool DestinationAvailable;
        public bool SourceRuleMatches;
        public bool DestinationHasCapacityForThing;
        public bool ItemAlreadyInDestination;
        public bool ItemIsDestroyed;
        public bool ItemIsContainer;
        public bool ItemIsImportant;
        public bool ItemIsInstalled;
        public bool ItemIsLockedHard;
        public bool ItemIsToolbelt;
        public bool ItemIsAbility;
        public bool DestinationIsPcOwned;
        public bool ItemCanBeDropped;
        public bool ItemIsEquipped;
        public bool ItemIsContainerWithContents;
        public bool ItemIsMoney;
        public bool ItemIsGifted;
        public bool ItemIsNpcProperty;
        public bool ItemIsPcOwned;
        public bool ItemIsPetOwned;
        public bool ItemIsOnHotbar;
    }

    public static class RelocationDecisionPolicy {
        public static bool CanPreview(RelocationDecisionInput input) {
            if (!input.DestinationAvailable || !input.SourceRuleMatches)
                return false;
            if (input.ItemAlreadyInDestination)
                return false;
            if (input.ItemIsDestroyed || input.ItemIsContainer)
                return false;
            if (input.ItemIsImportant || input.ItemIsInstalled || input.ItemIsLockedHard)
                return false;
            if (input.ItemIsToolbelt)
                return false;
            if (input.ItemIsAbility && !input.DestinationIsPcOwned)
                return false;
            if (!input.ItemCanBeDropped)
                return false;
            if (input.ItemIsEquipped)
                return false;
            if (input.ItemIsContainerWithContents && !input.DestinationIsPcOwned)
                return false;

            if (!input.DestinationIsPcOwned) {
                if (input.ItemIsMoney || input.ItemIsGifted || input.ItemIsNpcProperty)
                    return false;
            }

            if (!IsInScope(input))
                return false;
            if (input.ItemIsOnHotbar)
                return false;

            return true;
        }

        public static bool CanMove(RelocationDecisionInput input) {
            return CanPreview(input) && input.DestinationHasCapacityForThing;
        }

        private static bool IsInScope(RelocationDecisionInput input) {
            switch (input.Scope) {
            case RelocationScope.Inventory:
                return input.ItemIsPcOwned;
            case RelocationScope.ZoneOnly:
                return !input.ItemIsPcOwned && !input.ItemIsPetOwned;
            case RelocationScope.PetsOnly:
                return input.ItemIsPetOwned;
            case RelocationScope.Both:
            default:
                return !input.ItemIsPetOwned;
            }
        }
    }
}
