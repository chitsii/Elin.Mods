using System.Collections.Generic;
using System.Linq;
using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// 全ての一時ステータス変化とバフ・デバフを取り除くコマンド
    /// modInvoke: full_recovery()
    /// </summary>
    public class FullRecoveryCommand : IArenaCommand
    {
        public string Name => "full_recovery";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            var pc = EClass.pc;
            if (pc == null)
            {
                Debug.LogError("[FullRecovery] PC is null");
                return;
            }

            var recoveredItems = new List<string>();

            // 一時ステータス変化をクリア
            ClearTempElements(pc, recoveredItems);

            // バフ・デバフ（Condition）をクリア
            ClearConditions(pc, recoveredItems);

            if (recoveredItems.Count > 0)
            {
                pc.PlaySound("heal");
                pc.PlayEffect("heal");

                string message = string.Join(", ", recoveredItems);
                pc.Say($"{message}が取り除かれた！");
                ModLog.Log($"[FullRecovery] Cleared: {message}");
            }
            else
            {
                pc.Say("特に問題はないようだ。");
                ModLog.Log("[FullRecovery] Nothing to clear");
            }
        }

        private void ClearTempElements(Chara pc, List<string> recoveredItems)
        {
            if (pc.tempElements == null) return;

            foreach (var element in pc.tempElements.dict.Values.ToList())
            {
                string statName = EClass.sources.elements.map[element.id].GetName();
                int value = element.vBase;
                string sign = value > 0 ? "+" : "";
                recoveredItems.Add($"{statName}({sign}{value})");
                ModLog.Log($"[FullRecovery] Clearing tempElement: {statName} ({sign}{value})");
            }

            pc.ClearTempElements();
        }

        private void ClearConditions(Chara pc, List<string> recoveredItems)
        {
            if (pc.conditions == null || pc.conditions.Count == 0) return;

            // 削除対象のConditionをリストアップ（イテレーション中の変更を避ける）
            var conditionsToRemove = pc.conditions.ToList();

            foreach (var condition in conditionsToRemove)
            {
                string conditionName = condition.Name;
                recoveredItems.Add(conditionName);
                ModLog.Log($"[FullRecovery] Removing condition: {conditionName}");
                condition.Kill();
            }
        }
    }
}

