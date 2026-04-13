# -*- coding: utf-8 -*-
"""
Chapter-2 trace examine drama: wall shadow (壁の人影).

Chapter 2 section 8 (story/chapters/chapter-02.md). The echo layer's
burned-on human silhouette. Sora reads it as "ether dissolution" damage;
the scene plants unease about Yuu's fate without confirming anything.

Sets chitsii.elinikki.quest.event.trace_shadow on completion.
"""

from tools.drama.data import Actors, FlagKeys
from tools.drama.drama_builder import DramaBuilder


def define_elinikki_trace_shadow(builder: DramaBuilder) -> None:
    mina = builder.register_actor(Actors.MINA, "Mina", "Mina")
    sora = builder.register_actor(Actors.SORA, "Sora", "Sora")

    start = builder.label("main")
    end = builder.label("end")

    builder.drama_start(bg_id="bg3", fade_duration=0.2)
    builder.step(start)
    builder.set_dialog_style("Window")
    builder.conversation(
        [
            (
                "shadow_sora_burned",
                "ソラ:「人の形だ。壁に焼きついてる。立ったまま消えたみたいに」",
                "Sora: 'A human shape. Burned into the wall. Like someone vanished standing up.'",
                "",
                sora,
            ),
            (
                "shadow_sora_ether",
                "ソラ:「エーテル融解の痕跡だ。高濃度のエーテルに曝露されると、有機物の影が……残る」",
                "Sora: 'Ether dissolution. Enough exposure, organic tissue leaves a shadow like this.'",
                "",
                sora,
            ),
            (
                "shadow_sora_doubt",
                "ソラ:「これがユウの……？ いや、サイズが違う。もっと前からあるものかもしれない」",
                "Sora: 'Is it... Yuu? No. Wrong size. Could be much older.'",
                "",
                sora,
            ),
            (
                "shadow_mina_leave",
                "ミナ:「……こんなところに長くいたくない。先に行くぞ」",
                "Mina: '...Don't want to stand here. Moving on.'",
                "",
                mina,
            ),
        ]
    )
    builder.jump(end)

    builder.step(end)
    builder.set_flag(FlagKeys.ELINIKKI_TRACE_SHADOW, 1)
    builder.drama_end(0.3)
