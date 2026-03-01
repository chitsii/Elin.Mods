using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;
using UnityEngine;

namespace Elin_ItemRelocator {
    // Candidate フラグ定義
    public static class CF {
        public const byte IsPCOwned = 1 << 0;     // PC所有アイテムか
        public const byte IsRelocatable = 1 << 1; // 基本的な移動可能条件を満たしているか
        public const byte IsPetOwned = 1 << 2;    // ペット所有アイテムか
    }

    // 軽量な候補構造体
    public struct Candidate {
        public Thing Thing;
        public byte Flags;
    }

    public class RelocatorManager : Singleton<RelocatorManager> {
        public Dictionary<string, RelocationProfile> Profiles = [];
        public const int CurrentProfileVersion = 1;

        private PresetRepository _repository = new();

        // Repository Delegates
        public void SavePreset(string name, RelocationProfile profile) => _repository.Save(name, profile);
        public RelocationProfile LoadPreset(string name) => _repository.Load(name);
        public List<string> GetPresetList() => _repository.ListAll();
        public void RenamePreset(string oldName, string newName) => _repository.Rename(oldName, newName);
        public void DeletePreset(string name) => _repository.Delete(name);

        public void Init() { }

        // Runtime-only profile retrieval
        public RelocationProfile GetProfile(Thing container) {
            if (container is null)
                return new();

            string key = GetProfileKey(container);

            if (Profiles.TryGetValue(key, out var profile)) {
                return profile;
            }

            var newProfile = new RelocationProfile {
                ContainerName = container.Name,
                Version = CurrentProfileVersion
            };
            Profiles[key] = newProfile;
            return newProfile;
        }

        public string GetProfileKey(Thing container) {
            if (container is null)
                return null;
            return string.Format("{0}_{1}", Game.id, container.uid);
        }

        // ===== 最適化: Candidate構造体リストとHashSet再利用 =====
        private List<Candidate> _cachedCandidates = new();
        private readonly HashSet<Thing> _seen = new();
        private readonly HashSet<Thing> _hotbarPool = new();

        public void BuildCache(RelocationProfile profile, Thing container) {
            _cachedCandidates.Clear();
            _seen.Clear();

            // 事前計算: Container側の判定
            bool destIsPC = IsPCOwned(container);
            bool destIsToolbelt = container.trait is TraitToolBelt;
            bool destIsLocked = container.c_lockLv != 0;
            bool destIsNPC = container.isNPCProperty;

            // Early exit: 移動不可なコンテナ
            if (destIsToolbelt || destIsLocked || destIsNPC)
                return;

            // GatherCandidates を直接インライン展開して高速化
            // Inventory Scope
            GatherFromContainer(EClass.pc.things, container, destIsPC, destIsNPC);

            // Body Slots (equipped containers)
            var slots = EClass.pc.body.slots;
            for (int i = 0; i < slots.Count; i++) {
                var slot = slots[i];
                if (slot.thing is null || !slot.thing.IsContainer || slot.thing.things.Count == 0)
                    continue;
                if (slot.thing == container)
                    continue;
                GatherFromContainer(slot.thing.things, container, destIsPC, destIsNPC);
            }

            // Zone Scope
            GatherFromContainer(EClass._map.things, container, destIsPC, destIsNPC, includeInstalledChildren: true);

            // Pet Scope (Accompanying Party Members)
            if (EClass.pc.party != null) {
                foreach (var member in EClass.pc.party.members) {
                    if (member == null || member.IsPC)
                        continue;
                    if (!member.IsAliveInCurrentZone)
                        continue;

                    // Pet Inventory
                    GatherFromContainer(member.things, container, destIsPC, destIsNPC, isPetOwned: true);

                    // Pet Body Slots (equipped containers like backpacks)
                    var petSlots = member.body.slots;
                    for (int i = 0; i < petSlots.Count; i++) {
                        var slot = petSlots[i];
                        if (slot.thing is null || !slot.thing.IsContainer || slot.thing.things.Count == 0)
                            continue;
                        if (slot.thing == container)
                            continue;
                        GatherFromContainer(slot.thing.things, container, destIsPC, destIsNPC, isPetOwned: true);
                    }
                }
            }
        }

