# -*- coding: utf-8 -*-
"""
Chapter-4 truth conversation: floor map.

Chapter 4 "真相会話: 床の地図" (story/chapters/chapter-04.md).
The floor "signal diagram" is just Yuu's abandoned map of the layer.
Sora's reasoning "same brushwork -> same intelligence" was correct;
it just turned out the intelligence was Yuu.

Sets chitsii.elinikki.quest.event.truth_map = 1 on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_truth_map(builder: DramaBuilder) -> None:
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
                "truth_map_yuu_exit",
                "ユウ:「出口探すために地図描いてた。途中で奥行きすぎてわけ分かんなくなってやめた」",
                "Yuu: 'I was mapping the way out. Got too deep, lost track, gave up.'",
                "",
                yuu,
            ),
            (
                "truth_map_sora_brush",
                "ソラ:「壁の記号と同じ筆致だから、同じ存在が描いたと……」",
                "Sora: 'Same touch as the wall symbols. So I assumed the same entity drew both...'",
                "",
                sora,
            ),
            (
                "truth_map_yuu_same",
                "ユウ:「同じ存在だよ。俺だ。壁の正の字も地図も俺」",
                "Yuu: 'Same entity. Me. The tallies, the map, all me.'",
                "",
                yuu,
            ),
            (
                "truth_map_sora_silent",
                "ソラ:「……」",
                "Sora: '...'",
                "",
                sora,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRUTH_MAP, 1)
    builder.drama_end(0.3)
