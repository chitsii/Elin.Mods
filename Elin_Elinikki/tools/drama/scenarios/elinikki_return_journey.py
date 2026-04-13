# -*- coding: utf-8 -*-
"""
Chapter-5 return journey drama: four waves of conversation on the walk out.

Chapter 5 sections 1-2 (story/chapters/chapter-05.md). Triggered on
entry to the nefia entrance map after yuu_found. The stage advance to
Returned happens through the zone transition hook, not via this drama.

Four conversation waves, paced as separate labels so a future C# side
can re-enter at a specific wave if needed. For now all four run back
to back in one go.
"""

from tools.drama.data import Actors
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_return_journey(builder: DramaBuilder) -> None:
    mina = builder.register_actor(Actors.MINA, "Mina", "Mina")
    sora = builder.register_actor(Actors.SORA, "Sora", "Sora")
    yuu = builder.register_actor(Actors.YUU, "Yuu", "Yuu")

    start = builder.label("main")
    wave2 = builder.label("wave2")
    wave3 = builder.label("wave3")
    wave4 = builder.label("wave4")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.2)

    # -----------------------------------------------------------------
    # Wave 1 — Yuu's daily life, casual surface
    # -----------------------------------------------------------------
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.conversation(
        [
            (
                "return_yuu_air",
                "ユウ:「外の空気って、こんなだっけ。忘れてた」",
                "Yuu: 'Outside air feels like this, huh. I'd forgotten.'",
                "",
                yuu,
            ),
            (
                "return_yuu_food",
                "ユウ:「一番困ったのは飯の確保だな。キノコは生えてたけど、同じ味ばっかりで」",
                "Yuu: 'Biggest problem was food. Mushrooms grew, but they all tasted the same.'",
                "",
                yuu,
            ),
            (
                "return_yuu_bed",
                "ユウ:「あと寝場所。最初は地面で寝てたけど、石で台作ってからはマシになった」",
                "Yuu: 'Then sleeping. Slept on the ground at first. Got better once I piled stones into a bed.'",
                "",
                yuu,
            ),
            (
                "return_mina_coord",
                "ミナ:「あの石の台も『座標系』だったか？」",
                "Mina: 'Was that stone bed a \"coordinate system\" too?'",
                "",
                mina,
            ),
            (
                "return_sora_stop",
                "ソラ:「……やめてくれ」",
                "Sora: '...Please stop.'",
                "",
                sora,
            ),
        ]
    )
    builder.jump(wave2)

    # -----------------------------------------------------------------
    # Wave 2 — Mina's frustration, Yuu's transparency
    # -----------------------------------------------------------------
    builder.step(wave2)
    builder.conversation(
        [
            (
                "return_mina_no_comms",
                "ミナ:「なんで連絡手段持ってなかったんだ」",
                "Mina: 'Why didn't you carry any way to call out.'",
                "",
                mina,
            ),
            (
                "return_yuu_low_tier",
                "ユウ:「いつもの低難度ネフィアだと思ったんだよ。半日で終わるつもりだった」",
                "Yuu: 'Thought it was a low-tier run. Figured I'd be back by dusk.'",
                "",
                yuu,
            ),
            (
                "return_mina_months",
                "ミナ:「半日で終わるつもりのネフィアに、何ヶ月いたのか」",
                "Mina: 'A half-day run. Months.'",
                "",
                mina,
            ),
            (
                "return_yuu_cant_find",
                "ユウ:「出口が分からなかったんだってば」",
                "Yuu: 'I couldn't find the exit.'",
                "",
                yuu,
            ),
            (
                "return_mina_three_weeks",
                "ミナ:「……お前がいなくなったの、俺が気づいたの、3週間後だ」",
                "Mina: '...I didn't notice you were missing for three weeks.'",
                "",
                mina,
            ),
            (
                "return_yuu_casual",
                "ユウ:「へえ」",
                "Yuu: 'Huh.'",
                "",
                yuu,
            ),
            (
                "return_mina_hey",
                "ミナ:「『へえ』じゃないだろ」",
                "Mina: 'Don't just go \"huh.\"'",
                "",
                mina,
            ),
            (
                "return_yuu_normal",
                "ユウ:「いや、3週間は普通だろ。俺だってミナが3週間連絡こなくても気にしないし」",
                "Yuu: 'Three weeks is normal. I wouldn't notice either if you went quiet for three weeks.'",
                "",
                yuu,
            ),
            (
                "return_mina_silent",
                "ミナ:「……」",
                "Mina: '...'",
                "",
                mina,
            ),
        ]
    )
    builder.jump(wave3)

    # -----------------------------------------------------------------
    # Wave 3 — Sora's silence, the actual content of the crisis
    # -----------------------------------------------------------------
    builder.step(wave3)
    builder.conversation(
        [
            (
                "return_yuu_sora_quiet",
                "ユウ:「ソラ、さっきからずっと黙ってるけど」",
                "Yuu: 'Sora. You've been quiet this whole walk.'",
                "",
                yuu,
            ),
            (
                "return_sora_thinking",
                "ソラ:「……考えてる」",
                "Sora: '...Thinking.'",
                "",
                sora,
            ),
            (
                "return_yuu_what",
                "ユウ:「何を？」",
                "Yuu: 'About what?'",
                "",
                yuu,
            ),
            (
                "return_sora_everything",
                "ソラ:「あの場所で見たものが、全部、お前の生活の跡だったってこと」",
                "Sora: 'That everything we saw down there was a trace of you living in it.'",
                "",
                sora,
            ),
            (
                "return_yuu_yeah",
                "ユウ:「そうだよ」",
                "Yuu: 'Right.'",
                "",
                yuu,
            ),
            (
                "return_sora_but_saw",
                "ソラ:「でも、見たものは見たんだ。あの花は光ってた。あのエコーは返事に聞こえた」",
                "Sora: 'But I still saw what I saw. The flowers glowed. The echo sounded like a reply.'",
                "",
                sora,
            ),
            (
                "return_yuu_agree",
                "ユウ:「うん、俺もそう思ったよ。返事に聞こえた」",
                "Yuu: 'Yeah. Me too. Sounded like a reply.'",
                "",
                yuu,
            ),
            (
                "return_sora_you_closed",
                "ソラ:「お前はそれを『自分の声だ』で片付けたのか」",
                "Sora: 'And you closed the book with \"it's just my own voice.\"'",
                "",
                sora,
            ),
            (
                "return_yuu_maybe",
                "ユウ:「……うん。まあ」",
                "Yuu: '...Yeah. More or less.'",
                "",
                yuu,
            ),
            (
                "return_sora_push",
                "ソラ:「本当にそうか？ お前、長くいすぎて分からなくなってるだけじゃないのか」",
                "Sora: 'Is it really that simple? Or were you just down there long enough to stop being able to tell?'",
                "",
                sora,
            ),
            (
                "return_yuu_shrug",
                "ユウ:「かもな」",
                "Yuu: 'Could be.'",
                "",
                yuu,
            ),
        ]
    )
    builder.jump(wave4)

    # -----------------------------------------------------------------
    # Wave 4 — Mina's reversal seed
    # -----------------------------------------------------------------
    builder.step(wave4)
    builder.conversation(
        [
            (
                "return_mina_just_cave",
                "ミナ:「だから言っただろ。ただの洞窟だ」",
                "Mina: 'Like I said. It was just a cave.'",
                "",
                mina,
            ),
            (
                "return_sora_yeah",
                "ソラ:「……ああ」",
                "Sora: '...Yeah.'",
                "",
                sora,
            ),
            (
                "return_mina_nothing_meant",
                "ミナ:「意味なんてない。記号も石もエコーも花も、全部ただの……」",
                "Mina: 'None of it meant anything. The symbols, the stones, the echo, the flowers—all just...'",
                "",
                mina,
            ),
            (
                "return_mina_stop",
                "ミナ:「……」",
                "Mina: '...'",
                "",
                mina,
            ),
            (
                "return_sora_what",
                "ソラ:「どうした？」",
                "Sora: 'What is it?'",
                "",
                sora,
            ),
            (
                "return_mina_flowers_pretty",
                "ミナ:「花は綺麗だった」",
                "Mina: 'The flowers were beautiful.'",
                "",
                mina,
            ),
            (
                "return_sora_soft",
                "ソラ:「……うん」",
                "Sora: '...Yeah.'",
                "",
                sora,
            ),
            (
                "return_mina_from_trash",
                "ミナ:「ゴミから生えた花が、なんであんなに綺麗なんだ」",
                "Mina: 'Flowers that grew from garbage. Why were they that beautiful.'",
                "",
                mina,
            ),
            (
                "return_sora_ether_explain",
                "ソラ:「……エーテル変異だ。説明はつく」",
                "Sora: '...Ether mutation. There's an explanation.'",
                "",
                sora,
            ),
            (
                "return_mina_experience",
                "ミナ:「説明はつく。でも綺麗だったのは説明じゃない」",
                "Mina: 'An explanation fits. But the beauty isn't the explanation.'",
                "",
                mina,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.drama_end(0.3)
