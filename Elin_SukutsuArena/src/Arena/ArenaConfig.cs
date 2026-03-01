namespace Elin_SukutsuArena.Arena
{
    /// <summary>
    /// アリーナMod設定の中央集約クラス
    /// フラグ関連はArenaFlagKeys/SessionFlagKeysを使用
    /// </summary>
    public static class ArenaConfig
    {
        // ============================================================
        // Mod識別子
        // ============================================================

        /// <summary>ModのGUID</summary>
        public const string ModGuid = "tishi.elin.sukutsu_arena";

        /// <summary>Mod名</summary>
        public const string ModName = "Elin_SukutsuArena";

        // ============================================================
        // ゾーン
        // ============================================================

        /// <summary>アリーナゾーンID</summary>
        public const string ZoneId = "sukutsu_arena";

        // ============================================================
        // アイテムID
        // ============================================================

        public static class ItemIds
        {
            /// <summary>金箔の鎧</summary>
            public const string GildedArmor = "sukutsu_gilded_armor";

            /// <summary>影分身キャラ</summary>
            public const string ShadowSelf = "sukutsu_shadow_self";
        }

        // ============================================================
        // NPC ID
        // ============================================================

        public static class NpcIds
        {
            /// <summary>バルガス（アリーナマスター）</summary>
            public const string Balgas = "sukutsu_arena_master";

            /// <summary>リリィ（受付嬢）</summary>
            public const string Lily = "sukutsu_receptionist";

            /// <summary>ゼク（怪しい商人）</summary>
            public const string Zek = "sukutsu_shady_merchant";

            /// <summary>アスタロト（グランドマスター）</summary>
            public const string Astaroth = "sukutsu_astaroth";

            /// <summary>ヌル（暗殺人形）</summary>
            public const string Nul = "sukutsu_null";

            /// <summary>アイリス（トレーナー）</summary>
            public const string Iris = "sukutsu_trainer";

            /// <summary>デバッグマスター（開発用）</summary>
            public const string DebugMaster = "sukutsu_debug_master";
        }

        // ============================================================
        // ドラマID
        // ============================================================

        public static class DramaIds
        {
            /// <summary>アリーナマスター（バルガス）メインドラマ</summary>
            public const string ArenaMaster = "drama_sukutsu_arena_master";
        }
    }
}
