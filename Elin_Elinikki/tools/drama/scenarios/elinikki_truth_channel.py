# -*- coding: utf-8 -*-
"""
Chapter-4 truth conversation: water channel.

Chapter 4 "真相会話: 水路の溝" (story/chapters/chapter-04.md).
Yuu dug the channel with stones so he wouldn't have to walk to the
water source. Sora's "intelligence manipulating the environment"
theory dies as Yuu points out that humans are also, technically,
intelligences.

Sets chitsii.elinikki.quest.event.truth_channel = 1 on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_truth_channel(builder: DramaBuilder) -> None:
    sora = builder.register_actor(Actors.SORA, "Sora", "Sora")
    yuu = builder.register_actor(Actors.YUU, "Yuu", "Yuu")

    start = builder.label("main")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.2)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.conversation(
        [
            (
                "truth_channel_yuu_water",
                "ユウ:「飲み水。水場まで歩くの面倒だったから、こっちに引いた。石で溝掘って」",
                "Yuu: 'Drinking water. Walking to the spring was annoying, so I cut a channel with stones.'",
                "",
                yuu,
            ),
            (
                "truth_channel_sora_stones",
                "ソラ:「あの直線は石で……？ 自然浸食じゃないと思ったのは正しかったが」",
                "Sora: 'The straight lines—with stones? Well, I was right it wasn't erosion.'",
                "",
                sora,
            ),
            (
                "truth_channel_yuu_hard",
                "ユウ:「うん、俺が掘った。結構大変だった」",
                "Yuu: 'Yeah. I dug it. Pretty hard work, actually.'",
                "",
                yuu,
            ),
            (
                "truth_channel_sora_env",
                "ソラ:「知性体が環境を制御している証拠だと……」",
                "Sora: 'I thought it was proof of an intelligence modifying its environment...'",
                "",
                sora,
            ),
            (
                "truth_channel_yuu_gotcha",
                "ユウ:「まあ、人間も知性体だからな？」",
                "Yuu: 'I mean. Humans count as intelligences. Right?'",
                "",
                yuu,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRUTH_CHANNEL, 1)
    builder.drama_end(0.3)
