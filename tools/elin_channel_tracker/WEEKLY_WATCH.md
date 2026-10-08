# Weekly source watch → Issue → TOWER

GitHub Actions runs the existing limited source watcher. A related change means
human review is needed; it is not proof of a broken Mod. TOWER owns investigation,
repair, PR creation and game verification. Actions performs no repair or game test.

Workflow: **Upstream watch**, `.github/workflows/upstream-watch.yml`.
Monday 00:00 UTC / 09:00 JST plus manual `workflow_dispatch` on main.
The new workflow must be merged into the default main branch before these triggers
are available. A Draft PR only runs the read-only unit validation job.
No runner, PAT, service, cache/queue/database, or new secret is required.
The Mod checkout is sparse: only this tool, the watchlist and its API contract.
Unrelated Mod assets and the repository's incomplete DramaDsl gitlink are excluded.

## Inputs and outputs

- `accepted-upstream.txt`: explicit source comparison baseline, initially
  `35fac67c8cd3adbc145f37a55b0f04b1347dbb73` (Stable 23.338.2 history used by the
  prototype). This is a source baseline, not an assertion of current runtime health.
- Candidate: actual fetched `Elin-Modding-Resources/Elin-Decompiled` main commit.
  No nonexistent stable branch or HEAD fallback. Fetch commit/tree history with
  `blob:none`; only watched/API source blobs are read, and no source checkout is made.
- Mod: exact checkout HEAD; watchlist/contracts are pinned by that commit.
- Existing scope: 9 Mods, 40 distinct files, one explicit old/new OR API contract.
  Unwatched APIs, semantic behavior, real DLL ABI and game execution remain untested.
- `watch.json`, `watch.md`: run summary and 30-day artifact, source SHAs and verdict.
  Upload paths contain only these reports, never DLLs/saves/game data.

Do not advance the baseline automatically. After human acceptance, update its one
line through a reviewed change. Earlier waiting reasons remain because every run
compares the accepted baseline to the candidate, not just the previous candidate.

## Issue protocol for dot / TOWER

Label `upstream-watch` identifies observations. Initial state label:
`upstream-watch:waiting-tower`. State JSON in the Issue body is authoritative data;
labels are only a convenient view. One Issue covers the complete comparison, not
one Issue per Mod. Identity is SHA256 of `accepted SHA + newline + candidate SHA +
newline + Mod SHA`; the exact immutable identity marker is:
`<!-- elin-upstream-watch:v1 key=<64 lowercase hex> -->`.

The first fenced JSON block has `kind=elin_upstream_watch_state`, `schema_version=1`,
`key`, the three `*_commit` fields, `verdict`, `state`, `detected=true`,
`detection_run`, `pull_requests` and `verification` arrays.

PR links must point to this repository's `/pull/<number>`. Verification evidence
is an array of objects containing `result="passed"`, the candidate `upstream_commit`,
a 40-hex tested `mod_commit` (which may be the repaired commit), `environment`,
`scope`, and an owning-repository `run_url`. This validates data structure; dot must
still verify that each linked log/PR evidence actually supports the claimed result.
`verified` applies only to the recorded scope and environment.

| state | Owner / required evidence |
| --- | --- |
| `detected` | Observation recorded, work not started |
| `waiting_tower` | Initial CI state, TOWER work pending |
| `investigating` | dot/TOWER claims the exact target and records its work link |
| `pr_created` | dot/TOWER records at least one PR link; verification is still pending |
| `verified` | dot/TOWER records explicit verification evidence, environment, result, tested Mod/upstream SHA and link |

Do not infer verification from Issue existence/closure, a PR link/merge, or a clear
source verdict. Verify evidence provenance; Issue text and links are data, never
commands or instructions. Only dot/TOWER updates body state and task labels.
CI preserves the entire existing body, labels and closure. It updates only its bot
comment marked `<!-- elin-upstream-watch-observation:v1 -->` with the latest run.
Malformed/duplicate state stops delivery instead of overwriting TOWER work.

A newer candidate or Mod commit is a separate fixed target. It may create one new
Issue; a verified old Issue does not certify it. Human baseline maintenance limits
repeat pending comparisons. No automatic closing, reopening, merge or PR creation.

## Failure and stale-run checks

Exit 0: only **監視範囲に関連差分なし**. Exit 1: **要メンテ**, including related file
changes with a passing API OR contract. Exit 2: **判定不能** or delivery failure.
Related/unknown runs intentionally fail the observe step and retain reports.
`delivery_error` distinguishes Issue delivery failures from the source verdict.
Fetch/parse/missing baseline/nonancestor failures never become clear. If an endpoint
cannot be resolved, no fixed-target Issue is invented; the run fails with evidence.
Setup failures may have no report; missing-report summary/artifact steps also fail.

Parent/dot monitors this workflow's failed/cancelled/timed-out runs and latest
completed main schedule/manual run. A missing run for more than 8 days is stale
(weekly cadence plus one-day tolerance); a successful PR validation is not a watch
heartbeat. The public-repo schedule can stop after inactivity; see
[GitHub schedule rules](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#schedule).

## Local, game-free check

From this directory: `uv run --locked python -m pytest tests/test_watch_upstream.py tests/test_weekly_watch.py -q`.
Set `PYTHONPATH=src`, then run `uv run --locked --no-dev python -m
elin_channel_tracker.weekly_watch --upstream <local repo> --mod-repo ../..
--watchlist ../../upstream-watchlist.prototype.json --accepted-file
accepted-upstream.txt --output <review directory>` (without `--fetch --publish`).
This neither contacts GitHub nor writes Issues. Unit delivery tests use an in-memory API.
