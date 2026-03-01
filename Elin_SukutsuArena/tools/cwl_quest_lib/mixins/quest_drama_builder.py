"""
CWL Quest Library - Quest Drama Builder with Mixins

This module provides high-level quest-oriented DramaBuilder mixins that can be
reused across different CWL mods. Game-specific implementations inherit from
these mixins and add their own custom functionality.

Available Mixins:
- QuestDispatcherMixin: Quest availability checking and routing
- QuestInfoMixin: Quest information display
- GreetingMixin: Flag-based NPC greeting system
- MenuMixin: Conditional menu building

Usage:
    from cwl_quest_lib.quest_drama_builder import (
        QuestDispatcherMixin, QuestInfoMixin, GreetingMixin, MenuMixin
    )
    from cwl_quest_lib import DramaBuilder

    class MyDramaBuilder(
        QuestDispatcherMixin,
        GreetingMixin,
        MenuMixin,
        DramaBuilder
    ):
        pass
"""

from typing import Dict, List, Union, Callable, TYPE_CHECKING

from ..core.types import (
    GreetingDefinition,
    QuestEntry,
    MenuItem,
    QuestInfoDefinition,
    QuestStartDefinition,
)

if TYPE_CHECKING:
    from ..builders.drama_builder import DramaLabel, DramaActor


