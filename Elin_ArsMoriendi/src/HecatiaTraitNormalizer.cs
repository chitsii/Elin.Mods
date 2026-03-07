using System;

namespace Elin_ArsMoriendi
{
    internal static class HecatiaTraitNormalizer
    {
        private const string HecatiaId = "ars_hecatia";

        public static void Normalize(Chara? chara)
        {
            if (chara == null) return;
            if (!string.Equals(chara.id, HecatiaId, StringComparison.Ordinal)) return;

            try
            {
                bool removedBloodBond = RemoveFeat(chara, FEAT.featBloodBond);
                bool removedAshborn = RemoveFeat(chara, FEAT.featAshborn);
                if (removedBloodBond || removedAshborn)
                {
                    ModLog.Log(
                        "Normalized Hecatia traits: bloodBondRemoved={0}, ashbornRemoved={1}",
                        removedBloodBond,
                        removedAshborn);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn($"Hecatia trait normalization failed: {ex.Message}");
            }
        }

        private static bool RemoveFeat(Chara chara, int featId)
        {
            if (!chara.elements.Has(featId)) return false;
            chara.SetFeat(featId, 0);
            return true;
        }
    }
}
