using System;
using System.Collections.Generic;
using System.Linq;

namespace CwlQuestFramework
{
    /// <summary>
    /// フラグ値を取得するためのインターフェース
    /// </summary>
    public interface IFlagValueProvider
    {
        /// <summary>
        /// フラグの整数値を取得
        /// </summary>
        int GetInt(string key, int defaultValue = 0);

        /// <summary>
        /// フラグが存在するか確認
        /// </summary>
        bool HasKey(string key);
    }

    /// <summary>
    /// Enum文字列から整数値への変換を提供するインターフェース
    /// </summary>
    public interface IEnumMappingProvider
    {
        /// <summary>
        /// 指定したフラグキーに対応するEnumマッピングを取得
        /// </summary>
        /// <returns>マッピングが存在すればtrue</returns>
        bool TryGetMapping(string flagKey, out IDictionary<string, int> mapping);
    }

    /// <summary>
    /// CWL汎用クエスト条件チェッカー
    /// フラグ条件の評価ロジックを提供
    /// </summary>
    public class QuestConditionChecker
    {
        private readonly IFlagValueProvider _flagProvider;
        private readonly IEnumMappingProvider _enumMappingProvider;

        public QuestConditionChecker(IFlagValueProvider flagProvider, IEnumMappingProvider enumMappingProvider = null)
        {
            _flagProvider = flagProvider ?? throw new ArgumentNullException(nameof(flagProvider));
            _enumMappingProvider = enumMappingProvider;
        }

        /// <summary>
        /// 単一のフラグ条件をチェック
        /// </summary>
        public bool CheckCondition(IFlagCondition condition)
        {
            if (condition == null) return true;

            int currentValue = _flagProvider.GetInt(condition.FlagKey, 0);
            int expectedValue = ResolveExpectedValue(condition.FlagKey, condition.Value);

            return EvaluateComparison(currentValue, condition.Operator, expectedValue);
        }

        /// <summary>
        /// 複数のフラグ条件をすべてチェック（AND条件）
        /// </summary>
        public bool CheckAllConditions(IEnumerable<IFlagCondition> conditions)
        {
            if (conditions == null) return true;

            foreach (var condition in conditions)
            {
                if (!CheckCondition(condition))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 複数のフラグ条件のいずれかをチェック（OR条件）
        /// </summary>
        public bool CheckAnyCondition(IEnumerable<IFlagCondition> conditions)
        {
            if (conditions == null) return true;

            bool hasConditions = false;
            foreach (var condition in conditions)
            {
                hasConditions = true;
                if (CheckCondition(condition))
                {
                    return true;
                }
            }
            return !hasConditions; // 条件がない場合はtrue
        }

        /// <summary>
        /// 期待値を解決（文字列Enumの場合は整数に変換）
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// 文字列値がEnumマッピングに存在せず、整数としてもパースできない場合
        /// </exception>
        private int ResolveExpectedValue(string flagKey, object value)
        {
            if (value is int intValue)
            {
                return intValue;
            }
            if (value is long longValue)
            {
                return (int)longValue;
            }
            if (value is bool boolValue)
            {
                return boolValue ? 1 : 0;
            }
            if (value is string strValue)
            {
                // Enumマッピングを試行
                if (_enumMappingProvider != null &&
                    _enumMappingProvider.TryGetMapping(flagKey, out var mapping))
                {
                    if (mapping.TryGetValue(strValue, out int mappedValue))
                    {
                        return mappedValue;
                    }
                    // マッピングは存在するが、値が見つからない
                    throw new InvalidOperationException(
                        $"Unknown enum value '{strValue}' for flag '{flagKey}'. " +
                        $"Valid values: {string.Join(", ", mapping.Keys)}");
                }

                // 文字列を整数としてパース試行
                if (int.TryParse(strValue, out int parsedValue))
                {
                    return parsedValue;
                }

                // マッピングもなく、整数パースも失敗 = 設定ミス
                throw new InvalidOperationException(
                    $"Cannot resolve string value '{strValue}' for flag '{flagKey}'. " +
                    $"No enum mapping exists and value is not a valid integer.");
            }

            return Convert.ToInt32(value);
        }

        /// <summary>
        /// 比較演算を実行
        /// </summary>
        private static bool EvaluateComparison(int current, string op, int expected)
        {
            return op switch
            {
                FlagOperators.Equal => current == expected,
                FlagOperators.NotEqual => current != expected,
                FlagOperators.GreaterThanOrEqual => current >= expected,
                FlagOperators.GreaterThan => current > expected,
                FlagOperators.LessThanOrEqual => current <= expected,
                FlagOperators.LessThan => current < expected,
                _ => false
            };
        }
    }

    /// <summary>
    /// クエスト利用可能性を評価するクラス
    /// </summary>
    public class QuestAvailabilityEvaluator
    {
        private readonly QuestConditionChecker _conditionChecker;
        private readonly Func<string, bool> _isQuestCompleted;

        public QuestAvailabilityEvaluator(
            QuestConditionChecker conditionChecker,
            Func<string, bool> isQuestCompleted)
        {
            _conditionChecker = conditionChecker ?? throw new ArgumentNullException(nameof(conditionChecker));
            _isQuestCompleted = isQuestCompleted ?? throw new ArgumentNullException(nameof(isQuestCompleted));
        }

        /// <summary>
        /// クエストが利用可能かどうかを評価
        /// </summary>
        /// <param name="quest">評価するクエスト</param>
        /// <param name="currentPhase">現在のフェーズ</param>
        /// <param name="allQuests">全クエストリスト（ブロック判定用）</param>
        /// <param name="reason">利用不可の場合の理由</param>
        /// <returns>利用可能ならtrue</returns>
        public bool IsAvailable(IQuestDefinition quest, int currentPhase, IEnumerable<IQuestDefinition> allQuests, out string reason)
        {
            reason = null;

            // 1. 完了済み確認
            if (_isQuestCompleted(quest.QuestId))
            {
                reason = "Already completed";
                return false;
            }

            // 2. フェーズチェック
            if (quest.Phase > currentPhase)
            {
                reason = $"Phase {quest.Phase} > current {currentPhase}";
                return false;
            }

            // 3. 前提クエスト確認
            foreach (var reqQuestId in quest.RequiredQuests)
            {
                if (!_isQuestCompleted(reqQuestId))
                {
                    reason = $"Missing prerequisite: {reqQuestId}";
                    return false;
                }
            }

            // 4. フラグ条件チェック
            if (!_conditionChecker.CheckAllConditions(quest.RequiredFlags))
            {
                reason = "Flag condition not met";
                return false;
            }

            // 5. ブロック確認
            foreach (var otherQuest in allQuests)
            {
                if (otherQuest.BlocksQuests.Contains(quest.QuestId) &&
                    _isQuestCompleted(otherQuest.QuestId))
                {
                    reason = $"Blocked by: {otherQuest.QuestId}";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// クエストが利用可能かどうかを評価（理由なし版）
        /// </summary>
        public bool IsAvailable(IQuestDefinition quest, int currentPhase, IEnumerable<IQuestDefinition> allQuests)
        {
            return IsAvailable(quest, currentPhase, allQuests, out _);
        }
    }
}
