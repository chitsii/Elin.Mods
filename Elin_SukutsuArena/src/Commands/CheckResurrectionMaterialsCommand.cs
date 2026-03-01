using System.Linq;
using System.Text;
using System.Collections.Generic;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Flags;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// 蘇りの儀式に必要な素材をチェックするコマンド
    /// 使用: modInvoke check_resurrection_materials()
    ///
    /// 設定するセッションフラグ:
    /// - sukutsu_has_all_materials (0/1): 全素材所持
    /// - sukutsu_elixir_count: エリクシル所持数
    /// - sukutsu_loveplus_count: 産卵薬所持数
    /// - sukutsu_chicken_count: 鶏パーティ数
    /// </summary>
    public class CheckResurrectionMaterialsCommand : IArenaCommand
    {
        public string Name => "check_resurrection_materials";

        // Required item IDs (vanilla)
        private static readonly string[] ELIXIR_IDS = { "1264", "potion_EternalYouth" }; // 不老長寿のエリクシル
        private static readonly string[] LOVEPLUS_IDS = { "1254", "potion_LovePlus" };   // 産卵薬
        private const string CHICKEN_ID = "chicken";  // 鶏

        // Required quantities (for 2 resurrections)
        private const int REQUIRED_ELIXIR = 2;
        private const int REQUIRED_LOVEPLUS = 2;
        private const int REQUIRED_CHICKEN = 2;

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            // Count elixirs in player inventory
            int elixirCount = CountItemsByIds(ELIXIR_IDS);

            // Count loveplus potions in player inventory
            int loveplusCount = CountItemsByIds(LOVEPLUS_IDS);

            // Count chickens in party
            int chickenCount = CountPartyChickens();

            // Check if all materials are present
            bool hasAll = elixirCount >= REQUIRED_ELIXIR
                       && loveplusCount >= REQUIRED_LOVEPLUS
                       && chickenCount >= REQUIRED_CHICKEN;

            // Set session flags via ArenaContext storage
            ctx.Storage.SetInt(SessionFlagKeys.HasAllMaterials, hasAll ? 1 : 0);
            ctx.Storage.SetInt(SessionFlagKeys.ElixirCount, elixirCount);
            ctx.Storage.SetInt(SessionFlagKeys.LoveplusCount, loveplusCount);
            ctx.Storage.SetInt(SessionFlagKeys.ChickenCount, chickenCount);

            ModLog.Log($"[CheckResurrectionMaterials] Elixir: {elixirCount}/{REQUIRED_ELIXIR}, " +
                      $"Loveplus: {loveplusCount}/{REQUIRED_LOVEPLUS}, " +
                      $"Chicken: {chickenCount}/{REQUIRED_CHICKEN}, HasAll: {hasAll}");

            // Detailed diagnostics for Player.log
            LogMaterialDiagnostics(elixirCount, loveplusCount, chickenCount);
        }

        private int CountItemsByIds(string[] itemIds)
        {
            int count = 0;
            var pc = EClass.pc;
            if (pc?.things == null) return 0;

            var resolvedSources = ResolveThingSources(itemIds);

            foreach (var thing in EnumerateInventoryThings(pc))
            {
                if (MatchesThingId(thing, itemIds, resolvedSources))
                {
                    count += thing.Num;
                }
            }
            return count;
        }

        private bool MatchesThingId(Thing thing, string[] ids, object[] resolvedSources)
        {
            if (thing == null) return false;
            foreach (var id in ids)
            {
                if (thing.id == id) return true;
            }

            var source = thing.source;
            if (source == null) return false;

            // Compare against resolved source rows (from EClass.sources.things)
            if (resolvedSources != null && resolvedSources.Length > 0)
            {
                foreach (var resolved in resolvedSources)
                {
                    if (resolved == null) continue;
                    if (ReferenceEquals(source, resolved)) return true;

                    if (TryGetSourceId(source, out var sourceId) &&
                        TryGetSourceId(resolved, out var resolvedId) &&
                        sourceId == resolvedId)
                    {
                        return true;
                    }
                }
            }

            // Try source.id (int) and source.alias (string) via reflection to be robust across versions
            if (TryGetSourceId(source, out var sid))
            {
                foreach (var id in ids)
                {
                    if (int.TryParse(id, out var intId) && intId == sid) return true;
                }
            }

            if (TryGetSourceAlias(source, out var alias))
            {
                foreach (var id in ids)
                {
                    if (alias == id) return true;
                }
            }

            return false;
        }

        private void LogMaterialDiagnostics(int elixirCount, int loveplusCount, int chickenCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[CheckResurrectionMaterials][Diag] ==== Material Diagnostics ====");
            sb.AppendLine($"[CheckResurrectionMaterials][Diag] Elixir total: {elixirCount}, required: {REQUIRED_ELIXIR}");
            sb.AppendLine($"[CheckResurrectionMaterials][Diag] Loveplus total: {loveplusCount}, required: {REQUIRED_LOVEPLUS}");
            sb.AppendLine($"[CheckResurrectionMaterials][Diag] Chicken total: {chickenCount}, required: {REQUIRED_CHICKEN}");

            DumpMatchingItems(sb, "Elixir", ELIXIR_IDS);
            DumpMatchingItems(sb, "Loveplus", LOVEPLUS_IDS);
            DumpPartyChickens(sb);

            if (elixirCount < REQUIRED_ELIXIR || loveplusCount < REQUIRED_LOVEPLUS)
            {
                DumpInventorySnapshot(sb, 120);
            }

            ModLog.Log(sb.ToString());
        }

        private void DumpMatchingItems(StringBuilder sb, string label, string[] ids)
        {
            var pc = EClass.pc;
            if (pc?.things == null)
            {
                sb.AppendLine($"[CheckResurrectionMaterials][Diag] {label}: pc or inventory not found");
                return;
            }

            sb.AppendLine($"[CheckResurrectionMaterials][Diag] {label}: checking ids [{string.Join(", ", ids)}]");
            int matchCount = 0;
            var resolvedSources = ResolveThingSources(ids);
            foreach (var thing in EnumerateInventoryThings(pc))
            {
                if (!MatchesThingId(thing, ids, resolvedSources)) continue;
                matchCount++;

                var source = thing?.source;
                string sourceId = TryGetSourceId(source, out var sid) ? sid.ToString() : "?";
                string sourceAlias = TryGetSourceAlias(source, out var salias) ? salias ?? "?" : "?";

                sb.AppendLine($"[CheckResurrectionMaterials][Diag] {label} match: thing.id={thing?.id ?? "null"}, " +
                              $"num={thing?.Num ?? 0}, source.id={sourceId}, source.alias={sourceAlias}");
            }

            if (matchCount == 0)
            {
                sb.AppendLine($"[CheckResurrectionMaterials][Diag] {label}: no matching items found in inventory");
            }
        }

        private void DumpPartyChickens(StringBuilder sb)
        {
            var party = EClass.pc?.party;
            if (party == null)
            {
                sb.AppendLine("[CheckResurrectionMaterials][Diag] Party not found");
                return;
            }

            int members = 0;
            int chickens = 0;
            foreach (var member in party.members)
            {
                members++;
                var id = member?.id ?? "null";
                if (id == CHICKEN_ID) chickens++;
                sb.AppendLine($"[CheckResurrectionMaterials][Diag] Party member: id={id}");
            }

            sb.AppendLine($"[CheckResurrectionMaterials][Diag] Party members: {members}, chickens: {chickens}");
        }

        private void DumpInventorySnapshot(StringBuilder sb, int maxItems)
        {
            var pc = EClass.pc;
            if (pc?.things == null)
            {
                sb.AppendLine("[CheckResurrectionMaterials][Diag] Inventory snapshot: pc or inventory not found");
                return;
            }

            sb.AppendLine($"[CheckResurrectionMaterials][Diag] Inventory snapshot (up to {maxItems} items):");
            int count = 0;
            foreach (var thing in EnumerateInventoryThings(pc))
            {
                if (count >= maxItems)
                {
                    sb.AppendLine("[CheckResurrectionMaterials][Diag] Inventory snapshot truncated");
                    break;
                }

                var source = thing?.source;
                string sourceId = TryGetSourceId(source, out var sid) ? sid.ToString() : "?";
                string sourceAlias = TryGetSourceAlias(source, out var salias) ? salias ?? "?" : "?";
                string sourceName = "?";
                if (source != null)
                {
                    var sourceType = source.GetType();
                    var getName = sourceType.GetMethod("GetName", System.Type.EmptyTypes);
                    if (getName?.ReturnType == typeof(string))
                    {
                        sourceName = getName.Invoke(source, null) as string ?? "?";
                    }
                    else
                    {
                        var nameJpField = sourceType.GetField("name_JP");
                        if (nameJpField?.FieldType == typeof(string))
                        {
                            sourceName = nameJpField.GetValue(source) as string ?? "?";
                        }
                        else
                        {
                            var nameField = sourceType.GetField("name");
                            if (nameField?.FieldType == typeof(string))
                            {
                                sourceName = nameField.GetValue(source) as string ?? "?";
                            }
                        }
                    }
                }

                sb.AppendLine($"[CheckResurrectionMaterials][Diag] Inv: thing.id={thing?.id ?? "null"}, " +
                              $"num={thing?.Num ?? 0}, source.id={sourceId}, source.alias={sourceAlias}, name={sourceName}");
                count++;
            }
        }

        private IEnumerable<Thing> EnumerateInventoryThings(Chara pc)
        {
            if (pc?.things != null)
            {
                foreach (var t in EnumerateContainer(pc.things))
                    yield return t;
            }

            var slots = pc?.body?.slots;
            if (slots == null) yield break;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot?.thing == null) continue;
                if (!slot.thing.IsContainer || slot.thing.things is { Count: 0 }) continue;

                foreach (var t in EnumerateContainer(slot.thing.things))
                    yield return t;
            }
        }

        private IEnumerable<Thing> EnumerateContainer(ThingContainer things)
        {
            for (int i = 0; i < things.Count; i++)
            {
                var t = things[i];
                if (t == null) continue;
                yield return t;

                if (t.IsContainer && t.things is { Count: > 0 })
                {
                    foreach (var child in EnumerateContainer(t.things))
                        yield return child;
                }
            }
        }

        private object[] ResolveThingSources(string[] ids)
        {
            var sources = EClass.sources?.things;
            if (sources == null || ids == null || ids.Length == 0) return System.Array.Empty<object>();

            var srcType = sources.GetType();
            var aliasField = srcType.GetField("alias");
            var aliasProp = srcType.GetProperty("alias");
            var mapField = srcType.GetField("map");
            var mapProp = srcType.GetProperty("map");

            var aliasMap = (aliasField?.GetValue(sources) ?? aliasProp?.GetValue(sources, null)) as System.Collections.IDictionary;
            var map = (mapField?.GetValue(sources) ?? mapProp?.GetValue(sources, null)) as System.Collections.IDictionary;

            var list = new System.Collections.Generic.List<object>();
            foreach (var id in ids)
            {
                if (aliasMap != null && aliasMap.Contains(id))
                {
                    list.Add(aliasMap[id]);
                    continue;
                }

                if (int.TryParse(id, out var intId) && map != null && map.Contains(intId))
                {
                    list.Add(map[intId]);
                }
            }

            return list.ToArray();
        }

        private bool TryGetSourceId(object source, out int id)
        {
            id = 0;
            if (source == null) return false;
            var sourceType = source.GetType();
            var idField = sourceType.GetField("id");
            if (idField?.FieldType == typeof(int))
            {
                id = (int)idField.GetValue(source);
                return true;
            }

            var idProp = sourceType.GetProperty("id");
            if (idProp?.PropertyType == typeof(int))
            {
                id = (int)idProp.GetValue(source, null);
                return true;
            }

            return false;
        }

        private bool TryGetSourceAlias(object source, out string alias)
        {
            alias = null;
            if (source == null) return false;
            var sourceType = source.GetType();
            var aliasField = sourceType.GetField("alias");
            if (aliasField?.FieldType == typeof(string))
            {
                alias = (string)aliasField.GetValue(source);
                return true;
            }

            var aliasProp = sourceType.GetProperty("alias");
            if (aliasProp?.PropertyType == typeof(string))
            {
                alias = (string)aliasProp.GetValue(source, null);
                return true;
            }

            return false;
        }

        private int CountPartyChickens()
        {
            int count = 0;
            var party = EClass.pc?.party;
            if (party == null) return 0;

            foreach (var member in party.members)
            {
                if (member?.id == CHICKEN_ID)
                {
                    count++;
                }
            }
            return count;
        }
    }
}

