"""Source order/coverage contract; managed codex callback behavior is checked separately."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]

def ordered(source, *needles):
    positions = [source.index(needle) for needle in needles]
    assert positions == sorted(positions), f"Invalid callback isolation order: {needles}"

def verify(source):
    assert "IO.DeepCopy(" not in source and "MemberwiseClone" not in source
    assert "TestPlayer = new Player { zone = game.activeZone };" in source
    assert not re.search(r"TestPlayer\.\w+\s*=\s*originalPlayer", source)
    assert not re.search(r"original(?:Player|Actor)\.\w+\s*=(?!=)", source)
    assert "owned == null || !ReferenceEquals(owned, original)" in source
    ordered(source, 'ctx.RegisterRollback("pr8.restore_player_and_remove_owned_fixtures", f.Cleanup)', "f.Prepare();")
    ordered(source, "nativeState.Begin();", "ownership.Install();", "Actor = new Chara();", "TestPlayer.chara = originalActor;", "playerScope.Select(TestPlayer);", "CreateNativeActor(Actor, \"primary\")", "TestPlayer.chara = Actor;", "FinishActor(Actor);", "OtherActor = CreateActor();")
    assert "AssertOriginalState(\"after_primary_actor\")" in source and "AssertOriginalState(\"after_other_actor\")" in source
    cleanup = source[source.index("private void Cleanup()") : source.index("private static void TryCleanup")]
    ordered(cleanup, "playerScope.Select(TestPlayer)", "Observer.Dispose()", "item.Destroy()", "DestroyActor(actor)", "finally", "playerScope.Dispose()", "AssertOriginalState(\"after_cleanup\")")
    assert "actor.isSummon = true;" in cleanup
    for assertion in ("originalPlayer.chara == originalActor", "state.AssertUnchanged()", "UnityEngine.Random.state = randomState;", "item == null || item.isDestroyed", "actor.isDestroyed && !EClass._map.charas.Contains(actor)"):
        assert assertion in cleanup
    for assertion in ("originalPlayerState", "originalActorState", "SerializeObject(originalPlayer, IO.dpFormat, IO.dpSetting)", "SerializeObject(originalActor, IO.dpFormat, IO.dpSetting)", "RuntimeAssertions.Require(after == before", "Pr8StateDiff.Describe(before, after)"):
        assert assertion in source, f"Missing full preservation: {assertion}"

if __name__ == "__main__":
    source = (ROOT / 'src/cases/Pr8OfferingFixture.cs').read_text(encoding='utf-8-sig')
    verify(source)
    print('PASS: native creation/removal callbacks selected on isolated Player; full preservation assertions retained')
    changes = {
        'premature fixture PC selection': source.replace('TestPlayer.chara = originalActor;', 'TestPlayer.chara = Actor;'),
        'whole Player deepcopy': source.replace('TestPlayer = new Player { zone = game.activeZone };', 'TestPlayer = IO.DeepCopy(originalPlayer);'),
        'original PC assignment': source.replace('TestPlayer.chara = Actor;', 'originalPlayer.chara = Actor;'),
        'spawn before selection': source.replace('        playerScope.Select(TestPlayer);\n', '', 1),
        'early restoration': source.replace('            if (Observer != null) TryCleanup', '            playerScope.Dispose();\n            if (Observer != null) TryCleanup'),
        'disabled exact state assertion': source.replace('RuntimeAssertions.Require(after == before', 'RuntimeAssertions.Require(true'),
    }
    for name, changed in changes.items():
        try: verify(changed)
        except (AssertionError, ValueError): print('PASS: rejects ' + name)
        else: raise AssertionError('Counterexample falsely passed: ' + name)
    cases = (ROOT / 'src/cases/Pr8OfferingIntegrationCases.cs').read_text(encoding='utf-8-sig')
    assert 'SetFaith("ehekatl")' not in source + cases and 'SetFaith("eyth")' not in source + cases
    assert 'f.LuckFaith.id' in cases
    prereq = (ROOT / 'src/cases/Pr8NativePrerequisites.cs').read_text(encoding='utf-8-sig')
    assert 'SourceReligion.Row row' in prereq and 'dictAll.TryGetValue(row.id' in prereq and 'loaded.source == row' in prereq
    assert 'AuditOwnedInputs();' in source and 'RecipeCard' in source and 'SplitSize(meat)' in source
    reload = (ROOT / 'src/cases/Pr8ReloadCases.cs').read_text(encoding='utf-8-sig')
    assert reload.count('Pr8NativePrerequisites.AuditAll(ctx);') == 2
    assert 'RequirePreknownReloadIngredients();' in reload
    ordered(reload, 'ownership.GuardTree(box);', 'var generatedContents = new List<Thing>(box.things);', 'foreach (Thing item in generatedContents) item.Destroy();', 'EClass.pc.AddThing(box, tryStack: false);')
    assert 'reload_after_generation' in reload
    assert 'Native fixture box was not emptied.' in source
    assert reload.count('new Pr8ReloadPlayerPreservation(ctx)') == 2
    assert 'reload_before_process' in reload and 'reload_after_process' in reload
    assert 'reload_prepare_cleanup' in reload and 'reload_verify_cleanup' in reload
    assert len(re.findall(r'override string Id => "pr8.sleep.', cases + reload)) == 11
    print('PASS: all11 prerequisite coverage, loaded faith/source resolution and reload ingredient precondition')

    # Actual shared wiring is structural; executable native Stats/Rand + generic tree counterexamples run separately.
    ordered(cleanup := source[source.index("private void Cleanup()") : source.index("private static void TryCleanup")],
            'ownership.GuardAll();', 'item.Destroy()', 'finally', 'ownership.Dispose()', 'playerScope.Dispose()', 'nativeState.Dispose()', 'nativeState.AssertRestored()')
    assert 'ownership.RequireOwned(item);' in source
    assert 'foreach (Thing item in generatedContents) { Track(item); item.Destroy(); }' not in source
    assert 'private static void AfterCreate(Thing __result)' not in source
    ownership = (ROOT / 'src/cases/Pr8CardOwnership.cs').read_text(encoding='utf-8-sig')
    assert 'EClass.game.cards.uidNext' in ownership and 'ledger.CaptureOriginal(EClass.pc)' in ownership
    assert 'ledger.ObserveAllocation(__instance)' in ownership and 'BeforeDestroy' in ownership and 'BeforeSplit' in ownership
    assert ownership.index('GuardAll();', ownership.index('public void DestroyOwnedExcept')) < ownership.index('card.Destroy();')
    assert reload.count('nativeState.Begin();') == 2 and reload.count('finally { nativeState.Dispose(); }') == 2
    assert 'ownership.AuthorizePersisted(box)' in reload and 'ownership.DestroyOwnedExcept();' in reload
    state = (ROOT / 'src/cases/Pr8NativeStateScope.cs').read_text(encoding='utf-8-sig')
    for expected in ('typeof(Stats).GetFields', 'BaseStats.CC = originalCC', 'Rand._random = originalRandom', 'Rand.baseSeed = baseSeed', 'randomFields.AssertUnchanged()'):
        assert expected in state
    print('PASS: shared Stats/CC + Rand restoration wiring and unified recursive ownership guards')

    verify_prepare = reload[reload.index('public sealed class Pr8ReloadVerifyCase'):reload.index('public override void Execute', reload.index('public sealed class Pr8ReloadVerifyCase'))]
    ordered(verify_prepare, 'AssertRecord(box, record);', 'Persisted child source/name mismatch', 'ownership.AuthorizePersisted(box)')
    assert 'record.WaterUid != record.RejectUid' in verify_prepare
    print('PASS: exact persisted child source/name + both UID/quantity checks precede ownership authorization')

    ordered(source, 'Faction nativePcFaction = originalActor.faction;', 'Actor = new Chara();', 'Actor.faction = nativePcFaction;', 'TestPlayer.chara = originalActor;', 'playerScope.Select(TestPlayer);', 'CreateNativeActor(Actor, "primary")', 'TestPlayer.chara = Actor;')
    assert 'harmony.Patch(valueBonus, finalizer:' in source and 'return __exception;' in source
    destroy = source[source.index('private void DestroyActor'):source.index('private void FinishActor')]
    ordered(destroy, 'ownership.GuardTree(actor)', 'Pr8PartialCleanup.CanSupplyEmptyRenderer', 'RuntimeAssertions.Require(eligible', 'actor.renderer = new CardRenderer', 'actor.Destroy();')
    assert 'isDestroyed = true' not in source
    assert 'actor.currentZone == null' in destroy and 'actor.global != null' in destroy
    assert 'ctx.Logs.Count < 44' in source and 'diagnosticEvents.Add(record)' in source
    for stage in ('create_before_', 'create_failed_', 'value_bonus_failed', 'cleanup_before', 'cleanup_after'):
        assert stage in source
    print('PASS: loaded faction before PC selection/native Create; exception-preserving compact diagnostics; strictly gated real partial destruction')

    # Deferred diagnostic data is evaluated only within the shared non-throwing guard.
    diagnostic = source[source.index('private void Diagnostic('):source.index('private void DestroyActor')]
    ordered(diagnostic, 'Func<object> readData', 'Pr8Diagnostic.BestEffort(() =>', 'data = readData()', 'SerializeObject')
    calls = re.findall(r'Diagnostic\((?:"[^\n]*|error == null[^\n]*)', source)
    assert calls and all(', () => new {' in call for call in calls), calls
    finalizer = source[source.index('private static Exception ValueBonusFailed'):source.index('private void LogObservation')]
    assert 'Pr8Diagnostic.BestEffort(() =>' in finalizer and 'ctx.Log(' not in finalizer
    assert 'return __exception;' in finalizer
    print('PASS: deferred snapshot/JSON/file/log diagnostics cannot replace native exceptions or skip restoration')

    assert 'Pr8CreationOrder.CompleteBeforeSelection(' in source
    assert 'EClass.pc != actor && NativeBodyReady(EClass.pc)' in source
    assert 'AssertOriginalState("after_native_create_" + role)' in source
    assert 'NativeBodyReady(originalActor)' in source and 'TestPlayer.queues != null' in source
    set_ai = source[source.index('private static void BeforeSetAi'):source.index('private static Exception ValueBonusFailed')]
    assert set_ai.count('Pr8Diagnostic.BestEffort') == 2 and 'return __exception;' in set_ai
    print('PASS: isolated Player owns callbacks while a complete PC remains selected until native Create returns; SetAI probes preserve exceptions')

    inputs = (ROOT / 'src/cases/Pr8NativeInputs.cs').read_text(encoding='utf-8-sig')
    assert '!row.isOrigin && !row.isChara' in inputs
    assert 'ReferenceEquals(thingRow, row)' in inputs and 'ReferenceEquals(cardRow, row)' in inputs
    ordered(inputs, 'Record("create_returned"', 'requireOwned(item)', 'item.id == id && ReferenceEquals(item.source, requested)', 'item.SetNum(num)', 'item.Num == num')
    assert 'item.material != null' in inputs and 'ctx.Logs.Count < 44' in inputs
    for field in ('requestedId', 'actualId', 'category', 'trait', 'num', 'materialId', 'materialAlias', 'isOrigin'):
        assert field in inputs
    assert 'Pr8Diagnostic.BestEffort(() =>' in inputs and 'data = readData()' in inputs
    offering = source[source.index('public Thing Offering'):source.index('public int SplitSize')]
    for condition in ('Pr8NativeInputs.ConcreteRows()', 'Pr8NativeInputs.Matches(item.source, category)', 'Oracle.CanOffer(Actor, item)', '!matches || unit <= 0 || !accepted || !bounded'):
        assert condition in offering
    ingredient = source[source.index('public Thing ItemForIngredient'):source.index('public void RemoveSleep')]
    assert 'ingredient.IsValidIngredient(item)' in ingredient and 'if (!valid) { item.Destroy(); continue; }' in ingredient
    assert reload.count('inputs.CreateExact(') == 3
    print('PASS: registered concrete input identity retained; actual semantic category/faith/bounded split and native recipe validity required; all eleven case inputs diagnosed')

    assert 'int batch = unit > 0 ?' in offering and 'ordinaryFoodBranch = ordinary' in offering
    def verify_input_guards(payload):
        assert '!row.isOrigin && !row.isChara' in payload
        for required in ('ReferenceEquals(thingRow, row)', 'ReferenceEquals(cardRow, row)', 'item.id == id && ReferenceEquals(item.source, requested)', 'requireOwned(item)', 'item.material != null', 'item.Num == num'):
            assert required in payload
    verify_input_guards(inputs)
    for label, bad in {
        'origin prototype generation': inputs.replace('!row.isOrigin && !row.isChara', '!row.isChara'),
        'cards/things map mismatch': inputs.replace('ReferenceEquals(cardRow, row)', 'true'),
        'concrete fallback identity': inputs.replace('item.id == id && ReferenceEquals(item.source, requested)', 'true'),
        'foreign input mutation': inputs.replace('requireOwned(item)', '/* ownership omitted */'),
        'missing material': inputs.replace('item.material != null', 'true'),
        'wrong configured quantity': inputs.replace('item.Num == num', 'true'),
    }.items():
        try: verify_input_guards(bad)
        except AssertionError: print('PASS: rejects ' + label)
        else: raise AssertionError('Input counterexample falsely passed: ' + label)

    source_check = source[source.index('public static class Pr8SourceChecks'):]
    assert 'SerializeObject(chest)' not in source_check and 'SerializeObject(baseArray)' not in source_check
    assert 'fieldSnapshot.Values(baseArray) == fieldSnapshot.Values(customArray)' in source_check
    ordered(source_check, 'string beforeChest = fieldSnapshot.State(chest)', 'source.Init()', 'fieldSnapshot.State(chest) == beforeChest')
    assert '!ReferenceEquals(baseArray, customArray)' in source_check
    assert 'Pr8SourceSpriteCache.AssertIndependent(chest, custom)' in source_check
    assert '!Pr8SourceSpriteCache.IsField(field)' in source_check
    field_state = (ROOT / 'src/cases/Pr8SourceSnapshot.cs').read_text(encoding='utf-8-sig')
    assert 'GetProperties(' not in field_state and 'ReferenceLoopHandling.Ignore' not in field_state
    assert 'BindingFlags.DeclaredOnly' in field_state and 'property.Ignored = false' in field_state
    assert 'PreserveReferencesHandling.All' in field_state and 'ReferenceLoopHandling.Serialize' in field_state
    assert 'UnityEngine.Color color = material.color' in field_state
    assert 'field.DeclaringType == typeof(RenderRow)' in field_state and 'field.IsNotSerialized' in field_state
    print('PASS: source values/all instance fields/reference cycles compared safely; inherited array identity/value assertions retained')
