using UnityEngine;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Localization;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// 氷原の元素の傷跡ギミック
    /// 定期的にプレイヤーに元素の傷跡（ConWeakResEle）デバフを付与する
    /// </summary>
    [GameDependency("Inheritance", "ZoneEvent", "Medium", "Zone event base class may change")]
    public class ZoneEventElementalScar : ZoneEvent
    {
        // === 基本設定値 ===
        public float interval = 8f;           // 発動間隔（秒）
        public float startDelay = 5f;         // 開始遅延（秒）
        public int debuffPower = 50;          // デバフの強さ

        // === エスカレーション設定 ===
        public bool enableEscalation = true;
        public float escalationRate = 0.85f;  // 間隔減少率
        public float minInterval = 3f;        // 最小間隔

        // === 内部状態 ===
        private float timer = 0f;
        private float delayTimer = 0f;
        private bool started = false;
        private bool announced = false;
        private float currentInterval;
        private int triggerCount = 0;

        public override void OnTick()
        {
            // 初期化
            if (currentInterval <= 0)
            {
                currentInterval = interval;
            }

            // 開始遅延
            if (!started)
            {
                delayTimer += global::Core.delta;
                if (delayTimer >= startDelay)
                {
                    started = true;
                    if (!announced)
                    {
                        Msg.Say(ArenaLocalization.GimmickElementalScarStart);
                        announced = true;
                    }
                }
                return;
            }

            timer += global::Core.delta;
            if (timer < currentInterval) return;
            timer = 0f;

            // 元素の傷跡を付与
            ApplyElementalScar();

            // エスカレーション（間隔短縮）
            if (enableEscalation && currentInterval > minInterval)
            {
                currentInterval *= escalationRate;
                if (currentInterval < minInterval)
                {
                    currentInterval = minInterval;
                }
            }
        }

        /// <summary>
        /// 元素の傷跡デバフを付与
        /// </summary>
        private void ApplyElementalScar()
        {
            if (EClass.pc == null || EClass.pc.isDead) return;

            triggerCount++;

            // メッセージ（バリエーション）
            var messages = ArenaLocalization.ElementalScarMessages;
            Msg.Say(messages[EClass.rnd(messages.Length)]);

            // エフェクト（座標検証付き）
            try
            {
                if (EClass.pc.pos != null && EClass.pc.pos.IsValid)
                {
                    EClass.pc.pos.PlayEffect("cold");
                    EClass.Sound.Play("cold");
                }
            }
            catch
            {
                // エフェクトがなくても続行
            }

            // 元素の傷跡（ConWeakResEle）を付与
            try
            {
                // power が大きいほど持続時間が長い
                int power = debuffPower + (triggerCount * 10);  // 回数が増えるほど強くなる
                EClass.pc.AddCondition<ConWeakResEle>(power);
                ModLog.Log($"[SukutsuArena] Applied ConWeakResEle to player (power={power}, count={triggerCount})");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SukutsuArena] Failed to apply ConWeakResEle: {ex.Message}");
            }

            // 追加: 冷気ダメージ（軽微）
            if (triggerCount >= 3)
            {
                try
                {
                    int coldDamage = 5 + triggerCount;
                    // 冷気属性ダメージ (element ID: 951 = Cold)
                    EClass.pc.DamageHP(coldDamage, 910, 100, AttackSource.Condition);  // 910 = Cold element damage
                    ModLog.Log($"[SukutsuArena] Applied cold damage: {coldDamage}");
                }
                catch
                {
                    // ダメージ失敗は無視
                }
            }
        }
    }
}

