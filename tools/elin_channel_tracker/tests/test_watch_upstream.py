"""Behavior tests use real local Git history; no game, network, or mocked parser."""
import json
from pathlib import Path
import subprocess

import pytest

from elin_channel_tracker.cli import main


OLD = 'public static Point GetTeleportPos(Point org, int radius = 6) { return org; }'
NEW = 'public static Point GetTeleportPos(Point org, int radius = 6, Chara target = null) { return org; }'


def git(repo, *args):
    return subprocess.check_output(['git', '-C', str(repo), *args], encoding='utf-8').strip()


def commit(repo, text, path='Elin/ActEffect.cs'):
    p = repo / path
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding='utf-8')
    git(repo, 'add', '.')
    git(repo, 'commit', '-qm', 'fixture')
    return git(repo, 'rev-parse', 'HEAD')


@pytest.fixture
def history(tmp_path):
    repo = tmp_path / 'upstream'
    repo.mkdir()
    git(repo, 'init', '-q')
    git(repo, 'config', 'user.email', 'fixture@example.invalid')
    git(repo, 'config', 'user.name', 'Fixture')
    base = commit(repo, 'public class ActEffect { ' + OLD + ' }')
    targets = tmp_path / 'targets.json'
    targets.write_text(json.dumps({'kind': 'compat_targets', 'schema_version': '1.0.0', 'targets': [{
        'target': 'ActEffect.GetTeleportPos', 'check_kind': 'reflection',
        'type_name': 'ActEffect', 'canonical_name': 'GetTeleportPos',
        'candidate_names': ['GetTeleportPos'],
        'candidate_signatures': ['Point(Point,System.Int32)'],
    }]}), encoding='utf-8')
    watch = tmp_path / 'watch.json'
    watch.write_text(json.dumps({'version': 1, 'mods': [{
        'id': 'Ars', 'files': ['Elin/ActEffect.cs'], 'targets_file': 'targets.json',
        'apis': [{'target': 'ActEffect.GetTeleportPos', 'static': True}],
    }]}), encoding='utf-8')
    return repo, base, watch, targets, tmp_path


def run(history, candidate, accepted=None):
    repo, base, watch, _, out = history
    rc = main(['watch-upstream', '--repo', str(repo), '--watchlist', str(watch),
               '--accepted-ref', accepted or base, '--candidate-ref', candidate,
               '--report-json', str(out / 'report.json'), '--report-md', str(out / 'report.md')])
    report = json.loads((out / 'report.json').read_text(encoding='utf-8'))
    assert report['exit_code'] == rc
    assert (out / 'report.md').read_text(encoding='utf-8').strip()
    return rc, report


def test_removed_two_arg_contract_is_broken(history):
    tip = commit(history[0], 'public class ActEffect { ' + NEW + ' }')
    rc, report = run(history, tip)
    assert rc == 1
    assert report['mods'][0]['status'] == 'needs_maintenance'
    check = report['mods'][0]['api_checks'][0]
    assert check['status'] == 'broken'
    assert check['reason_code'] == 'signature_mismatch'
    assert check['nightly_signatures'] == ['Point(Point,System.Int32,Chara)']


def test_reviewed_or_contract_accepts_both_but_body_still_needs_review(history):
    targets = history[3]
    doc = json.loads(targets.read_text())
    doc['targets'][0]['candidate_signatures'].append('Point(Point,System.Int32,Chara)')
    targets.write_text(json.dumps(doc))
    tip = commit(history[0], 'public class ActEffect { ' + NEW + ' }')
    rc, report = run(history, tip)
    assert rc == 1
    assert report['mods'][0]['api_checks'][0]['status'] == 'ok'
    assert report['mods'][0]['reasons'][0]['code'] == 'watched_file_changed'
    assert run(history, tip, accepted=tip)[0] == 0


@pytest.mark.parametrize('text,path', [
    ('// unrelated', 'Elin/Other.cs'),
    ('// new comment\npublic class ActEffect { ' + OLD + ' }', 'Elin/ActEffect.cs'),
    ('public class ActEffect { /* changed */ ' + OLD + ' }', 'Elin/ActEffect.cs'),
])
def test_unrelated_and_comment_only_changes_are_clear(history, text, path):
    tip = commit(history[0], text, path)
    assert run(history, tip)[0] == 0


