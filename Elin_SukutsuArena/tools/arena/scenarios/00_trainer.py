# -*- coding: utf-8 -*-
"""
00_trainer.py - アイリス（トレーナー）のメインダイアログ
NPCクリック時の会話処理
"""

from arena.builders import ArenaDramaBuilder
from arena.data import QUEST_DONE_PREFIX, Actors, DramaNames, Keys, QuestIds


def define_trainer_main_drama(builder: ArenaDramaBuilder):
    """
    アイリスのメインダイアログ
    NPCクリック時に表示される会話
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You")
    trainer = builder.register_actor("sukutsu_trainer", "アイリス", "Iris")
    null = builder.register_actor("sukutsu_null", "ヌル", "Null")
    astaroth = builder.register_actor("sukutsu_astaroth", "アスタロト", "Astaroth")

    # ラベル定義
    main = builder.label("main")
    greeting_pre_game = builder.label("greeting_pre_game")
    greeting_post_game = builder.label("greeting_post_game")
    quest_done_last_battle = f"{QUEST_DONE_PREFIX}{QuestIds.LAST_BATTLE}"
    choices = builder.label("choices")
    end = builder.label("end")

    # 特別のトレーニング用ラベル
    special_training_menu = builder.label("special_training_menu")
    training_sense = builder.label("training_sense")
    training_leg = builder.label("training_leg")
    training_hotspring = builder.label("training_hotspring")

    # 傷の手当て用ラベル
    cure_damage = builder.label("cure_damage")
    cure_explain = builder.label("cure_explain")
    cure_has_wrath = builder.label("cure_has_wrath")
    cure_wrath_confirm = builder.label("cure_wrath_confirm")
    cure_wrath_paid = builder.label("cure_wrath_paid")
    cure_wrath_no_money = builder.label("cure_wrath_no_money")
    cure_no_wrath = builder.label("cure_no_wrath")

    # 特別なトレーニング導入用ラベル
    tail_intro = builder.label("tail_intro")

    # 秘密メニュー用ラベル
    secret_menu = builder.label("secret_menu")
    # ボス与ダメ倍率設定メニュー
    boss_damage_menu = builder.label("boss_damage_menu")
    boss_damage_check = builder.label("boss_damage_check")
    boss_damage_current_hard = builder.label("boss_damage_current_hard")
    boss_damage_current_vanilla = builder.label("boss_damage_current_vanilla")
    boss_damage_current_normal = builder.label("boss_damage_current_normal")
    boss_damage_current_easy = builder.label("boss_damage_current_easy")
    boss_damage_current_story = builder.label("boss_damage_current_story")
    boss_damage_select = builder.label("boss_damage_select")
    boss_damage_hard = builder.label("boss_damage_hard")
    boss_damage_vanilla = builder.label("boss_damage_vanilla")
    boss_damage_normal = builder.label("boss_damage_normal")
    boss_damage_easy = builder.label("boss_damage_easy")
    boss_damage_story = builder.label("boss_damage_story")

    # アンインストール処理用ラベル
    uninstall_menu = builder.label("uninstall_menu")
    uninstall_confirm1 = builder.label("uninstall_confirm1")
    uninstall_confirm2 = builder.label("uninstall_confirm2")
    uninstall_confirm3 = builder.label("uninstall_confirm3")
    uninstall_skit = builder.label("uninstall_skit")
    uninstall_final = builder.label("uninstall_final")
    uninstall_execute = builder.label("uninstall_execute")

    # 周回処理用ラベル
    newgame_menu = builder.label("newgame_menu")
    newgame_confirm1 = builder.label("newgame_confirm1")
    newgame_confirm2 = builder.label("newgame_confirm2")
    newgame_execute = builder.label("newgame_execute")

    # ========================================
    # エントリーポイント（エピローグ完了チェック）
    # ========================================
    builder.step(main).branch_if(
        quest_done_last_battle, "==", 1, greeting_post_game
    ).jump(greeting_pre_game)

    # ========================================
    # 選択肢（エピローグ前）
    # ========================================
    builder.step(greeting_pre_game).say(
        "greet",
        "やぁ君！私と一緒に……汗を流さない？",
        "Welcome! Want to... work up a sweat with me?",
        "欢迎！要不要和我一起……流点汗呢？",
        actor=trainer,
    ).inject_unique().choice(
        special_training_menu,
        "特別なトレーニングを所望する",
        "Receive special training",
        "接受特别训练",
        text_id="c_special_training",
    ).choice(
        secret_menu,
        "（Mod設定）",
        "(Mod settings)",
        "（Mod设置）",
        text_id="c_secret",
    ).choice(
        end, "また今度", "Perhaps another time", "下次再说", text_id="c_bye"
    ).on_cancel(end)

    # ========================================
    # 選択肢（エピローグ後）
    # ========================================
    builder.step(greeting_post_game).say(
        "greet_post",
        "やぁ君！また会えたね。何か用？",
        "Hey there! Good to see you again. Need something?",
        "嗨！又见面了呢。有什么事吗？",
        actor=trainer,
    ).inject_unique().choice(
        special_training_menu,
        "特別なトレーニングを所望する",
        "Receive special training",
        "接受特别训练",
        text_id="c_pg_special_training",
    ).choice(
        secret_menu,
        "（Mod設定）",
        "(Mod settings)",
        "（Mod设置）",
        text_id="c_pg_secret",
    ).choice(
        end, "また今度", "Perhaps another time", "下次再说", text_id="c_pg_bye"
    ).on_cancel(end)

    # 戻り先用（他のメニューから戻る時に使用）
    # エピローグ後かどうかで適切な画面に遷移
    builder.step(choices).jump(main)

    # ========================================
    # 特別のトレーニング選択メニュー
    # ========================================
    builder.step(special_training_menu).say(
        "special_menu",
        "どのトレーニングにする？",
        "Which training do you want?",
        "想要做什么训练？",
        actor=trainer,
    ).choice(
        training_sense,
        "闇で当てろ",
        "Darkness Training",
        "黑暗训练",
        text_id="c_training_sense",
    ).choice(
        training_leg,
        "足腰マジで裏切らん",
        "Leg Training: Never Betrays",
        "腿部训练：绝不背叛",
        text_id="c_training_leg",
    ).choice(
        training_hotspring,
        "境界の足湯、今日も行こ",
        "Boundary Hot Spring",
        "边界足汤",
        text_id="c_training_hotspring",
    ).choice(
        cure_damage,
        "傷の手当て",
        "Heal Wounds",
        "治疗伤势",
        text_id="c_cure_damage",
    ).choice(
        tail_intro,
        "…特別中の、特別なやつ…わかるでしょ？",
        "...Something extra special...?",
        "……特别中的特别……有吗？",
        text_id="c_training_tail",
    ).choice(
        choices,
        "やめておく",
        "Never mind",
        "算了",
        text_id="c_training_cancel",
    ).on_cancel(choices)

    # ========================================
    # 各トレーニングへの遷移
    # ========================================
    # 感覚遮断訓練
    builder.step(training_sense).say(
        "sense_intro",
        "ね、闇耐性の訓練しよっか。視界奪われた時のために",
        "Hey, let's do some darkness resistance training. For when your vision gets taken",
        "诶，来做暗适应的训练吧。为了应对视野被夺走的时候",
        actor=trainer,
    ).start_drama(DramaNames.IRIS_SENSE_TRAINING).finish()

    # 足腰訓練
    builder.step(training_leg).say(
        "leg_intro",
        "はい集合〜。足元が悪い日もあるからね。だからレッグデイだよ。逃げんな",
        "Gather up! Some days the footing is bad. So, legs. Don't run away",
        "集合ー。有时候脚下不稳嘛。所以是腿部。别跑",
        actor=trainer,
    ).start_drama(DramaNames.IRIS_LEG_TRAINING).finish()

    # 足湯リカバリー
    builder.step(training_hotspring).say(
        "hotspring_intro",
        "ね、顔。疲れすぎ。足湯、行こ。今日も",
        "Hey, your face. Too tired. Let's go to the hot spring. Again today",
        "诶，你的脸。太累了。去泡脚吧。今天也是",
        actor=trainer,
    ).start_drama(DramaNames.IRIS_HOTSPRING).finish()

    # ========================================
    # 傷の手当て（全回復）
    # ========================================
    # 説明の選択肢
    builder.step(cure_damage).say(
        "cure_ask",
        "傷の手当てね。どうする？",
        "Wound treatment, huh? What would you like?",
        "治疗伤势啊。要怎么做？",
        actor=trainer,
    ).choice(
        cure_explain,
        "詳しく教えて",
        "Tell me more",
        "详细说说",
        text_id="c_cure_explain",
    ).choice(
        cure_has_wrath,
        "治療を受ける",
        "Receive treatment",
        "接受治疗",
        text_id="c_cure_receive",
    ).choice(
        choices,
        "やめておく",
        "Never mind",
        "算了",
        text_id="c_cure_cancel",
    ).on_cancel(choices)

    # 説明
    builder.step(cure_explain).say(
        "cure_explain_1",
        "特殊な病気とか、永続的なダメージとかを取り除けるよ",
        "I can remove special diseases, permanent damage, things like that",
        "我可以治疗特殊疾病、永久性伤害之类的",
        actor=trainer,
    ).say(
        "cure_explain_2",
        "例えば……ステータスへの永続ダメージ、毒、狂気、その他もろもろのデバフとか",
        "For example... permanent stat damage, poison, insanity, various other debuffs",
        "比如……永久属性伤害、毒素、疯狂、还有各种减益状态",
        actor=trainer,
    ).say(
        "cure_explain_3",
        "街の治癒師には真似できないやつ。……私の手には、ちょっと特殊な力があるから",
        "Stuff that city healers can't handle. ...My hands have a bit of a special power",
        "街上的治愈师做不到的。……因为我的手有一点特殊的力量",
        actor=trainer,
    ).say(
        "cure_explain_4",
        "一瞬、彼女の指先が揺らいだ気がした。",
        "For a moment, her fingertips seemed to waver.",
        "一瞬间，她的指尖似乎在摇晃。",
        actor=Actors.NARRATOR,
    ).say(
        "cure_explain_5",
        "ただ、良性のバフも一緒に消えちゃうから、そこは注意ね",
        "Just so you know, beneficial buffs will be removed too, so keep that in mind",
        "不过，有益的增益效果也会一起消失，这点要注意",
        actor=trainer,
    ).say(
        "cure_explain_6",
        "……まぁ、深く考えないで。基本的にはお代は貰わないけど、神罰だけは別。あれを取り除くなら...300万gpもらおうかな",
        "...Well, don't think too hard about it. It's basically free, but divine punishment is different. Removing that costs 3 million gp",
        "……嘛，别想太多。基本上是免费的，但神罚除外。解除神罚需要300万gp",
        actor=trainer,
    ).jump(cure_damage)

    # 神罰チェック（フラグにセットしてからbranch_ifで分岐）
    builder.step(cure_has_wrath).action(
        "modInvoke", param="check_has_wrath()"
    ).branch_if("temp_has_wrath", "==", 1, cure_wrath_confirm).jump(cure_no_wrath)

    # 神罰あり - 確認
    builder.step(cure_wrath_confirm).say(
        "cure_wrath_detected",
        "……あ、神罰受けてるね。これを取り除くには300万gpかかるけど、いい？",
        "...Ah, you've got divine punishment. Removing this costs 3 million gp. Is that okay?",
        "……啊，你受了神罚啊。解除这个需要300万gp，可以吗？",
        actor=trainer,
    ).choice(
        cure_wrath_paid,
        "払う",
        "Pay",
        "付钱",
        text_id="c_wrath_pay",
    ).choice(
        choices,
        "やめておく",
        "Never mind",
        "算了",
        text_id="c_wrath_cancel",
    ).on_cancel(choices)

    # 神罰治療 - 支払い（フラグにセットしてからbranch_ifで分岐）
    builder.step(cure_wrath_paid).action(
        "modInvoke", param="pay_wrath_fee()"
    ).branch_if("temp_paid_fee", "==", 1, cure_no_wrath).jump(cure_wrath_no_money)

    # お金不足
    builder.step(cure_wrath_no_money).say(
        "cure_no_money",
        "……ごめん、お金足りないみたい。また来てね",
        "...Sorry, looks like you don't have enough money. Come back later",
        "……抱歉，看来你钱不够。下次再来吧",
        actor=trainer,
    ).jump(choices)

    # 治療実行（神罰なし or 支払い済み）
    builder.step(cure_no_wrath).say(
        "cure_intro",
        "ん、ちょっと診せて。……うん、診断完了",
        "Hmm, let me take a look. ...Alright, diagnosis complete",
        "嗯，让我看看。……好，诊断完成",
        actor=trainer,
    ).action("modInvoke", param="full_recovery()").say(
        "cure_done",
        "はい、これでよし。無理しないでね",
        "There, all done. Don't push yourself too hard",
        "好了，这样就好了。别太勉强自己",
        actor=trainer,
    ).jump(choices)

    # ========================================
    # 特別なトレーニング（_tail導入）
    # ========================================
    builder.step(tail_intro).say(
        "tail_intro_msg",
        "…え、マジ？ そっちのトレーニング？ …いや、別にいいけどさ…ちょい恥ずいんだけど…",
        "...Wait, for real? That kind of training? ...I mean, it's fine, but... kinda embarrassing, y'know...",
        "……诶，认真的？那种训练？……不是说不行啦……就是有点害羞……",
        actor=trainer,
    ).say(
        "tail_vision_1",
        "一瞬、アイリスの輪郭が揺らいだ。その奥に、何かが蠢いているのが見えた気がした。",
        "For a moment, Iris's silhouette wavered. You thought you saw something writhing beneath.",
        "一瞬间，艾丽丝的轮廓摇晃了。你仿佛看到了某种东西在其下蠕动。",
        actor=Actors.NARRATOR,
    ).say(
        "tail_vision_2",
        "背筋を這う、理屈のわからない悪寒。内臓が裏返るような嫌悪感が、喉元までせり上がってくる。",
        "An inexplicable chill crawled up your spine. A nauseating revulsion, as if your insides were turning inside out, rose to your throat.",
        "一股无法解释的寒意爬上脊背。仿佛内脏被翻了出来的恶心感涌上喉咙。",
        actor=Actors.NARRATOR,
    ).say(
        "tail_vision_3",
        "彼女の微笑みの隙間から、赤黒い何かが覗いている。腸か、舌か、それとも――",
        "Through the gaps of her smile, something dark red peeked out. Intestines, a tongue, or perhaps--",
        "从她微笑的缝隙中，窥见了某种暗红色的东西。是肠子，是舌头，还是--",
        actor=Actors.NARRATOR,
    ).flash_lut("LUT_Invert", duration=10.0, fade_time=0.3).action(
        "modInvoke", param="apply_deep_ecstasy()"
    ).jump(builder.label("_tail"))

    # ========================================
    # 秘密メニュー
    # ========================================
    builder.step(secret_menu).say(
        "secret_ask",
        "ん？どしたの？",
        "Hm? What's up?",
        "嗯？怎么了？",
        actor=trainer,
    ).choice(
        boss_damage_menu,
        "ボス与ダメ倍率を変更する",
        "Boss damage multiplier",
        "更改首领伤害倍率",
        text_id="c_boss_damage_menu",
    ).choice(
        newgame_menu,
        "闘技場を発見した時点に戻りたい（※周回用）",
        "I want to return to when I first found the Arena (※Cannot be undone)",
        "想回到初次来到斗技场的时候（※无法撤销）",
        text_id="c_newgame",
    ).choice(
        uninstall_menu,
        "Modをアンインストールしたい（※より徹底した削除）",
        "I want to uninstall this mod (※Cannot be undone)",
        "我想卸载这个Mod（※无法撤销）",
        text_id="c_uninstall_yes",
    ).choice(
        choices,
        "なんでもない",
        "It's nothing",
        "没什么",
        text_id="c_secret_no",
    ).on_cancel(choices)

    # ========================================
    # ボス与ダメ倍率設定
    # ========================================
    # 導入台詞
    builder.step(boss_damage_menu).say(
        "boss_damage_title",
        "この闘技場は時空の裂け目にあるでしょ？\u3000だから……ちょっとだけ、世界線をずらせるの。ボスの凶暴さが違う世界線にね。\n\nちなみに調整なしの世界線は「★過酷な現実」だよ。「バニラ」ではないから注意してね",
        "This arena sits in a rift between dimensions, right? So... I can shift your timeline just a little. To one where the bosses hit differently. \n\nBy the way, the unadjusted timeline is '★Harsh Reality.' It's not 'Vanilla,' so don't mix them up",
        "这个斗技场在时空裂缝里对吧？所以呢……我可以稍微偏移一下世界线。偏移到Boss凶暴程度不同的世界线。\n\n顺便说一下，未调整的世界线是「★残酷现实」哦。不是「原版」，别搞混了",
        actor=trainer,
    ).jump(boss_damage_check)

    # 現在の設定を判定して表示
    builder.step(boss_damage_check).branch_if(
        Keys.BOSS_DAMAGE_RATE, "==", 200, boss_damage_current_hard
    ).branch_if(
        Keys.BOSS_DAMAGE_RATE, "==", 100, boss_damage_current_vanilla
    ).branch_if(Keys.BOSS_DAMAGE_RATE, "==", 25, boss_damage_current_easy).branch_if(
        Keys.BOSS_DAMAGE_RATE, "==", 5, boss_damage_current_story
    ).branch_if(Keys.BOSS_DAMAGE_RATE, "==", 40, boss_damage_current_normal).jump(
        boss_damage_select
    )

    builder.step(boss_damage_current_hard).say(
        "boss_damage_now_200",
        "今は阿修羅の世界線ね。……変える？",
        "You're in the Asura timeline right now. ...Want to change?",
        "你现在在阿修罗世界线。……要换吗？",
        actor=trainer,
    ).jump(boss_damage_select)

    builder.step(boss_damage_current_vanilla).say(
        "boss_damage_now_vanilla",
        "今はバニラの世界線。……変える？",
        "You're in the vanilla timeline. ...Want to change?",
        "你现在在原版世界线。……要换吗？",
        actor=trainer,
    ).jump(boss_damage_select)

    builder.step(boss_damage_current_normal).say(
        "boss_damage_now_normal",
        "今は初期状態の世界線……いつもの闘技場ね。変える？",
        "You're in the default timeline... the arena as it was. Want to change?",
        "现在是初始状态的世界线……你熟悉的斗技场。要换吗？",
        actor=trainer,
    ).jump(boss_damage_select)

    builder.step(boss_damage_current_easy).say(
        "boss_damage_now_65",
        "今はやさしい世界線にいるよ。……変える？",
        "You're in a gentle timeline. ...Want to change?",
        "你现在在温柔的世界线。……要换吗？",
        actor=trainer,
    ).jump(boss_damage_select)

    builder.step(boss_damage_current_story).say(
        "boss_damage_now_25",
        "今はストーリーの世界線ね。……変える？",
        "You're in the story timeline. ...Want to change?",
        "现在是剧情世界线。……要换吗？",
        actor=trainer,
    ).jump(boss_damage_select)

    # 選択肢メニュー
    builder.step(boss_damage_select).say(
        "boss_damage_select_prompt",
        "どの世界線にする？",
        "Which timeline?",
        "选哪个世界线？",
        actor=trainer,
    ).choice(
        boss_damage_hard,
        "200%（阿修羅）",
        "200% (Asura)",
        "200%（阿修罗）",
        text_id="c_boss_dmg_hard",
    ).choice(
        boss_damage_vanilla,
        "100%（バニラ）",
        "100% (Vanilla)",
        "100%（原版）",
        text_id="c_boss_dmg_vanilla",
    ).choice(
        boss_damage_normal,
        "40%（★過酷な現実）",
        "40% (★Harsh Reality)",
        "40%（★残酷现实）",
        text_id="c_boss_dmg_normal",
    ).choice(
        boss_damage_easy,
        "25%（やさしい世界）",
        "25% (Gentle World)",
        "25%（温柔世界）",
        text_id="c_boss_dmg_easy",
    ).choice(
        boss_damage_story,
        "5%（ストーリー）",
        "5% (Story)",
        "5%（剧情）",
        text_id="c_boss_dmg_story",
    ).choice(
        choices,
        "やめておく",
        "Never mind",
        "算了",
        text_id="c_boss_dmg_cancel",
    ).on_cancel(choices)

    builder.step(boss_damage_hard).set_flag(Keys.BOSS_DAMAGE_RATE, 200).say(
        "boss_damage_set_200",
        "……この世界線のボスは容赦ないよ。あたしでもちょっと怖い。……気をつけてね",
        "...The bosses in this timeline show no mercy. Even I find them a bit scary. ...Be careful, okay?",
        "……这条世界线的Boss毫不留情。连我都觉得有点可怕。……小心点啊",
        actor=trainer,
    ).jump(secret_menu)

    builder.step(boss_damage_vanilla).set_flag(Keys.BOSS_DAMAGE_RATE, 100).say(
        "boss_damage_set_vanilla",
        "この世界線はいわゆる「オリジナル」だ。ボスは本来の力で戦う。まあ、あんたなら大丈夫でしょ。たぶんね",
        "This timeline is the 'original.' Bosses fight at full power. Well, you can handle it. ...Probably.",
        "这条世界线是所谓的「原版」。Boss会以本来的力量战斗。不过，你应该没问题吧。……大概",
        actor=trainer,
    ).jump(secret_menu)

    builder.step(boss_damage_normal).set_flag(Keys.BOSS_DAMAGE_RATE, 40).say(
        "boss_damage_set_normal",
        "はい、初期状態に戻したよ。あんたが最初に来た時の闘技場ね",
        "There, back to the default. The arena as it was when you first arrived",
        "好，恢复初始状态了。就是你刚来时的斗技场",
        actor=trainer,
    ).jump(secret_menu)

    builder.step(boss_damage_easy).set_flag(Keys.BOSS_DAMAGE_RATE, 25).say(
        "boss_damage_set_65",
        "この世界線のボスは、少しだけ大人しいよ。……恥ずかしいことじゃないからね？",
        "The bosses in this timeline are a bit more tame. ...Nothing to be embarrassed about, okay?",
        "这条世界线的Boss稍微温和一点。……这不丢人的，好吗？",
        actor=trainer,
    ).jump(secret_menu)

    builder.step(boss_damage_story).set_flag(Keys.BOSS_DAMAGE_RATE, 5).say(
        "boss_damage_set_25",
        "この世界線なら、だいぶ楽に戦えるよ。……物語を楽しんで、自分のペースでいいからね",
        "You'll have a much easier fight in this timeline. ...Just enjoy the story at your own pace, okay?",
        "在这条世界线战斗会轻松很多。……好好享受故事就好，按自己的节奏来",
        actor=trainer,
    ).jump(secret_menu)

    # ========================================
    # 周回処理
    # ========================================
    builder.step(newgame_menu).say(
        "newgame_warn",
        "周回…ね。今までの進行状況は全部消えるけど、本当にいいの？",
        "Start over... huh. All your progress will be erased, are you sure?",
        "重新开始……是吗。所有进度都会被清除，真的可以吗？",
        actor=trainer,
    ).choice(
        newgame_confirm1,
        "いい",
        "Yes",
        "可以",
        text_id="c_newgame_yes",
    ).choice(
        choices,
        "やめておく",
        "Never mind",
        "算了",
        text_id="c_newgame_no",
    ).on_cancel(choices)

    builder.step(newgame_confirm1).say(
        "newgame_confirm",
        "……ホントに？ ランクもクエストも、全部最初からだよ？",
        "...Really? Your rank, quests, everything starts from scratch?",
        "……真的吗？等级和任务，全部从头开始哦？",
        actor=trainer,
    ).choice(
        newgame_confirm2,
        "それでいい",
        "That's fine",
        "没问题",
        text_id="c_newgame_confirm_yes",
    ).choice(
        choices,
        "やっぱりやめる",
        "On second thought, no",
        "还是算了",
        text_id="c_newgame_confirm_no",
    ).on_cancel(choices)

    builder.step(newgame_confirm2).say(
        "newgame_accept",
        "……わかった。じゃあ、目をつぶって。記憶を巻き戻すから",
        "...Alright. Close your eyes then. I'll rewind your memories",
        "……好吧。那么，闭上眼睛。我来回溯你的记忆",
        actor=trainer,
    ).jump(newgame_execute)

    builder.step(newgame_execute).action("modInvoke", param="start_newgame()").finish()

    # ========================================
    # アンインストール処理
    # ========================================
    builder.step(uninstall_menu).say(
        "uninstall_warn",
        "アンインストール…ね。進行状況は全部消えるよ。それでもいいの？",
        "Uninstall... huh. All your progress will be erased. Is that okay?",
        "卸载……是吗。所有进度都会被清除。这样也可以吗？",
        actor=trainer,
    ).choice(
        uninstall_confirm1,
        "それでいい",
        "That's fine",
        "没问题",
        text_id="c_uninstall_menu_yes",
    ).choice(
        choices,
        "やめておく",
        "Never mind",
        "算了",
        text_id="c_uninstall_menu_no",
    ).on_cancel(choices)

    builder.step(uninstall_confirm1).say(
        "uninstall_really",
        "……ホントの本当に？",
        "...Really really?",
        "……真的真的吗？",
        actor=trainer,
    ).choice(
        uninstall_confirm2,
        "本当に",
        "Really",
        "真的",
        text_id="c_uninstall_really_yes",
    ).choice(
        choices,
        "やっぱりやめる",
        "On second thought, no",
        "还是算了",
        text_id="c_uninstall_really_no",
    ).on_cancel(choices)

    builder.step(uninstall_confirm2).say(
        "uninstall_reconsider",
        "考え直さない？まだやれるよ！",
        "Won't you reconsider? You can still do it!",
        "不再考虑一下吗？你还能行的！",
        actor=trainer,
    ).choice(
        uninstall_confirm3,
        "考えは変わらない",
        "My decision won't change",
        "我的决定不会变",
        text_id="c_uninstall_final_yes",
    ).choice(
        choices,
        "…そうだな、もう少し頑張ってみる",
        "...You're right, I'll try a bit more",
        "……说得对，我再努力一下",
        text_id="c_uninstall_final_no",
    ).on_cancel(choices)

    builder.step(uninstall_confirm3).say(
        "uninstall_accept",
        "……決心は揺るがないみたいだね",
        "...Your decision seems firm",
        "……看来你的决心不会动摇呢",
        actor=trainer,
    ).jump(uninstall_skit)

    # 小ネタ
    builder.step(uninstall_skit).say(
        "uninstall_null",
        "話は聞かせてもらった！！ 我々は滅亡する！！",
        "I heard everything!! We are doomed!!",
        "我都听到了！！我们要灭亡了！！",
        actor=null,
    ).say(
        "uninstall_astaroth",
        "な、なんだってーー！！",
        "Wh-whaaaat?!",
        "什、什么--！！",
        actor=astaroth,
    ).jump(uninstall_final)

    # 最終確認
    builder.step(uninstall_final).say(
        "uninstall_final_ask",
        "……バレちゃったみたい。どうする？",
        "...So, what will you do?",
        "……怎么办？",
        actor=trainer,
    ).choice(
        uninstall_execute,
        "やっぱりアンインストールする",
        "I'll still uninstall",
        "还是要卸载",
        text_id="c_uninstall_skit_yes",
    ).choice(
        choices,
        "やっぱりやめる",
        "On second thought, no",
        "还是算了",
        text_id="c_uninstall_skit_no",
    ).on_cancel(choices)

    builder.step(uninstall_execute).say(
        "uninstall_farewell",
        "……では、処理を始めるね。目をつぶって…",
        "...Alright, I'll start the process. Close your eyes...",
        "……那么，我开始处理了。闭上眼睛……",
        actor=trainer,
    ).action("modInvoke", param="start_uninstall()").finish()

    # ========================================
    # 終了
    # ========================================
    builder.step(end).finish()
