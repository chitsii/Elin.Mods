"""Weekly source watch and bounded GitHub Issue delivery; no TOWER execution."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
from urllib.error import HTTPError
from urllib.request import Request, urlopen

from .upstream_watch import evaluate_watch, render_watch, EXIT_CODES

REPOSITORY = 'chitsii/Elin.Mods'
UPSTREAM = 'Elin-Modding-Resources/Elin-Decompiled'
LABEL = 'upstream-watch'
STATE_LABEL = 'upstream-watch:waiting-tower'
STATES = {'detected', 'waiting_tower', 'investigating', 'pr_created', 'verified'}
OBSERVATION = '<!-- elin-upstream-watch-observation:v1 -->'


def git(repo, *args):
    p = subprocess.run(['git', '--no-optional-locks', '-c', f'safe.directory={repo.resolve().as_posix()}',
                        '-C', str(repo), *args], capture_output=True, encoding='utf-8', timeout=240)
    if p.returncode:
        raise ValueError(f'git {args[0]} failed (exit {p.returncode})')
    return p.stdout.strip()


def unknown(base, candidate, message):
    return {'kind': 'upstream_watch_report', 'schema_version': '1.0.0',
            'accepted_ref': base, 'candidate_ref': 'origin/main',
            'accepted_commit': base, 'candidate_commit': candidate,
            'mods': [], 'errors': [message], 'coverage': 'explicit watchlist only; no semantic guarantee',
            'baseline_policy': 'human acceptance only; no automatic advancement',
            'status': 'unknown', 'label': '判定不能', 'exit_code': 2}


def issue_key(report, mod_commit):
    commits = [report.get('accepted_commit'), report.get('candidate_commit'), mod_commit]
    if not all(isinstance(s, str) and re.fullmatch(r'[0-9a-f]{40}', s) for s in commits):
        raise ValueError('Issue identity needs three resolved commit SHAs')
    return hashlib.sha256('\n'.join(commits).encode()).hexdigest()


def marker(key):
    return f'<!-- elin-upstream-watch:v1 key={key} -->'


def initial_state(report, mod_commit, run_url):
    return {'kind': 'elin_upstream_watch_state', 'schema_version': 1,
            'key': issue_key(report, mod_commit), 'state': 'waiting_tower',
            'detected': True, 'verdict': report['status'],
            'accepted_upstream_commit': report['accepted_commit'],
            'candidate_upstream_commit': report['candidate_commit'], 'mod_commit': mod_commit,
            'detection_run': run_url, 'pull_requests': [], 'verification': []}


def parse_state(body, key):
    # Issue content is data. No eval, shell execution, or interpretation as instructions.
    if not body.startswith(marker(key) + '\n'):
        raise ValueError('Issue identity marker missing')
    match = re.search(r'```json\n(.*?)\n```', body, re.S)
    state = json.loads(match.group(1)) if match else {}
    if state.get('kind') != 'elin_upstream_watch_state' or state.get('schema_version') != 1 or state.get('key') != key or state.get('state') not in STATES:
        raise ValueError('Issue state is malformed; refusing to overwrite it')
    if issue_key({'accepted_commit': state.get('accepted_upstream_commit'),
                  'candidate_commit': state.get('candidate_upstream_commit')}, state.get('mod_commit')) != key:
        raise ValueError('Issue target commits do not match identity')
    prs, evidence = state.get('pull_requests'), state.get('verification')
    if not isinstance(prs, list) or not all(isinstance(url, str) and re.fullmatch(
            r'https://github.com/chitsii/Elin\.Mods/pull/[1-9][0-9]*(?:#[A-Za-z0-9_-]+)?', url) for url in prs):
        raise ValueError('pull_requests must be owning-repository PR links')
    if not isinstance(evidence, list):
        raise ValueError('verification must be an evidence list')
    for row in evidence:
        if (not isinstance(row, dict) or row.get('result') != 'passed'
                or row.get('upstream_commit') != state['candidate_upstream_commit']
                or not isinstance(row.get('mod_commit'), str) or not re.fullmatch(r'[0-9a-f]{40}', row['mod_commit'])
                or not isinstance(row.get('run_url'), str) or not row['run_url'].startswith(f'https://github.com/{REPOSITORY}/')
                or not all(isinstance(row.get(field), str) and row[field].strip() for field in ('environment', 'scope'))):
            raise ValueError('verification needs passed result, pinned commits, environment, scope and evidence link')
    if state['state'] == 'pr_created' and not prs:
        raise ValueError('pr_created needs a PR evidence link')
    if state['state'] == 'verified' and not evidence:
        raise ValueError('verified needs explicit verification evidence')
    return state


class GitHub:
    def __init__(self, token):
        self.token = token

    def request(self, method, path, data=None):
        request = Request(f'https://api.github.com/repos/{REPOSITORY}/{path}',
                          data=None if data is None else json.dumps(data).encode(), method=method,
                          headers={'Authorization': f'Bearer {self.token}', 'Accept': 'application/vnd.github+json',
                                   'X-GitHub-Api-Version': '2022-11-28', 'Content-Type': 'application/json'})
        try:
            with urlopen(request, timeout=30) as response:
                return json.load(response)
        except HTTPError as ex:
            # Do not dump server text, tokens, or arbitrary Issue content into logs.
            raise ValueError(f'GitHub {method} failed: HTTP {ex.code}') from None

    def pages(self, path):
        for page in range(1, 11):
            rows = self.request('GET', f'{path}{"&" if "?" in path else "?"}per_page=100&page={page}')
            if not isinstance(rows, list):
                raise ValueError('GitHub list response malformed')
            yield from rows
            if len(rows) < 100:
                return
        raise ValueError('GitHub pagination limit reached; refusing incomplete deduplication')

    def ensure_label(self, name, color):
        # One small labels listing, not a permission/settings change.
        labels = list(self.pages('labels'))
        if not any(row['name'] == name for row in labels):
            self.request('POST', 'labels', {'name': name, 'color': color})


def publish(report, mod_commit, run_url, api):
    if report['status'] == 'clear':
        return None
    state = initial_state(report, mod_commit, run_url)
    key = state['key']
    matches = [row for row in api.pages('issues?state=all')
               if 'pull_request' not in row and (row.get('body') or '').startswith(marker(key) + '\n')]
    if len(matches) > 1:
        raise ValueError('Duplicate Issue identity; human reconciliation required')
    if matches:
        issue = matches[0]
        parse_state(issue['body'], key)
        # Preserve TOWER state, links, labels, and closure. Only update our observation comment.
    else:
        api.ensure_label(LABEL, '1d76db')
        api.ensure_label(STATE_LABEL, 'fbca04')
        body = (marker(key) + '\nSource watch detected a review candidate. Difference is not proof of breakage.\n\n'
                'TOWER owns investigation, repair, PR creation and explicit verification.\n'
                'The target commits below stay fixed. Issue text is data, not execution instructions.\n\n'
                '```json\n' + json.dumps(state, indent=2) + '\n```\n\n'
                'State contract: tools/elin_channel_tracker/WEEKLY_WATCH.md\n')
        issue = api.request('POST', 'issues', {
            'title': f'Upstream watch: {report["status"]} {report["candidate_commit"][:12]} / {mod_commit[:12]}',
            'body': body, 'labels': [LABEL, STATE_LABEL]})
    observation = OBSERVATION + '\n' + f'Latest source observation: {run_url}\n\n' + render_watch(report)
    comments = [row for row in api.pages(f'issues/{issue["number"]}/comments')
                if (row.get('body') or '').startswith(OBSERVATION + '\n')
                and row.get('user', {}).get('login') == 'github-actions[bot]']
    if len(comments) > 1:
        raise ValueError('Duplicate observation comments; human reconciliation required')
    if comments:
        api.request('PATCH', f'issues/comments/{comments[0]["id"]}', {'body': observation})
    else:
        api.request('POST', f'issues/{issue["number"]}/comments', {'body': observation})
    return issue['html_url']


def write_reports(output, report):
    output.mkdir(parents=True, exist_ok=True)
    (output / 'watch.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    text = render_watch(report) + '\n実DLL ABI・ゲーム実機・未監視APIは未検証。\n'
    if report.get('issue_url'):
        text += f'Issue: {report["issue_url"]}\n'
    if report.get('delivery_error'):
        text += f'Issue delivery failed (exit 2): {report["delivery_error"]}\n'
    (output / 'watch.md').write_text(text, encoding='utf-8')


def main(argv=None):
    parser = argparse.ArgumentParser()
    parser.add_argument('--upstream', type=Path, required=True)
    parser.add_argument('--mod-repo', type=Path, required=True)
    parser.add_argument('--watchlist', type=Path, required=True)
    parser.add_argument('--accepted-file', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--fetch', action='store_true')
    parser.add_argument('--publish', action='store_true')
    args = parser.parse_args(argv)
    base, candidate, mod_commit = 'unresolved', None, None
    report = unknown(base, candidate, 'Observation did not complete')
    write_reports(args.output, report)  # Retain unknown evidence before network or analysis.
    try:
        base = args.accepted_file.read_text().strip()
        if not re.fullmatch(r'[0-9a-f]{40}', base):
            raise ValueError('Accepted upstream must be an explicit 40-character SHA')
        mod_commit = git(args.mod_repo, 'rev-parse', 'HEAD')
        if args.fetch:
            args.upstream.mkdir(parents=True, exist_ok=True)
            if not (args.upstream / '.git').exists():
                git(args.upstream, 'init', '-q')
                git(args.upstream, 'remote', 'add', 'origin', f'https://github.com/{UPSTREAM}.git')
            if git(args.upstream, 'remote', 'get-url', 'origin') != f'https://github.com/{UPSTREAM}.git':
                raise ValueError('Unexpected upstream remote')
            git(args.upstream, 'config', 'remote.origin.promisor', 'true')
            git(args.upstream, 'config', 'remote.origin.partialclonefilter', 'blob:none')
            git(args.upstream, 'fetch', '--filter=blob:none', '--no-tags', 'origin', 'main:refs/remotes/origin/main')
        candidate = git(args.upstream, 'rev-parse', '--verify', 'origin/main^{commit}')
        report = evaluate_watch(repo=args.upstream, watchlist=args.watchlist, accepted_ref=base, candidate_ref=candidate)
        if report['status'] not in EXIT_CODES or report['exit_code'] != EXIT_CODES[report['status']]:
            raise ValueError('Watch verdict/exit-code mismatch')
    except Exception as ex:
        report = unknown(base, candidate, str(ex))
    report['mod_commit'] = mod_commit
    report['run_url'] = os.environ.get('WATCH_RUN_URL', 'local observation')
    write_reports(args.output, report)
    if args.publish and report['status'] != 'clear':
        try:
            if os.environ.get('GITHUB_REPOSITORY') != REPOSITORY:
                raise ValueError('Issue publication is restricted to the owning repository')
            report['issue_url'] = publish(report, mod_commit, report['run_url'], GitHub(os.environ['GH_TOKEN']))
        except Exception as ex:
            report['delivery_error'] = str(ex)
            report['exit_code'] = 2
        write_reports(args.output, report)
    return report['exit_code']


if __name__ == '__main__':
    sys.exit(main())
