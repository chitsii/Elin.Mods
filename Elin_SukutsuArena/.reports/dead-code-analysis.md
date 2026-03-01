# Dead Code Analysis Report

**Project:** Elin_SukutsuArena
**Date:** 2026-02-01 (Updated)
**Previous:** 2026-01-28
**Analyzed Directory:** `src/` (C#) + `tools/` (Python)

---

## Summary

| Category | Count | Description |
|----------|-------|-------------|
| SAFE | 11 items | Clearly unused, can safely remove |
| CAUTION | 12 items | Verify before removal (may be called via reflection/Excel) |
| DANGER | 7 categories | Must NOT remove (entry points, Unity/CWL reflection) |

### Latest Update (2026-02-01) - Cleanup Completed

| Category | Count | Description |
|----------|-------|-------------|
| Deleted methods | 6 | GetCharacterWeaknesses + 5 debug helpers |
| False positive | 1 | GetAllGimmickDescriptions (used in drama - kept) |
| Drama updated | 1 | 99_debug_menu.py (removed unused menu option) |
| Lines removed | ~110 | C# + Python combined |

### Python Analysis (2026-01-28)

| Category | Count | Description |
|----------|-------|-------------|
| Build Scripts | 16 | All in use by build.bat |
| Utility Scripts | 11 | Manual tools (SAFE to keep) |
| Framework Modules | 2 | arena/, cwl_quest_lib/ - ALL IN USE |

---

## SAFE - Clearly Unused Code

### ~~WeaknessAnalyzer.GetCharacterWeaknesses~~ (2026-02-01) - DELETED

**File:** `src/RandomBattle/WeaknessAnalyzer.cs`

| Method | Status | Action Taken |
|--------|--------|--------------|
| `GetCharacterWeaknesses(Chara chara)` | Was marked "後方互換性のために残す" but unused | **DELETED** (38 lines) |

### ~~ArenaGimmickSystem.GetAllGimmickDescriptions~~ (ACTUALLY IN USE)

**File:** `src/RandomBattle/ArenaGimmick.cs:396-426`

| Method | Status | Reason |
|--------|--------|--------|
| `GetAllGimmickDescriptions(bool _)` | **IN USE** | Called from `tools/arena/scenarios/00_arena_master.py:971` via `eval` action |

**DO NOT DELETE** - Called from drama script for gimmick explanation dialog.

### ~~RandomBattleDebug Helper Methods~~ (2026-02-01) - DELETED

**File:** `src/Debug/RandomBattleDebug.cs`

| Method | Status | Action Taken |
|--------|--------|--------------|
| `ShowPartyWeaknesses()` | Was called from debug menu only | **DELETED** (+ drama reference removed) |
| `PreviewRandomBattle()` | Internal helper | **DELETED** |
| `TestGimmick()` | Called PreviewRandomBattle | **DELETED** |
| `ListAvailableGimmicks()` | UNUSED | **DELETED** |
| `ListAvailablePatterns()` | UNUSED | **DELETED** |

**Also updated:** `tools/arena/scenarios/99_debug_menu.py` - removed "弱点情報表示" menu option

---

### 1. JumpLabelMapping Methods (Partially Unused)

**File:** `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_SukutsuArena\src\Generated\JumpLabelMapping.cs`

| Method | Status | Reason |
|--------|--------|--------|
| `IsRegistered(string label)` | UNUSED | No references found in codebase |
| `ToString(JumpLabel label)` | UNUSED | No references found in codebase |

**Note:** Only `FromString()` is used (in `CheckQuestAvailableCommand.cs:42`).

### 2. ArenaCommandRegistry Methods (Partially Unused)

**File:** `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_SukutsuArena\src\Commands\ArenaCommandRegistry.cs`

| Method | Status | Reason |
|--------|--------|--------|
| `GetRegisteredCommands()` | UNUSED | No callers found - only defined |
| `IsRegistered(string name)` | UNUSED | No callers found - only defined |

### 3. ArenaQuestMarkerManager - Unused Field

**File:** `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_SukutsuArena\src\ArenaQuestMarkerManager.cs`

| Item | Line | Reason |
|------|------|--------|
| `NpcIdMappings` dictionary | 19 | Defined but never referenced |

```csharp
private static readonly Dictionary<string, string> NpcIdMappings = new Dictionary<string, string>
{
    { ArenaConfig.NpcIds.Lily, ArenaConfig.NpcIds.Lily },
    // ... redundant mapping to self
};
```

### 4. InMemoryFlagStorage.Count Property

**File:** `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_SukutsuArena\src\Core\InMemoryFlagStorage.cs`

| Property | Status | Reason |
|----------|--------|--------|
| `Count` | UNUSED | No references found |

### 5. BattleStageLoader.ClearCache() Method

**File:** `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_SukutsuArena\src\BattleStageConfig.cs`

| Method | Status | Reason |
|--------|--------|--------|
| `ClearCache()` | UNUSED | Debug utility never called |

### 6. TodaysBattleCache.ClearCache() Method

**File:** `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_SukutsuArena\src\RandomBattle\TodaysBattleCache.cs`

| Method | Status | Reason |
|--------|--------|--------|
| `ClearCache()` | UNUSED | Debug utility never called |

### 7. QuestManager.RemoveObserver() Method

**File:** `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_SukutsuArena\src\CwlQuestFramework\QuestManager.cs`

| Method | Status | Reason |
|--------|--------|--------|
| `RemoveObserver(IQuestStateObserver observer)` | UNUSED | No callers found |

### 8. ArenaQuestMarkerManager.Dispose() Method

**File:** `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_SukutsuArena\src\ArenaQuestMarkerManager.cs`

| Method | Status | Reason |
|--------|--------|--------|
| `Dispose()` | UNUSED | Never called - potential memory leak |

---

## CAUTION - Verify Before Removal

### 1. ArenaFlagHelpers Static Class

**File:** `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_SukutsuArena\src\Generated\ArenaFlags.cs`

The entire `ArenaFlagHelpers` class (line 150+) contains:
- 9x `ToFlagValue()` methods
- 9x `Parse*()` methods

| Method Group | Count | Notes |
|--------------|-------|-------|
| ToFlagValue overloads | 9 | May be used in drama scripts |
| Parse* methods | 9 | May be used in drama scripts |

**Warning:** These may be called from Excel drama scripts via reflection.

### 2. ArenaManager Story Choice Methods

**File:** `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_SukutsuArena\src\ArenaManager.cs`

These methods are likely called via `modInvoke` from Excel drama files:

| Method | Line | Check Excel Files |
|--------|------|-------------------|
| `GrantMakumaReward()` | 606 | drama_makuma.xlsx |
| `CompleteZekStealBottleQuest()` | 638 | drama_zek_steal_bottle.xlsx |
| `CompleteBalgasTrainingQuest()` | 654 | drama_balgas_training.xlsx |
| `Makuma2ConfessToLily()` | 674 | drama_makuma2.xlsx |
| `Makuma2BlameZek()` | 689 | drama_makuma2.xlsx |
| `Makuma2DenyInvolvement()` | 698 | drama_makuma2.xlsx |
| `Makuma2ConfessAboutKain()` | 740 | drama_makuma2.xlsx |
| `Makuma2LieAboutKain()` | 755 | drama_makuma2.xlsx |
| `Makuma2ChooseTrust()` | 763 | drama_makuma2.xlsx |
| `Makuma2ChooseKnowledge()` | 771 | drama_makuma2.xlsx |

**Verification Required:** Check `LangMod/JP/Dialog/Drama/drama_*.xlsx` for `modInvoke` action calls.

### 3. QuestDefinition Properties

**File:** `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_SukutsuArena\src\ArenaQuestManager.cs`

| Property | Line | Notes |
|----------|------|-------|
| `BranchChoices` | 364 | Set but never read in code |

---

## DANGER - Do NOT Remove

### 1. Plugin Entry Points

**File:** `src\Plugin.cs`

All public methods and the class itself are BepInEx entry points.

### 2. Trait Classes (CWL Reflection)

**Files:**
- `TraitArenaMaster.cs`
- `TraitSukutsuNPC.cs` (contains both `TraitSukutsuNPC` and `TraitSukutsuMerchant`)

Instantiated by CWL via string type matching from SourceChara.xlsx.

### 3. Feat Classes (CWL Reflection)

**File:** `src\Feats\FeatArenaSpirit.cs`

The `_OnApply` method is called by CWL via reflection.

### 4. Zone Classes (Elin/CWL Reflection)

**Files:**
- `Zone_SukutsuArena.cs`
- `Zone_FieldFine.cs`
- `Zone_FieldSnow.cs`

Zone types are instantiated via string type name from SourceZone.xlsx.

### 5. ZoneEvent and ZoneInstance Classes

**Files:**
- `ZoneEventArenaBattle.cs`
- `ZoneInstanceArenaBattle.cs`

Instantiated by Elin's zone system.

### 6. All Harmony Patches

Every class decorated with `[HarmonyPatch]` is loaded via reflection.

### 7. IArenaCommand Implementations

All command classes in `src\Commands\` are registered and invoked dynamically.

---

## Files Deleted in Git (Verify)

According to git status, these files are deleted:

```
D src/CwlAdapters/ElinDramaAdapter.cs
D src/CwlAdapters/ElinGameAdapter.cs
D src/CwlAdapters/IDramaAdapter.cs
D src/CwlAdapters/IGameAdapter.cs
```

**Status:** Intentionally deleted as part of a refactoring. Confirm no longer needed.

---

## Immediate Safe Cleanup Actions

### Code to Remove

```csharp
// JumpLabelMapping.cs - Remove these methods:
public static bool IsRegistered(string label) { ... }
public static string ToString(JumpLabel label) { ... }

// ArenaCommandRegistry.cs - Remove these methods:
public static IEnumerable<string> GetRegisteredCommands() { ... }
public static bool IsRegistered(string name) { ... }

// ArenaQuestMarkerManager.cs - Remove this field:
private static readonly Dictionary<string, string> NpcIdMappings = ...

// InMemoryFlagStorage.cs - Remove this property:
public int Count => _flags.Count;
```

**Estimated LOC reduction:** ~25-30 lines

---

## Cleanup Script (SAFE items only)

```bash
# After manual code edits, verify with build
cd C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_SukutsuArena
build.bat debug
```

---

## Analysis Methodology

1. Used `grep` to search for method/property references across codebase
2. Cross-referenced method definitions with call sites
3. Identified Unity/CWL reflection-based invocation patterns
4. Checked git status for deleted files
5. Noted Excel drama files as potential callers (cannot be statically analyzed)

---

## Notes on Excel Drama Files

Many C# methods are invoked from Excel drama files via the `modInvoke` action. These calls cannot be detected by static C# analysis. Before removing any CAUTION item, manually verify these folders:

- `LangMod/JP/Dialog/Drama/drama_*.xlsx`
- `LangMod/EN/Dialog/Drama/drama_*.xlsx`

Search for:
- `modInvoke` in the action column
- Method names in the param column

---

---

## Python Analysis (2026-01-28)

### Build Scripts (ALL IN USE)

All scripts in `tools/builder/` are referenced by `build.bat`:

| Script | Build Step | Status |
|--------|-----------|--------|
| `excel_diff_manager.py` | Step 0, Final | IN USE |
| `create_zone_excel.py` | Step 2 | IN USE |
| `create_chara_excel.py` | Step 3 | IN USE |
| `create_thing_excel.py` | Step 4 | IN USE |
| `create_merchant_stock.py` | Step 5 | IN USE |
| `create_element_excel.py` | Step 6 | IN USE |
| `create_stat_excel.py` | Step 7 | IN USE |
| `create_quest_excel.py` | Step 7.5 | IN USE |
| `create_bgm_json.py` | Step 8 | IN USE |
| `create_drama_excel.py` | Step 9 | IN USE |
| `generate_flags.py` | Step 10 | IN USE |
| `generate_jump_label_mapping.py` | Step 10 | IN USE |
| `generate_enum_mappings.py` | Step 10 | IN USE |
| `validate_scenario_flags.py` | Step 10 | IN USE |
| `generate_quest_data.py` | Step 10 | IN USE |
| `extract_dependencies.py` | Step 1.5 | IN USE |

### Utility Scripts (SAFE - Manual Development Tools)

These are standalone utilities not used in automated build but valuable for development:

| Script | Purpose | Status |
|--------|---------|--------|
| `tools/builder/diff_excel.py` | Excel diff helper | SAFE - imported by excel_diff_manager |
| `tools/builder/localize_drama.py` | Translation helper | SAFE - may be used manually |
| `tools/builder/verify_dependencies.py` | CI validation | SAFE - documented in MAINTENANCE.md |
| `tools/utils/check_excel.py` | Debug utility | SAFE - manual debugging |
| `tools/utils/check_headers.py` | Debug utility | SAFE - manual debugging |
| `tools/utils/compare_excel.py` | Debug utility | SAFE - manual debugging |
| `tools/utils/excel_diff.py` | Debug utility | SAFE - manual debugging |
| `tools/utils/inspect_excel.py` | Debug utility | SAFE - manual debugging |
| `tools/graphics/manage_images.py` | Image management | SAFE - asset pipeline |
| `tools/graphics/process_graphics.py` | Image processing | SAFE - asset pipeline |
| `tools/graphics/resize_images.py` | Image resizing | SAFE - asset pipeline |

### CwlQuestFramework Classes (SAFE - Framework Code)

These classes in `src/CwlQuestFramework/` are not directly called but provide framework infrastructure:

| Class | File | Status | Reason |
|-------|------|--------|--------|
| `GenericQuestDefinition` | QuestDefinition.cs | SAFE | Generic IQuestDefinition impl for CWL compat |
| `GenericFlagCondition` | FlagCondition.cs | SAFE | Generic IFlagCondition impl for CWL compat |
| `InMemoryFlagStorage` | Core/InMemoryFlagStorage.cs | SAFE | Test utility for unit testing |

**Note:** These are intentionally kept as framework infrastructure for potential future use or external mod compatibility.

---

## Verification Commands

```bash
# Build verification (run after any cleanup)
build.bat debug

# Python refactoring verification
cd tests
uv run python verify_refactoring.py

# Dependency verification
cd tools
uv run python builder/verify_dependencies.py
```

---

*Report generated by dead code analysis*
*Last updated: 2026-02-01*