        private void GatherFromContainer(ThingContainer things, Thing container, bool destIsPC, bool destIsNPC, bool includeInstalledChildren = false, bool isPetOwned = false) {
            for (int i = 0; i < things.Count; i++) {
                var t = things[i];
                if (t == container)
                    continue;
                TryAddCandidate(t, container, destIsPC, destIsNPC, isPetOwned);

                // Recurse into containers
                bool recurse = t.IsContainer && t.things is { Count: > 0 };
                if (includeInstalledChildren)
                    recurse = recurse && t.placeState == PlaceState.installed;

                if (recurse) {
                    for (int j = 0; j < t.things.Count; j++) {
                        var child = t.things[j];
                        if (child == container)
                            continue;
                        TryAddCandidate(child, container, destIsPC, destIsNPC, isPetOwned);
                    }
                }
            }
        }

        // Overload for List<Thing> (e.g., EClass._map.things)
        private void GatherFromContainer(List<Thing> things, Thing container, bool destIsPC, bool destIsNPC, bool includeInstalledChildren = false, bool isPetOwned = false) {
            for (int i = 0; i < things.Count; i++) {
                var t = things[i];
                if (t == container)
                    continue;
                TryAddCandidate(t, container, destIsPC, destIsNPC, isPetOwned);

                // Recurse into containers
                bool recurse = t.IsContainer && t.things is { Count: > 0 };
                if (includeInstalledChildren)
                    recurse = recurse && t.placeState == PlaceState.installed;

                if (recurse) {
                    for (int j = 0; j < t.things.Count; j++) {
                        var child = t.things[j];
                        if (child == container)
                            continue;
                        TryAddCandidate(child, container, destIsPC, destIsNPC, isPetOwned);
                    }
                }
            }
        }

        private void TryAddCandidate(Thing t, Thing container, bool destIsPC, bool destIsNPC, bool isPetOwned = false) {
            // Deduplication
            if (!_seen.Add(t))
                return;

            // 安価なチェックを先頭に
            if (t.isDestroyed)
                return;
            if (t.c_lockedHard)
                return;
            if (t.c_isImportant)
                return;
            if (t.placeState == PlaceState.installed)
                return;

            // 型判定と再帰は後
            if (t.trait is TraitToolBelt)
                return;
            if (t.trait is TraitAbility && !destIsPC)
                return;
            if (!t.trait.CanBeDropped)
                return;
            if (t.isEquipped && t.IsCursed)
                return;
            if (t.IsContainer && t.things.Count > 0 && !destIsPC)
                return;

            if (!destIsPC) {
                if (t.id == "money")
                    return;
                if (t.isGifted || t.isNPCProperty)
                    return;
            }

            // フラグ計算: GetRootCard() は1回だけ
            bool tOwned = t.GetRootCard() == EClass.pc;

            _cachedCandidates.Add(new Candidate {
                Thing = t,
                Flags = (byte)((tOwned ? CF.IsPCOwned : 0) | (isPetOwned ? CF.IsPetOwned : 0) | CF.IsRelocatable)
            });
        }

        public void ClearCache() {
            _cachedCandidates.Clear();
        }

        public IEnumerable<Thing> GetMatches(Thing container, int searchLimit = -1) {
            var profile = GetProfile(container);
            if (profile is null || !profile.Enabled)
                return Array.Empty<Thing>();

            // Ensure Cache Exists
            if (_cachedCandidates.Count == 0) {
                BuildCache(profile, container);
            }

            // ホットバーアイテムを再利用HashSetに収集
            _hotbarPool.Clear();
            var bars = EClass.player.hotbars.bars;
            for (int b = 0; b < bars.Length; b++) {
                var bar = bars[b];
                if (bar is null)
                    continue;
                var pages = bar.pages;
                for (int p = 0; p < pages.Count; p++) {
                    var items = pages[p].items;
                    for (int i = 0; i < items.Count; i++) {
                        var item = items[i];
                        if (item is { Thing: not null })
                            _hotbarPool.Add(item.Thing);
                    }
                }
            }

            List<Thing> matches = new();
            var scope = profile.Scope;
            var rules = profile.Rules;
            int ruleCount = rules.Count;
            if (ruleCount == 0)
                return matches;

            // 高速イテレーション
            for (int i = 0; i < _cachedCandidates.Count; i++) {
                var c = _cachedCandidates[i];

                // コンテナを除外
                if (c.Thing.IsContainer)
                    continue;

                // スコープ判定 (高速: ビット演算)
                bool isOwned = (c.Flags & CF.IsPCOwned) != 0;
                bool isPet = (c.Flags & CF.IsPetOwned) != 0;

                if (scope == RelocationProfile.FilterScope.Inventory && !isOwned)
                    continue;
                if (scope == RelocationProfile.FilterScope.ZoneOnly && (isOwned || isPet))
                    continue;
                if (scope == RelocationProfile.FilterScope.PetsOnly && !isPet)
                    continue;
                if (scope == RelocationProfile.FilterScope.Both && isPet) // Both = PC + Zone (exclude Pets)
                    continue;

                // ホットバー判定 (高速: HashSet参照)
                if (_hotbarPool.Contains(c.Thing))
                    continue;

                // Limit Check
                if (searchLimit > 0 && matches.Count >= searchLimit)
                    break;

                // ルール判定 (最も重い処理)
                bool match = false;
                for (int r = 0; r < ruleCount; r++) {
                    if (rules[r].IsMatch(c.Thing)) {
                        match = true;
                        break;
                    }
                }
                if (match)
                    matches.Add(c.Thing);
            }

            // Apply Sorting
            ApplySorting(matches, profile);
            return matches;
        }

