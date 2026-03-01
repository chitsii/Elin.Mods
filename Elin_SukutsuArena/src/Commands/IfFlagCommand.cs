using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// if_flag(flagKey, operatorValue, jumpTarget)
    /// フラグ条件を評価し、trueならPendingJumpTargetにジャンプ先を設定
    /// switch_flagと同じパターンでDramaManager_PatchのjumpFuncが処理する
    /// operatorValue は "==1" のように演算子と値が結合された形式
    /// </summary>
    public class IfFlagCommand : IArenaCommand
    {
        public string Name => "if_flag";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            // PendingJumpTargetをリセット（条件falseの場合はnullのまま→ジャンプしない）
            SwitchFlagCommand.PendingJumpTarget = null;

            if (args.Length < 3)
            {
                Debug.LogError("[IfFlag] Requires 3 args: flagKey, operatorValue, jumpTarget");
                return;
            }

            var flagKey = args[0];
            var operatorValue = args[1];
            var jumpTarget = args[2];

            // Parse operator and value
            var (op, valueStr) = ParseOperator(operatorValue);
            if (op == null)
            {
                Debug.LogError($"[IfFlag] Cannot parse operator from: {operatorValue}");
                return;
            }

            // Get current flag value
            int currentValue = ctx.Storage.GetInt(flagKey, 0);

            // Evaluate condition
            bool condition = false;
            if (int.TryParse(valueStr, out int expectedValue))
            {
                condition = EvaluateCondition(currentValue, op, expectedValue);
                ModLog.Log($"[IfFlag] {flagKey}={currentValue} {op} {expectedValue} => {condition}");
            }
            else
            {
                Debug.LogWarning($"[IfFlag] Non-integer comparison not fully supported: {flagKey} {op} {valueStr}");
            }

            if (condition)
            {
                ModLog.Log($"[IfFlag] Condition true, setting jump target: {jumpTarget}");
                SwitchFlagCommand.PendingJumpTarget = jumpTarget;
            }
        }

        private (string op, string value) ParseOperator(string operatorValue)
        {
            if (operatorValue.StartsWith("=="))
                return ("==", operatorValue.Substring(2));
            if (operatorValue.StartsWith("!="))
                return ("!=", operatorValue.Substring(2));
            if (operatorValue.StartsWith(">="))
                return (">=", operatorValue.Substring(2));
            if (operatorValue.StartsWith("<="))
                return ("<=", operatorValue.Substring(2));
            if (operatorValue.StartsWith(">"))
                return (">", operatorValue.Substring(1));
            if (operatorValue.StartsWith("<"))
                return ("<", operatorValue.Substring(1));

            return (null, null);
        }

        private bool EvaluateCondition(int current, string op, int expected)
        {
            return op switch
            {
                "==" => current == expected,
                "!=" => current != expected,
                ">=" => current >= expected,
                ">" => current > expected,
                "<=" => current <= expected,
                "<" => current < expected,
                _ => false
            };
        }
    }
}
