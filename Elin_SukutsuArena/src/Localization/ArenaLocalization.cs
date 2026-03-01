namespace Elin_SukutsuArena.Localization
{
    /// <summary>
    /// アリーナMod用ローカライズヘルパー
    /// JP/CN/EN の3言語対応
    /// </summary>
    public static class ArenaLocalization
    {
        private static bool IsJP => Lang.isJP;
        private static bool IsCN => !Lang.isJP && Lang.langCode == "CN";

        /// <summary>
        /// 3言語から選択するヘルパー
        /// </summary>
        private static string L(string jp, string en, string cn) =>
            IsJP ? jp : (IsCN ? cn : en);

        // ============================================================
        // ランク名
        // ============================================================

        /// <summary>
        /// ランク表示名を取得
        /// </summary>
        public static string GetRankDisplayName(int rank) => rank switch
        {
            0 => IsJP ? "ランク外" : "Unranked",
            1 => IsJP ? "G - 屑肉" : "G - Scrap",
            2 => IsJP ? "F - 泥犬" : "F - Mud Dog",
            3 => IsJP ? "E - 鉄屑" : "E - Iron Scrap",
            4 => IsJP ? "D - 銅貨稼ぎ" : "D - Copper Earner",
            5 => IsJP ? "C - 朱砂食い" : "C - Cinnabar Eater",
            6 => IsJP ? "B - 銀翼" : "B - Silver Wing",
            7 => IsJP ? "A - 戦鬼" : "A - War Demon",
            8 => IsJP ? "S - 竜断ち" : "S - Dragon Slayer",
            9 => IsJP ? "SS - 天穿ち" : "SS - Heaven Piercer",
            _ => $"Rank {rank} (Unknown)"
        };

        /// <summary>
        /// 現在のランク表示メッセージ
        /// </summary>
        public static string CurrentRankMessage(string rankName) =>
            IsJP ? $"現在のランク: 『{rankName}』 " : $"Current Rank: [{rankName}] ";

        // ============================================================
        // 戦闘メッセージ
        // ============================================================

        public static string CannotReturnDuringBattle =>
            IsJP ? "戦闘中は帰還できない！" : "Cannot return during battle!";

        public static string EnemiesDefeated =>
            IsJP ? "敵を殲滅した！" : "All enemies defeated!";

        public static string ForcedRetreat =>
            IsJP ? "このままではやられてしまう！アリーナから撤退した..." : "Forced to retreat from the arena...";

        public static string Retreated =>
            IsJP ? "アリーナから撤退した..." : "Retreated from the arena...";

        // ============================================================
        // 報酬メッセージ
        // ============================================================

        public static string VictoryRewardWithPotion(int plat, int potion) =>
            IsJP
                ? $"勝利の報酬としてプラチナコイン {plat} 枚、媚薬 {potion} 本を獲得した！"
                : $"Victory reward: {plat} platinum coins and {potion} love potions!";

        public static string VictoryReward(int plat) =>
            IsJP
                ? $"勝利の報酬としてプラチナコイン {plat} 枚を獲得した！"
                : $"Victory reward: {plat} platinum coins!";

        // ============================================================
        // プラチナコイン関連
        // ============================================================

        public static string NotEnoughPlatinum =>
            IsJP ? "プラチナコインが足りない。" : "Not enough platinum coins.";

        public static string PaidPlatinum(int amount) =>
            IsJP ? $"プラチナコイン {amount} 枚を支払った。" : $"Paid {amount} platinum coins.";

        // ============================================================
        // バトルプレビュー
        // ============================================================

        public static string Unknown =>
            L("不明", "Unknown", "未知");

        public static string NoGimmicks =>
            L("  - なし", "  - None", "  - 无");

        public static string NoGimmicksShort =>
            L("なし", "None", "无");

        public static string FormatEnemyInfoSingle(string enemyName, int count) =>
            L($"{enemyName} × {count}体", $"{enemyName} × {count}", $"{enemyName} × {count}只");

        public static string FormatEnemyInfoMultiple(string enemyName, int count) =>
            L($"{enemyName}など {count}体", $"{enemyName} etc. × {count}", $"{enemyName}等 {count}只");

        // ============================================================
        // ギミック説明 - 開始メッセージ
        // ============================================================

        public static string GimmickNoHealingStart =>
            IsJP
                ? "この場では癒しの力が封じられている...回復は期待できない。"
                : "Healing is sealed in this place... Don't expect any recovery.";

        public static string GimmickHealingBlocked =>
            IsJP ? "癒しの力が封じられている！" : "Healing is sealed!";

        public static string GimmickMagicAffinityStart =>
            IsJP
                ? "この闘技場は魔力で満ちている...物理攻撃は通じず、魔法だけが力を持つ。"
                : "This arena is filled with magical energy... Physical attacks are useless, only magic has power.";

        public static string GimmickEmpatheticStart =>
            IsJP
                ? "共感の力がこの場を満たしている...全ての存在が互いを感知する。"
                : "Empathic energy fills this place... All beings sense each other.";

        public static string GimmickAntiMagicStart =>
            IsJP
                ? "この場には強力なアンチマジックフィールドが展開されている..."
                : "A powerful anti-magic field is deployed here...";

        public static string GimmickCriticalDamageStart =>
            IsJP
                ? "この闘技場では、耐性のない攻撃はより致命的となる..."
                : "In this arena, unresisted attacks are more lethal...";

        public static string GimmickElementalScarStart =>
            IsJP ? "氷原の冷気が肌を刺す..." : "The icy cold pierces your skin...";

        public static string GimmickAudienceStart =>
            IsJP ? "観客席がざわめき始めた..." : "The audience grows restless...";

        // ============================================================
        // ギミック - 観客妨害
        // ============================================================

        public static string[] AudienceEscalationMessages => IsJP
            ? new[]
            {
                "観客の興奮が高まっている！",
                "飽食者たちがより多くを求めている...",
                "上位存在の悪意が強まる！",
                "闘技場全体が狂気に包まれていく...",
                "爆発の威力が増している！"
            }
            : new[]
            {
                "The audience's excitement is growing!",
                "The gluttons demand more...",
                "The malice of higher beings intensifies!",
                "Madness engulfs the entire arena...",
                "The explosions are getting stronger!"
            };

        public static string[] AudienceFallingWarnings => IsJP
            ? new[]
            {
                "頭上から何かが降ってくる！",
                "観客席から物が投げ込まれた！",
                "上位存在の悪意が形を成す！",
                "魔力の塊が落下してくる！"
            }
            : new[]
            {
                "Something is falling from above!",
                "Something was thrown from the audience!",
                "The malice of higher beings takes form!",
                "A mass of magical energy is falling!"
            };

        public static string SomethingFell =>
            IsJP ? "何かが落ちてきた！" : "Something fell!";

        // ============================================================
        // マップ兵器/スクロール
        // ============================================================

        public static string MapWeaponBlockedInHome =>
            L("自拠点では使用できない。",
              "Cannot be used in your own territory.",
              "无法在自家领地使用。");

        public static string ScrollAttackResult(int count) =>
            L($"禁術が発動し、{count}体の敵に破滅をもたらした。",
              $"The forbidden spell unleashes, bringing ruin to {count} enemies.",
              $"禁术发动，使{count}个敌人走向毁灭。");

        public static string ScrollHarvestResult(int count) =>
            L($"大地が震え、{count}個のオブジェクトが収穫された。",
              $"The earth trembles, and {count} objects are harvested.",
              $"大地震动，收获了{count}个物体。");

        public static string NothingHappens =>
            L("何も起こらなかった。",
              "Nothing happens.",
              "什么都没有发生。");

        // ============================================================
        // 戦闘開始/演出
        // ============================================================

        public static string BattleStart(string displayName) =>
            L($"{displayName} 開始！",
              $"{displayName} begins!",
              $"{displayName}开始！");

        // ============================================================
        // 装備効果
        // ============================================================

        public static string GildedArmorGoldShattered(long goldSpent) =>
            L($"金貨が{goldSpent}枚剥がれ落ちた！",
              $"{goldSpent} gold coins flake away!",
              $"有{goldSpent}枚金币剥落了！");

        // ============================================================
        // 双子の鏡
        // ============================================================

        public static string TwinMirrorSummon =>
            L("鏡面が裂け、もう一人のあなたが這い出した。",
              "The mirror splits, and another you crawls out.",
              "镜面裂开，另一个你爬了出来。");

        public static string TwinMirrorDismiss =>
            L("影は鏡の奥へ溶け、気配が消えた。",
              "The shadow melts back into the mirror and vanishes.",
              "影子融回镜中，气息消失了。");

        // ============================================================
        // 周回/リセット
        // ============================================================

        public static string NewGameCycleStarted =>
            L("記憶が巻き戻された…。闘技場での挑戦が、再び始まる。",
              "Your memories rewind... The arena challenge begins anew.",
              "记忆回溯……竞技场的挑战再次开始。");

        // ============================================================
        // ギミック - 元素の傷跡
        // ============================================================

        public static string[] ElementalScarMessages => IsJP
            ? new[]
            {
                "極寒の空気が耐性を蝕む！",
                "凍える風が身体を貫く！",
                "氷原の呪いがお前を蝕む...",
                "冷気が魂まで凍りつかせる！"
            }
            : new[]
            {
                "The freezing air erodes your resistance!",
                "The icy wind pierces your body!",
                "The curse of the frozen plains consumes you...",
                "The cold freezes even your soul!"
            };

        // ============================================================
        // リセット機能
        // ============================================================

        public static string CannotUninstallInArena =>
            L("アリーナ内ではUninstallできません。先にアリーナから出てください。",
              "Cannot uninstall while in the arena. Please exit first.",
              "在竞技场内无法卸载。请先离开竞技场。");

        public static string NewGamePlusResetComplete(int flags, int pets) =>
            L($"周回リセット完了: {flags}フラグ削除, {pets}ペット離脱",
              $"New Game+ reset complete: {flags} flags removed, {pets} pets dismissed",
              $"周目重置完成: 删除了{flags}个标记, {pets}个宠物离队");

        public static string UninstallResetComplete(int flags, int feats, int pets, bool zoneRemoved)
        {
            if (IsJP)
            {
                var msg = $"Mod削除準備完了: {flags}フラグ, {feats}フィート, {pets}ペット";
                if (zoneRemoved) msg += ", ゾーン削除";
                return msg;
            }
            else if (IsCN)
            {
                var msg = $"Mod卸载准备完成: {flags}标记, {feats}专长, {pets}宠物";
                if (zoneRemoved) msg += ", 区域已删除";
                return msg;
            }
            else
            {
                var msg = $"Uninstall ready: {flags} flags, {feats} feats, {pets} pets";
                if (zoneRemoved) msg += ", zone removed";
                return msg;
            }
        }

        // ============================================================
        // アンインストール個別ログ
        // ============================================================

        public static string UninstallStarting =>
            L("【狭間の闘技場】アンインストール開始: ",
              "[Sukutsu Arena] Uninstall: ",
              "【狭间竞技场】卸载: ");

        public static string UninstallFlagsRemoved(int count) =>
            L($"フラグ{count}件削除 ",
              $"Flags:{count} ",
              $"标记{count}个 ");

        public static string UninstallFeatsRemoved(int count) =>
            L($"フィート{count}件削除 ",
              $"Feats:{count} ",
              $"专长{count}个 ");

        public static string UninstallConditionsRemoved(int count) =>
            L($"状態異常{count}件解除 ",
              $"Conditions:{count} ",
              $"状态{count}个 ");

        public static string UninstallQuestsRemoved(int count) =>
            L($"クエスト{count}件削除 ",
              $"Quests:{count} ",
              $"任务{count}个 ");

        public static string UninstallPetsDismissed(int count) =>
            L($"ペット{count}体離脱 ",
              $"Pets:{count} ",
              $"宠物{count}只 ");

        public static string UninstallArenaZoneRemoved =>
            L("闘技場削除 ",
              "Arena removed ",
              "竞技场已删除 ");

        public static string UninstallExtraZonesRemoved(int count) =>
            L($"戦闘場{count}件削除 ",
              $"Fields:{count} ",
              $"战场{count}个 ");

        public static string UninstallComplete =>
            L("...完了！",
              "...Done!",
              "...完成！");

        public static string NewGamePlusStarting =>
            L("【狭間の闘技場】周回リセット: ",
              "[Sukutsu Arena] NG+ Reset: ",
              "【狭间竞技场】周目重置: ");

        // ============================================================
        // エンディング
        // ============================================================

        public static string GetEndingName(int endingType, bool balgasKilled, bool lilyHostile)
        {
            if (balgasKilled && lilyHostile)
                return IsJP ? "孤独エンド" : "Solitude Ending";

            return endingType switch
            {
                0 => IsJP ? "解放エンド" : "Liberation Ending",
                1 => IsJP ? "継承エンド" : "Inheritance Ending",
                _ => IsJP ? "エンディング" : "Ending"
            };
        }

        public static string EndingCreditTitle(string endingName) =>
            IsJP
                ? $"【狭間の闘技場】メインストーリークリア！\n～{endingName}～"
                : $"Main Story Complete!\n~{endingName}~";

        public static string EndingCreditDetail =>
            IsJP
                ? "物語は完結しましたが、闘技場の通常戦闘は引き続きお楽しみいただけます。\n" +
                  "敵の強さはネフィア最深到達階層に応じてスケールするため、エンドコンテンツとしてご活用ください。\n\n" +
                  "制作: chitsii\n" +
                  "連絡先: X @chitsii"
                : "The story has concluded, but you can continue enjoying regular arena battles.\n" +
                  "Enemy strength scales with your deepest Nefia floor, making it suitable as endgame content.\n\n" +
                  "Created by: chitsii\n" +
                  "Contact: X @chitsii";

        // ============================================================
        // フィート関連
        // ============================================================

        public static string ArenaFeatGained(int level, int staminaBonus) =>
            IsJP
                ? $"【闘志】Lv{level}を獲得！（活力+{staminaBonus}）"
                : $"[Arena Spirit] Lv{level} gained! (Vigor+{staminaBonus})";

        // ============================================================
        // マクマ報酬
        // ============================================================

        public static string Makuma2Reward =>
            IsJP
                ? "【虚空の心臓】小さなコイン30枚、プラチナコイン15枚 を獲得！"
                : "[Heart of the Void] Obtained 30 small coins and 15 platinum coins!";

        // ============================================================
        // バトルプレビューダイアログ
        // ============================================================

        public static string BattlePreviewTitle =>
            L("=== 本日の対戦 ===", "=== Today's Battle ===", "=== 今日的对战 ===");

        public static string BattlePreviewEnemy =>
            L("・敵", "・Enemy", "・敌人");

        public static string BattlePreviewSpawn =>
            L("・出現パターン", "・Spawn Pattern", "・出现模式");

        public static string BattlePreviewReward =>
            L("・報酬", "・Reward", "・奖励");

        public static string BattlePreviewGimmicks =>
            L("・特殊ルール", "・Special Rules", "・特殊规则");

        public static string BattlePreviewPlatinum =>
            L("プラチナ", "Platinum", "白金币");

        public static string BattlePreviewPotion =>
            L("媚薬", "Love Potion", "媚药");

        /// <summary>
        /// バトルプレビューダイアログのフォーマット済みテキストを取得
        /// </summary>
        public static string FormatBattlePreview(string enemyInfo, string spawnInfo, string gimmickInfo, int rewardPlat, int rewardPotion)
        {
            return $"{BattlePreviewTitle}\n\n" +
                   $"{BattlePreviewEnemy}: {enemyInfo}\n" +
                   $"{BattlePreviewSpawn}: {spawnInfo}\n" +
                   $"{BattlePreviewReward}: {BattlePreviewPlatinum}×{rewardPlat}、{BattlePreviewPotion}×{rewardPotion}\n" +
                   $"{BattlePreviewGimmicks}:\n{gimmickInfo}";
        }
    }
}