        private void ApplySorting(List<Thing> matches, RelocationProfile profile) {
            switch (profile.SortMode) {
            case RelocationProfile.ResultSortMode.PriceAsc:
                matches.Sort((a, b) => a.GetPrice() - b.GetPrice());
                break;
            case RelocationProfile.ResultSortMode.PriceDesc:
                matches.Sort((a, b) => b.GetPrice() - a.GetPrice());
                break;
            case RelocationProfile.ResultSortMode.TotalEnchantMagDesc:
                matches.Sort((a, b) => {
                    int valA = 0, valB = 0;
                    if (a.elements?.dict != null)
                        foreach (var e in a.elements.dict.Values)
                            if (e.Value > 0)
                                valA += e.Value;
                    if (b.elements?.dict != null)
                        foreach (var e in b.elements.dict.Values)
                            if (e.Value > 0)
                                valB += e.Value;
                    return valB - valA;
                });
                break;
            case RelocationProfile.ResultSortMode.EnchantMagAsc:
            case RelocationProfile.ResultSortMode.EnchantMagDesc:
                List<int> targetEleIds = GetTargetEnchantIDs(profile);
                matches.Sort((a, b) => {
                    int valA = 0, valB = 0;
                    for (int i = 0; i < targetEleIds.Count; i++) {
                        int id = targetEleIds[i];
                        valA += a.elements.Value(id);
                        valB += b.elements.Value(id);
                    }
                    return profile.SortMode == RelocationProfile.ResultSortMode.EnchantMagDesc
                        ? valB - valA : valA - valB;
                });
                break;
            case RelocationProfile.ResultSortMode.TotalWeightAsc:
                matches.Sort((a, b) => (a.ChildrenAndSelfWeight * a.Num) - (b.ChildrenAndSelfWeight * b.Num));
                break;
            case RelocationProfile.ResultSortMode.TotalWeightDesc:
                matches.Sort((a, b) => (b.ChildrenAndSelfWeight * b.Num) - (a.ChildrenAndSelfWeight * a.Num));
                break;
            case RelocationProfile.ResultSortMode.UnitWeightAsc:
                matches.Sort((a, b) => a.SelfWeight - b.SelfWeight);
                break;
            case RelocationProfile.ResultSortMode.UnitWeightDesc:
                matches.Sort((a, b) => b.SelfWeight - a.SelfWeight);
                break;
            case RelocationProfile.ResultSortMode.UidAsc:
                matches.Sort((a, b) => a.uid - b.uid);
                break;
            case RelocationProfile.ResultSortMode.UidDesc:
                matches.Sort((a, b) => b.uid - a.uid);
                break;
            case RelocationProfile.ResultSortMode.GenLvlAsc:
                matches.Sort((a, b) => a.genLv - b.genLv);
                break;
            case RelocationProfile.ResultSortMode.GenLvlDesc:
                matches.Sort((a, b) => b.genLv - a.genLv);
                break;
            case RelocationProfile.ResultSortMode.DnaAsc:
                matches.Sort((a, b) => (a.c_DNA?.cost ?? 0) - (b.c_DNA?.cost ?? 0));
                break;
            case RelocationProfile.ResultSortMode.DnaDesc:
                matches.Sort((a, b) => (b.c_DNA?.cost ?? 0) - (a.c_DNA?.cost ?? 0));
                break;
            case RelocationProfile.ResultSortMode.FoodPowerAsc:
            case RelocationProfile.ResultSortMode.FoodPowerDesc:
                List<int> foodIds = GetTargetFoodElements(profile);
                matches.Sort((a, b) => {
                    int valA = 0, valB = 0;
                    if (a.elements != null)
                        for (int i = 0; i < foodIds.Count; i++)
                            valA += GetLevelValue(a.elements.Value(foodIds[i]));
                    if (b.elements != null)
                        for (int i = 0; i < foodIds.Count; i++)
                            valB += GetLevelValue(b.elements.Value(foodIds[i]));
                    return profile.SortMode == RelocationProfile.ResultSortMode.FoodPowerDesc
                        ? valB - valA : valA - valB;
                });
                break;
            case RelocationProfile.ResultSortMode.TotalFoodPowerAsc:
            case RelocationProfile.ResultSortMode.TotalFoodPowerDesc:
                matches.Sort((a, b) => {
                    int valA = 0, valB = 0;
                    if (a.elements?.dict != null)
                        foreach (var e in a.elements.dict.Values)
                            if (e.source.foodEffect is { Length: > 0 })
                                valA += GetLevelValue(e.Value);
                    if (b.elements?.dict != null)
                        foreach (var e in b.elements.dict.Values)
                            if (e.source.foodEffect is { Length: > 0 })
                                valB += GetLevelValue(e.Value);
                    return profile.SortMode == RelocationProfile.ResultSortMode.TotalFoodPowerDesc
                        ? valB - valA : valA - valB;
                });
                break;
            }
        }

