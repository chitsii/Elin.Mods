from drama.data import DramaIds
from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx


def save_erenos_ambush_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")
    r = ArsModCommands()
    narrator = d.chara('narrator')

    d.node(
        'main',
        *d.seq(
            r.quest_check('ars_erenos_ambush', 'chitsii.ars.tmp.can_start.ars_erenos_ambush'),
            d.play_bgm('BGM/AshAndHolyLance'),
            d.lines([('……影が、追ってきた。\n\nエレノスの残滓が再び形を成す。', '...The shadow has followed you.\n\nThe remnant of Erenos takes form once more.', '……影，追上来了。\n\n艾雷诺斯的残滓再度成形。')], actor=narrator),
            d.set_flag('chitsii.ars.tmp.can_start.ars_erenos_ambush', 0, actor=None),
            d.end(),
        ),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=False)
    save_xlsx(workbook, path, sheet=DramaIds.ERENOS_AMBUSH)
