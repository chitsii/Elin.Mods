using System;
using System.Reflection;
using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Core;
using HarmonyLib;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// リリィの店在庫をクエスト進行に合わせて更新
    /// 使用: modInvoke update_lily_shop_stock()
    /// </summary>
    public class UpdateLilyShopStockCommand : IArenaCommand
    {
        public string Name => "update_lily_shop_stock";

        private const string LilyId = "sukutsu_receptionist";
        private const string ScrollStockId = "sukutsu_scroll_showcase";
        private const string QuestId = "pg_03_scroll_showcase";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            bool unlocked = ArenaQuestManager.Instance?.IsQuestCompleted(QuestId) ?? false;

            // CWL API経由で在庫を切り替える
            // ClearStockは現在のCWLバージョンでは利用不可の場合があるため、失敗しても続行
            TryClearStock(LilyId);

            // ベース在庫を再登録（stock_sukutsu_receptionist.json）
            TryAddStock(LilyId, "");

            // 解放後のみ追加在庫を付与
            if (unlocked)
            {
                ModLog.Log($"[UpdateLilyShopStock] Quest {QuestId} completed, adding scroll stock");
                TryAddStock(LilyId, ScrollStockId);
            }
            else
            {
                ModLog.Log($"[UpdateLilyShopStock] Quest {QuestId} not completed yet");
            }
        }

        [GameDependency("Reflection", "Cwl.API.Custom.CustomMerchant.AddStock", "High", "CWL stock API may change signatures")]
        private bool TryAddStock(string cardId, string stockId)
        {
            var customMerchantType = AccessTools.TypeByName("Cwl.API.Custom.CustomMerchant");
            if (customMerchantType == null)
            {
                Debug.LogWarning("[UpdateLilyShopStock] CWL CustomMerchant not found");
                return false;
            }

            MethodInfo method = AccessTools.Method(customMerchantType, "AddStock", new[] { typeof(string), typeof(string) });
            object[] args = { cardId, stockId };
            if (method == null)
            {
                method = AccessTools.Method(customMerchantType, "AddStock", new[] { typeof(string) });
                args = new object[] { cardId };
            }

            if (method == null)
            {
                Debug.LogWarning("[UpdateLilyShopStock] CustomMerchant.AddStock not found");
                return false;
            }

            method.Invoke(null, args);
            ModLog.Log($"[UpdateLilyShopStock] Added stock '{(string.IsNullOrEmpty(stockId) ? cardId : stockId)}' to {cardId}");
            return true;
        }

        [GameDependency("Reflection", "Cwl.API.Custom.CustomMerchant.ClearStock", "High", "CWL stock API may be unavailable in some versions")]
        private bool TryClearStock(string cardId)
        {
            var customMerchantType = AccessTools.TypeByName("Cwl.API.Custom.CustomMerchant");
            if (customMerchantType == null)
            {
                Debug.LogWarning("[UpdateLilyShopStock] CWL CustomMerchant not found");
                return false;
            }

            MethodInfo method = AccessTools.Method(customMerchantType, "ClearStock", new[] { typeof(string) });
            if (method == null)
            {
                Debug.LogWarning("[UpdateLilyShopStock] CustomMerchant.ClearStock not found");
                return false;
            }

            method.Invoke(null, new object[] { cardId });
            ModLog.Log($"[UpdateLilyShopStock] Cleared custom stocks for {cardId}");
            return true;
        }
    }
}