class QuestDispatcherMixin:
    """
    Quest dispatcher system mixin.

    Provides methods for checking quest availability and routing players
    to the appropriate quest step based on preconditions.

    Methods:
    - build_quest_dispatcher(): Build quest routing based on availability
    - build_quest_info_steps(): Build info display steps
    - build_quest_start_steps(): Build confirmation and start steps
    """

    def build_quest_dispatcher(
        self,
        quests: List[QuestEntry],
        entry_step: Union[str, "DramaLabel"],
        fallback_step: Union[str, "DramaLabel"],
        actor: Union[str, "DramaActor"],
        intro_message: str = "",
        intro_message_en: str = "",
        intro_message_cn: str = "",
        intro_id: str = "",
        target_flag: str = "quest_target",
        found_flag: str = "quest_found",
    ) -> Dict[str, "DramaLabel"]:
        """
        Build a quest dispatcher that routes to available quests.

        Uses modInvoke check_quests_for_dispatch to check quest preconditions
        and set a flag with the index of the first available quest.

        Flag values:
        - 0: No quest available (fallback)
        - 1: First quest in list available
        - 2: Second quest in list available
        - ...

        Args:
            quests: List of QuestEntry (ordered by priority)
            entry_step: Entry step label
            fallback_step: Fallback when no quests available
            actor: Speaking actor
            intro_message: Optional intro message (JP)
            intro_message_en: Optional intro message (EN)
            intro_message_cn: Optional intro message (CN)
            intro_id: Text ID for intro message
            target_flag: Flag name for dispatch routing (stores quest index)
            found_flag: Flag name for quest found indicator

        Returns:
            Dict of {step_name: label} for generated steps
        """
        labels = {}

        # Create labels for each quest
        for quest in quests:
            labels[quest.step_name] = self.label(quest.step_name)

        # Build entry step
        step_builder = self.step(entry_step)

        if intro_message:
            msg_en = intro_message_en or intro_message
            msg_cn = intro_message_cn or ""
            step_builder.say(intro_id or "quest_check", intro_message, msg_en, msg_cn, actor=actor)

        # Initialize flags and check quest availability
        step_builder.set_flag(found_flag, 0).set_flag(target_flag, 0).debug_log_quests()

        # Check quests and set flag to first available quest's index
        quest_ids = [quest.quest_id for quest in quests]
        step_builder.check_quests_for_dispatch(target_flag, quest_ids)

        # Build switch cases (index 0 = fallback, 1 = first quest, etc.)
        cases_list = [fallback_step]
        for quest in quests:
            cases_list.append(labels[quest.step_name])

        step_builder.switch_flag(target_flag, cases_list)

        return labels

    def build_quest_info_steps(
        self,
        infos: List[QuestInfoDefinition],
        actor: Union[str, "DramaActor"],
        return_step: Union[str, "DramaLabel"],
    ) -> Dict[str, "DramaLabel"]:
        """
        Build quest info display steps.

        Creates steps where an NPC provides information about quests
        without immediately starting them.

        Args:
            infos: List of QuestInfoDefinition
            actor: Speaking actor
            return_step: Step to return to after info

        Returns:
            Dict of {step_name: label}
        """
        labels = {}

        for info in infos:
            label = self.label(info.step_name)
            labels[info.step_name] = label

            step_builder = self.step(label)

            messages_cn = getattr(info, "messages_cn", []) or []
            for i, msg in enumerate(info.messages):
                msg_en = info.messages_en[i] if i < len(info.messages_en) else msg
                msg_cn = messages_cn[i] if i < len(messages_cn) else ""
                step_builder.say(
                    f"{info.text_id_prefix}{i + 1}" if i > 0 else info.text_id_prefix,
                    msg,
                    msg_en,
                    msg_cn,
                    actor=actor,
                )

            step_builder.jump(return_step)

        return labels

    def build_quest_start_steps(
        self,
        starts: List[QuestStartDefinition],
        actor: Union[str, "DramaActor"],
        cancel_step: Union[str, "DramaLabel"],
        end_step: Union[str, "DramaLabel"],
    ) -> Dict[str, "DramaLabel"]:
        """
        Build quest start confirmation steps.

        Creates steps with info display, confirmation dialog, and quest start.

        Args:
            starts: List of QuestStartDefinition
            actor: Speaking actor
            cancel_step: Step on cancel/decline
            end_step: Step after starting

        Returns:
            Dict of {step_name: label}
        """
        labels = {}

        for start in starts:
            info_label = self.label(start.info_step)
            start_label = self.label(start.start_step)
            labels[start.info_step] = info_label
            labels[start.start_step] = start_label

            # Info display step
            step_builder = self.step(info_label)

            info_messages_cn = getattr(start, "info_messages_cn", []) or []
            for i, msg in enumerate(start.info_messages):
                msg_en = (
                    start.info_messages_en[i]
                    if i < len(start.info_messages_en)
                    else msg
                )
                msg_cn = info_messages_cn[i] if i < len(info_messages_cn) else ""
                step_builder.say(
                    f"{start.info_id_prefix}{i + 1}"
                    if start.info_id_prefix
                    else f"{start.info_step}_{i + 1}",
                    msg,
                    msg_en,
                    msg_cn,
                    actor=actor,
                )

            accept_button_cn = getattr(start, "accept_button_cn", "") or ""
            decline_button_cn = getattr(start, "decline_button_cn", "") or ""
            step_builder.choice(
                start_label,
                start.accept_button,
                start.accept_button_en,
                accept_button_cn,
                text_id=start.accept_id,
            ).choice(
                cancel_step,
                start.decline_button,
                start.decline_button_en,
                decline_button_cn,
                text_id=start.decline_id,
            ).on_cancel(cancel_step)

            # Start step
            if start.start_message:
                start_msg_en = (
                    start.start_message_en if start.start_message_en else start.start_message
                )
                start_msg_cn = getattr(start, "start_message_cn", "") or ""
                self.step(start_label).say(
                    f"{start.start_step}_msg", start.start_message, start_msg_en, start_msg_cn, actor=actor
                ).mod_invoke(f"start_drama({start.drama_name})").finish().jump(end_step)
            else:
                self.step(start_label).mod_invoke(
                    f"start_drama({start.drama_name})"
                ).finish().jump(end_step)

        return labels


