using System.Collections.Generic;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Flags;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// 蘇りの儀式で素材を消費するコマンド
    /// 使用: modInvoke consume_resurrection_materials()
    ///
    /// 処理:
    /// 1. 素材が十分あるか再確認
    /// 2. インベントリからエリクシル×2、産卵薬×2を消費
    /// 3. パーティから鶏を2羽除去・Destroy
    /// 4. 消費成功/失敗をフラグに設定
    ///
    /// 設定するフラグ:
    /// - sukutsu_consume_success (0/1): 消費成功フラグ
    /// </summary>
    public class ConsumeResurrectionMaterialsCommand : IArenaCommand
    {
        public string Name => "consume_resurrection_materials";

        // Required item IDs (vanilla)
        private static readonly string[] ELIXIR_IDS = { "1264", "potion_EternalYouth" }; // 不老長寿のエリクシル
        private static readonly string[] LOVEPLUS_IDS = { "1254", "potion_LovePlus" };   // 産卵薬
        private const string CHICKEN_ID = "chicken";  // 鶏

        // Required quantities
        private const int REQUIRED_ELIXIR = 2;
        private const int REQUIRED_LOVEPLUS = 2;
        private const int REQUIRED_CHICKEN = 2;

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            var pc = EClass.pc;
            if (pc == null)
            {
                Debug.LogError("[ConsumeResurrectionMaterials] Player character not found");
                ctx.Storage.SetInt(SessionFlagKeys.ConsumeSuccess, 0);
                return;
            }

            // Pre-check: verify materials are still available
            int elixirCount = CountItemsByIds(pc, ELIXIR_IDS);
            int loveplusCount = CountItemsByIds(pc, LOVEPLUS_IDS);
            int chickenCount = CountPartyChickens(pc);

            if (elixirCount < REQUIRED_ELIXIR ||
                loveplusCount < REQUIRED_LOVEPLUS ||
                chickenCount < REQUIRED_CHICKEN)
            {
                Debug.LogError($"[ConsumeResurrectionMaterials] Insufficient materials! " +
                              $"Elixir: {elixirCount}/{REQUIRED_ELIXIR}, " +
                              $"Loveplus: {loveplusCount}/{REQUIRED_LOVEPLUS}, " +
                              $"Chicken: {chickenCount}/{REQUIRED_CHICKEN}");
                ctx.Storage.SetInt(SessionFlagKeys.ConsumeSuccess, 0);
                return;
            }

            // Consume elixirs
            int elixirConsumed = ConsumeItemsByIds(pc, ELIXIR_IDS, REQUIRED_ELIXIR);
            ModLog.Log($"[ConsumeResurrectionMaterials] Consumed {elixirConsumed} elixirs");

            // Consume loveplus potions
            int loveplusConsumed = ConsumeItemsByIds(pc, LOVEPLUS_IDS, REQUIRED_LOVEPLUS);
            ModLog.Log($"[ConsumeResurrectionMaterials] Consumed {loveplusConsumed} loveplus potions");

            // Remove chickens from party
            int chickensRemoved = RemovePartyChickens(pc, REQUIRED_CHICKEN);
            ModLog.Log($"[ConsumeResurrectionMaterials] Removed {chickensRemoved} chickens from party");

            // Verify consumption succeeded
            bool success = elixirConsumed >= REQUIRED_ELIXIR &&
                          loveplusConsumed >= REQUIRED_LOVEPLUS &&
                          chickensRemoved >= REQUIRED_CHICKEN;

            ctx.Storage.SetInt(SessionFlagKeys.ConsumeSuccess, success ? 1 : 0);
            ModLog.Log($"[ConsumeResurrectionMaterials] Consumption success: {success}");
        }

        private int CountItemsByIds(Chara pc, string[] itemIds)
        {
            int count = 0;
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

        private int CountPartyChickens(Chara pc)
        {
            int count = 0;
            var party = pc?.party;
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

        private int ConsumeItemsByIds(Chara pc, string[] itemIds, int amount)
        {
            if (pc?.things == null) return 0;

            int consumed = 0;
            var toRemove = new List<Thing>();
            var resolvedSources = ResolveThingSources(itemIds);

            foreach (var thing in EnumerateInventoryThings(pc))
            {
                if (!MatchesThingId(thing, itemIds, resolvedSources)) continue;
                if (consumed >= amount) break;

                int toConsume = System.Math.Min(thing.Num, amount - consumed);
                consumed += toConsume;

                if (toConsume >= thing.Num)
                {
                    toRemove.Add(thing);
                }
                else
                {
                    thing.ModNum(-toConsume);
                }
            }

            // Remove fully consumed items
            foreach (var thing in toRemove)
            {
                thing.Destroy();
            }

            return consumed;
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

        private bool MatchesThingId(Thing thing, string[] ids, object[] resolvedSources)
        {
            if (thing == null) return false;
            foreach (var id in ids)
            {
                if (thing.id == id) return true;
            }

            var source = thing.source;
            if (source == null) return false;

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

        private int RemovePartyChickens(Chara pc, int count)
        {
            var party = pc?.party;
            if (party == null) return 0;

            int removed = 0;
            var chickensToRemove = new List<Chara>();

            // Find chickens to remove
            foreach (var member in party.members)
            {
                if (member?.id == CHICKEN_ID)
                {
                    chickensToRemove.Add(member);
                    if (chickensToRemove.Count >= count) break;
                }
            }

            // Remove and destroy chickens
            foreach (var chicken in chickensToRemove)
            {
                party.RemoveMember(chicken);
                chicken.Destroy();
                removed++;
            }

            return removed;
        }
    }
}

