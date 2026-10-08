using System;
using System.Collections.Generic;
using HarmonyLib;

// Read-only startup regression: source availability alone did not detect
// the SetRenderData exception that skipped native material and Card registration.
public sealed class CharacterSourcesInitializedCase : RuntimeCaseBase
{
    public override string Id => "source.characters.native_registration";
    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "compat", "source" };
    private const string PackageId = "chitsii.elin._ars_moriendi";
    private static readonly string[] Ids =
    {
        "ars_erenos_guard",
        "ars_erenos_pet",
        "ars_erenos_shade",
        "ars_erenos_shadow",
        "ars_hecatia",
        "ars_karen",
        "ars_temple_knight",
        "ars_temple_scout"
    };

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.sources != null, "Game sources unavailable.");
        var target = AccessTools.Method(typeof(SourceCard), nameof(SourceCard.AddRow),
            new[] { typeof(CardRow), typeof(bool) });
        var patches = Harmony.GetPatchInfo(target);
        RuntimeAssertions.Require(HarmonyCompatFacade.HasPatch(patches, "prefix",
            "Elin_ArsMoriendi.CharacterSourceDefaultsPatch", "Prefix"),
            "Character source default prefix was not applied.");
        RuntimeAssertions.Require(patches != null && patches.Owners.Contains("chitsii.elin.ars_moriendi"),
            "Product Harmony owner missing.");
    }

    public override void Execute(RuntimeTestContext ctx) { }

    public override void Verify(RuntimeTestContext ctx)
    {
        var owned = EClass.sources.charas.rows.FindAll(
            row => ModUtil.FindSourceRowPackage(row)?.id == PackageId);
        RuntimeAssertions.Require(owned.Count == Ids.Length,
            "Owned Chara inventory changed; update coverage explicitly. Count=" + owned.Count);
        foreach (var id in Ids)
        {
            SourceChara.Row row;
            RuntimeAssertions.Require(EClass.sources.charas.map.TryGetValue(id, out row),
                "Missing Chara source: " + id);
            RuntimeAssertions.Require(owned.Exists(item => object.ReferenceEquals(item, row)),
                "Source package ownership mismatch: " + id);
            RuntimeAssertions.Require(row.colorType != null, "Null colorType: " + id);
            RuntimeAssertions.Require(row.DefaultMaterial != null, "Null DefaultMaterial: " + id);
            CardRow card;
            RuntimeAssertions.Require(EClass.sources.cards.map.TryGetValue(id, out card)
                && object.ReferenceEquals(row, card), "SourceCard.map missing/wrong row: " + id);
            RuntimeAssertions.Require(EClass.sources.cards.rows.FindAll(
                item => item.id == id).Count == 1 && EClass.sources.cards.rows.Contains(row),
                "SourceCard.rows missing/duplicate: " + id);
            ctx.Log(id + ":colorType=" + row.colorType + ";material=" + row.DefaultMaterial.id
                + ";same_map_row=true;rows_count=1");
        }
        ctx.Log("Native source initialization verified: 8/8.");
    }
}
