from drama.data import DramaIds
from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx


def save_servant_lost_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")
    r = ArsModCommands()
    narrator = d.chara('narrator')

    d.node(
        'main',
        *d.seq(
            r.quest_check('ars_servant_lost', 'chitsii.ars.tmp.can_start.ars_servant_lost'),
            d.fade_out(0.3, color='black'),
            d.fade_in(0.3, color='black'),
            d.play_bgm_with_fallback('BGM/Emotional_Sorrow', 'BGM/TheFadingSignature'),
            d.wait(0.3),
            d.switch_on_flag('chitsii.ars.drama.soulbind_sacrifice', cases=[('==', 1, 'soulbind_start')], actor='pc'),
            d.lines([('従者の名が禁書から消えていく。インクが乾くように、ゆっくりと。', "The servant's name fades from the tome. Slowly, like ink drying.", '仆从的名字从禁书上消退。如墨迹干涸般，缓缓地。'), ('魂が器から離れる。目から光が消え、身体が崩れ落ちる。もう...ただの死体だ。', 'The soul leaves the vessel. Light fades from the eyes, the body crumbles. Now -- just a corpse.', '灵魂脱离容器。双目失去光泽，躯体轰然倒塌。如今……不过是具尸体。')], actor=narrator),
            d.go('common'),
        ),
    )

    d.node(
        'soulbind_start',
        *d.seq(
            d.lines([('身代わりとなった従者の名が禁書から消える。魂が燃え尽きるように、一瞬で。', 'The name of the servant who took your place vanishes from the tome. The soul burns out in an instant.', '替身仆从的名字从禁书上消失。灵魂如燃尽般，转瞬即逝。'), ('従者は最後まで命令に従った。……感謝すべきだろうか。それとも...いや。考えるな。', "The servant obeyed to the very end. ...Should you be grateful? Or -- no. Don't think about it.", '仆从至死服从了命令。……应该感激吗？还是……不。别想了。')], actor=narrator),
            d.go('common'),
        ),
    )

    d.node(
        'common',
        *d.seq(
            d.wait(0.5),
            d.lines([('……名前があったことすら、やがて忘れるのだろう。\n禁書は覚えているかもしれない。だが、教えてはくれない。', "...Eventually, you'll forget there was even a name. \nThe tome may remember. But it won't tell you.", '……终有一天，连曾有名字这件事都会忘却。\n禁书或许还记得。但它不会告诉你。'), ('エレノスは従者を何人失っただろう。カレンの手帳には「少なくとも十二人」と書かれていた。', "How many servants did Erenos lose? Karen's journal says 'at least twelve.'", '艾雷诺斯失去过多少仆从？卡伦的手账上写着「至少十二人」。'), ('十二人。それぞれに名前があった。それぞれに...生前があった。', 'Twelve. Each had a name. Each had -- a life before.', '十二人。每一个都有名字。每一个都有……生前。'), ('……道具か、仲間か。その問いに答えを出す必要はない。結果だけが残る。\n...エレノスの口癖だ。いつの間にか、自分の口癖になっていた。', "...Tool or companion. You need not answer that question. Only results remain. \n-- Erenos's favorite phrase. Somehow, it became yours.", '……道具，还是同伴？无须回答这个问题。只有结果留存。\n……这是艾雷诺斯的口头禅。不知何时，也成了你的口头禅。')], actor=narrator),
            d.set_flag('chitsii.ars.tmp.can_start.ars_servant_lost', 0, actor=None),
            d.fade_out(0.5, color='black'),
            d.end(),
        ),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=False)
    save_xlsx(workbook, path, sheet=DramaIds.SERVANT_LOST)