def test_monitored_body_change_is_maintenance_not_proven_break(history):
    tip = commit(history[0], 'public class ActEffect { ' + OLD.replace('return org', 'return null') + ' }')
    rc, report = run(history, tip)
    assert rc == 1
    assert report['mods'][0]['api_checks'][0]['status'] == 'ok'


@pytest.mark.parametrize('text', [
    'public class ActEffect { public static Point GetTeleportPos<T>(Point org) { return org; } }',
    'public class ActEffect { ' + OLD,
    'public class ActEffect { #if UNKNOWN\n' + OLD + '\n#endif }',
    'public class ActEffect { public static Point GetTeleportPos(ref Point org) { return org; } }',
])
def test_unsupported_or_malformed_source_is_unknown(history, text):
    tip = commit(history[0], text)
    rc, report = run(history, tip)
    assert rc == 2
    assert report['mods'][0]['status'] == 'unknown'


def test_wrong_static_or_accessibility_does_not_accept_shape(history):
    tip = commit(history[0], 'public class ActEffect { ' + OLD.replace('public static', 'private') + ' }')
    rc, report = run(history, tip)
    assert rc == 1
    assert report['mods'][0]['api_checks'][0]['status'] == 'broken'


def test_local_function_does_not_replace_removed_member(history):
    tip = commit(history[0], 'public class ActEffect { public void X() { ' + OLD + ' } }')
    rc, report = run(history, tip)
    assert rc == 1
    assert report['mods'][0]['api_checks'][0]['reason_code'] == 'missing_symbol'


def test_r2_unrelated_preserves_r1_waiting_reason_and_aggregate_count(history):
    r1 = commit(history[0], 'public class ActEffect { ' + NEW + ' }')
    _, first = run(history, r1)
    r2 = commit(history[0], '// unrelated', 'Elin/Other.cs')
    rc, second = run(history, r2)
    assert rc == 1
    assert second['commit_count'] == 2
    assert second['mods'][0]['reasons'] == first['mods'][0]['reasons']
    assert second['accepted_commit'] == history[1]


def test_missing_watched_file_is_unknown_not_empty_success(history):
    git(history[0], 'rm', 'Elin/ActEffect.cs')
    git(history[0], 'commit', '-qm', 'delete')
    rc, report = run(history, git(history[0], 'rev-parse', 'HEAD'))
    assert rc == 2
    assert report['mods'][0]['status'] == 'unknown'


@pytest.mark.parametrize('field,value', [('candidate-ref', 'missing-stable'), ('accepted-ref', 'missing-stable')])
def test_missing_ref_has_json_unknown_without_head_fallback(history, field, value):
    rc, report = run(history, value if field == 'candidate-ref' else history[1],
                     accepted=value if field == 'accepted-ref' else None)
    assert rc == 2
    assert report['status'] == 'unknown'
    assert report['errors']


def test_literal_comment_markers_do_not_hide_body_change(history):
    tip = commit(history[0], 'public class ActEffect { ' + OLD.replace('return org;', 'var s = "https://x/*y*/"; return org;') + ' }')
    assert run(history, tip)[0] == 1


def test_empty_watchlist_fails_closed(history):
    history[2].write_text('{"version":1,"mods":[]}')
    assert run(history, history[1])[0] == 2


def test_invalid_target_or_unknown_config_key_fails_closed(history):
    doc = json.loads(history[2].read_text())
    doc['mods'][0]['apis'][0]['target'] = 'ActEffect.DoesNotExist'
    history[2].write_text(json.dumps(doc))
    assert run(history, history[1])[0] == 2


def test_nonancestor_candidate_is_unknown(history):
    tip = commit(history[0], '// newer', 'Elin/Other.cs')
    rc, report = run(history, history[1], accepted=tip)
    assert rc == 2
    assert report['status'] == 'unknown'


def test_nested_helper_class_does_not_make_outer_contract_unknown(history):
    text = 'public class ActEffect { private class WishItem { public string n; } ' + OLD + ' }'
    tip = commit(history[0], text)
    rc, report = run(history, tip)
    assert rc == 1
    assert report['mods'][0]['api_checks'][0]['status'] == 'ok'


