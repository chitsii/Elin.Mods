# Servant System Specification

## Design Principles

- **No party slot consumption**: Servants don't occupy pet slots. They travel via `listSummon` (c_uidMaster + MinionType.Default).
- **No summon limit**: Servants are excluded from `Zone.CountMinions` so they don't count against MaxSummon.
- **Any character**: Uniques, bosses, and regular characters can all become servants.
- **Persistent**: Servants survive save/load and zone transitions via globalCharas + SetGlobal.
- **Revivable**: Dead servants auto-revive in homeZone or can be revived via scroll/tavern.
- **Correct API order**: Uses `AddMemeber → MakeMinion` instead of `_MakeAlly` + manual c_uidMaster.
- **Soul economy**: Souls serve as shared currency between spell unlocks and servant creation, providing natural cost constraints.

## Creation Flow (PerformRitual)

```
PerformRitual(corpse, soulAmounts)
  → CalculateTotalSP(soulAmounts)           // Sum SP from soul types × quantities
  → CalculateFinalLevel(totalSP)            // floor(K × sqrt(SP)), capped by deepest × 1.5
  → CharaGen.Create(sourceId, level)        // Create at calculated level
  → AddCard(servant, pc.pos)                // Place in zone
  → homeBranch.AddMemeber(servant)          // SetGlobal + SetFaction(Home) + homeZone
    [or SetGlobal + SetFaction fallback]
  → Set ally attributes                     // hostility, orgPos, isSummon=false
  → MakeMinion(pc)                          // c_uidMaster = pc.uid, MinionType.Default
  → isSummon = false                        // Ensure permanent
  → c_idTrait = "TraitUndeadServant"        // Block party invite dialogue
  → AddServant(uid)                         // Track in config
  → ConsumeSouls(soulAmounts)               // Consume from PC inventory
```

**Why this order works**:
1. `AddMemeber` calls `SetGlobal()` internally, which resets `c_uidMaster = 0`. This is fine because c_uidMaster hasn't been set yet.
2. `MakeMinion(pc)` calls `ReleaseMinion()` (c_uidMaster already 0, no-op) then sets `c_uidMaster = pc.uid`.
3. Result: Global + Home faction + homeZone + c_uidMaster + MinionType.Default — all set correctly.

## Release Flow (ReleaseServant)

```
RemoveServant(uid)                        // Remove from tracking
  → ReleaseMinion()                       // Clear c_uidMaster, remove from listSummon
  → party.RemoveMember (safety)           // Servants shouldn't be in party
  → homeBranch.RemoveMemeber              // Clears bed, sales, global, faction
  → RemoveGlobal()                        // Fallback for uniques
  → Destroy()                             // Hard-despawn servant card (no Chara.Die dependency)
```

## Zone Transition

1. PC calls `MoveZone`
2. `listSummon` collects characters where `c_uidMaster == pc.uid && MinionType.Default`
3. Servants are automatically placed in the new zone

## AI Follow

- `AI_Idle` detects master via `c_uidMaster`
- `MinionType.Default` + `PetFollow` logic makes servant follow PC

## Death

- `Die()` sets `isDead = true`
- Servant stays in `globalCharas` (not destroyed, since `isSummon = false`)

## Revival

### Passive (Zone.Revive)
- Zone checks: `isDead && homeZone == thisZone` → `Revive()`
- Servant auto-revives when PC returns to homeZone

### Active (scroll/tavern)
- `GetRevived()` → `Revive()` → `AddMemeber` (blocked by patch) → Postfix safety net
- `c_uidMaster` persists through death → `listSummon` auto-resumes follow

### Safety Net (Patch_Chara_Revive_RestoreMaster)
- If `c_uidMaster` is lost during revival, the postfix restores it
- Fires a Warning log — indicates a design issue if triggered

## Temporary Summons (ActSummonUndead)

- Uses `MakeMinion(pc) + SetSummon(duration)` — standard vanilla pattern
- On death: `Destroy()` removes from globalCharas
- `GetAliveServants()` detects null → removes orphaned UID automatically
- Subject to MaxSummon limit for summon skills.
- `Zone.CountMinions` exclusion patch applies only to permanent ritual servants (`!isSummon`).

## Servant-Core Harmony Patches (9 total)

| Patch | Target | Type | Purpose |
|-------|--------|------|---------|
| `Patch_Party_AddMemeber_BlockServant` | `Party.AddMemeber` | Prefix | Block permanent servants from joining party |
| `Patch_Chara_Revive_RestoreMaster` | `Chara.Revive` | Postfix | Safety net: restore c_uidMaster if lost |
| `Patch_Chara_GetRevived_ServantInPlace` | `Chara.GetRevived` | Postfix | Pull revived servants back to current PC zone |
| `Patch_Zone_CountMinions_ExcludeServants` | `Zone.CountMinions` | Postfix | Exclude permanent servants from summon limit |
| `Patch_Card_RefreshColor_ServantTint` | `Card.RefreshColor` | Postfix | Reapply servant tint + loop aura after renderer rebuild |
| `Patch_Zone_Activate_KnightEncounter` | `Zone.Activate` | Postfix | Restore dormant/visual state on zone transitions |
| `Patch_Tactics_Ctor_ServantOverride` | `Tactics..ctor(Chara)` | Postfix | Apply per-servant custom tactics |
| `Patch_Chara_SetAIAggro_DormantBlock` | `Chara.SetAIAggro` | Prefix | Block aggro for dormant servants |
| `Patch_Chara_ChooseNewGoal_DormantBlock` | `Chara.ChooseNewGoal` | Prefix | Force dormant servants to stay on `NoGoal` |

## TraitUndeadServant

- Inherits `TraitChara` (not `Trait`)
- `CanJoinParty => false` — hides "invite to party" in dialogue
- Applied via `c_idTrait = "TraitUndeadServant"` + `ApplyTrait()` in PerformRitual
- Note: Replaces the original trait (e.g., TraitUniqueChara), losing special trait behaviors

## Enhancement System

従者の蘇生レベル制御、アトリビュート強化、部位増設、暴走リスクについては別仕様書を参照:

→ [enhancement-system.md](./enhancement-system.md)

## Persistence

- Servant UIDs stored in BepInEx ConfigFile: `ServantUIDs` (comma-separated)
- Enhancement data stored in: `EnhancementData` (custom serialized format)
- Character data persists via vanilla `globalCharas` (SetGlobal)
- Body part additions persist via vanilla `rawSlots` serialization
- `GetAliveServants()` performs lazy cleanup of orphaned UIDs (also cleans enhancement data)

## Known Constraints

- **Trait replacement**: Applying TraitUndeadServant replaces the original trait. Unique/boss special behaviors tied to their original trait are lost.
- **Safety net patch**: If `Patch_Chara_Revive_RestoreMaster` fires frequently, it means some code path is clearing c_uidMaster during revival, and the root cause should be investigated.

## Vanilla API Reference

| API | Usage |
|-----|-------|
| `homeBranch.AddMemeber(chara)` | Register as home member (SetGlobal + faction + homeZone) |
| `chara.MakeMinion(master)` | Set c_uidMaster + MinionType.Default (zone follow + AI) |
| `chara.ReleaseMinion()` | Clear c_uidMaster, remove from master's listSummon |
| `chara.SetGlobal()` | Register in globalCharas (resets c_uidMaster=0!) |
| `chara.RemoveGlobal()` | Remove from globalCharas |
| `chara.SetFaction(faction)` | Set character faction |
| `chara.ApplyTrait()` | Apply trait from c_idTrait string |
| `Zone.CountMinions(chara)` | Count minions with c_uidMaster match |
