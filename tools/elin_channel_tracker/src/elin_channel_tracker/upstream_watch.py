"""Local-only watchlist probe. Always compare from the last human-accepted SHA."""
import json
from pathlib import Path, PurePosixPath
import subprocess

from .evaluator import CompatEvaluator
from .source_contracts import code_tokens, method_signatures


LABELS = {'clear': '監視範囲に関連差分なし', 'needs_maintenance': '要メンテ', 'unknown': '判定不能'}
EXIT_CODES = {'clear': 0, 'needs_maintenance': 1, 'unknown': 2}


def _git(repo: Path, *args: str) -> str:
    result = subprocess.run(['git', '--no-optional-locks', '-c', f'safe.directory={repo.resolve().as_posix()}',
                             '-C', str(repo), *args], capture_output=True, encoding='utf-8', check=False)
    if result.returncode:
        raise ValueError(result.stderr.strip() or 'Git read failed')
    return result.stdout


def _path(value):
    if not isinstance(value, str) or not value or '\\' in value:
        raise ValueError('paths must be nonempty repository-relative POSIX paths')
    path = PurePosixPath(value)
    if path.is_absolute() or '..' in path.parts or ':' in value or any(c in value for c in '*?['):
        raise ValueError('only exact relative paths are supported')
    return value


def _load_watchlist(path: Path):
    doc = json.loads(path.read_text(encoding='utf-8-sig'))
    if set(doc) != {'version', 'mods'} or doc['version'] != 1 or not isinstance(doc['mods'], list) or not doc['mods']:
        raise ValueError('watchlist needs version 1 and a nonempty mods list')
    mods, ids = [], set()
    for row in doc['mods']:
        if set(row) - {'id', 'files', 'targets_file', 'apis'}:
            raise ValueError('unrecognized mod watchlist key')
        if not isinstance(row.get('id'), str) or not row['id'] or row['id'] in ids:
            raise ValueError('mod IDs must be nonempty and unique')
        ids.add(row['id'])
        if not isinstance(row.get('files'), list) or not row['files']:
            raise ValueError('each mod needs at least one watched file')
        files = sorted(set(_path(p) for p in row['files']))
        apis = row.get('apis', [])
        if not isinstance(apis, list):
            raise ValueError('apis must be a list')
        targets = {}
        if apis:
            targets_doc = json.loads((path.parent / _path(row['targets_file'])).read_text(encoding='utf-8-sig'))
            if targets_doc.get('kind') != 'compat_targets' or targets_doc.get('schema_version') != '1.0.0':
                raise ValueError('unsupported compat targets document')
            targets = {t['target']: t for t in targets_doc['targets']}
        checks = []
        for api in apis:
            if set(api) != {'target', 'static'} or not isinstance(api['static'], bool):
                raise ValueError('API entries require target and boolean static')
            target = targets.get(api['target'])
            if not target or not target.get('candidate_signatures') or target.get('check_kind') == 'dynamic':
                raise ValueError('selected API needs an explicit supported signature contract')
            owner = target['type_name']
            source = f'Elin/{owner}.cs'
            if source not in files:
                raise ValueError(f'selected API file is not watched: {source}')
            checks.append((target, api['static'], source))
        mods.append((row['id'], files, checks))
    return mods


