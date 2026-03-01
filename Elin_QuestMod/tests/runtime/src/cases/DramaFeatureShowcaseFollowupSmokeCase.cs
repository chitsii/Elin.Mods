using System;
using System.Collections.Generic;
using System.IO;

// Minimal smoke case: feature showcase follow-up drama exists and can be activated.
public sealed class DramaFeatureShowcaseFollowupSmokeCase : RuntimeCaseBase
{
    private const string DramaBookId = "drama_quest_drama_feature_followup";
    private const string DramaId = "quest_drama_feature_followup";

    public override string Id => "questmod.drama.feature_showcase_followup.activate_smoke";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "drama" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.pc != null, "EClass.pc is null.");
        RuntimeAssertions.Require(EClass.ui != null, "EClass.ui is null.");
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        string dramaPath = Path.Combine(
            ctx.ModRoot,
            "LangMod",
            "EN",
            "Dialog",
            "Drama",
            "drama_quest_drama_feature_followup.xlsx");

        bool fileExists = File.Exists(dramaPath);
        ctx.Set("dramaPath", dramaPath);
        ctx.Set("fileExists", fileExists);
        RuntimeAssertions.Require(fileExists, "Feature showcase follow-up drama file is missing: " + dramaPath);

        string activateError = string.Empty;
        LayerDrama layer = null;
        try
        {
            layer = LayerDrama.Activate(DramaBookId, DramaId, null, EClass.pc, null, null);
        }
        catch (Exception ex)
        {
            activateError = ex.ToString();
        }

        bool activated = layer != null || LayerDrama.Instance != null;
        ctx.Set("activated", activated);
        ctx.Set("activateError", activateError);

        ctx.RegisterRollback("close_drama_layer", SafeCloseDramaLayers);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(ctx.Get<bool>("fileExists"), "Feature showcase follow-up drama xlsx is not found.");

        bool activated = ctx.Get<bool>("activated");
        string error = ctx.Get<string>("activateError");
        RuntimeAssertions.Require(
            activated,
            "LayerDrama.Activate failed for feature showcase follow-up drama."
            + (string.IsNullOrEmpty(error) ? string.Empty : " " + error));

        ctx.Log("Feature showcase follow-up drama smoke verified.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
        SafeCloseDramaLayers();
    }

    private static void SafeCloseDramaLayers()
    {
        try
        {
            LayerDrama layer = LayerDrama.Instance;
            if (layer != null)
            {
                if (layer.drama != null && layer.drama.sequence != null)
                {
                    layer.drama.sequence.Exit();
                }
                layer.Close();
            }

            if (EClass.ui != null)
            {
                Layer top = EClass.ui.TopLayer;
                if (top != null)
                {
                    LayerDrama dramaTop = top as LayerDrama;
                    if (dramaTop != null && dramaTop.drama != null && dramaTop.drama.sequence != null)
                    {
                        dramaTop.drama.sequence.Exit();
                    }
                    top.Close();
                }

                EClass.ui.HideCover(0f, null);
                EClass.ui.Show(0f);
            }
        }
        catch
        {
        }
    }
}
