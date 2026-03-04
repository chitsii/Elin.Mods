from drama.data import DramaIds
from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx


def save_scout_ambush_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")
    r = ArsModCommands()
    narrator = d.chara('narrator')

    d.node(
        'main',
        *d.seq(
            r.quest_check('ars_scout_ambush', 'chitsii.ars.tmp.can_start.ars_scout_ambush'),
            d.play_bgm('BGM/AshAndHolyLance'),
            d.lines([('……また、気配がする。\n\n騎士団の偵察兵が追跡を続けている。', "...That presence again.\n\nThe order's scouts continue their pursuit.", '……又是那股气息。\n\n骑士团的侦察兵仍在追踪。')], actor=narrator),
            d.set_flag('chitsii.ars.tmp.can_start.ars_scout_ambush', 0, actor=None),
            d.end(),
        ),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=False)
    save_xlsx(workbook, path, sheet=DramaIds.SCOUT_AMBUSH)