def test_operator_spacing_that_changes_lexemes_is_not_ignored(history):
    before = OLD.replace('return org;', 'int a=1,b=2; var n=a+++b; return org;')
    base = commit(history[0], 'public class ActEffect { ' + before + ' }')
    tip = commit(history[0], 'public class ActEffect { ' + before.replace('a+++b', 'a+ ++b') + ' }')
    assert run(history, tip, accepted=base)[0] == 1


def test_expression_bodied_supported_method_is_readable(history):
    tip = commit(history[0], 'public class ActEffect { public static Point GetTeleportPos(Point org, int radius = 6) => org; }')
    rc, report = run(history, tip)
    assert rc == 1
    assert report['mods'][0]['api_checks'][0]['status'] == 'ok'


# These valid string forms must never be normalized by the ordinary-string reader.
UNSUPPORTED_STRINGS = [
    '$"{F("//a")}"',
    '$"{F("/*a*/")}"',
    '$"//a"',
    '@"//a"',
    '@"/*a*/ and ""quotes"""',
    '$@"{F("//a")}"',
    '@$"{F("//a")}"',
    '"""//a"""',
    '$"""{F("//a")}"""',
    '$$"""{{F("//a")}}"""',
]


def string_change(history, literal, with_api=False, comment_only=False):
    if not with_api:
        doc = json.loads(history[2].read_text())
        doc['mods'][0].pop('apis')
        history[2].write_text(json.dumps(doc))
    text = 'public class ActEffect { ' + OLD.replace('return org;', f'var s = {literal}; return org;') + ' }'
    base = commit(history[0], text)
    after = text + '\n// comment' if comment_only else text.replace('//a', '//b').replace('/*a*/', '/*b*/')
    tip = commit(history[0], after)
    return run(history, tip, accepted=base)


@pytest.mark.parametrize('literal', UNSUPPORTED_STRINGS)
def test_unsupported_string_changes_use_raw_diff_not_clear(history, literal):
    rc, report = string_change(history, literal)
    assert rc == 1
    assert report['mods'][0]['reasons'][0]['comparison'] == 'raw_fallback'
    assert not report['mods'][0]['errors']


def test_unsupported_string_in_api_source_is_unknown(history):
    rc, report = string_change(history, UNSUPPORTED_STRINGS[0], with_api=True)
    assert rc == 2
    assert report['mods'][0]['reasons'][0]['comparison'] == 'raw_fallback'
    assert report['mods'][0]['errors'][0]['code'] == 'signature_unreadable'


def test_comment_edit_in_unsupported_string_file_is_conservatively_reviewed(history):
    rc, report = string_change(history, UNSUPPORTED_STRINGS[0], comment_only=True)
    assert rc == 1
    assert report['mods'][0]['reasons'][0]['comparison'] == 'raw_fallback'


def test_escaped_local_identifier_does_not_block_ordinary_api_contract(history):
    text = 'public class ActEffect { ' + OLD.replace('return org;', 'string @ref = "//safe"; return org;') + ' }'
    tip = commit(history[0], text)
    rc, report = run(history, tip)
    assert rc == 1
    assert report['mods'][0]['api_checks'][0]['status'] == 'ok'


def test_changed_verbatim_literal_line_endings_cannot_be_cleared_by_text_conversion(history):
    repo = history[0]
    doc = json.loads(history[2].read_text())
    doc['mods'][0].pop('apis')
    history[2].write_text(json.dumps(doc))
    git(repo, 'config', 'core.autocrlf', 'false')
    text = 'public class ActEffect { ' + OLD.replace('return org;', 'var s = @"line1\nline2"; return org;') + ' }'
    path = repo / 'Elin/ActEffect.cs'
    path.write_bytes(text.encode('utf-8'))
    git(repo, 'add', '.')
    git(repo, 'commit', '-qm', 'literal LF')
    base = git(repo, 'rev-parse', 'HEAD')
    path.write_bytes(text.replace('line1\nline2', 'line1\r\nline2').encode('utf-8'))
    git(repo, 'add', '.')
    git(repo, 'commit', '-qm', 'literal CRLF')
    rc, report = run(history, git(repo, 'rev-parse', 'HEAD'), accepted=base)
    assert rc == 1
    assert report['mods'][0]['reasons'][0]['comparison'] == 'raw_fallback'