def evaluate_watch(*, repo: Path, watchlist: Path, accepted_ref: str, candidate_ref: str) -> dict:
    report = {'kind': 'upstream_watch_report', 'schema_version': '1.0.0',
              'accepted_ref': accepted_ref, 'candidate_ref': candidate_ref, 'mods': [], 'errors': [],
              'coverage': 'explicit watchlist only; file bodies need human review; no semantic guarantee',
              'baseline_policy': 'always compare from last human-accepted commit; no automatic advancement'}
    try:
        mods = _load_watchlist(watchlist)
        base = _git(repo, 'rev-parse', '--verify', '--end-of-options', accepted_ref + '^{commit}').strip()
        tip = _git(repo, 'rev-parse', '--verify', '--end-of-options', candidate_ref + '^{commit}').strip()
        report.update(accepted_commit=base, candidate_commit=tip)
        _git(repo, 'merge-base', '--is-ancestor', base, tip)
        report['commit_count'] = int(_git(repo, 'rev-list', '--count', f'{base}..{tip}'))
        changed = set(_git(repo, 'diff', '--name-only', '--no-renames', '-z', base, tip, '--').split('\0'))
        watched_files = sorted({file for _, files, _ in mods for file in files})
        available = {sha: set(_git(repo, 'ls-tree', '-r', '--name-only', '-z', sha, '--', *watched_files).split('\0'))
                     for sha in (base, tip)}
        cache = {}

        def source(sha, file):
            key = (sha, file)
            if key not in cache:
                cache[key] = _git(repo, 'show', f'{sha}:{file}')
            return cache[key]

        for mod_id, files, contracts in mods:
            reasons, errors, checks = [], [], []
            for file in files:
                try:
                    # Check both trees even when the file is unchanged: missing inputs fail closed.
                    if file not in available[base] or file not in available[tip]:
                        raise ValueError('watched file missing from accepted or candidate tree')
                    if file in changed:
                        before, after = source(base, file), source(tip, file)
                        comparison = 'raw'
                        different = before != after
                        if file.endswith('.cs'):
                            try:
                                different = code_tokens(before) != code_tokens(after)
                                comparison = 'ordinary_tokens'
                            except ValueError:
                                comparison = 'raw_fallback'
                                different = True  # Git already reported this path changed; never clear uncertain text.
                        if different:
                            reasons.append({'code': 'watched_file_changed', 'file': file,
                                            'comparison': comparison,
                                            'detail': 'file body/declaration changed; meaning is not evaluated'})
                except (ValueError, UnicodeError) as ex:
                    errors.append({'code': 'source_unavailable_or_unreadable', 'file': file, 'detail': str(ex)})
            for target, is_static, file in contracts:
                try:
                    symbols = []
                    for sha in (base, tip):
                        catalog = {}
                        for name in target.get('candidate_names') or [target['canonical_name']]:
                            sigs = method_signatures(source(sha, file), target['type_name'], name, is_static)
                            if sigs:
                                catalog[f"{target['type_name']}.{name}"] = sigs
                        symbols.append(catalog)
                    check = CompatEvaluator().evaluate([target], *symbols)['checks'][0]
                    check['provenance'] = 'limited source reader; not real DLL metadata'
                    checks.append(check)
                    if check['status'] != 'ok':
                        reasons.append({'code': check['reason_code'], 'target': target['target'],
                                        'detail': check['status']})
                except (ValueError, UnicodeError, IndexError) as ex:
                    errors.append({'code': 'signature_unreadable', 'target': target['target'], 'detail': str(ex)})
            status = 'unknown' if errors else 'needs_maintenance' if reasons else 'clear'
            report['mods'].append({'id': mod_id, 'status': status, 'label': LABELS[status],
                                   'watched_files': files, 'reasons': reasons, 'errors': errors, 'api_checks': checks})
        status = max((m['status'] for m in report['mods']), key=EXIT_CODES.get)
    except (ValueError, KeyError, TypeError, OSError) as ex:
        report['errors'].append(str(ex))
        status = 'unknown'
    report.update(status=status, label=LABELS[status], exit_code=EXIT_CODES[status])
    return report


def render_watch(report: dict) -> str:
    lines = ['# Upstream watch prototype', '', f"判定: {report['label']}",
             f"合格済み基準: {report.get('accepted_commit', report['accepted_ref'])}",
             f"候補: {report.get('candidate_commit', report['candidate_ref'])}",
             f"集約commit数: {report.get('commit_count', 'unknown')}", '', report['coverage'],
             '基準SHAは人が確認を完了するまで進めない。', '']
    for mod in report['mods']:
        lines.append(f"- {mod['id']}: {mod['label']}")
        for row in mod['reasons'] + mod['errors']:
            lines.append(f"  - {row['code']}: {row.get('file', row.get('target'))} ({row['detail']})")
        for check in mod['api_checks']:
            lines.append(f"  - API {check['target']}: {check['status']} ({check['reason_code']})")
    lines.extend(f'- ERROR: {e}' for e in report['errors'])
    return '\n'.join(lines) + '\n'
