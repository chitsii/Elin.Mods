from drama.data import DramaIds
from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx


def save_servant_rampage_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")
    r = ArsModCommands()
    narrator = d.chara('narrator')

    d.node(
        'main',
        *d.seq(
            r.quest_check('ars_servant_rampage', 'chitsii.ars.tmp.can_start.ars_servant_rampage'),
            d.shake(),
            d.play_bgm('BGM/AshAndHolyLance'),
            d.lines([('従者の体が痙攣する。突然に。警告もなく。', "The servant's body convulses. Suddenly. Without warning.", '仆从的身体痉挛起来。毫无预兆。毫无征兆。'), ('魂が器を拒絶している...あるいは、器が魂を超えようとしている。\nどちらにせよ、制御が外れた。', 'The soul rejects the vessel -- or the vessel tries to exceed the soul. \nEither way, control is lost.', '灵魂在排斥容器……或者说，容器试图超越灵魂。\n无论如何，控制已经脱离。'), ('禁書が脈打っている。警告か。……いや、観察だ。禁書は警告しない。記録するだけだ。', "The tome pulses. A warning? ...No, observation. The tome doesn't warn. It only records.", '禁书在搏动。警告吗？……不，是观察。禁书不会警告。它只做记录。'), ('...結果が出る。', '-- The result manifests.', '……结果显现了。')], actor=narrator),
            d.switch_on_flag('chitsii.ars.drama.rampage_type', cases=[('==', 1, 'dark'), ('==', 2, 'berserk'), ('==', 3, 'destruct'), ('==', 4, 'mutate'), ('==', 0, 'dark')], actor='pc'),
        ),
    )

    d.node(
        'dark',
        *d.seq(
            d.lines([('……闇の中から、別の何かが目覚めた。制御を超えた力が、従者に宿る。', '...From the darkness, something else awakens. Power beyond control settles into the servant.', '……从黑暗之中，另一种存在觉醒了。超越控制的力量寄宿于仆从体内。'), ('従者の目に知性が灯った。だが、それは...以前の主の知性ではない。', "Intelligence lights in the servant's eyes. But it is -- not the former master's intelligence.", '仆从眼中点燃了智慧之光。然而那——并非旧主的智慧。')], actor=narrator),
            d.go('done'),
        ),
    )

    d.node(
        'berserk',
        *d.seq(
            d.lines([('理性が消えた。だが、肉体は覚えている。殺すための動きを。', 'Reason is gone. But the body remembers. How to kill.', '理性消失了。然而身体还记得。杀戮的方式。'), ('……これが代償だ。強化には限界がある。限界を超えた先にあるのは、破壊だけだ。', '...This is the price. Enhancement has limits. Beyond those limits, only destruction.', '……这就是代价。强化有其极限。超越极限之后，只剩毁灭。')], actor=narrator),
            d.go('done'),
        ),
    )

    d.node(
        'destruct',
        *d.seq(
            d.lines([('器が砕けた。魂が爆ぜ、周囲を巻き込む。', 'The vessel shatters. The soul detonates, engulfing the surroundings.', '容器碎裂了。灵魂爆裂，波及周遭。'), ('禁書から、従者の名が消えていく。インクが乾くように。...痛みはない。禁書にとっては。', "The servant's name fades from the tome. Like ink drying. -- There is no pain. Not for the tome.", '禁书上，仆从的名字正在消退。如墨迹干涸。……没有痛感。对禁书而言。')], actor=narrator),
            d.go('done'),
        ),
    )

    d.node(
        'mutate',
        *d.seq(
            d.lines([('骨が伸びる。肉が裂ける。新しい部位が這い出す。', 'Bones stretch. Flesh tears. New appendages crawl forth.', '骨骼伸长。血肉撕裂。新的肢体蠕动而出。'), ('……進化か、崩壊か。禁書はこれを「適応」と記録している。\n人間なら「怪物」と呼ぶだろう。', '...Evolution or collapse. The tome records this as "adaptation." A human would call it "monster."', '……进化，还是崩溃？禁书将此记录为「适应」。\n人类大概会称之为「怪物」吧。')], actor=narrator),
            d.go('done'),
        ),
    )

    d.node(
        'done',
        *d.seq(
            d.set_flag('chitsii.ars.tmp.can_start.ars_servant_rampage', 0, actor=None),
            d.fade_out(0.3, color='black'),
            d.end(),
        ),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=False)
    save_xlsx(workbook, path, sheet=DramaIds.SERVANT_RAMPAGE)
