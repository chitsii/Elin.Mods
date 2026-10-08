using HarmonyLib;

namespace Elin_ArsMoriendi
{
    // Older Chara spreadsheets leave colorType null. Stable 23.352 splits it
    // before assigning DefaultMaterial and registering the row in SourceCard.
    [HarmonyPatch(typeof(SourceCard), nameof(SourceCard.AddRow),
        new[] { typeof(CardRow), typeof(bool) })]
    internal static class CharacterSourceDefaultsPatch
    {
        internal const string PackageId = "chitsii.elin._ars_moriendi";

        internal static void Prefix(CardRow row)
        {
            if (row is SourceChara.Row && row.colorType == null
                && ModUtil.FindSourceRowPackage(row)?.id == PackageId)
            {
                row.colorType = string.Empty;
            }
        }
    }
}
