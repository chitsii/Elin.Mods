using System.Collections.Generic;
using UnityEngine;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Localization;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// 観客（上位存在/飽食者）からの妨害ギミック
    /// 定期的にプレイヤー周辺に落下物を発生させる
    /// 時間経過で頻度と範囲がエスカレートする
    /// </summary>
    [GameDependency("Inheritance", "ZoneEvent", "Medium", "Zone event base class may change")]
    public class ZoneEventAudienceInterference : ZoneEvent
    {
        // === 基本設定値 ===
        public float interval = 5f;           // 初期落下間隔（秒）
        public int damage = 15;               // 基本ダメージ量
        public int radius = 3;                // 初期落下範囲
        public float startDelay = 3f;         // 開始遅延（秒）
        public string effectName = "explosion";
        public string soundName = "explosion";

        // === エスカレーション設定 ===
        public bool enableEscalation = true;
        public float escalationRate = 0.9f;
        public float minInterval = 0.5f;
        public int maxRadius = 8;
        public float radiusGrowthInterval = 30f;

        // === アイテムドロップ設定 ===
        public bool enableItemDrop = true;
        public float itemDropChance = 0.15f;

        // === 命中率設定 ===
        public int blastRadius = 2;           // 爆発の範囲（この範囲内のキャラにダメージ）
        public float directHitChance = 0.4f;  // プレイヤー直撃確率（40%）
        public int explosionCount = 1;        // 同時爆発数

        // === 内部状態 ===
        private float timer = 0f;
        private float delayTimer = 0f;
        private float totalTime = 0f;
        private bool started = false;
        private bool announced = false;
        private float currentInterval;
        private int currentRadius;
        private int escalationLevel = 0;
        private int currentBlastRadius;
        private int currentExplosionCount;

        // ダメージ属性リスト
        private static readonly int[] DamageElements = new int[]
        {
            920,  // 魔法 (Ether)
            914,  // 轟音 (Sound)
            923   // 切傷 (Cut)
        };

        public override void OnTick()
        {
            // 初期化
            if (currentInterval <= 0)
            {
                currentInterval = interval;
                currentRadius = radius;
                currentBlastRadius = blastRadius;
                currentExplosionCount = explosionCount;
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
                        Msg.Say(ArenaLocalization.GimmickAudienceStart);
                        announced = true;
                    }
                }
                return;
            }

            // 経過時間追跡
            totalTime += global::Core.delta;

            // エスカレーション処理
            if (enableEscalation && radiusGrowthInterval > 0)
            {
                int newLevel = (int)(totalTime / radiusGrowthInterval);
                if (newLevel > escalationLevel)
                {
                    escalationLevel = newLevel;

                    // 爆発範囲拡大
                    if (currentBlastRadius < 4)
                    {
                        currentBlastRadius++;
                    }

                    // 同時爆発数増加（最大3）
                    if (escalationLevel >= 2 && currentExplosionCount < 3)
                    {
                        currentExplosionCount++;
                    }

                    AnnounceEscalation();
                }
            }

            timer += global::Core.delta;
            if (timer < currentInterval) return;
            timer = 0f;

            // 複数の落下物を発生
            for (int i = 0; i < currentExplosionCount; i++)
            {
                SpawnFallingObject(i == 0); // 最初の1発のみメッセージ表示
            }

            // エスカレーション処理（間隔短縮）
            if (enableEscalation && currentInterval > minInterval)
            {
                currentInterval *= escalationRate;
                if (currentInterval < minInterval)
                {
                    currentInterval = minInterval;
                }
            }
        }

        private void AnnounceEscalation()
        {
            var messages = ArenaLocalization.AudienceEscalationMessages;
            Msg.Say(messages[EClass.rnd(messages.Length)]);
        }

        private void SpawnFallingObject(bool showMessage = true)
        {
            Point target = GetTargetPoint();
            if (target == null || !target.IsValid) return;

            if (showMessage)
            {
                var warnings = ArenaLocalization.AudienceFallingWarnings;
                Msg.Say(warnings[EClass.rnd(warnings.Length)]);
            }

            // エフェクト再生（座標検証付き）
            if (!string.IsNullOrEmpty(effectName) && target.IsValid)
            {
                target.PlayEffect(effectName);
            }

            // サウンド再生
            if (!string.IsNullOrEmpty(soundName))
            {
                EClass.Sound.Play(soundName);
            }

            // 画面揺れ
            if (escalationLevel >= 1)
            {
                try { Shaker.ShakeCam("ball"); } catch { }
            }

            // 範囲ダメージ判定
            ApplyAreaDamage(target);

            // アイテムドロップ
            if (enableItemDrop && EClass.rnd(100) < (int)(itemDropChance * 100))
            {
                SpawnRandomItem(target);
            }
        }

        /// <summary>
        /// 狙う位置を決定（プレイヤー直撃率を考慮）
        /// </summary>
        private Point GetTargetPoint()
        {
            if (EClass.pc == null) return null;

            Point pcPos = EClass.pc.pos;
            // pcPos の有効性チェック
            if (pcPos == null || !pcPos.IsValid) return null;

            // エスカレーションが進むほど直撃率UP
            float actualDirectHitChance = directHitChance + (escalationLevel * 0.1f);
            if (actualDirectHitChance > 0.8f) actualDirectHitChance = 0.8f;

            // 直撃判定
            if (EClass.rnd(100) < (int)(actualDirectHitChance * 100))
            {
                // プレイヤーの現在位置を直接狙う
                return pcPos.Copy();
            }

            // プレイヤーの近くを狙う（狭い範囲）
            int nearRadius = Mathf.Max(1, currentRadius / 2);
            for (int i = 0; i < 10; i++)
            {
                int dx = EClass.rnd(nearRadius * 2 + 1) - nearRadius;
                int dz = EClass.rnd(nearRadius * 2 + 1) - nearRadius;

                Point p = new Point(pcPos.x + dx, pcPos.z + dz);
                if (p.IsValid && !p.IsBlocked)
                {
                    return p;
                }
            }

            // フォールバック：プレイヤー位置（検証付き）
            Point result = pcPos.Copy();
            return (result != null && result.IsValid) ? result : null;
        }

        /// <summary>
        /// 範囲ダメージを与える
        /// </summary>
        private void ApplyAreaDamage(Point center)
        {
            List<Chara> hitCharas = new List<Chara>();

            // 爆発範囲内の全キャラを取得
            foreach (Chara c in EClass._map.charas)
            {
                if (c.isDead) continue;

                int dx = Mathf.Abs(c.pos.x - center.x);
                int dz = Mathf.Abs(c.pos.z - center.z);
                int distance = Mathf.Max(dx, dz); // チェビシェフ距離

                if (distance <= currentBlastRadius)
                {
                    hitCharas.Add(c);
                }
            }

            if (hitCharas.Count == 0) return;

            // ダメージ属性選択
            int elementId = DamageElements[EClass.rnd(DamageElements.Length)];

            // ダメージ量（エスカレーションで増加）
            int baseDamage = damage + (escalationLevel * 5);

            foreach (Chara c in hitCharas)
            {
                // 距離による減衰（中心ほど高ダメージ）
                int dx = Mathf.Abs(c.pos.x - center.x);
                int dz = Mathf.Abs(c.pos.z - center.z);
                int distance = Mathf.Max(dx, dz);

                float damageMultiplier = 1.0f - (distance * 0.15f);
                if (damageMultiplier < 0.5f) damageMultiplier = 0.5f;

                int actualDamage = (int)(baseDamage * damageMultiplier);

                try
                {
                    c.DamageHP(actualDamage, elementId, 100, AttackSource.Trap);
                }
                catch
                {
                    c.DamageHP(actualDamage, AttackSource.Trap);
                }

                // 出血付与
                if (elementId == 923)
                {
                    try { c.AddCondition<ConBleed>(100 + escalationLevel * 20); } catch { }
                }

                // 直撃時の追加効果
                if (distance == 0)
                {
                    // 直撃ボーナスダメージ
                    try { c.DamageHP(actualDamage / 2, AttackSource.Trap); } catch { }
                }

                ModLog.Log($"[SukutsuArena] Hit: {c.Name} for {actualDamage} (dist={distance}, elem={elementId})");
            }
        }

        private void SpawnRandomItem(Point pos)
        {
            string[] itemIds = new string[]
            {
                "water_dirty",
                "water",
                "potion",
                "drug",
                "junk",
                "head2"
            };

            string itemId = itemIds[EClass.rnd(itemIds.Length)];

            Thing item = ThingGen.Create(itemId);
            if (item != null)
            {
                EClass._zone.AddCard(item, pos);
                Msg.Say(ArenaLocalization.SomethingFell);
            }
        }
    }
}

