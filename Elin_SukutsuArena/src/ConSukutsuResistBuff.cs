using UnityEngine;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// 巣窟アリーナ専用の一時的耐性バフCondition
    ///
    /// ConResEle（元素保護）と同様のパターンでBaseBuffを継承。
    /// UseElementsを有効にしてelements.SetBaseで一時的なエレメント変更を実現。
    /// Conditionが終了すると自動的に効果が解除される。
    ///
    /// 使用方法:
    /// c.AddCondition(Condition.Create(power, delegate(ConSukutsuResistBuff con) {
    ///     con.SetRefVal(elementId, buffValue);
    /// }));
    /// </summary>
    public class ConSukutsuResistBuff : BaseBuff
    {
        /// <summary>
        /// elementsコンテナを有効化
        /// これにより、Condition終了時に自動的にelements.SetParent()が呼ばれ、
        /// 変更が解除される
        /// </summary>
        public override bool UseElements => true;

        /// <summary>
        /// ConditionType（_sourceがnullの場合のフォールバック）
        /// </summary>
        public override ConditionType Type => _source != null ? base.Type : ConditionType.Buff;

        /// <summary>
        /// 名前（_sourceがnullの場合のフォールバック）
        /// </summary>
        public override string Name => _source != null ? base.Name : "耐性強化";

        /// <summary>
        /// 複数インスタンスを許可（異なるエレメントに対して複数バフ可能）
        /// </summary>
        public override bool AllowMultipleInstance => true;

        /// <summary>
        /// オーナー設定時にエレメント変更を適用
        /// refVal = エレメントID
        /// refVal2 = 増加値
        /// </summary>
        public override void SetOwner(Chara _owner, bool onDeserialize = false)
        {
            base.SetOwner(_owner);

            // エレメント値を設定
            elements.SetBase(refVal, refVal2);
            elements.SetParent(owner);

            ModLog.Log($"[SukutsuArena] ConSukutsuResistBuff.SetOwner: elementId={refVal}, value={refVal2}, owner={owner?.Name}");
        }

        /// <summary>
        /// 同じエレメントに対するConditionはスタック可能
        /// </summary>
        public override bool CanStack(Condition c)
        {
            if (c is ConSukutsuResistBuff other)
            {
                return other.refVal == refVal;
            }
            return base.CanStack(c);
        }

        /// <summary>
        /// フェーズ表示（UIに表示される文字列）
        /// </summary>
        public override string GetPhaseStr()
        {
            // エレメント名を取得して表示
            var element = EClass.sources.elements.map.TryGetValue(refVal);
            if (element != null)
            {
                return $"resist_{element.alias}".lang();
            }
            return Name;
        }
    }
}

