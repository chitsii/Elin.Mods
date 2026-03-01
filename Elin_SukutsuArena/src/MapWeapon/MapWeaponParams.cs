namespace Elin_SukutsuArena.MapWeapon
{
    /// <summary>
    /// マップ兵器のパラメータ設定
    /// </summary>
    public class MapWeaponParams
    {
        /// <summary>属性エイリアス (例: "eleFire", "eleIce", "" = 無属性)</summary>
        public string ElementAlias { get; set; } = "";

        /// <summary>効果範囲 (0 = マップ全体)</summary>
        public float Radius { get; set; } = 0f;

        /// <summary>効果威力</summary>
        public int Power { get; set; } = 100;

        /// <summary>カスタムエフェクトID (null = デフォルト)</summary>
        public string EffectId { get; set; } = null;

        /// <summary>敵のみを攻撃対象にする</summary>
        public bool TargetHostileOnly { get; set; } = true;

        /// <summary>自拠点を保護する（採掘・掘削対象外）</summary>
        public bool ProtectPCZone { get; set; } = true;

        /// <summary>カルマ変動を適用する</summary>
        public bool ApplyKarma { get; set; } = true;

        /// <summary>壁を採掘する</summary>
        public bool MineWalls { get; set; } = true;

        /// <summary>素材を収穫する</summary>
        public bool HarvestResources { get; set; } = true;

        /// <summary>敵にダメージを与える</summary>
        public bool DamageEnemies { get; set; } = true;

        /// <summary>
        /// 収穫プリセットを生成
        /// </summary>
        public static MapWeaponParams HarvestPreset() => new MapWeaponParams
        {
            MineWalls = true,
            HarvestResources = true,
            DamageEnemies = false,
            ProtectPCZone = true,
            ApplyKarma = true,
        };

        /// <summary>
        /// 攻撃プリセットを生成
        /// </summary>
        /// <param name="element">属性エイリアス</param>
        /// <param name="power">威力</param>
        public static MapWeaponParams AttackPreset(string element = "eleFire", int power = 500)
            => new MapWeaponParams
            {
                ElementAlias = element,
                Power = power,
                MineWalls = false,
                HarvestResources = false,
                DamageEnemies = true,
                TargetHostileOnly = true,
            };
    }
}
