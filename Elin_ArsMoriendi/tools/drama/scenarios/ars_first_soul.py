# -*- coding: utf-8 -*-
"""Drama 1: ars_first_soul (Stage 0->1: First soul capture)"""

from drama.data import BGM, Actors
from drama.drama_builder import DramaBuilder


def define_first_soul(builder: DramaBuilder):
    narrator = builder.register_actor(Actors.NARRATOR, "禁書", "The Tome", name_cn="禁书")
    main = builder.label("main")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_first_soul"

    builder.step(main)
    builder.quest_check("ars_first_soul", can_start_tmp_flag)
    builder.fade_out(0.8)
    builder.fade_in(0.8)
    builder.play_bgm(BGM.REVELATION)
    builder.wait(0.3)
    builder.conversation(
        [
            (
                "fs_1",
                "あなたが魂を捕えた瞬間、\nてのひらに温もりが残った。\n死者のものだったはずの熱が——\nまだ、脈打っている。",
                "The moment you captured the soul,\nwarmth lingered in your palm.\nHeat that should have belonged to the dead --\nstill pulsing.",
                "你捕获灵魂的那一刻，\n掌心还残留着温热。\n本应属于死者的余温——\n仍在搏动。",
            ),
            (
                "fs_2",
                "鞄の中で、禁書が震えた。\nあなたがこの本を手にしてから、\n初めての反応だった。\nまるで、目を覚ましたかのように。",
                "Inside your bag, the tome trembled.\nThe first reaction\nsince you acquired it.\nAs though it had just awakened.",
                "包中的禁书震颤了。\n自你得到这本书以来，\n这是第一次反应。\n仿佛刚刚苏醒一般。",
            ),
            (
                "fs_3",
                "あなたは禁書を開いた。\n白紙だったはずの頁に、\n黒い文字が滲み出している。\n「第肆章」——その見出しだけが、はっきりと読めた。",
                "You opened the tome.\nOn pages that should have been blank,\nblack characters are seeping through.\n\"Chapter Four\" -- that heading alone was legible.",
                "你翻开了禁书。\n本应空白的书页上，\n黑色文字正在渗出。\n「第肆章」——唯有这标题清晰可辨。",
            ),
        ],
        actor=narrator,
    )
    builder.shake()
    builder.wait(0.3)
    builder.conversation(
        [
            (
                "fs_4",
                "……声が聞こえた。\n頁の奥から。\n遠い、とても遠い場所から。",
                "...You heard a voice.\nFrom deep within the pages.\nFrom a distant, very distant place.",
                "……有声音传来。\n从书页深处。\n从遥远的、极为遥远的地方。",
            ),
            (
                "fs_5",
                "「……よくやった」\nそれは囁きだった。\n禁書の頁に、新しいインクの染みが\n静かに広がっていく。",
                '"...Well done."\nA whisper.\nAcross the tome\'s pages,\nnew stains of ink spread silently.',
                "「……做得好」\n那是一声低语。\n禁书的书页上，新的墨迹\n正悄然蔓延开来。",
            ),
            (
                "fs_6",
                "あなたは禁書を閉じた。\nてのひらには、まだあの温もりが残っている。\n不快ではなかった。むしろ——\n……あなたは、何も考えないことにした。",
                "You closed the tome.\nThe warmth still lingers in your palm.\nIt was not unpleasant. Rather --\n...You chose not to think about it.",
                "你合上了禁书。\n掌心依然残留着那份温热。\n并不觉得不适。倒不如说——\n……你决定什么都不去想。",
            ),
        ],
        actor=narrator,
    )
    builder.set_flag(can_start_tmp_flag, 0)
    builder.drama_end(0.8)
