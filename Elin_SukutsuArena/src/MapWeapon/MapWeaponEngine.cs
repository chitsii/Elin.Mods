using System.Collections.Generic;
using UnityEngine;
using Elin_SukutsuArena.Localization;

namespace Elin_SukutsuArena.MapWeapon
{
    /// <summary>
    /// マップ兵器の実行エンジン
    /// </summary>
    public static class MapWeaponEngine
    {
        /// <summary>
        /// マップ兵器を実行
        /// </summary>
        /// <param name="caster">発動者</param>
        /// <param name="param">パラメータ</param>
        /// <returns>実行結果</returns>
        public static MapWeaponResult Execute(Chara caster, MapWeaponParams param)
        {
            var result = new MapWeaponResult();

            // 自拠点保護チェック（収穫・採掘の場合のみ）
            if (param.ProtectPCZone && EClass._zone.IsPCFaction)
            {
                if (param.MineWalls || param.HarvestResources)
                {
                    // 自拠点では使用不可
                    Msg.Say(ArenaLocalization.MapWeaponBlockedInHome);
                    return result;
                }
            }

            // エフェクト再生
            PlayEffect(caster, param);

            // 対象ポイントを取得
            List<Point> points = GetTargetPoints(caster, param);

            // 収穫・採掘処理
            if (param.MineWalls || param.HarvestResources)
            {
                result.HarvestCount = ExecuteHarvest(caster, points, param);
            }

            // 攻撃処理
            if (param.DamageEnemies)
            {
                result.DamageCount = ExecuteAttack(caster, param);
            }

            // カルマ処理
            if (param.ApplyKarma && EClass._zone.HasLaw && !EClass._zone.IsPCFaction)
            {
                int karmaLoss = (result.HarvestCount + result.DamageCount) / 10;
                if (karmaLoss > 0)
                {
                    EClass.player.ModKarma(-karmaLoss);
                    result.KarmaLost = karmaLoss;
                }
            }

            return result;
        }

        /// <summary>
        /// 対象ポイントを取得
        /// </summary>
        private static List<Point> GetTargetPoints(Chara caster, MapWeaponParams param)
        {
            if (param.Radius <= 0)
            {
                // マップ全体
                var points = new List<Point>();
                EClass._map.bounds.ForeachPoint(p => points.Add(p.Copy()));
                return points;
            }
            else
            {
                // 円形範囲
                return EClass._map.ListPointsInCircle(
                    caster.pos, param.Radius, mustBeWalkable: false, los: false
                );
            }
        }

        /// <summary>
        /// エフェクト再生
        /// </summary>
        private static void PlayEffect(Chara caster, MapWeaponParams param)
        {
            caster.PlayEffect("cast");

            string soundId = param.EffectId ?? "spell_earthquake";
            caster.PlaySound(soundId);

            if (caster.IsInMutterDistance())
            {
                Shaker.ShakeCam("ball");
            }
        }

        /// <summary>
        /// 収穫・採掘を実行
        /// </summary>
        private static int ExecuteHarvest(Chara caster, List<Point> points, MapWeaponParams param)
        {
            int count = 0;

            foreach (var p in points)
            {
                if (!p.IsValid) continue;

                // 壁採掘
                if (param.MineWalls && p.cell.HasBlock && !p.cell.HasObj)
                {
                    EClass._map.MineBlock(p, recoverBlock: false, caster, mineObj: true);
                    count++;
                }

                // 素材収穫
                if (param.HarvestResources && p.HasObj)
                {
                    EClass._map.MineObj(p, null, caster);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 攻撃を実行
        /// </summary>
        private static int ExecuteAttack(Chara caster, MapWeaponParams param)
        {
            int count = 0;

            // 対象の敵リスト作成（イテレーション中の変更を避けるためコピー）
            var targets = new List<Chara>();
            foreach (Chara c in EClass._map.charas)
            {
                if (c.IsPC) continue;
                if (param.TargetHostileOnly && !c.IsHostile()) continue;
                targets.Add(c);
            }

            // 属性ID取得
            int eleId = 0;
            if (!string.IsNullOrEmpty(param.ElementAlias))
            {
                var row = EClass.sources.elements.alias.TryGetValue(param.ElementAlias);
                if (row != null)
                {
                    eleId = row.id;
                }
            }

            // ダメージ適用
            foreach (var target in targets)
            {
                // エフェクト
                string effectId = param.EffectId;
                if (string.IsNullOrEmpty(effectId) && !string.IsNullOrEmpty(param.ElementAlias))
                {
                    // 属性名からエフェクトID生成（eleFire → ball_Fire）
                    string eleName = param.ElementAlias.Replace("ele", "");
                    effectId = $"ball_{eleName}";
                }

                if (!string.IsNullOrEmpty(effectId))
                {
                    Effect effect = Effect.Get(effectId);
                    if (effect != null)
                    {
                        effect.Play(target.pos);
                    }
                }

                // ダメージ計算（Dice.Rollを使用）
                int damage = Dice.Roll(param.Power / 10, 6, param.Power / 5, caster);

                // ダメージ適用
                target.DamageHP(damage, eleId, param.Power, AttackSource.None, caster);

                count++;
            }

            return count;
        }
    }

    /// <summary>
    /// マップ兵器の実行結果
    /// </summary>
    public class MapWeaponResult
    {
        /// <summary>収穫・採掘したオブジェクト数</summary>
        public int HarvestCount { get; set; }

        /// <summary>ダメージを与えた敵の数</summary>
        public int DamageCount { get; set; }

        /// <summary>失ったカルマ</summary>
        public int KarmaLost { get; set; }

        /// <summary>処理が成功したか</summary>
        public bool Success => HarvestCount > 0 || DamageCount > 0;
    }
}
