from drama.data import DramaIds
from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx


def save_karen_ambush_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")
    r = ArsModCommands()
    ars_karen = d.chara('ars_karen')

    d.node(
        'main',
        *d.seq(
            r.quest_check('ars_karen_ambush', 'chitsii.ars.tmp.can_start.ars_karen_ambush'),
            d.play_bgm('BGM/AshAndHolyLance'),
            d.lines([('……逃がさないわ、死霊術師。', "...You won't escape, necromancer.", '……休想逃走，死灵术士。')], actor=ars_karen),
            d.set_flag('chitsii.ars.tmp.can_start.ars_karen_ambush', 0, actor=None),
            d.end(),
        ),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=False)
    save_xlsx(workbook, path, sheet=DramaIds.KAREN_AMBUSH)
