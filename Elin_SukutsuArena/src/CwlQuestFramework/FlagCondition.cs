namespace CwlQuestFramework
{
    /// <summary>
    /// CWL汎用フラグ条件クラス
    /// IFlagConditionの標準実装
    /// </summary>
    public class GenericFlagCondition : IFlagCondition
    {
        public string FlagKey { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }

        public GenericFlagCondition() { }

        public GenericFlagCondition(string flagKey, string op, object value)
        {
            FlagKey = flagKey;
            Operator = op;
            Value = value;
        }

        public override string ToString()
        {
            return $"{FlagKey} {Operator} {Value}";
        }
    }

    /// <summary>
    /// フラグ条件の演算子定数
    /// </summary>
    public static class FlagOperators
    {
        public const string Equal = "==";
        public const string NotEqual = "!=";
        public const string GreaterThanOrEqual = ">=";
        public const string GreaterThan = ">";
        public const string LessThanOrEqual = "<=";
        public const string LessThan = "<";
    }
}
