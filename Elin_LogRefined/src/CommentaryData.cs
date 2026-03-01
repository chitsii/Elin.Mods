using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elin_LogRefined
{
    public static class CommentaryData
    {
        private static List<string> _customDamage;
        private static List<string> _customHeal;
        private static List<string> _customDebuff;
        private static bool _initialized = false;

        // 外部ファイルのパス
        private static string GetCommentaryDir()
        {
            return Path.Combine(Path.GetDirectoryName(typeof(CommentaryData).Assembly.Location), "commentary");
        }

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            string dir = GetCommentaryDir();
            string lang = Lang.langCode?.ToLower() ?? "jp";

            _customDamage = LoadCustomFile(dir, "damage", lang);
            _customHeal = LoadCustomFile(dir, "heal", lang);
            _customDebuff = LoadCustomFile(dir, "debuff", lang);
        }

        private static List<string> LoadCustomFile(string dir, string category, string lang)
        {
            // 言語固有のファイル -> フォールバック
            string[] candidates = new string[]
            {
                Path.Combine(dir, $"{category}_{lang}.txt"),
                Path.Combine(dir, $"{category}.txt")
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var lines = File.ReadAllLines(path, System.Text.Encoding.UTF8)
                            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
                            .ToList();

                        if (lines.Count > 0)
                        {
                            return lines;
                        }
                        else
                        {
                            // ファイルは存在するが空またはコメントのみ
                            ShowWarning($"Commentary file is empty: {Path.GetFileName(path)}");
                            return null;
                        }
                    }
                    catch (Exception e)
                    {
                        ShowWarning($"Failed to load commentary file: {Path.GetFileName(path)}\n{e.Message}");
                        return null;
                    }
                }
            }
            return null;
        }

        private static void ShowWarning(string message)
        {
            // ゲーム内ダイアログで警告表示
            try
            {
                Dialog.Ok(message);
            }
            catch
            {
                // ダイアログが使えない場合は無視
            }
        }

        // 戦闘中かどうかをチェック（combatCount ベース）
        public static bool IsInCombat()
        {
            try
            {
                return EClass.pc != null && EClass.pc.combatCount > 0;
            }
            catch
            {
                return false;
            }
        }

        public static string GetRandomDamage()
        {
            Initialize();
            if (_customDamage != null && _customDamage.Count > 0)
                return _customDamage.RandomItem();

            if (Lang.langCode == "CN") return Damage_CN.RandomItem();
            if (Lang.langCode == "EN") return Damage_EN.RandomItem();
            return Damage_JP.RandomItem();
        }

        public static string GetRandomHeal()
        {
            Initialize();
            if (_customHeal != null && _customHeal.Count > 0)
                return _customHeal.RandomItem();

            if (Lang.langCode == "CN") return Heal_CN.RandomItem();
            if (Lang.langCode == "EN") return Heal_EN.RandomItem();
            return Heal_JP.RandomItem();
        }

        public static string GetRandomDebuff()
        {
            Initialize();
            if (_customDebuff != null && _customDebuff.Count > 0)
                return _customDebuff.RandomItem();

            if (Lang.langCode == "CN") return Debuff_CN.RandomItem();
            if (Lang.langCode == "EN") return Debuff_EN.RandomItem();
            return Debuff_JP.RandomItem();
        }

        // テンプレートファイルを生成
        public static void GenerateTemplateFiles(bool overwrite)
        {
            string dir = GetCommentaryDir();

            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                WriteTemplateFile(Path.Combine(dir, "damage_jp.txt"), Damage_JP, overwrite);
                WriteTemplateFile(Path.Combine(dir, "damage_en.txt"), Damage_EN, overwrite);
                WriteTemplateFile(Path.Combine(dir, "damage_cn.txt"), Damage_CN, overwrite);
                WriteTemplateFile(Path.Combine(dir, "heal_jp.txt"), Heal_JP, overwrite);
                WriteTemplateFile(Path.Combine(dir, "heal_en.txt"), Heal_EN, overwrite);
                WriteTemplateFile(Path.Combine(dir, "heal_cn.txt"), Heal_CN, overwrite);
                WriteTemplateFile(Path.Combine(dir, "debuff_jp.txt"), Debuff_JP, overwrite);
                WriteTemplateFile(Path.Combine(dir, "debuff_en.txt"), Debuff_EN, overwrite);
                WriteTemplateFile(Path.Combine(dir, "debuff_cn.txt"), Debuff_CN, overwrite);
            }
            catch (Exception e)
            {
                Dialog.Ok("Failed to create template files: " + e.Message);
            }
        }

        public static void OpenCommentaryDir()
        {
            string dir = GetCommentaryDir();
            if (Directory.Exists(dir))
            {
                System.Diagnostics.Process.Start(dir);
            }
            else
            {
                Dialog.Ok("Folder not found: " + dir);
            }
        }

        private static void WriteTemplateFile(string path, List<string> lines, bool overwrite)
        {
            if (File.Exists(path) && !overwrite)
            {
                return;
            }

            string header = "# Commentary lines - one per line\n# Lines starting with # are comments\n\n";
            File.WriteAllText(path, header + string.Join("\n", lines), System.Text.Encoding.UTF8);
        }

        // ファイルが存在するかどうか
        public static bool TemplateFilesExist()
        {
            string dir = GetCommentaryDir();
            return Directory.Exists(dir) && Directory.GetFiles(dir, "*.txt").Length > 0;
        }

        // --- JP Data ---
        public static readonly List<string> Damage_JP = new List<string>
        {
            "強烈な一撃、入りましたーッ！",
            "これは痛い！あまりにも痛いーッ！",
            "クリティカルヒットかーッ！？",
            "素晴らしい攻撃だーッ！",
            "大地を揺るがすような一撃ッ！",
            "防御の上から叩き割ったーッ！",
            "火力が！火力が違いますーッ！",
            "見事な手際！これぞ一流の戦いーッ！",
            "あまりの威力に観客も総立ちだーッ！",
            "これが本気の力なのかーッ！",
            "信じられないダメージが出ましたーッ！",
            "骨まで響くような衝撃音ーッ！",
            "相手はたまらず吹き飛んだーッ！",
            "慈悲はないのかーッ！",
            "圧倒的！圧倒的なパワーですーッ！",
            "まさに一撃必殺の威力ーッ！",
            "空気が！空気が震えていますーッ！",
            "これは致命傷になりかねませんーッ！",
            "芸術的なまでの破壊衝動ッ！",
            "止まらない！この猛攻は誰にも止められないッ！",
            "魂まで震える衝撃だーッ！",
            "防御など紙切れ同然ッ！",
            "見えなかった！速すぎるーッ！",
            "急所を的確に捉えたーッ！",
            "これは重い！腰に入った一撃です！",
            "観客席まで風圧が届くーッ！",
            "慈悲なき鉄槌が下されたーッ！",
            "本能が警鐘を鳴らしているッ！",
            "一瞬の静寂…そして爆音ッ！",
            "悲鳴すら上げられないーッ！",
            "地獄への片道切符だーッ！",
            "空間ごと削り取ったーッ！",
            "相手の心をも折る一撃ッ！",
            "瞬きする間もない早業ッ！",
            "まさに規格外！！",
            "残虐！しかし美しいーッ！",
            "うわぁ…これは痛い…",
            "子供には見せられない惨状だーッ！",
            "会場が静まり返っています…",
            "ちょっと引くレベルのダメージですね…"
        };

        public static readonly List<string> Heal_JP = new List<string>
        {
            "奇跡的な回復を見せましたッ！",
            "まるで泉のような生命力だーッ！",
            "起死回生のヒールが決まったーッ！",
            "傷が！傷がみるみる塞がっていきますーッ！",
            "なんという回復量だーッ！",
            "これは大きい！戦況をひっくり返す回復だーッ！",
            "神の加護が降り注いだーッ！",
            "素晴らしいリカバリーですーッ！",
            "まだだ！まだ終わらんよーッ！",
            "脅威の生存本能を見せつけますーッ！",
            "癒やしの光が包み込むーッ！",
            "まさに不死鳥！甦りましたーッ！",
            "絶望の淵からの生還だーッ！",
            "これには相手も苦笑いーッ！",
            "溢れ出るようなバイタリティだーッ！",
            "完全復活かーッ！？",
            "心地よい光が戦場を照らすーッ！",
            "この粘り！これこそが真骨頂ーッ！",
            "何度でも立ち上がるつもりだーッ！",
            "聖なる力が彼を守っているッ！",
            "不屈の闘志が肉体を凌駕するッ！",
            "まだ倒れない！膝をつかないーッ！",
            "輝きが戦場を支配する！",
            "驚異的な代謝能力だーッ！",
            "死神の手を振り払ったーッ！",
            "生命の鼓動が聞こえてきます！",
            "傷がなかったことになったーッ！？",
            "底知れぬスタミナ！化け物かーッ！",
            "この回復は計算通りなのかッ！？",
            "希望の灯火は消えていないーッ！",
            "奇跡は起きる！起こしてみせたーッ！",
            "なんというタフネス！",
            "傷が塞がる音が聞こえるようだーッ！",
            "決して屈しない！その意志が体を動かすッ！",
            "命の輝きが増していくーッ！",
            "まだ戦える！まだ舞えるッ！",
            "その時、守護神が微笑むッ！",
            "なんてしぶといんだーッ！",
            "ゾンビのような回復力ッ！",
            "まだやる気なのかーッ！",
            "不死身かお前はーッ！"
        };

        public static readonly List<string> Debuff_JP = new List<string>
        {
            "おっと、これは厄介なッ！",
            "動きが鈍ったかーッ！？",
            "これは苦しい！精神的にも削られます！",
            "まともに食らってしまったーッ！",
            "戦況が悪化したぞーッ！",
            "ジワジワと！ジワジワと追い詰められていくーッ！",
            "なんて卑怯な！いや、これも戦術ッ！",
            "体が思うように動かないのかーッ！？",
            "呪詛の言葉が刻み込まれるーッ！",
            "これは痛恨ッ！",
            "顔色が悪いぞ、大丈夫かーッ！？",
            "防御が崩されたーッ！",
            "相手のペースに巻き込まれていますーッ！",
            "冷や汗が止まらないーッ！",
            "このままではジリ貧だーッ！",
            "早く！早く治療しないと手遅れになるぞーッ！",
            "なんという搦め手だーッ！",
            "足元がふらついているーッ！",
            "これは悪夢だ！悪夢を見ているようだーッ！",
            "目に見えない恐怖が襲いかかるッ！",
            "何かがおかしい…何かが変だーッ！",
            "戦術の罠に嵌まってしまったかーッ！",
            "じわじわと真綿で首を絞めるような攻撃ッ！",
            "思考が鈍る！体が重いーッ！",
            "これは計算外の事態だーッ！",
            "蝕まれている！確実に蝕まれているぞーッ！",
            "不穏な空気が漂い始めたーッ！",
            "相手の動きが明らかに精彩を欠いている！",
            "精神（メンタル）へのダイレクトアタックだーッ！",
            "泥沼に引きずり込むような戦い方だーッ！",
            "鉛のように体が重いッ！",
            "逃げ場なし！完全なる包囲網ッ！",
            "じりじりと戦力を奪っていくーッ！",
            "呪いか！？これは呪いなのかーッ！",
            "相手の持ち味を完全に消しているーッ！",
            "盤面を支配する一手ッ！",
            "混乱の渦に叩き落としたーッ！",
            "自由を奪う鎖が見えるようだーッ！",
            "性格が悪い！性格が悪すぎるーッ！",
            "地味だが一番嫌な攻撃だーッ！",
            "これは友達をなくす戦法だーッ！",
            "陰湿！あまりにも陰湿な攻め手ッ！",
            "泥仕合の予感がします！",
            "卑怯とは言わせない！それが勝負の世界ッ！"
        };

        // --- EN Data ---
        public static readonly List<string> Damage_EN = new List<string>
        {
            "What a massive hit!",
            "That's gotta hurt!",
            "Is that a critical hit!?",
            "An incredible attack!",
            "A ground-shaking blow!",
            "Smashed right through the defense!",
            "The power! The power level is off the charts!",
            "What skill! A masterclass in combat!",
            "The crowd goes wild from that impact!",
            "Is this their true power!?",
            "Unbelievable damage!",
            "I can hear the bones cracking from here!",
            "Sent flying!",
            "No mercy shown!",
            "Overwhelming! Absolute power!",
            "A one-hit KO potential right there!",
            "The air is trembling from the shockwave!",
            "That looks like a fatal wound!",
            "Artistic destruction!",
            "Unstoppable! No one can stop this onslaught!"
        };

        public static readonly List<string> Heal_EN = new List<string>
        {
            "A miraculous recovery!",
            "A fountain of vitality!",
            "A game-changing heal!",
            "The wounds! The wounds are closing up!",
            "What an amount of healing!",
            "This turns the tables!",
            "Divine protection shines down!",
            "An amazing recovery!",
            "Not yet! It's not over yet!",
            "A terrifying survival instinct!",
            "Wrapped in healing light!",
            "Like a phoenix rising!",
            "Back from the brink of despair!",
            "The opponent can only smile bitterly at this!",
            "Overflowing vitality!",
            "A complete comeback!?",
            "A soothing light illuminates the battlefield!",
            "This resilience! That's the true spirit!",
            "Standing up again and again!",
            "Holy power protects them!"
        };

        public static readonly List<string> Debuff_EN = new List<string>
        {
            "Uh oh, that's a nasty status effect!",
            "Movement slowed down!?",
            "This is tough! A mental blow as well!",
            "Took that one head-on!",
            "The situation has worsened!",
            "Bit by bit! Cornered bit by bit!",
            "How underhanded! No, that's tactics!",
            "Body not moving as intended!?",
            "Cursed words carved in!",
            "A painful debuff!",
            "Looking pale there, are you okay!?",
            "Poison!? Paralysis!? Confusion!?",
            "Defenses crumbled!",
            "Dragged into the opponent's pace!",
            "Cold sweat won't stop!",
            "It's a losing battle at this rate!",
            "Quick! Need treatment before it's too late!",
            "What a tricky move!",
            "Stumbling on their feet!",
            "It's a nightmare! Like a living nightmare!"
        };

        // --- CN Data ---
        public static readonly List<string> Damage_CN = new List<string>
        {
            "这一击太猛烈了！",
            "这太痛了！！",
            "是暴击吗！？",
            "精彩！",
            "震撼大地的一击！",
            "直接击穿了防御！",
            "这火力！火力的级别完全不同！",
            "漂亮！这才是顶级战斗！",
            "威力之大让观众都站了起来！",
            "这就是真正的力量吗！？",
            "打出了难以置信的伤害！",
            "冲击声响彻骨髓！",
            "对手直接被击飞了！",
            "毫无慈悲吗！",
            "压倒性！压倒性的力量！",
            "简直是一击必杀的威力！",
            "空气！空气都在震动！",
            "这可能会造成致命伤！",
            "充满艺术感的破坏冲动！",
            "停不下来！谁也无法阻止这猛攻！"
        };

        public static readonly List<string> Heal_CN = new List<string>
        {
            "奇迹般的恢复！",
            "简直是生命的源泉！",
            "起死回生的治疗！",
            "伤口！伤口眼看着在愈合！",
            "多惊人的恢复量！",
            "这很关键！足以扭转战局的恢复！",
            "神的加护降临了！",
            "精彩的复原！",
            "还没完！还没有结束！",
            "展现了惊人的生存本能！",
            "治愈之光笼罩全身！",
            "简直像凤凰涅槃！",
            "从绝望的深渊生还！",
            "对手也只能苦笑了！",
            "溢出的生命力！",
            "完全复活了吗！？",
            "舒适的光芒照亮战场！",
            "这份顽强！这才是真本领！",
            "无论多少次都会站起来！",
            "神圣的力量在守护着他！"
        };

        public static readonly List<string> Debuff_CN = new List<string>
        {
            "哎呀，这个状态异常很麻烦！",
            "动作变迟钝了吗！？",
            "这很痛苦！精神上也被削弱了！",
            "结结实实地吃了一招！",
            "战况恶化了！",
            "一点点地！一点点地被逼入绝境！",
            "太卑鄙了！不，这也是战术！",
            "身体无法随心所欲地活动吗！？",
            "诅咒的言灵被刻下！",
            "这是令人痛恨的减益！",
            "脸色很难看啊，没事吧！？",
            "是毒！？麻痹！？还是混乱！？",
            "防御崩溃了！",
            "被卷入了对手的节奏！",
            "冷汗止不住！",
            "这样下去会越来越糟！",
            "快点！不快点治疗就来不及了！",
            "什么样的手段啊！",
            "脚步踉跄了！",
            "这是噩梦！简直像在做噩梦！"
        };
    }
}
