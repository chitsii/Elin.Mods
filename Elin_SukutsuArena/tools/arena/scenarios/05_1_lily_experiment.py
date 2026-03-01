"""
05_1_lily_experiment.md - リリィの私的依頼『残響の器』
材料提供型クエスト: 骨を集めてリリィに渡す
"""

from arena.builders import DramaBuilder
from arena.data import Keys, Actors, QuestIds


def define_lily_experiment(builder: DramaBuilder):
    """
    リリィの私的依頼「残響の器」
    シナリオ: 05_1_lily_experiment.md

    変更: 製作クエスト → 材料提供クエスト
    必要材料: 骨 (bone) x1
    報酬: プラチナコイン20枚
    """
    # アクター登録
    pc = builder.register_actor(Actors.PC, "あなた", "You", "你")
    lily = builder.register_actor(Actors.LILY, "リリィ", "Lily", "莉莉")
    narrator = Actors.NARRATOR

    # ラベル定義
    main = builder.label("main")
    scene1 = builder.label("scene1_request")
    choice1 = builder.label("choice1")
    react1_what = builder.label("react1_what")
    react1_reward = builder.label("react1_reward")
    react1_help = builder.label("react1_help")
    scene2 = builder.label("scene2_requirements")
    choice2 = builder.label("choice2")
    react2_accept = builder.label("react2_accept")
    react2_bone = builder.label("react2_bone")
    react2_silent = builder.label("react2_silent")
    check_materials = builder.label("check_materials")
    has_material = builder.label("has_material")
    no_material = builder.label("no_material")
    scene4 = builder.label("scene4_delivery")
    ending = builder.label("ending")

    # ========================================
    # シーン1: 受付嬢の不機嫌な午後
    # ========================================
    builder.step(main).play_bgm("BGM/Lobby_Normal").focus_chara(Actors.LILY).say(
        "narr_1",
        "ロビーは相変わらず、異次元の歪みが軋む不快な音に満ちている。\n\nリリィは眉間に皺を寄せ、羽根ペンを乱暴に机に置いた。その前には、どこか禍々しい幾何学模様が描かれた、古ぼけた設計図が広げられている。",
        "The lobby is filled with the same unpleasant sounds of dimensional distortions creaking.\n\nLily furrows her brow and roughly sets down her quill pen. Before her lies an aged blueprint covered in sinister geometric patterns.",
        "大厅里依旧充斥着异次元扭曲发出的刺耳声响。\n\n莉莉紧锁眉头，粗暴地将羽毛笔放在桌上。她面前摊开着一张陈旧的设计图，上面绘有某种不祥的几何图案。",
        actor=narrator,
    ).say(
        "lily_1",
        "……あぁ、忌々しい。\nこのロビーの『雑音』のせいで、私の大切な研究がちっとも進みません。戦士たちの絶望、観客の哄笑、次元の摩擦音……これらは本来、純粋な『魔力の残滓』として回収されるべきものなのに。",
        "...Oh, how vexing.\nThis lobby's 'noise' prevents my precious research from progressing at all. The despair of warriors, the roaring laughter of spectators, the friction of dimensions... These should all be collected as pure 'magical residue.'",
        "……啊，真是烦人。\n这大厅里的『杂音』害得我珍贵的研究毫无进展呢。战士们的绝望、观众的哄笑、次元的摩擦声……这些本应作为纯粹的『魔力残渣』被回收才对。",
        actor=lily,
    ).say(
        "narr_3",
        "彼女はあなたを見つけ、わずかに目を細める。",
        "She spots you and narrows her eyes slightly.",
        "她发现了你，微微眯起眼睛。",
        actor=narrator,
    ).say(
        "lily_3",
        "ねえ、そこの『泥犬』さん。少しは私に牙以外の役に立つところを見せてくださる？\n\n私の研究のために、特別な『実験用具』が必要なのです。……材料さえあれば、私が作れるのですが。",
        "Oh my, you there, 'Mud Dog.' Would you care to show me you're useful for something other than biting?\n\nI require special 'experimental apparatus' for my research. ...I can craft it myself, if only I had the materials.",
        "呵呵，那边的『泥狗』先生。您能否展示一下除了獠牙之外还有什么用处呢？\n\n我的研究需要特殊的『实验器具』呢。……只要有材料，我自己就能制作。",
        actor=lily,
    )

    # プレイヤーの選択肢
    builder.choice(
        react1_what, "どんな材料が必要なんだ？", "What kind of materials do you need?", text_id="c1_what", text_cn="需要什么材料？"
    ).choice(
        react1_reward, "報酬次第だな", "Depends on the reward.", text_id="c1_reward", text_cn="看报酬如何。"
    ).choice(
        react1_help, "手伝おう", "I'll help.", text_id="c1_help", text_cn="我来帮忙。"
    )

    # 選択肢反応
    builder.step(react1_what).say(
        "lily_r1",
        "ふふ、興味を持っていただけましたか。では、詳しくご説明いたしましょう。",
        "Hehe, you're interested? Then allow me to explain in detail.",
        "呵呵，您感兴趣了吗？那么，请让我详细说明吧。",
        actor=lily,
    ).jump(scene2)

    builder.step(react1_reward).say(
        "lily_r2",
        "まあ、現実的ですこと。ええ、もちろん報酬はお支払いいたしますよ。",
        "My my, how pragmatic. Yes, of course I shall compensate you.",
        "哎呀，真是现实呢。是的，报酬当然会支付的。",
        actor=lily,
    ).jump(scene2)

    builder.step(react1_help).say(
        "lily_r3",
        "あら、素直ですこと。では、お願いいたしますね。",
        "Oh my, how obedient. Then I shall leave it to you.",
        "哎呀，真是乖巧呢。那么，就拜托您了。",
        actor=lily,
    ).jump(scene2)

    # ========================================
    # シーン2: 材料の説明
    # ========================================
    builder.step(scene2).play_bgm("BGM/Mystical_Ritual").say(
        "narr_4",
        "リリィは細長い指先で設計図の一点を鋭く指し示した。そこには、内部に複雑な空洞を持つ、特殊な瓶のような構造が描かれている。",
        "Lily points sharply at a spot on the blueprint with her slender fingers. There, a peculiar bottle-like structure with intricate hollow chambers is depicted.",
        "莉莉用纤细的指尖锐利地指向设计图上的一点。那里绘有一个内部带有复杂空腔的特殊瓶状结构。",
        actor=narrator,
    ).say(
        "lily_5",
        "必要なのは『死の共鳴瓶』。この器があれば、アリーナに満ちる雑音を……純粋な魔力として回収できるのです。\n器の製作は私が行います。あなたには、材料を集めてきてほしいのです。",
        "What I need is a 'Resonance Vessel of Death.' With this vessel, I can collect the noise filling the arena... as pure magical essence.\nI shall craft the vessel myself. I need you to gather the materials for me.",
        "我需要的是『死亡共鸣瓶』。有了这个器皿，就能将充斥角斗场的杂音……作为纯粹的魔力回收呢。\n器皿的制作由我来完成。我需要您去收集材料。",
        actor=lily,
    ).say(
        "lily_7",
        "必要なのは『骨』。強大な生き物の骨は、魂の入れ物として使えます……それが、器の核となるのです。\n闘技場で倒した敵から手に入るでしょう。……一つで十分ですよ。",
        "What I require is 'bone.' The bones of mighty creatures serve as receptacles for souls... They shall become the core of the vessel.\nYou should be able to obtain them from enemies you defeat in the arena. ...One will suffice.",
        "需要的是『骨头』。强大生物的骨头可以作为灵魂的容器……那将成为器皿的核心呢。\n在角斗场击败的敌人那里应该能得到。……一个就足够了。",
        actor=lily,
    )

    # プレイヤーの選択肢
    builder.choice(
        react2_accept, "任せてくれ", "Leave it to me.", text_id="c2_accept", text_cn="交给我吧。"
    ).choice(
        react2_bone, "骨……どこで手に入る？", "Bone... Where do I get it?", text_id="c2_bone", text_cn="骨头……在哪能弄到？"
    ).choice(
        react2_silent, "（無言で頷く）", "(Nod silently)", text_id="c2_silent", text_cn="（默默点头）"
    )

    # 選択肢反応
    builder.step(react2_accept).say(
        "lily_r4",
        "ふふ、頼もしいこと。では、お待ちしておりますね。",
        "Hehe, how dependable. I shall await your return.",
        "呵呵，真是可靠呢。那么，我就等着您了。",
        actor=lily,
    ).jump(check_materials)

    builder.step(react2_bone).say(
        "lily_r5",
        "闘技場の敵を倒せば手に入りますよ。骨を持った生き物ならどれでも構いません。",
        "You can obtain them by defeating enemies in the arena. Any creature with bones will do.",
        "在角斗场击败敌人就能得到呢。只要是有骨头的生物都可以。",
        actor=lily,
    ).jump(check_materials)

    builder.step(react2_silent).say(
        "lily_r6",
        "……無口ですが、仕事はきっちりこなしてくださるのでしょう？",
        "...Not one for words, but you'll get the job done properly, won't you?",
        "……虽然沉默寡言，但您会好好完成工作的吧？",
        actor=lily,
    ).jump(check_materials)

    # ========================================
    # シーン3: 材料チェック（選択肢ベース）
    # ========================================
    builder.step(check_materials).say(
        "lily_check",
        "……さて、骨はお持ちですか？",
        "...Now then, do you have the bone?",
        "……那么，骨头带来了吗？",
        actor=lily,
    )

    # 条件付き選択肢: 骨を持っている場合のみ「渡す」が表示される
    builder.choice_if(
        has_material, "骨を渡す", "hasItem,bone", text_en="Hand over the bone", text_id="c_give_bone", text_cn="交出骨头"
    ).choice(no_material, "まだ持っていない", "Not yet.", text_id="c_no_bone", text_cn="还没有。")

    # 材料あり → 消費して納品へ
    builder.step(has_material).cs_eval(
        'var mat = EClass.pc.things.Find(t => t.id == "bone"); if(mat != null) mat.Destroy();'
    ).say(
        "lily_take",
        "……ありがとうございます。優秀ですこと。",
        "...Thank you. How delightfully efficient.",
        "……谢谢您。真是优秀呢。",
        actor=lily,
    ).jump(scene4)

    # 材料なし → 会話終了（再度話しかけで再試行可能）
    builder.step(no_material).say(
        "lily_no_mat",
        "そうですか。骨がまだ揃っていないのですね。\n闘技場で戦えば手に入りますよ。……探してきてくださいな。",
        "I see. You haven't gathered the bone yet.\nYou can obtain it by fighting in the arena. ...Do go find one for me.",
        "是吗。骨头还没收集到呢。\n在角斗场战斗就能得到呢。……请去找来吧。",
        actor=lily,
    ).finish()

    # ========================================
    # シーン4: 器の製作と報酬
    # ========================================
    builder.step(scene4).play_bgm("BGM/Lily_Tranquil").say(
        "narr_5",
        "リリィは骨を受け取ると、それを机の上に置いた。\n彼女は骨に指先を当て、何やら呪文を唱え始める。骨が淡く光り、徐々にその形を変えていく。",
        "Lily accepts the bone and places it on her desk.\nShe places her fingertips on the bone and begins chanting an incantation. The bone glows faintly and gradually transforms.",
        "莉莉接过骨头，将它放在桌上。\n她将指尖放在骨头上，开始吟唱某种咒语。骨头泛起微光，逐渐改变形态。",
        actor=narrator,
    ).say(
        "narr_7",
        "数分後、そこには精緻な模様が刻まれた、小さな器が現れた。",
        "Several minutes later, a small vessel etched with intricate patterns appears.",
        "数分钟后，一个刻有精致纹样的小器皿出现了。",
        actor=narrator,
    ).say(
        "lily_11",
        "……完成です。",
        "...It is complete.",
        "……完成了。",
        actor=lily,
    ).say(
        "narr_8",
        "不意に、彼女の尻尾が満足げに揺れた。",
        "Her tail sways with satisfaction.",
        "她的尾巴不经意间满足地摇曳起来。",
        actor=narrator,
    ).say(
        "lily_12",
        "ふふ、良い素材でした。これなら、このアリーナに満ちる『死の残響』を存分に吸い取ってくれるでしょう。",
        "Hehe, such delicious material. This vessel shall devour the 'echoes of death' that fill this arena.",
        "呵呵，真是上好的素材呢。这样的话，就能尽情吸收充斥这角斗场的『死亡残响』了吧。",
        actor=lily,
    ).say(
        "narr_9",
        "彼女は器を丁寧に棚に置き、台帳を開く。",
        "She carefully places the vessel on a shelf and opens her ledger.",
        "她小心翼翼地将器皿放在架子上，翻开账本。",
        actor=narrator,
    ).say(
        "lily_13",
        "これはお礼です。",
        "This is your reward.",
        "这是谢礼。",
        actor=lily,
    ).cs_eval(
        'for(int i=0; i<20; i++) { EClass.pc.Pick(ThingGen.Create("plat")); }'
    ).say(
        "lily_14",
        "（プラチナコインを20枚もらった）",
        "(Received 20 platinum coins)",
        "（获得了20枚白金币）",
        actor=lily,
    ).say(
        "lily_15",
        "あなたの協力に感謝します。ふふ、あなたは暴力だけではないのですね。\n\nこれからも、何か特別な依頼があれば、あなたにお願いするかもしれません。……期待していますよ。",
        "I appreciate your cooperation. Hehe, it seems you're good for more than just violence.\n\nIf I have any special requests in the future, I may call upon you again. ...I look forward to it.",
        "感谢您的协助。呵呵，您不只是会使用暴力呢。\n\n今后如果有什么特别的委托，可能还会拜托您呢。……期待您的表现。",
        actor=lily,
    ).jump(ending)

    # ========================================
    # 終了処理
    # ========================================
    builder.step(ending).complete_quest(QuestIds.LILY_EXPERIMENT).finish()
