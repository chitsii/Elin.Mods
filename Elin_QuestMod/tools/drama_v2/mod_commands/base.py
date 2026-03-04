from __future__ import annotations

from typing import Union

from ..drama_dsl import Chara, Id, StepSpec


def _actor_alias(actor: Union[Chara, str, None]) -> str | None:
    if actor is None:
        return None
    if isinstance(actor, Chara):
        return actor.alias
    return str(actor)


def _id_value(value: Id | str) -> str:
    if isinstance(value, Id):
        return value.value
    return str(value)


class ModCommands:
    def __init__(self, runtime_type: str = "Elin_CommonDrama.DramaRuntime"):
        self.runtime_type = runtime_type

    def resolve_flag(
        self,
        dep_key: str,
        out_flag: Id | str,
        actor: Chara | str = "pc",
    ) -> StepSpec:
        return StepSpec(
            "resolve_flag",
            {
                "dependency_key": dep_key,
                "out_flag": _id_value(out_flag),
                "actor": _actor_alias(actor),
                "runtime_type": self.runtime_type,
            },
        )

    def resolve_flags_all(
        self,
        dep_keys: list[str],
        out_flag: Id | str,
        actor: Chara | str = "pc",
    ) -> StepSpec:
        return StepSpec(
            "resolve_flags_all",
            {
                "dependency_keys": [str(k) for k in dep_keys],
                "out_flag": _id_value(out_flag),
                "actor": _actor_alias(actor),
            },
        )

    def resolve_flags_any(
        self,
        dep_keys: list[str],
        out_flag: Id | str,
        actor: Chara | str = "pc",
    ) -> StepSpec:
        return StepSpec(
            "resolve_flags_any",
            {
                "dependency_keys": [str(k) for k in dep_keys],
                "out_flag": _id_value(out_flag),
                "actor": _actor_alias(actor),
            },
        )

    def resolve_run(
        self,
        command_key: str,
        actor: Chara | str | None = None,
    ) -> StepSpec:
        return StepSpec(
            "resolve_run",
            {
                "dependency_key": command_key,
                "actor": _actor_alias(actor),
                "runtime_type": self.runtime_type,
            },
        )

    def quest_check(
        self,
        drama_id: str,
        out_flag: Id | str,
        actor: Chara | str = "pc",
    ) -> StepSpec:
        return self.quest_can_start(drama_id, out_flag=out_flag, actor=actor)

    def quest_can_start(
        self,
        drama_id: str,
        out_flag: Id | str,
        actor: Chara | str = "pc",
    ) -> StepSpec:
        return self.resolve_flag(
            f"state.quest.can_start.{drama_id}",
            out_flag=out_flag,
            actor=actor,
        )

    def quest_is_done(
        self,
        drama_id: str,
        out_flag: Id | str,
        actor: Chara | str = "pc",
    ) -> StepSpec:
        return self.resolve_flag(
            f"state.quest.is_done.{drama_id}",
            out_flag=out_flag,
            actor=actor,
        )

    def quest_try_start(
        self,
        drama_id: str,
        actor: Chara | str | None = None,
    ) -> StepSpec:
        return self.resolve_run(f"cmd.quest.try_start.{drama_id}", actor=actor)

    def quest_try_start_repeatable(
        self,
        drama_id: str,
        actor: Chara | str | None = None,
    ) -> StepSpec:
        return self.resolve_run(f"cmd.quest.try_start_repeatable.{drama_id}", actor=actor)

    def quest_try_start_until_complete(
        self,
        drama_id: str,
        actor: Chara | str | None = None,
    ) -> StepSpec:
        return self.resolve_run(
            f"cmd.quest.try_start_until_complete.{drama_id}",
            actor=actor,
        )

    def quest_complete(
        self,
        drama_id: str,
        actor: Chara | str | None = None,
    ) -> StepSpec:
        return self.resolve_run(f"cmd.quest.complete.{drama_id}", actor=actor)

    def run_cue(
        self,
        cue_key: str,
        actor: Chara | str | None = None,
    ) -> StepSpec:
        return self.resolve_run(f"cue.{cue_key}", actor=actor)

    def play_pc_fx(
        self,
        effect_id: str,
        actor: Chara | str | None = None,
    ) -> StepSpec:
        return self.resolve_run(f"fx.pc.{effect_id}", actor=actor)

    def play_pc_fx_with_sfx(
        self,
        effect_id: str,
        sfx_id: str,
        actor: Chara | str | None = None,
    ) -> StepSpec:
        return self.resolve_run(f"fx.pc.{effect_id}+sfx.pc.{sfx_id}", actor=actor)
