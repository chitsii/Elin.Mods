from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]


def read_source(name: str) -> str:
    return (ROOT / "src" / name).read_text(encoding="utf-8-sig")


def assert_order(source: str, earlier: str, later: str) -> None:
    earlier_index = source.find(earlier)
    later_index = source.find(later)
    assert earlier_index >= 0, f"missing {earlier!r}"
    assert later_index >= 0, f"missing {later!r}"
    assert earlier_index < later_index, f"{earlier!r} must appear before {later!r}"


def test_altar_deity_is_set_before_can_offer_and_uses_actor_argument() -> None:
    source = read_source("OfferLogic.cs")

    assert "fakeAltar.CanOffer(actor, t)" in source
    assert_order(source, "fakeAltar.SetDeity(faith.id)", "fakeAltar.CanOffer(actor, t)")


def test_fx_scope_is_bounded_and_does_not_change_inventory_position() -> None:
    source = read_source("OfferLogic.cs")
    context = read_source("OfferingEffectContext.cs")
    scope = read_source("OfferingEffectScope.cs")
    assert_order(source, "OfferingEffectContext.TryCreate", "fakeAltar.SetDeity")
    assert 'using (effectContext.Enter())' in source and 'effectContext.IsCurrent()' in source
    assert not re.search(r'\b(?:container|owner)\.pos\s*=(?!=)', source + context)
    for token in ['ReferenceEquals(Point.map, map)', 'ReferenceEquals(map.cells, cells)', 'EClass.game.activeZone == zone', 'OfferingMapBounds.Contains', 'actor.pos.Copy()']:
        assert token in context
    assert '[ThreadStatic]' in scope and 'ReferenceEquals(from, scope.ownerPoint)' in scope
    assert 'current = previous' in scope


def test_sleep_postfix_skips_dead_or_incomplete_sleep_removal() -> None:
    source = read_source("PatchConSleep.cs")

    assert "owner.isDead" in source
    assert "!__instance.slept" in source
    assert_order(source, "owner.isDead", "Checking for offerings")


def test_custom_box_identity_does_not_reuse_deity_slot() -> None:
    source = read_source("PatchRecipe.cs")

    assert "c_idDeity = Plugin.ID_OFFERING_BOX" not in source


def test_custom_box_source_patch_targets_declared_source_manager_init() -> None:
    source = read_source("PatchRecipe.cs")

    assert "AccessTools.DeclaredMethod(typeof(SourceManager), nameof(SourceManager.Init), Type.EmptyTypes)" in source
    assert "method.DeclaringType != typeof(SourceManager)" in source
    assert "method.IsGenericMethod" in source
    assert "method.GetParameters().Length != 0" in source
    assert "HarmonyPatch(typeof(SourceThing), \"Init\")" not in source


def test_custom_box_injection_uses_rows_and_leaves_map_to_source_init() -> None:
    source = read_source("PatchRecipe.cs")

    assert "InjectOfferingBoxRow(__instance.things)" in source
    assert "source == null || source.rows == null" in source
    assert "foreach (SourceThing.Row sourceRow in source.rows)" in source
    assert "sourceRow.id == customId" in source
    assert "sourceRow.id == \"chest6\"" in source
    assert "__instance.things.Init()" not in source
    assert ".map[customId]" not in source
    assert ".factory = new string[]" in source
    assert ".recipeKey = new string[]" in source


def test_custom_box_injection_avoids_reinit_duplicates_and_shared_arrays() -> None:
    source = read_source("PatchRecipe.cs")

    assert "__instance.initialized" in source
    assert "sourceRow.id == customId" in source
    assert "return;" in source[source.find("sourceRow.id == customId"):]
    assert "field.FieldType.IsArray" in source
    assert "array.Clone()" in source
    assert "field.SetValue(clone" in source


if __name__ == "__main__":
    tests = [
        test_altar_deity_is_set_before_can_offer_and_uses_actor_argument,
        test_fx_scope_is_bounded_and_does_not_change_inventory_position,
        test_sleep_postfix_skips_dead_or_incomplete_sleep_removal,
        test_custom_box_identity_does_not_reuse_deity_slot,
        test_custom_box_source_patch_targets_declared_source_manager_init,
        test_custom_box_injection_uses_rows_and_leaves_map_to_source_init,
        test_custom_box_injection_avoids_reinit_duplicates_and_shared_arrays,
    ]
    for test in tests:
        test()
        print(f"PASS {test.__name__}")


