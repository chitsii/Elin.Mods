from drama.data import BGM, DramaIds
from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx


def save_dormant_flavor_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")
    r = ArsModCommands()
    narrator = d.chara("narrator")
    can_start_tmp_flag = "chitsii.ars.tmp.can_start.ars_dormant_flavor"

    d.node(
        "main",
        r.quest_check("ars_dormant_flavor", can_start_tmp_flag, actor=None),
        d.wait(0.3),
        d.play_bgm(BGM.REVELATION),
        d.line(
            "動かぬ従者たちが並んでいる。整列し、沈黙し、命令を待っている。",
            en="The motionless servants stand in a row. Aligned, silent, awaiting orders.",
            cn="不动的仆从们排成一列。整齐、沉默，等待命令。",
            actor=narrator,
        ),
        d.line(
            "コレクション。...その言葉がどこから来たのか。自分の語彙か、禁書の語彙か。",
            en="Collection. -- Where did that word come from? Your vocabulary? Or the tome's?",
            cn="收藏品。……这个词从何而来？自己的用语？还是禁书的？",
            actor=narrator,
        ),
        d.line(
            "生前、彼らには名前があった。家族があった。夢があったかもしれない。今は...番号がある。",
            en="In life, they had names. Families. Perhaps dreams. Now -- they have numbers.",
            cn="生前，他们有名字。有家人。或许还有梦想。如今……只剩编号。",
            actor=narrator,
        ),
        d.line(
            "禁書が静かに満足している。そう感じる。\n禁書に感情があるのか、それとも自分が禁書に感情を投影しているのか。",
            en="The tome is quietly satisfied. You sense it. \nDoes the tome have emotions, or are you projecting your own onto it?",
            cn="禁书在静静地满足着。你能感知到。\n禁书有感情吗？还是你在将自己的感情投射于它？",
            actor=narrator,
        ),
        d.line(
            "……エレノスも同じ光景を見たのだろうか。同じことを考えたのだろうか。\n同じ結論に達したのだろうか。...やめよう。",
            en="...Did Erenos see the same sight? Think the same thoughts? \nReach the same conclusion? -- Stop.",
            cn="……艾雷诺斯也曾见过同样的光景吗？想过同样的事吗？\n得出过同样的结论吗？……算了。",
            actor=narrator,
        ),
        d.set_flag(can_start_tmp_flag, 0, actor=None),
        d.end(),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=True)
    save_xlsx(workbook, path, sheet=DramaIds.DORMANT_FLAVOR)
