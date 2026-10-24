---
name: sonarcloud-pr-analysis
description: >-
  Reads and handles the SonarQube Cloud analysis of a pull request on this
  repository (new issues, security hotspots, quality gate) for its head commit.
  Use before marking a draft PR ready for review, after each push to a PR that
  touches analyzed files, before merging, or when the user asks to "traiter
  l'analyse Sonar" or similar.
---

# SonarQube Cloud PR analysis

The `.speckit/` is the source of truth: policy in
`.speckit/gestion-projet/revue-tickets.md` → « Analyse SonarQube Cloud », setup
in `.speckit/gestion-projet/depot.md` → « Analyse de la qualité du code ». This
skill is only the operational procedure; on any divergence, the speckit applies.

The project (`Tetram76_bdtheque-claude`) is public: every query below works
without a token. Never ask the user for a SonarQube Cloud token. The queries
are PowerShell (`Invoke-RestMethod`): neither Python nor `jq` is installed.

## 1. Wait for the analysis of the head commit

The `Code quality` workflow analyzes every push, draft PRs included.

```powershell
$base = 'https://sonarcloud.io/api'; $p = 'Tetram76_bdtheque-claude'; $pr = '<PR>'
$head = gh pr view $pr --json headRefOid -q .headRefOid
$analysis = (Invoke-RestMethod "$base/project_pull_requests/list?project=$p").pullRequests |
    Where-Object key -eq $pr
[pscustomobject]@{ IsHead = $analysis.commit.sha -eq $head; Date = $analysis.analysisDate; Gate = $analysis.status.qualityGateStatus }
```

The analysis is the head's only when `IsHead` is true. Otherwise check the run
(`gh run list --workflow code-quality.yml --branch <branch> -L 1`): still
running → wait for it in the background (never a foreground sleep); failed or
absent → no quality gate, so the PR cannot be merged (the Ruleset requires the
`SonarCloud Code Analysis` check): find the cause of a failed run and fix it;
an outage of the service or a fork PR (no `SONAR_TOKEN`) is reported to the
user, the merge waiting. A failed query is never read as « no analysis »:
retry it.

## 2. Read the findings

```powershell
(Invoke-RestMethod "$base/qualitygates/project_status?projectKey=$p&pullRequest=$pr").projectStatus
(Invoke-RestMethod "$base/issues/search?componentKeys=$p&pullRequest=$pr&resolved=false&ps=500").issues |
    Select-Object key, rule, severity, type, component, line, message
(Invoke-RestMethod "$base/hotspots/search?projectKey=$p&pullRequest=$pr&status=TO_REVIEW&ps=500").hotspots |
    Select-Object key, ruleKey, vulnerabilityProbability, component, line, message
```

A rule's rationale: `(Invoke-RestMethod "$base/rules/show?key=<rule>&organization=tetram76").rule.htmlDesc`.

## 3. Handle each finding

Each issue and hotspot is a review finding (`revue-tickets.md` →
« Traiter un retour »): counter-analysis on verified facts in the global
context of the code, root cause, scope of the PR, then fix at the root (one
commit per finding), defer (ticket or plan note) or reject with its reason.
Push fixes while the PR is a draft; on a ready PR, fold them into the next push
of a Codex fix when one is coming.

The quality gate must pass before marking the PR ready and before merging:
deferred and rejected findings stop counting once marked (step 5); a coverage
shortfall is fixed by the missing tests, or, for a file the .NET tests cannot
cover, by a coverage exclusion justified in `depot.md`. Once findings are
marked, check that the `SonarCloud Code Analysis` check of the head commit
shows the gate passed (`gh pr checks <PR>`); if it still shows the earlier
verdict, re-run the analysis job (`gh run rerun <run-id>`), which publishes a
fresh one without a new commit.

## 4. Trace deferred and rejected findings

Keep one PR comment, headed `## Analyse SonarQube Cloud`, listing each deferred
or rejected finding (key, rule, file:line, decision, justification), and edit
it (`gh api -X PATCH repos/Tetram76/bdtheque-claude/issues/comments/<id> -F body=@<file>`)
at each handled analysis instead of posting a new one. Never write the Codex
mention in it.

## 5. Mark the deferred and rejected findings in SonarQube Cloud

Marking them is the agent's job, never handed to the user. Do it through the
user's SonarQube Cloud session in the built-in browser (no token is ever
handled): open `https://sonarcloud.io/project/issues?id=Tetram76_bdtheque-claude`;
on the sign-in page, ask the user to sign in there with GitHub, which is the
only step left to them. Then, from that page, call the web API with the session
cookie and its XSRF token, one small batch per call (the tool times out after
about 45 s; never re-run a batch before checking what the previous one did):

```javascript
const xsrf = decodeURIComponent(document.cookie.match(/XSRF-TOKEN=([^;]+)/)[1]);
const post = (url, body) => fetch(url, { method: 'POST', body: new URLSearchParams(body),
  headers: { 'X-XSRF-TOKEN': xsrf, 'Content-Type': 'application/x-www-form-urlencoded' } });
// per finding: a comment pointing to the justification, then the transition
await post('/api/issues/add_comment', { issue: key, text: 'Contre-analysé : justification dans le commentaire « Analyse SonarQube Cloud » de <PR URL>' });
await post('/api/issues/do_transition', { issue: key, transition: 'falsepositive' }); // or 'accept'
// per security hotspot reviewed and judged safe
await post('/api/hotspots/change_status', { hotspot: key, status: 'REVIEWED', resolution: 'SAFE', comment: '<justification>' });
```

`falsepositive` for a finding whose premise is wrong; `accept` for a rule that
does not fit the case, and for a valid finding whose fix is deferred (the
comment then names the ticket or plan note that carries the fix): accepted
means acknowledged but not fixed now, and an open finding would fail the gate.
Check the outcome with `/api/issues/search?issues=<keys>&additionalFields=comments`
(`issueStatus` `FALSE_POSITIVE` or `ACCEPTED`, one comment each).
