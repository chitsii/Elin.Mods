"""V5.1 drama builder entrypoint.

This module keeps a familiar `drama_builder` import path while exposing
the V5.1 DSL surface.
"""

from .drama_dsl import (
    Chara,
    Cond,
    DramaDsl,
    Id,
    NodeRef,
    NodeSpec,
    OptionSpec,
    StepSpec,
    StorySpec,
    compile_xlsx,
    save_xlsx,
)

# Backwards-friendly alias for "builder" wording.
DramaBuilder = DramaDsl

__all__ = [
    "Chara",
    "Cond",
    "DramaBuilder",
    "DramaDsl",
    "Id",
    "NodeRef",
    "NodeSpec",
    "OptionSpec",
    "StepSpec",
    "StorySpec",
    "compile_xlsx",
    "save_xlsx",
]