        private int GetLevelValue(int raw) {
            if (raw == 0)
                return 0;
            int lvl = raw / 10;
            return (raw < 0) ? (lvl - 1) : (lvl + 1);
        }

        public bool IsPCOwned(Thing t) {
            if (t == null)
                return false;
            return t.GetRootCard() == EClass.pc;
        }

        public List<int> GetTargetEnchantIDs(RelocationProfile profile) {
            List<int> targetEleIds = new();
            var rules = profile.Rules;
            for (int ri = 0; ri < rules.Count; ri++) {
                var r = rules[ri];
                if (!r.Enabled)
                    continue;
                var conditions = r.Conditions;
                for (int ci = 0; ci < conditions.Count; ci++) {
                    var cond = conditions[ci];
                    if (cond is not ConditionEnchantOr ceo)
                        continue;
                    var runes = ceo.Runes;
                    for (int ti = 0; ti < runes.Count; ti++) {
                        var term = runes[ti];
                        if (string.IsNullOrEmpty(term))
                            continue;
                        ConditionRegistry.ParseKeyOp(term, out string key, out _, out _);
                        if (EClass.sources.elements.alias.TryGetValue(key, out var source)) {
                            if (!targetEleIds.Contains(source.id))
                                targetEleIds.Add(source.id);
                        }
                    }
                }
            }
            return targetEleIds;
        }

        public List<int> GetTargetFoodElements(RelocationProfile profile) {
            List<int> ids = new();
            var rules = profile.Rules;
            for (int ri = 0; ri < rules.Count; ri++) {
                var r = rules[ri];
                if (!r.Enabled)
                    continue;
                var conditions = r.Conditions;
                for (int ci = 0; ci < conditions.Count; ci++) {
                    var cond = conditions[ci];
                    if (cond is not ConditionFoodElement cfe)
                        continue;
                    var elementIds = cfe.ElementIds;
                    foreach (var idStr in elementIds) {
                        ConditionRegistry.ParseKeyOp(idStr, out string key, out _, out _);
                        if (EClass.sources.elements.alias.TryGetValue(key, out var source)) {
                            if (!ids.Contains(source.id))
                                ids.Add(source.id);
                        }
                    }
                }
            }
            return ids;
        }

        public void ExecuteRelocation(Thing container) {
            var matches = new List<Thing>(GetMatches(container, -1));
            int count = 0;

            for (int i = 0; i < matches.Count; i++) {
                var t = matches[i];
                if (t.isDestroyed || t.c_isImportant || t.placeState == PlaceState.installed)
                    continue;

                if (container.things.IsFull()) {
                    Msg.Say(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_ContainerFull));
                    break;
                }

                container.AddThing(t);
                count++;
            }