class GreetingMixin:
    """
    Flag-based greeting system mixin.

    Provides methods for building NPC greetings that vary based on
    flag values (e.g., player rank, story progress).

    Methods:
    - build_greetings(): Generate greeting steps for each flag value
    - build_greeting_dispatcher(): Route to appropriate greeting
    """

    def build_greetings(
        self,
        greetings: List[GreetingDefinition],
        actor: Union[str, "DramaActor"],
        add_choices_func: Callable,
        default_greeting: GreetingDefinition = None,
    ) -> Dict[int, "DramaLabel"]:
        """
        Build flag-based greeting steps.

        Args:
            greetings: List of GreetingDefinition
            actor: Speaking actor
            add_choices_func: Function(builder) that adds choices after greeting
            default_greeting: Default greeting when no match

        Returns:
            Dict of {flag_value: label}
        """
        labels = {}

        for greet in greetings:
            label_name = f"greet_{greet.flag_value}"
            label = self.label(label_name)
            labels[greet.flag_value] = label

            b = self.step(label).say(
                greet.text_id,
                greet.text_jp,
                greet.text_en or greet.text_jp,
                actor=actor,
            )
            add_choices_func(b)

        # Default greeting
        if default_greeting:
            default_label = self.label("greet_default")
            labels["default"] = default_label
            b = self.step(default_label).say(
                default_greeting.text_id,
                default_greeting.text_jp,
                default_greeting.text_en or default_greeting.text_jp,
                actor=actor,
            )
            add_choices_func(b)

        return labels

    def build_greeting_dispatcher(
        self,
        greeting_labels: Dict[int, "DramaLabel"],
        entry_step: Union[str, "DramaLabel"],
        flag_key: str,
        default_label: "DramaLabel" = None,
    ) -> None:
        """
        Build greeting dispatcher based on flag value.

        Args:
            greeting_labels: Output from build_greetings()
            entry_step: Entry step label
            flag_key: Flag key to check
            default_label: Default label (or greeting_labels['default'])
        """
        fallback = default_label or greeting_labels.get("default")

        # Convert dict to list format for switch_flag
        int_cases = {
            v: label for v, label in greeting_labels.items() if isinstance(v, int)
        }
        if int_cases:
            max_value = max(int_cases.keys())
            cases_list = [int_cases.get(i, fallback) for i in range(max_value + 1)]
            cases_list.append(fallback)  # Add fallback
        else:
            cases_list = [fallback]

        self.step(entry_step).switch_flag(flag_key, cases_list)


class MenuMixin:
    """
    Conditional menu building mixin.

    Provides methods for building menus with optional conditions on items.

    Methods:
    - add_menu(): Add menu items with optional conditions
    """

    def add_menu(
        self,
        items: List[MenuItem],
        cancel: Union[str, "DramaLabel"] = None,
    ) -> "MenuMixin":
        """
        Add menu (choice list) with optional conditions.

        Args:
            items: List of MenuItem
            cancel: Cancel target step

        Returns:
            self for chaining
        """
        for item in items:
            text_cn = getattr(item, "text_cn", "") or ""
            if item.condition:
                self.choice_if(
                    item.jump_to,
                    item.text_jp,
                    item.condition,
                    item.text_en,
                    text_cn,
                    item.text_id,
                )
            else:
                self.choice(item.jump_to, item.text_jp, item.text_en, text_cn, item.text_id)

        if cancel:
            self.on_cancel(cancel)

        return self


class RewardMixin:
    """
    Reward granting mixin.

    Provides methods for granting items and setting flags as rewards.

    Methods:
    - grant_items(): Grant item rewards to player
    - apply_flags(): Set flag values
    """

    def grant_items(self, items: list) -> "RewardMixin":
        """
        Grant item rewards to player.

        Args:
            items: List of RewardItem

        Returns:
            self for chaining
        """
        # Aggregate same items
        item_counts = {}
        for item in items:
            if item.item_id not in item_counts:
                item_counts[item.item_id] = 0
            item_counts[item.item_id] += item.count

        # Generate C# code for item creation
        parts = []
        for item_id, count in item_counts.items():
            if count == 1:
                parts.append(f'EClass.pc.Pick(ThingGen.Create("{item_id}"));')
            else:
                parts.append(
                    f'for(int i=0; i<{count}; i++) {{ EClass.pc.Pick(ThingGen.Create("{item_id}")); }}'
                )

        if parts:
            script = " ".join(parts)
            self.action("eval", param=script)

        return self

    def apply_flags(self, flags: dict) -> "RewardMixin":
        """
        Apply flag changes.

        Args:
            flags: Dict of {flag_key: value}

        Returns:
            self for chaining
        """
        if not flags:
            return self

        for flag_key, value in flags.items():
            self.set_flag(flag_key, value)

        return self


# ============================================================================
# Combined Quest Drama Builder
# ============================================================================


class QuestDramaBuilder(
    QuestDispatcherMixin,
    GreetingMixin,
    MenuMixin,
    RewardMixin,
):
    """
    Combined quest drama builder with all generic mixins.

    This is a convenience class that combines all generic mixins.
    Game-specific implementations should inherit from this class
    along with DramaBuilder.

    Example:
        from cwl_quest_lib import DramaBuilder
        from cwl_quest_lib.quest_drama_builder import QuestDramaBuilder

        class MyGameDramaBuilder(QuestDramaBuilder, DramaBuilder):
            # Add game-specific methods
            pass
    """

    pass
