using System.Collections.Generic;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Narrative text content for Chapter Four: Lineage of Ashes.
    /// Each stage accumulates — the journal grows as the quest progresses.
    /// </summary>
    public static class ChapterFourContent
    {
        // ── Ink Colors (handwriting authors) ──
        public static readonly Color ErenosInk = new(0.45f, 0.28f, 0.55f);    // Dark purple
        public static readonly Color PlayerInk = new(0.55f, 0.18f, 0.15f);    // Dark crimson
        public static readonly Color UnknownInk = new(0.50f, 0.48f, 0.44f);   // Faded gray
        public static readonly Color KarenInk = new(0.25f, 0.40f, 0.60f);     // Steel blue
        public static readonly Color FirstInk = new(0.40f, 0.32f, 0.20f);     // Ancient bronze
        public static readonly Color SystemInk = new(0.65f, 0.55f, 0.30f);    // Tarnished gold

        public struct JournalEntry
        {
            public string TextJP;
            public string TextEN;
            public string? TextCN;
            public Color InkColor;
            public string AuthorLabelJP;
            public string AuthorLabelEN;
            public string? AuthorLabelCN;

            public string Text =>
                Lang.langCode == "CN" ? (TextCN ?? TextEN) :
                Lang.isJP ? TextJP : TextEN;

            public string AuthorLabel =>
                Lang.langCode == "CN" ? (AuthorLabelCN ?? AuthorLabelEN) :
                Lang.isJP ? AuthorLabelJP : AuthorLabelEN;
        }

        /// <summary>
        /// Get all journal entries visible at the given stage (cumulative).
        /// </summary>
        public static List<JournalEntry> GetEntries(UnhallowedStage stage)
        {
            var entries = new List<JournalEntry>();

            if (stage >= UnhallowedStage.Stage1)
                AddStage1(entries);
            if (stage >= UnhallowedStage.Stage2)
                AddStage2(entries);
            if (stage >= UnhallowedStage.Stage3)
                AddStage3(entries);
            if (stage >= UnhallowedStage.Stage4)
                AddStage4(entries);
            if (stage >= UnhallowedStage.Stage5)
                AddStage5(entries);
            if (stage >= UnhallowedStage.Stage6)
                AddStage6(entries);
            if (stage >= UnhallowedStage.Stage7)
                AddStage7(entries);
            if (stage >= UnhallowedStage.Stage8)
                AddStage8(entries);
            if (stage >= UnhallowedStage.Stage9)
                AddStage9(entries);
            if (stage >= UnhallowedStage.Stage10)
                AddStage10(entries);

            return entries;
        }

        // ── Stage 1: Blood-Stained Prologue ──
        private static void AddStage1(List<JournalEntry> entries)
        {
            entries.Add(new JournalEntry
            {
                TextJP = "─ 序 ─",
                TextEN = "─ Prologue ─",
                TextCN = "─ 序 ─",
                InkColor = SystemInk
            });
            entries.Add(new JournalEntry
            {
                TextJP = "魂を手にした時、禁書の白紙の頁に文字が滲み出した。\n" +
                          "古い、しかし力強い筆跡...誰かの記録が、ここに眠っていたのだ。",
                TextEN = "When you grasped the soul, text bled through the blank pages of the tome.\n" +
                         "An old yet powerful hand — someone's records had been sleeping here.",
                TextCN = "当你握住灵魂时，禁书空白的书页上渗出了文字。\n" +
                         "古老而有力的笔迹……某人的记录，一直沉睡在此。",
                InkColor = PlayerInk,
                AuthorLabelJP = "◇ あなたの記録",
                AuthorLabelEN = "◇ Your Record",
                AuthorLabelCN = "◇ 你的记录"
            });
            entries.Add(new JournalEntry
            {
                TextJP = "『この書を手に取る者へ。\n" +
                          "　私はエレノス。かつてアルス・モリエンディの継承者であった者だ。\n" +
                          "　この書は汝を選んだ。だが、それが祝福か呪いか...\n" +
                          "　まだ知る由もあるまい。』",
                TextEN = "'To whoever takes up this tome.\n" +
                         " I am Erenos. Once the inheritor of Ars Moriendi.\n" +
                         " This tome has chosen you. But whether that is blessing or curse —\n" +
                         " you cannot yet know.'",
                TextCN = "「致取此书之人。\n" +
                         "　吾乃艾雷诺斯。曾为 Ars Moriendi 之继承者。\n" +
                         "　此书已然选择了汝。然而，此乃祝福抑或诅咒……\n" +
                         "　汝尚无从知晓。」",
                InkColor = ErenosInk,
                AuthorLabelJP = "◆ エレノスの手記",
                AuthorLabelEN = "◆ Erenos's Writing",
                AuthorLabelCN = "◆ 艾雷诺斯的手记"
            });
        }

        // ── Stage 2: Indelible Ink ──
        private static void AddStage2(List<JournalEntry> entries)
        {
            entries.Add(new JournalEntry
            {
                TextJP = "─ 消えない墨跡 ─",
                TextEN = "─ Indelible Ink ─",
                TextCN = "─ 不褪的墨迹 ─",
                InkColor = SystemInk
            });
            entries.Add(new JournalEntry
            {
                TextJP = "禁書を開いた。新たな頁が現れ、読み進めるうちに\n" +
                          "指先が痺れるような感覚が走った。\n" +
                          "文字が...身体に染み込んでいく。",
                TextEN = "You opened the tome. New pages appeared, and as you read,\n" +
                         "a tingling sensation ran through your fingertips.\n" +
                         "The text — seeping into your body.",
                TextCN = "翻开了禁书。新的书页浮现，阅读之际\n" +
                         "指尖传来一阵麻痹般的感觉。\n" +
                         "文字……正渗入体内。",
                InkColor = PlayerInk,
                AuthorLabelJP = "◇ あなたの記録",
                AuthorLabelEN = "◇ Your Record",
                AuthorLabelCN = "◇ 你的记录"
            });
            entries.Add(new JournalEntry
            {
                TextJP = "『気づいたか。書の力が汝に流れ始めている。\n" +
                          "　魔力と意志...それが死霊術の礎だ。\n" +
                          "　だが、力を得れば狩人が来る。\n" +
                          "　神殿の騎士どもだ。奴らは必ず来る。』",
                TextEN = "'You've noticed it. The tome's power flows into you.\n" +
                         " Magic and will — the foundation of necromancy.\n" +
                         " But gain power, and hunters will come.\n" +
                         " The Temple Knights. They will come without fail.'",
                TextCN = "「汝已然察觉。书之力正向汝流淌。\n" +
                         "　魔力与意志……此乃死灵术之基石。\n" +
                         "　然而，获得力量之后，猎人便会前来。\n" +
                         "　神殿骑士团。彼等必将到来。」",
                InkColor = ErenosInk,
                AuthorLabelJP = "◆ エレノスの手記",
                AuthorLabelEN = "◆ Erenos's Writing",
                AuthorLabelCN = "◆ 艾雷诺斯的手记"
            });
        }

        // ── Stage 3: Ashen Sentinel ──
        private static void AddStage3(List<JournalEntry> entries)
        {
            entries.Add(new JournalEntry
            {
                TextJP = "─ 灰の番人 ─",
                TextEN = "─ Ashen Sentinel ─",
                TextCN = "─ 灰之守望 ─",
                InkColor = SystemInk
            });
            entries.Add(new JournalEntry
            {
                TextJP = "エレノスの警告通りだった。\n" +
                          "神殿騎士...聖騎士カレンと名乗る女が、部下を率いて現れた。\n" +
                          "禁書を渡せと。",
                TextEN = "Erenos's warning proved true.\n" +
                         "Temple Knights — a woman calling herself Holy Knight Karen appeared with her subordinates.\n" +
                         "She demanded the tome.",
                TextCN = "艾雷诺斯的警告应验了。\n" +
                         "神殿骑士团……一位自称圣骑士卡伦的女性率领部下现身。\n" +
                         "她要求交出禁书。",
                InkColor = PlayerInk,
                AuthorLabelJP = "◇ あなたの記録",
                AuthorLabelEN = "◇ Your Record",
                AuthorLabelCN = "◇ 你的记录"
            });
        }

        // ── Stage 4: Ashen Records ──
        private static void AddStage4(List<JournalEntry> entries)
        {
            entries.Add(new JournalEntry
            {
                TextJP = "─ 灰燼記録 ─",
                TextEN = "─ Ashen Records ─",
                TextCN = "─ 灰烬记录 ─",
                InkColor = SystemInk
            });
            entries.Add(new JournalEntry
            {
                TextJP = "カレンは撤退した。彼女が落とした手帳を読んだ。\n" +
                          "そこには...アルス・モリエンディの前の継承者について、\n" +
                          "神殿が執拗に追っていた記録があった。",
                TextEN = "Karen retreated. You read the journal she dropped.\n" +
                         "In it — records of the temple's relentless pursuit\n" +
                         "of the previous inheritor of Ars Moriendi.",
                TextCN = "卡伦撤退了。你阅读了她遗落的笔记。\n" +
                         "其中记载着……神殿对 Ars Moriendi 前代继承者\n" +
                         "穷追不舍的记录。",
                InkColor = PlayerInk,
                AuthorLabelJP = "◇ あなたの記録",
                AuthorLabelEN = "◇ Your Record",
                AuthorLabelCN = "◇ 你的记录"
            });
            entries.Add(new JournalEntry
            {
                TextJP = "『私は逃げ続けた。だが、いずれ力尽きることは分かっていた。\n" +
                          "　だからこそ、この書に全てを記した。\n" +
                          "　次の継承者が、私と同じ過ちを繰り返さぬように。』",
                TextEN = "'I kept running. But I knew my strength would fail eventually.\n" +
                         " That is why I recorded everything in this tome.\n" +
                         " So the next inheritor would not repeat my mistakes.'",
                TextCN = "「吾一直在逃。然吾知，终有力竭之时。\n" +
                         "　正因如此，吾将一切记于此书。\n" +
                         "　唯愿下一位继承者，不再重蹈吾之覆辙。」",
                InkColor = ErenosInk,
                AuthorLabelJP = "◆ エレノスの手記",
                AuthorLabelEN = "◆ Erenos's Writing",
                AuthorLabelCN = "◆ 艾雷诺斯的手记"
            });
            entries.Add(new JournalEntry
            {
                TextJP = "『一度退けたとて、油断するな。\n" +
                          "　奴らが諦めたとは思えない。\n" +
                          "　二度目の襲撃があるかもしれぬ。備えよ。』",
                TextEN = "'Do not let your guard down after repelling them once.\n" +
                         " I doubt they have given up.\n" +
                         " A second attack may come. Be prepared.'",
                TextCN = "「切勿因击退一次而掉以轻心。\n" +
                         "　吾不信他们已经放弃。\n" +
                         "　或许还会有第二次袭击。务必警惕。」",
                InkColor = ErenosInk,
                AuthorLabelJP = "◆ エレノスの手記",
                AuthorLabelEN = "◆ Erenos's Writing",
                AuthorLabelCN = "◆ 艾雷诺斯的手记"
            });
        }

        // ── Stage 5: Stigmata ──
        private static void AddStage5(List<JournalEntry> entries)
        {
            entries.Add(new JournalEntry
            {
                TextJP = "─ 聖痕 ─",
                TextEN = "─ Stigmata ─",
                TextCN = "─ 圣痕 ─",
                InkColor = SystemInk
            });
            entries.Add(new JournalEntry
            {
                TextJP = "禁書の頁が深くなっていく。エレノスの記述が、\n" +
                          "次第に切迫したものに変わっている。\n" +
                          "最後の数頁は...震えた筆跡で書かれていた。",
                TextEN = "The pages of the tome grow deeper. Erenos's writing\n" +
                         "becomes increasingly desperate.\n" +
                         "The final pages — written in a trembling hand.",
                TextCN = "禁书的页面愈发深邃。艾雷诺斯的笔迹\n" +
                         "渐渐变得急迫。\n" +
                         "最后数页……是以颤抖的字迹写就的。",
                InkColor = PlayerInk,
                AuthorLabelJP = "◇ あなたの記録",
                AuthorLabelEN = "◇ Your Record",
                AuthorLabelCN = "◇ 你的记录"
            });
            entries.Add(new JournalEntry
            {
                TextJP = "──カレンの記録（禁書に挟まれていた断片）──\n" +
                          "「アルヴィンは笑っていた。自分が何を失ったか理解していない\n" +
                          "　...あるいは理解した上で、それを損失と思わなくなっていた。\n" +
                          "　どちらが正しいのかを確かめる術は、もう私にはない。」",
                TextEN = "— Karen's record (a fragment tucked in the tome) —\n" +
                         "'Alvin was smiling. He did not understand what he had lost\n" +
                         " — or perhaps he understood, and no longer considered it a loss.\n" +
                         " I no longer have the means to determine which is true.'",
                TextCN = "──卡伦的记录（夹在禁书中的残页）──\n" +
                         "「阿尔文在笑。他不明白自己失去了什么\n" +
                         "　……或者说，他明白了，却不再视之为损失。\n" +
                         "　究竟哪个才是真相，我已无从确认。」",
                InkColor = KarenInk,
                AuthorLabelJP = "◈ カレンの手帳より",
                AuthorLabelEN = "◈ From Karen's Journal",
                AuthorLabelCN = "◈ 摘自卡伦的笔记"
            });
            entries.Add(new JournalEntry
            {
                TextJP = "『最後の試練が残っている。\n" +
                          "　この書の力を完全に継承するには、先代の影を超えなければならない。\n" +
                          "　つまり...私自身の影だ。\n" +
                          "　禁書を開け。私が待っている。』",
                TextEN = "'One final trial remains.\n" +
                         " To fully inherit the tome\\'s power, you must surpass the predecessor\\'s shadow.\n" +
                         " In other words — my own shadow.\n" +
                         " Open the tome. I await you.'",
                TextCN = "「最后的试炼尚存。\n" +
                         "　欲完全继承此书之力，汝须超越前代之影。\n" +
                         "　换言之……吾自身的影。\n" +
                         "　打开禁书。吾在此等候。」",
                InkColor = ErenosInk,
                AuthorLabelJP = "◆ エレノスの手記",
                AuthorLabelEN = "◆ Erenos's Writing",
                AuthorLabelCN = "◆ 艾雷诺斯的手记"
            });
        }

        // ── Stage 6: The Seventh Sign ──
        private static void AddStage6(List<JournalEntry> entries)
        {
            entries.Add(new JournalEntry
            {
                TextJP = "─ 第七の徴 ─",
                TextEN = "─ The Seventh Sign ─",
                TextCN = "─ 第七之兆 ─",
                InkColor = SystemInk
            });
            entries.Add(new JournalEntry
            {
                TextJP = "禁書を開いた瞬間、頁が増えていることに気づいた。\n" +
                          "新しい頁には文字がない。だが白紙ではない。インクが頁の奥から滲み出している。\n" +
                          "まだ存在しない誰かの文字...連鎖は続いている。",
                TextEN = "The moment you open the tome, you notice more pages.\n" +
                         "The new pages have no writing, but they are not blank. Ink seeps from within.\n" +
                         "Characters of someone who does not yet exist — the chain continues.",
                TextCN = "打开禁书的瞬间，发现书页增加了。\n" +
                         "新的书页上没有文字。但并非空白。墨水正从书页深处渗出。\n" +
                         "尚不存在之人的文字……连锁仍在继续。",
                InkColor = PlayerInk,
                AuthorLabelJP = "◇ あなたの記録",
                AuthorLabelEN = "◇ Your Record",
                AuthorLabelCN = "◇ 你的记录"
            });
            entries.Add(new JournalEntry
            {
                TextJP = "禁書の最後の頁に、一行だけ浮かび上がった。自分の字ではない。\n" +
                          "先代の字でもない。もっと古い...初代の筆跡だ。",
                TextEN = "On the tome's last page, a single line surfaces. Not your handwriting.\n" +
                         "Not the predecessor's either. Older — the first inheritor's hand.",
                TextCN = "禁书最后一页，浮现出一行字。不是自己的字迹。\n" +
                         "也不是前代的。更加古老……是初代的笔迹。",
                InkColor = PlayerInk,
                AuthorLabelJP = "◇ あなたの記録",
                AuthorLabelEN = "◇ Your Record",
                AuthorLabelCN = "◇ 你的记录"
            });
            entries.Add(new JournalEntry
            {
                TextJP = "『七代目は、問いを持つ者であれ』",
                TextEN = "'Let the seventh be one who carries questions'",
                TextCN = "「第七代，当为怀疑问之人」",
                InkColor = FirstInk,
                AuthorLabelJP = "✧ 初代の筆跡",
                AuthorLabelEN = "✧ The First's Hand",
                AuthorLabelCN = "✧ 初代的笔迹"
            });
        }

        // ── Stage 7: Karen's Shadow ──
        private static void AddStage7(List<JournalEntry> entries)
        {
            entries.Add(new JournalEntry
            {
                TextJP = "─ 先代の影法師 ─",
                TextEN = "─ Karen's Shadow ─",
                TextCN = "─ 前代之影 ─",
                InkColor = SystemInk
            });
            entries.Add(new JournalEntry
            {
                TextJP = "聖騎士カレンが再び現れた。従者の数が先代と同じ水準に達したことへの警告。\n" +
                          "「あなたの目を見ている。先代の目と同じ色だ。だが...光が違う」と。\n" +
                          "先代の影を受け止め、最後の試練に備えよ。",
                TextEN = "Holy Knight Karen has appeared once more, warning that your servant count matches the predecessor's.\n" +
                         "'I'm looking at your eyes. The same color as the predecessor's. But the light is different,' she says.\n" +
                         "Accept the shadow of the past and prepare for the final trial.",
                TextCN = "圣骑士卡伦再度现身。她警告你的仆从数已达到与前代同等的水平。\n" +
                         "「我在看你的眼睛。和前代相同的颜色。但是……光芒不同」她如此说道。\n" +
                         "承受前代之影，为最后的试炼做好准备。",
                InkColor = PlayerInk,
                AuthorLabelJP = "◇ あなたの記録",
                AuthorLabelEN = "◇ Your Record",
                AuthorLabelCN = "◇ 你的记录"
            });
        }

        // ── Stage 8: Shadow of the Predecessor ──
        private static void AddStage8(List<JournalEntry> entries)
        {
            entries.Add(new JournalEntry
            {
                TextJP = "─ 先代の影 ─",
                TextEN = "─ Shadow of the Predecessor ─",
                TextCN = "─ 前代之影 ─",
                InkColor = SystemInk
            });
            entries.Add(new JournalEntry
            {
                TextJP = "エレノスの影を打ち倒した。\n" +
                          "禁書が最後の章を開き、昇華の儀式が記されていた。\n" +
                          "古い紙片が頁の間から零れ落ちた──初代の覚書だ。",
                TextEN = "You defeated Erenos's shadow.\n" +
                         "The tome opens its final chapter, revealing the apotheosis ritual.\n" +
                         "Old papers fell from between the pages \u2014 the predecessor's memorandum.",
                TextCN = "击败了艾雷诺斯之影。\n" +
                         "禁书翻开了最后的篇章，升华仪式赫然在列。\n" +
                         "古老的纸片从书页间滑落——是初代的备忘录。",
                InkColor = PlayerInk,
                AuthorLabelJP = "◇ あなたの記録",
                AuthorLabelEN = "◇ Your Record",
                AuthorLabelCN = "◇ 你的记录"
            });
            entries.Add(new JournalEntry
            {
                TextJP = "『よくぞここまで辿り着いた。\n" +
                          "　汝は私を超えた。』",
                TextEN = "'You have come far enough.\n" +
                         " You have surpassed me.'",
                TextCN = "「汝已然走到此处。\n" +
                         "　汝已超越了吾。」",
                InkColor = ErenosInk,
                AuthorLabelJP = "◆ エレノスの手記",
                AuthorLabelEN = "◆ Erenos's Writing",
                AuthorLabelCN = "◆ 艾雷诺斯的手记"
            });
        }

        // ── Stage 9: Unhallowed Awakening ──
        private static void AddStage9(List<JournalEntry> entries)
        {
            entries.Add(new JournalEntry
            {
                TextJP = "─ 不浄なる覚醒 ─",
                TextEN = "─ Unhallowed Awakening ─",
                TextCN = "─ 不洁的觉醒 ─",
                InkColor = SystemInk
            });
            entries.Add(new JournalEntry
            {
                TextJP = "昇華の儀式を終えた。身体中に力が漲る。\n" +
                          "禁書の著者名が...私の名前に変わっていた。",
                TextEN = "The apotheosis ritual is complete. Power surges through your body.\n" +
                         "The tome's author name has changed — to yours.",
                TextCN = "升华仪式已毕。力量充盈全身。\n" +
                         "禁书上的著者之名……已变为自己的名字。",
                InkColor = PlayerInk,
                AuthorLabelJP = "◇ あなたの記録",
                AuthorLabelEN = "◇ Your Record",
                AuthorLabelCN = "◇ 你的记录"
            });
            entries.Add(new JournalEntry
            {
                TextJP = "『最後にひとつだけ。\n" +
                          "　いつか聖騎士カレンに会ったら...伝えてくれ。\n" +
                          "　「悪かったな」と。\n\n" +
                          "　　　　　　　　　　　──エレノス 最後の記述』",
                TextEN = "'One last thing.\n" +
                         " If you ever meet Holy Knight Karen — tell her.\n" +
                         " \"I\\'m sorry.\"\n\n" +
                         "                    — Erenos, final entry'",
                TextCN = "「最后只有一件事。\n" +
                         "　若有朝一日见到圣骑士卡伦……替吾传达。\n" +
                         "　「对不起」。\n\n" +
                         "　　　　　　　　　　　──艾雷诺斯　最后的记述」",
                InkColor = ErenosInk,
                AuthorLabelJP = "◆ エレノスの手記",
                AuthorLabelEN = "◆ Erenos's Writing",
                AuthorLabelCN = "◆ 艾雷诺斯的手记"
            });
        }

        // ── Stage 10: Successor's Notes ──
        private static void AddStage10(List<JournalEntry> entries)
        {
            entries.Add(new JournalEntry
            {
                TextJP = "─ 継承者の手記 ─",
                TextEN = "─ Successor's Notes ─",
                TextCN = "─ 继承者的手记 ─",
                InkColor = SystemInk
            });
            entries.Add(new JournalEntry
            {
                TextJP = "昇華を経て世界の見え方が変わった。生者の魂が色として見え、死者の残滓が匂いとして感じられる。\n" +
                          "六代目としての章が始まった。禁書が自動的に記録を刻んでいる。",
                TextEN = "The world looks different after Apotheosis. Living souls appear as colors, the residue of the dead as scent.\n" +
                         "Your chapter as the sixth inheritor has begun. The tome inscribes records on its own.",
                TextCN = "经历升华后，世界的面貌已然改变。生者之魂化为色彩，死者之残渣化为气息。\n" +
                         "作为第六代继承者的篇章已经开始。禁书正自行刻录着记录。",
                InkColor = PlayerInk,
                AuthorLabelJP = "◇ あなたの記録",
                AuthorLabelEN = "◇ Your Record",
                AuthorLabelCN = "◇ 你的记录"
            });
        }
    }
}
