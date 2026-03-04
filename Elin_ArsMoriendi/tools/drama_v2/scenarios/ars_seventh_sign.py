from drama.data import DramaIds
from drama_v2 import ArsModCommands, DramaDsl, compile_xlsx, save_xlsx


def save_seventh_sign_xlsx(path: str) -> None:
    d = DramaDsl(mod_name="ArsMoriendi")
    r = ArsModCommands()
    narrator = d.chara('narrator')

    d.node(
        'main',
        *d.seq(
            r.quest_check('ars_seventh_sign', 'chitsii.ars.tmp.can_start.ars_seventh_sign'),
            d.fade_out(1.0, color='black'),
            d.fade_in(1.0, color='black'),
            d.play_bgm('BGM/ManuscriptByCandlelight'),
            d.wait(0.5),
            d.lines([('禁書を開いた瞬間、気づいた。頁が増えている。', 'The moment you open the tome, you notice. There are more pages.', '翻开禁书的瞬间，你察觉了。书页增多了。'), ('新しい頁には文字がない。だが...白紙ではない。インクが、頁の奥から滲み出している。', 'The new pages have no writing. But -- they are not blank. Ink seeps from within the pages.', '新的书页上没有文字。然而……并非空白。墨迹正从书页深处渗出。'), ('これは自分が書いた文字ではない。先代の文字でもない。\n……まだ存在しない誰かの文字だ。', "These are not characters you wrote. Nor the predecessor's. \n...They are the characters of someone who does not yet exist.", '这不是你写的字。也不是先代的。\n……是某个尚未存在之人的字迹。'), ('禁書が次の読者を待っている。次の継承者を。', 'The tome awaits its next reader. Its next successor.', '禁书在等待下一个读者。下一个继承者。')], actor=narrator),
            d.shake(),
            d.wait(0.3),
            d.lines([('六代目として記した言葉が、頁に定着している。消そうとしても消えない。試した。何度も。', 'The words you inscribed as the sixth have settled into the pages. You tried to erase them. Many times.', '你以第六代之名写下的文字，已深深定着于书页之上。试过擦去。试了很多次。'), ('先代の言葉もそうだった。四代目の言葉も。三代目も。二代目も。初代も。...消えない。消せない。', "The predecessor's words were the same. The fourth's too. The third. The second. The first. -- They don't disappear. They can't be erased.", '先代的话语亦然。第四代的也是。第三代。第二代。初代。……不会消失。无法抹去。'), ('連鎖は止まらない。自分が禁書を破壊しても、視座は残る。自分の中に。自分の行動の中に。', 'The chain will not stop. Even if you destroy the tome, the perspective remains. Within you. Within your actions.', '连锁不会停止。即使你毁掉禁书，视角依然留存。在你之中。在你的行为之中。'), ('そして、いつか...自分の視座を受け取る者が現れる。禁書がなくても。言葉がなくても。', 'And someday -- someone will receive your perspective. Even without the tome. Even without words.', '终有一日……会有人接受你的视角。即使没有禁书。即使没有言语。')], actor=narrator),
            d.wait(0.5),
            d.lines([('……これは呪いだろうか。それとも遺産だろうか。', '...Is this a curse? Or a legacy?', '……这是诅咒？还是遗产？'), ('初代は何を思ってこの連鎖を始めたのだろう。後悔しているだろうか。\nそれとも...満足しているだろうか。', 'What did the first think when they started this chain? Do they regret it? \nOr -- are they satisfied?', '初代怀着怎样的心情开启了这条连锁？是否后悔？\n还是……感到满足？'), ('禁書の最後の頁に、一行だけ浮かび上がった。自分の字ではない。\n先代の字でもない。もっと古い...初代の字かもしれない。', "On the tome's last page, a single line surfaces. Not your handwriting. \nNot the predecessor's either. Older -- perhaps the first's.", '禁书的最后一页上，浮现了一行字。不是你的笔迹。\n也不是先代的。更为古老……或许是初代的。'), ('「七代目は、問いを持つ者であれ」...それだけだった。\n意味はわからない。だが、禁書が震えた。嬉しそうに。', '"Let the seventh be one who carries questions" -- that was all. You don\'t understand the meaning. But the tome trembled. As if pleased.', '「愿第七代，是持有疑问之人」……仅此而已。\n不明白其意。然而禁书震颤了。似乎很欣慰。')], actor=narrator),
            d.set_flag('chitsii.ars.tmp.can_start.ars_seventh_sign', 0, actor=None),
            d.fade_out(1.0, color='black'),
            d.end(),
        ),
    )

    spec = d.story(start="main")
    workbook = compile_xlsx(spec, strict=False)
    save_xlsx(workbook, path, sheet=DramaIds.SEVENTH_SIGN)