            if (count > 0) {
                EClass.pc.PlaySound("grab");
                Msg.Say(string.Format(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_RelocatedResult), count, container.Name));
            } else {
                Msg.Say(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_NoMatchLog));
            }
        }

        public void RelocateSingleThing(Thing t, Thing container) {
            if (t.isDestroyed || t.c_isImportant)
                return;

            // 事前判定
            bool destIsPC = IsPCOwned(container);
            if (container.trait is TraitToolBelt || container.c_lockLv != 0 || container.isNPCProperty) {
                EClass.pc.PlaySound("beep");
                return;
            }

            // アイテム側の判定
            if (t.c_lockedHard || t.placeState == PlaceState.installed)
                return;
            if (t.trait is TraitToolBelt)
                return;
            if (t.trait is TraitAbility && !destIsPC)
                return;
            if (!t.trait.CanBeDropped)
                return;
            if (t.isEquipped && t.IsCursed)
                return;
            if (t.IsContainer && t.things.Count > 0 && !destIsPC)
                return;
            if (!destIsPC && (t.id == "money" || t.isGifted || t.isNPCProperty))
                return;

            if (container.things.IsFull()) {
                Msg.Say(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_ContainerFull));
                return;
            }

            container.AddThing(t);
            EClass.pc.PlaySound("grab");
            Msg.Say(string.Format(RelocatorLang.GetText(RelocatorLang.LangKey.Msg_Moved), t.Name));
        }

        public string GetDisplayValue(Thing t, RelocationProfile profile) {
            switch (profile.SortMode) {
            case RelocationProfile.ResultSortMode.PriceAsc:
            case RelocationProfile.ResultSortMode.PriceDesc:
                return t.GetValue().ToString("#,0") + " gp";
            case RelocationProfile.ResultSortMode.TotalWeightAsc:
            case RelocationProfile.ResultSortMode.TotalWeightDesc:
                return (t.ChildrenAndSelfWeight * t.Num * 0.001f).ToString("0.0") + "s";
            case RelocationProfile.ResultSortMode.UnitWeightAsc:
            case RelocationProfile.ResultSortMode.UnitWeightDesc:
                return (t.SelfWeight * 0.001f).ToString("0.0") + "s";
            case RelocationProfile.ResultSortMode.DnaAsc:
            case RelocationProfile.ResultSortMode.DnaDesc:
                return (t.c_DNA?.cost ?? 0).ToString();
            case RelocationProfile.ResultSortMode.TotalEnchantMagDesc:
                int totalMag = 0;
                if (t.elements?.dict != null)
                    foreach (var e in t.elements.dict.Values)
                        if (e.Value > 0)
                            totalMag += e.Value;
                return "Total Mag: " + totalMag;
            case RelocationProfile.ResultSortMode.EnchantMagAsc:
            case RelocationProfile.ResultSortMode.EnchantMagDesc:
                List<int> targetEleIds = GetTargetEnchantIDs(profile);
                int val = 0;
                for (int i = 0; i < targetEleIds.Count; i++)
                    val += t.elements.Value(targetEleIds[i]);
                return "Mag: " + val;
            case RelocationProfile.ResultSortMode.UidAsc:
            case RelocationProfile.ResultSortMode.UidDesc:
                return "ID: " + t.uid.ToString();
            case RelocationProfile.ResultSortMode.GenLvlAsc:
            case RelocationProfile.ResultSortMode.GenLvlDesc:
                return "Lv " + t.genLv.ToString();
            case RelocationProfile.ResultSortMode.FoodPowerAsc:
            case RelocationProfile.ResultSortMode.FoodPowerDesc:
                List<int> fIds = GetTargetFoodElements(profile);
                int fVal = 0;
                if (t.elements != null)
                    for (int i = 0; i < fIds.Count; i++) {
                        int raw = t.elements.Value(fIds[i]);
                        if (raw != 0) {
                            int lvl = raw / 10;
                            fVal += (raw < 0) ? (lvl - 1) : (lvl + 1);
                        }
                    }
                return "Food Level: " + fVal;
            case RelocationProfile.ResultSortMode.TotalFoodPowerAsc:
            case RelocationProfile.ResultSortMode.TotalFoodPowerDesc:
                int totalF = 0;
                if (t.elements?.dict != null)
                    foreach (var e in t.elements.dict.Values)
                        if (e.source.foodEffect is { Length: > 0 }) {
                            int raw = e.Value;
                            if (raw != 0) {
                                int lvl = raw / 10;
                                totalF += (raw < 0) ? (lvl - 1) : (lvl + 1);
                            }
                        }
                return "Total Level: " + totalF;
            default:
                string info = "";
                if (t.SelfWeight > 0)
                    info += (t.SelfWeight * 0.001f).ToString("0.0") + "s ";
                if (!t.IsIdentified)
                    info += "(UnID) ";
                return info;
            }
        }
    }
}
