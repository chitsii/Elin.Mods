using System.Collections.Generic;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Centralized bilingual message definitions for Ars Moriendi.
    /// Provides key-based access with JP/EN resolution.
    /// Uses Msg.Say for game log output with #1/#2 card name substitution.
    /// </summary>
    public static class LangHelper
    {
        private static readonly Dictionary<string, (string jp, string en, string cn)> Messages = new()
        {
            // Spell casting messages (#1 = caster, #2 = target)
            ["castSoulTrap"] = (
                "#1は#2に魂魄保存を唱えた。",
                "#1 casts Preserve Soul on #2.", "#1对#2施放了灵魂封存。"),
            ["castCurseWeakness"] = (
                "#1は#2に衰弱の呪いを唱えた。",
                "#1 casts Curse of Weakness on #2.", "#1对#2施放了衰弱诅咒。"),
            ["castTerror"] = (
                "#1は#2に恐怖を植え付けた。",
                "#1 instills terror in #2.", "#1在#2心中植入了恐惧。"),
            ["castPlagueTouch"] = (
                "#1は#2に疫病を植え付けた。",
                "#1 infects #2 with plague.", "#1向#2植入了瘟疫。"),
            ["castCurseFrailty"] = (
                "#1は#2に重い衰弱の呪いを唱えた。",
                "#1 casts Curse of Frailty on #2.", "#1对#2施放了沉重的衰弱诅咒。"),
            ["castLifeDrain"] = (
                "#1は#2の生命力を吸い取った。",
                "#1 drains the life force from #2.", "#1汲取了#2的生命力。"),
            ["castStaminaDrain"] = (
                "#1は#2の精力を吸い取った。",
                "#1 drains stamina from #2.", "#1汲取了#2的精力。"),
            ["castManaDrain"] = (
                "#1は#2の魔力を吸い取った。",
                "#1 drains mana from #2.", "#1汲取了#2的魔力。"),
            ["castBoneShield"] = (
                "#1は骸骨壁を纏った。",
                "#1 is surrounded by Wall of Skeleton.", "#1被骸骨之墙所环绕。"),
            ["castBoneAegisLegion"] = (
                "#1は骸骨壁を展開した。",
                "#1 deploys Wall of Skeleton.", "#1展开了骸骨之墙。"),
            ["castPreserveCorpse"] = (
                "#1は屍体保存の力を得た。",
                "#1 gains the power of corpse preservation.", "#1获得了保存尸体之力。"),
            ["castEmpowerUndead"] = (
                "#1は従者たちを強化した。",
                "#1 empowers the undead servants.", "#1强化了亡灵仆从们。"),
            ["castFuneralMarch"] = (
                "#1は死軍号令を発した。",
                "#1 calls the Funeral March.", "#1发出了死军号令。"),
            ["castSummonSkeletonWarrior"] = (
                "#1は骸骨の戦士を召喚した。",
                "#1 summons a skeleton warrior.", "#1召唤了骷髅战士。"),
            ["castSoulBind"] = (
                "#1は従者と魂で繋がった。",
                "#1 binds their soul to a servant.", "#1将灵魂与仆从相连。"),
            ["castDeathZone"] = (
                "#1の周囲に死の領域が展開された。",
                "A zone of death expands around #1.", "#1的周围展开了死亡领域。"),
            ["castSoulTrapMass"] = (
                "#1は魂縛の檻を展開した。",
                "#1 unleashes Soul Snare.", "#1展开了灵魂囚笼。"),
            ["castUnholyVigor"] = (
                "#1は不浄な力でHPとスタミナを回復した。",
                "#1 restores HP and stamina with unholy vigor.", "#1以不洁之力恢复了HP和体力。"),
            ["castGraveQuagmire"] = (
                "#1は黄泉の泥濘を広げた。",
                "#1 spreads a Grave Quagmire.", "#1扩散了黄泉泥泞。"),
            ["castCorpseChainBurst"] = (
                "#1は屍鎖爆砕を放った。",
                "#1 unleashes Corpse Chain Burst.", "#1释放了尸锁爆砕。"),
            ["castSoulRecall"] = (
                "#1は死兵を呼び戻した。",
                "#1 recalls fallen servants.", "#1唤回了死兵。"),
            ["castGraveExile"] = (
                "#1は共連れ転送を発動した。",
                "#1 triggers Grave Exile.", "#1发动了共连转送。"),
            ["noServants"] = (
                "従者がいない。",
                "You have no servants.", "没有仆从。"),
            ["soulRecallNoTarget"] = (
                "還生できる従者がいない。",
                "There are no fallen servants to recall.", "没有可还生的仆从。"),
            ["graveExileNeedServant"] = (
                "従者を指定する必要がある。",
                "You must target one of your servants.", "你必须指定一名仆从。"),
            ["soulBindTrigger"] = (
                "#2が#1の身代わりとなって消滅した！",
                "#2 sacrifices itself to save #1!", "#2替#1牺牲了自己！"),

            // Knowledge tab
            ["spellLearned"] = (
                "新たな死霊術を習得した！",
                "You have learned a new necromancy spell!", "习得了新的死灵术！"),
            ["notEnoughSouls"] = (
                "魂が足りない。",
                "Not enough souls.", "灵魂不足。"),

            // Ritual tab
            ["ritualSuccess"] = (
                "死者蘇生の儀式が成功した！アンデッドの従者が蘇った。",
                "The resurrection ritual was successful! An undead servant has risen.", "复活仪式成功了！亡灵仆从已经苏醒。"),
            ["ritualFailed"] = (
                "儀式に失敗した…",
                "The ritual failed...", "仪式失败了……"),

            // Servant tab
            ["servantReleased"] = (
                "従者を解放した。",
                "Servant released.", "仆从已解放。"),

            // Offering system
            ["actOffer"] = (
                "捧げ物をする",
                "Make Offering", "进行供奉"),
            ["noOffering"] = (
                "捧げられるものがない。",
                "You have nothing to offer.", "没有可供奉之物。"),
            ["offeringSuccess"] = (
                "禁書が捧げ物を吸収し、呪文のストックが回復した。",
                "The forbidden tome absorbs the offering, restoring spell charges.", "禁书吸收了供品，咒文的蓄力恢复了。"),

            // Tome flavor text
            ["tomeFlavor"] = (
                "禁書に刻まれた知識を紐解く…",
                "Unraveling the knowledge inscribed in the forbidden tome...", "解读禁书中铭刻的知识……"),
            ["ritualFlavor"] = (
                "死体と魂を捧げ、アンデッドの従者を蘇らせる。投入する魂が多いほど、強い従者が蘇る。",
                "Offer a corpse and souls to raise an undead servant. More souls yield a stronger servant.", "献上尸体和灵魂，复活亡灵仆从。投入的灵魂越多，复活的仆从越强。"),
            ["servantFlavor"] = (
                "汝の従者たちを統べよ。",
                "Command your undead servants.", "统御你的亡灵仆从。"),

            // Offering section
            ["offeringSection"] = (
                "魂や心臓を捧げて、呪文のストックを回復する。",
                "Offer souls or hearts to restore spell charges.", "献上灵魂或心脏，恢复咒文蓄力。"),

            // Confirmation
            ["cancel"] = ("やめる", "Cancel", "取消"),

            // Servant status
            ["statusAlive"] = ("生存", "Alive", "存活"),
            ["statusDead"] = ("死亡", "Dead", "死亡"),
            ["servantReleaseAction"] = ("従者を解放", "Release Servant", "解放仆从"),
            ["servantReleaseConfirm"] = ("{0} を解放する (取り消し不可)", "Release {0} (cannot be undone)", "解放 {0}（无法撤销）"),

            // Enhancement system
            ["enhanceAttrSuccess"] = (
                "#1の能力が強化された！",
                "#1's attribute has been enhanced!", "#1的能力得到了强化！"),
            ["augmentSuccess"] = (
                "#1に新たな部位が生えた！",
                "A new body part has grown on #1!", "#1长出了新的身体部位！"),
            ["augmentFailed"] = (
                "増設に失敗した…素材は失われた。",
                "Augmentation failed... The materials were consumed.", "增设失败了……素材已消耗。"),

            // Rampage messages
            ["rampageDarkAwakening"] = (
                "#1の闇が覚醒した！強化効果が倍増し、屍気の恩寵が宿った！",
                "#1 has awakened to dark power! Enhancement doubled and blessed with the Grace of Death!", "#1觉醒了暗之力量！强化效果翻倍，且获得了死亡的恩宠！"),
            ["rampageBerserk"] = (
                "#1が狂戦士化した！",
                "#1 has gone berserk!", "#1狂暴化了！"),
            ["rampageSelfDestruct"] = (
                "#1が暴走して自壊した！",
                "#1 has gone out of control and self-destructed!", "#1暴走并自我毁灭了！"),
            ["rampageMutationAwakening"] = (
                "#1が変異覚醒した！強力な部位が生え、一時的に狂戦士化した！",
                "#1 has undergone a mutation awakening! A powerful body part has grown, driving them temporarily berserk!", "#1发生了变异觉醒！长出了强力的身体部位，并暂时陷入狂暴！"),

            // Augmentation with resonance
            ["augmentFailedResonance"] = (
                "増設に失敗した…素材は失われたが、共鳴度が上昇した。",
                "Augmentation failed... Materials consumed, but resonance has increased.", "增设失败了……素材已消耗，但共鸣度上升了。"),

            // ── Unhallowed Path quest messages ──
            ["questStage1"] = (
                "…禁書の頁が、ひとりでに捲れた。",
                "...The pages of the forbidden tome turn on their own.", "……禁书的书页自行翻动了。"),
            ["questStage2"] = (
                "禁書に新たな文字が浮かび上がった。読めない筈の文字が、何故か理解できる。",
                "New text surfaces in the tome. Characters you shouldn't understand somehow make sense.", "禁书中浮现了新的文字。明明不应该看懂的文字，不知为何却能理解。"),
            ["questStage3"] = (
                "神殿騎士団が現れた！",
                "Temple Knights have appeared!", "神殿骑士团出现了！"),
            ["questStage4"] = (
                "禁書の頁がさらに開かれた。",
                "More pages of the tome have been revealed.", "禁书的更多书页被揭开了。"),
            ["questStage5"] = (
                "禁書が震えている。何かが近づいている…",
                "The tome trembles. Something approaches...", "禁书在颤抖。有什么在逼近……"),
            ["questStage6"] = (
                "禁書に見知らぬ文字が浮かんだ。まだ存在しない誰かの文字…連鎖は続いている。",
                "Unknown characters have appeared in the tome. Written by someone who does not yet exist. The chain continues.", "禁书上浮现了陌生的文字。是尚不存在之人书写的文字……连锁仍在继续。"),
            ["questStage7"] = (
                "聖騎士カレンが再び現れた。従者の数が先代と同じ水準に達したことへの警告だ。",
                "Holy Knight Karen has appeared once more, warning that your servant count matches the predecessor's.", "圣骑士卡伦再次现身。她警告你的仆从数量已达到与前代同等的水平。"),
            ["questStage8"] = (
                "先代の影を打ち倒した。禁書が最後の章を開く。",
                "The shadow of the predecessor has been defeated. The tome opens its final chapter.", "击败了前代之影。禁书翻开了最后的篇章。"),
            ["questStage9"] = (
                "不浄なる覚醒。あなたはアルス・モリエンディの新たな継承者となった。",
                "Unhallowed Awakening. You have become the new inheritor of Ars Moriendi.", "不洁的觉醒。你已成为死亡的艺术的新继承者。"),
            ["questStage10"] = (
                "昇華を経て世界の見え方が変わった。六代目としての章が始まった。",
                "The world looks different after Apotheosis. Your chapter as the sixth inheritor has begun.", "经历升华后，世界的面貌已然改变。作为第六代继承者的篇章已经开始。"),

            // Unhallowed Path gate messages
            ["questNeedJournal"] = (
                "カレンの手帳を読んでから、禁書を開くべきだ。",
                "You should read Karen's journal before opening the tome.", "应该先阅读卡伦的笔记，再打开禁书。"),
            ["questNeedServant"] = (
                "禁書は沈黙している。死霊術の実践が足りないようだ。",
                "The tome is silent. You lack practical experience in necromancy.", "禁书沉默了。似乎缺乏死灵术的实践经验。"),

            // Apotheosis
            ["apotheosisNoSoul"] = (
                "伝説の魂が必要だ。",
                "A legendary soul is required.", "需要传说之灵魂。"),
            ["apotheosisNoMaterials"] = (
                "儀式に必要な素材が揃っていない。",
                "The ritual materials are not yet complete.", "仪式所需的素材尚未备齐。"),
            ["apotheosisComplete"] = (
                "昇華の儀式が完了した。あなたの魔力は次元を超えた。",
                "The apotheosis ritual is complete. Your magical power has transcended dimensions.", "升华仪式完成了。你的魔力已超越次元。"),
            ["apotheosisSoulHarvest"] = (
                "死者の残響が凝縮し、魂がひとつ生まれた。",
                "The dead leave behind an echo. A soul is born from it.",
                "死者的余响凝聚成形，诞生了一枚灵魂。"),

            // ── Ritual material slots (6-slot system) ──
            ["ritualSlotSoul"] = (
                "伝説の魂",
                "Legendary Soul", "传说之灵魂"),
            ["ritualSlotHeart"] = (
                "心臓",
                "Heart", "心脏"),
            ["ritualSlotBlood"] = (
                "堕落した血",
                "Blood of the Fallen", "堕落之血"),
            ["ritualSlotPoison"] = (
                "毒",
                "Poison", "毒"),
            ["ritualSlotMercury"] = (
                "柔らかな銀",
                "Soft Silver", "柔软之银"),
            ["ritualSlotCursedDew"] = (
                "穢された清浄の泡",
                "Defiled Bubbles of Purity", "被玷污的净化之泡"),
            ["ritualCommentSoul"] = (
                "死を超越する鍵。これなくして儀式は成らぬ。",
                "The key to transcending death. Without it, the ritual cannot proceed.", "超越死亡的钥匙。没有它，仪式无法进行。"),
            ["ritualCommentHeart"] = (
                "生命の座。肉体の楔として。",
                "The seat of life. An anchor for the flesh.", "生命之座。肉体的楔。"),
            ["ritualCommentBlood"] = (
                "堕天使の血。……堕落は力だ。それを知る者は少ない。",
                "Fallen angel's blood. ...A fall from grace is power. Few understand that.", "堕天使之血。……堕落即是力量。知晓此理者寥寥无几。"),
            ["ritualCommentPoison"] = (
                "死は始まりだ。術式はそこから動き出す。",
                "Death is the beginning. The formula starts from there.", "死亡是开端。术式由此启动。"),
            ["ritualCommentMercury"] = (
                "水銀。……覚書の筆者は「世界の血」と呼んでいたか。",
                "Mercury. ...The author called it \"the blood of the world.\"", "水银。……覚书的笔者称之为「世界之血」。"),
            ["ritualCommentCursedDew"] = (
                "呪われたラムネ。清浄と穢れの矛盾を一つの器に。",
                "Cursed ramune. Purity and corruption, contradicted in a single vessel.", "被诅咒的弹珠汽水。将净化与污秽的矛盾封于一器之中。"),
            ["ritualNotesButton"] = (
                "覚書を読む",
                "Read Notes", "阅读备忘录"),
            ["ritualNotesGiven"] = (
                "禁書から古い覚書が見つかった。",
                "An old memorandum fell from the tome.", "从禁书中发现了古老的备忘录。"),
            ["ritualHeader"] = (
                "◆ 昇華の儀式 ── 素材",
                "◆ Apotheosis Ritual — Materials", "◆ 升华仪式 ── 素材"),

            // Karen encounter
            ["karenAppears"] = (
                "「止まれ、死霊術師！　その禁書を渡してもらう！」",
                "\"Halt, necromancer! Hand over that forbidden tome!\"", "「住手，死灵术士！交出那本禁书！」"),
            ["karenRetreats"] = (
                "「…ッ！　この力、あの書の…！　撤退する！」",
                "\"...Tch! This power, from that tome...! I'm retreating!\"", "「……啧！这股力量，是那本书的……！撤退！」"),
            ["karenJournalDrop"] = (
                "カレンが手帳を落として去った。",
                "Karen dropped her journal and fled.", "卡伦丢下笔记逃走了。"),

            // Erenos battle
            ["erenosAppears"] = (
                "禁書の中から、影が這い出てきた…エレノスの影だ。",
                "A shadow crawls out of the tome... It is the shadow of Erenos.", "一道影从禁书中爬出……是艾雷诺斯之影。"),
            ["erenosDefeated"] = (
                "エレノスの影が消え去った。禁書が最後の秘密を明かす。",
                "The shadow of Erenos vanishes. The tome reveals its final secret.", "艾雷诺斯之影消散了。禁书揭示了最后的秘密。"),

            // Chapter Four tab
            ["chapterFourTitle"] = (
                "第肆章 遺灰の系譜",
                "IV - Lineage of Ashes", "第肆章 遗灰之谱系"),

            // ── Scout encounter (施策1) ──
            ["scoutAppears"] = (
                "死霊術の気配を感じ取ったのか、神殿騎士の偵察兵が現れた！",
                "Sensing the aura of necromancy, temple scouts have appeared!", "察觉到死灵术的气息，神殿骑士的侦察兵出现了！"),

            // ── Grimoire prompts (施策4) ──
            ["tomeReacting"] = (
                "禁書が……震えている？",
                "The tome... is it trembling?", "禁书……在颤抖？"),
            ["tomeChanged"] = (
                "禁書に何か変化が……",
                "Something changed in the tome...", "禁书似乎发生了什么变化……"),

            // ── Post-drama dialog (施策5) ──
            ["questDramaComplete"] = (
                "禁書に新たな記述が現れた。",
                "New writings have appeared in the tome.", "禁书中出现了新的记述。"),
            ["questChapterFourAppeared"] = (
                "禁書に新たな章が浮かび上がった。『第肆章』",
                "A new chapter has surfaced in the tome. \u2014 Chapter IV \u2014", "禁书中浮现了新的章节。「第肆章」"),

            // ── Servant context menu ──
            ["servantRename"] = ("名前を変更", "Rename", "改名"),
            ["servantTactics"] = ("作戦を変更", "Change Tactics", "更改战术"),
            ["servantDormant"] = ("展示モードにする", "Enter Display Mode", "进入展示模式"),
            ["servantActivate"] = ("行動を再開する", "Resume Activity", "恢复活动"),
            ["servantDormantOn"] = (
                "#1は静かに佇んでいる…",
                "#1 stands quietly...", "#1静静地伫立着……"),
            ["servantDormantOff"] = (
                "#1は再び動き始めた。",
                "#1 begins to move again.", "#1再次开始行动了。"),
            ["tacticDefault"] = ("デフォルト", "Default", "默认"),

            // ── Pursuit pause (追跡一時停止) ──
            ["pursuitPauseOffer"] = (
                "禁書が僅かに震えている。\nあなたが危険な状況にあることを感知しているようだ。\n力を抑え、あなたへの襲撃を遅らせますか？",
                "The tome trembles faintly.\nIt seems to sense that you are in danger.\nSuppress its power to delay attacks against you?",
                "禁书微微颤抖着。\n它似乎感知到你正处于危险之中。\n要抑制力量，延缓对你的袭击吗？"),
            ["pursuitPauseActive"] = (
                "禁書の力は抑えられている。\nあなたを追う者たちは痕跡を見失っているようだ。",
                "The tome's power is suppressed.\nThose who pursue you seem to have lost your trail.",
                "禁书的力量被抑制了。\n追踪你的人似乎失去了你的踪迹。"),
            ["pursuitPauseOn"] = (
                "力を抑える",
                "Suppress Power", "抑制力量"),
            ["pursuitPauseOff"] = (
                "力を解放する",
                "Release Power", "释放力量"),

            // Soulless creature guard
            ["spellNoSoul"] = (
                "#1の術は#2には効かない。魂がないのだ。",
                "#1's spell has no effect on #2. It has no soul.", "#1的术对#2无效。它没有灵魂。"),
        };

        /// <summary>
        /// Get the localized text for a message key.
        /// Falls back to the key itself if not found.
        /// </summary>
        public static string Get(string key)
        {
            if (Messages.TryGetValue(key, out var msg))
            {
                if (Lang.langCode == "CN")
                    return string.IsNullOrEmpty(msg.cn) ? msg.en : msg.cn;
                return Lang.isJP ? msg.jp : msg.en;
            }
            return key;
        }

        /// <summary>
        /// Display a localized message in the game log.
        /// With card references: uses Msg.Say for #1/#2 name substitution.
        /// Without card references: uses Msg.SayRaw for direct text display.
        /// </summary>
        public static void Say(string key, Card? ref1 = null, Card? ref2 = null)
        {
            var text = Get(key);
            if (ref1 != null && ref2 != null)
                Msg.Say(text, ref1, ref2);
            else if (ref1 != null)
                Msg.Say(text, ref1);
            else
                Msg.SayRaw(text);
        }

        /// <summary>Get attribute name for display.</summary>
        public static string GetAttrName(int attrId)
        {
            return attrId switch
            {
                70 => "STR",
                71 => "END",
                72 => "DEX",
                73 => "PER",
                74 => "LER",
                75 => "WIL",
                76 => "MAG",
                77 => "CHA",
                79 => "SPD",
                _ => $"Attr{attrId}",
            };
        }

        /// <summary>Get soul type display name.</summary>
        public static string GetSoulName(string soulId)
        {
            return soulId switch
            {
                "ars_soul_weak" => Lang.langCode == "CN" ? "微弱的灵魂" : Lang.isJP ? "弱い魂" : "Weak Soul",
                "ars_soul_normal" => Lang.langCode == "CN" ? "灵魂" : Lang.isJP ? "魂" : "Soul",
                "ars_soul_strong" => Lang.langCode == "CN" ? "强大的灵魂" : Lang.isJP ? "強い魂" : "Strong Soul",
                "ars_soul_legendary" => Lang.langCode == "CN" ? "传说之灵魂" : Lang.isJP ? "伝説の魂" : "Legendary Soul",
                _ => soulId,
            };
        }
    }
}
