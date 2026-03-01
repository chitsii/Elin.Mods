# -*- coding: utf-8 -*-
"""
arena/scenarios - Scenario Definitions

Scenario modules are imported using importlib in the builder.
"""

import importlib

# Export LILY_QUESTS for testing
# Module names starting with digits require importlib
_lily_module = importlib.import_module("arena.scenarios.00_lily")
LILY_QUESTS = _lily_module.LILY_QUESTS

__all__ = ["LILY_QUESTS"]
