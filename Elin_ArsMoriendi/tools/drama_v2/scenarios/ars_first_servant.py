from drama.data import DramaIds
from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx


def save_first_servant_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")
    r = ArsModCommands()
    narrator = d.chara('narrator')

    d.node(
        'main',
        *d.seq(
            r.quest_check('ars_first_servant', 'chitsii.ars.tmp.can_start.ars_first_servant'),
            d.fade_out(0.5, color='black'),
            d.fade_in(0.5, color='black'),
            d.play_bgm('BGM/ManuscriptByCandlelight'),
            d.wait(0.3),
            d.lines([('死体に魂が宿る。指先が痙攣する。目が...開いた。', 'A soul settles into the corpse. Fingers twitch. Eyes -- open.', '灵魂注入尸体。指尖痉挛。双目……睁开了。'), ('従者が立ち上がる。ぎこちなく、だが確実に。汝の意志に従って。', 'The servant rises. Awkwardly, but surely. Obeying your will.', '仆从站了起来。笨拙，却确实地。遵从你的意志。'), ('……できてしまった。禁書に書かれていた通りに。書かれていた通りに、だ。', '...You did it. Exactly as the tome described. Exactly.', '……成了。与禁书所载分毫不差。分毫不差。')], actor=narrator),
            d.shake(),
            d.lines([('禁書の頁に、新しい名前が記されていく。従者の名だ。インクが自動的に広がっていく。', "In the tome's pages, a new name is being inscribed. The servant's name. Ink spreads automatically.", '禁书的书页上，一个新的名字正在被铭刻。是仆从的名字。墨迹自行蔓延开来。'), ('汝が書いたのか。それとも禁書が書いたのか。...その区別に、意味はあるのだろうか。', 'Did you write it? Or did the tome? -- Does the distinction even matter?', '是你写的吗？还是禁书写的？……这种区分，还有意义吗？')], actor=narrator),
            d.wait(0.3),
            d.lines([('エレノスは何人の従者を持っていたのだろう。彼らは今、どこにいるのだろう。', 'How many servants did Erenos have? Where are they now?', '艾雷诺斯曾拥有多少仆从？他们如今身在何处？'), ('……従者の目が、一瞬だけ...何かを宿したように見えた。気のせいだ。気のせいにしておこう。', "...For just a moment, the servant's eyes seemed to hold -- something. Your imagination. Let it be your imagination.", '……仆从的眼中，有一瞬似乎……映出了什么。错觉。就当是错觉吧。')], actor=narrator),
            d.set_flag('chitsii.ars.tmp.can_start.ars_first_servant', 0, actor=None),
            d.fade_out(0.5, color='black'),
            d.end(),
        ),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=False)
    save_xlsx(workbook, path, sheet=DramaIds.FIRST_SERVANT)
